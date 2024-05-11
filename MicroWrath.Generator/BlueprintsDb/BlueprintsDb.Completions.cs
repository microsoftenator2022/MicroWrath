using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using MicroWrath.Generator.Extensions;
using System.IO;


namespace MicroWrath.Generator
{
    internal partial class BlueprintsDb
    {
        private class Completion
        {
            [ExportCompletionProvider($"{nameof(BlueprintsDb)}.{nameof(Completion)}.{nameof(Provider)}", LanguageNames.CSharp)]
            public class Provider : CompletionProvider
            {
                public override async Task ProvideCompletionsAsync(CompletionContext context)
                {
                    var doc = context.Document;
                    var text = await doc.GetTextAsync();

                    if (!ShouldTriggerCompletion(text, context.Position, context.Trigger, context.Options)) return;

                    var service = doc.Project.Services.GetService<CompletionService>();
                    if (service is null) return;

                    var cheatDataJson = doc.Project.AnalyzerOptions.AdditionalFiles
                        .FirstOrDefault(static at => Path.GetFileName(at.Path).ToLower() == "cheatdata.json");
                    if (cheatDataJson is null) return;

                    var semanticModel = await doc.GetSemanticModelAsync();
                    if (semanticModel is null) return;

                    var owlcatDbType = TryGetOwlcatDbType(semanticModel).Value;
                    if (owlcatDbType is null) return;

                    var root = await doc.GetSyntaxRootAsync();
                    if (root is null) return;

                    var span = service.GetDefaultCompletionListSpan(text, context.Position);

                    var node = root.FindNode(span);

                    var maybeBp = TryGetBlueprintMemberAccessInChildren(node, context.Position, owlcatDbType, semanticModel);

                    if (maybeBp is null && !node.ChildThatContainsPosition(context.Position).IsNode)
                    {
                        maybeBp = TryGetBlueprintMemberAccessInChildren(node, context.Position - 1, owlcatDbType, semanticModel);
                        var newStart = span.Start;
                        var newEnd = span.End - 1;

                        if (newEnd < newStart) newStart--;

                        span = new TextSpan(newStart, newEnd);
                    }

                    if (maybeBp is null) return;

                    var (typeName, bpName) = maybeBp.Value;

                    if (typeName is null || bpName is null) return;

                    ImmutableArray<BlueprintInfo> blueprints;

                    if (Blueprints.BlueprintList.Keys.FirstOrDefault(key => key.Name == typeName.Identifier.Text) is { } key)
                    {
                        blueprints = Blueprints.BlueprintList[key];
                    }
                    else
                    {
                        var blueprintsInfo = Blueprints.ReadBlueprintsInfo(cheatDataJson, context.CancellationToken);
                        (key, blueprints) = 
                            Blueprints.GetDictionaryEntries(blueprintsInfo, semanticModel.Compilation, context.CancellationToken)
                                .Where(pair =>
                                {
                                    var (symbol, _) = pair;

                                    return symbol.Name == typeName.Identifier.Text;
                                })
                                .FirstOrDefault();

                        if (key is null)
                            return;
                    }

                    var completionItems = blueprints
                        .Select(bp => CreateCompletion(bp, owlcatDbType, semanticModel));

                    context.AddItems(completionItems);
                }

                private static (SimpleNameSyntax BlueprintType, SimpleNameSyntax? BlueprintName)? TryGetBlueprintMemberAccessInChildren(
                    SyntaxNode node, int position, INamedTypeSymbol owlcatDbType, SemanticModel sm) =>
                    SyntaxNodeQuery.TryPickInChildren<MemberAccessExpressionSyntax, (SimpleNameSyntax, SimpleNameSyntax?)?>(
                        node,
                        position,
                        n => TryGetBlueprintMemberAccess(n, owlcatDbType, sm));

                private static (SimpleNameSyntax BlueprintType, SimpleNameSyntax? BlueprintName)? TryGetBlueprintMemberAccess(
                    SyntaxNode node, INamedTypeSymbol owlcatDbType, SemanticModel sm)
                {
                    MemberAccessExpressionSyntax? member;
                    if (node is MemberAccessExpressionSyntax ma) member = ma;
                    else member = (node.Parent) as MemberAccessExpressionSyntax;

                    if (member is null) return null;

                    if (member.Expression is not MemberAccessExpressionSyntax leftExpr) return null;

                    if (owlcatDbType.Equals(sm.GetSymbolInfo(leftExpr.Name).Symbol, SymbolEqualityComparer.Default))
                        //return (expr.Name, null);
                        return null;

                    if (leftExpr.Expression is not MemberAccessExpressionSyntax grandparent) return null;

                    if (owlcatDbType.Equals(sm.GetSymbolInfo(grandparent.Name).Symbol, SymbolEqualityComparer.Default))
                        return (leftExpr.Name, member.Name);

                    return null;
                }

                public override async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
                {
                    //return null;

                    var properties = item.Properties;

                    if (!properties.ContainsKey("owlcatDbTypeName") || !properties.ContainsKey("blueprintTypeName"))
                        return null;

                    var owlcatDbTypeName = properties["owlcatDbTypeName"];
                    var blueprintTypeName = properties["blueprintTypeName"];
                    var guid = properties.ContainsKey("guid") ? properties["guid"] : "<null>";

                    var sm = await document.GetSemanticModelAsync();

                    var bpType = sm?.Compilation.GetTypeByMetadataName(blueprintTypeName);
                    var bpTypeNameShort = bpType?.Name ?? "";

                    var bpTypeTag = bpType?.TypeKind is TypeKind.Struct ? TextTags.Struct : TextTags.Class;

                    var taggedText = new TaggedText[]
                    {
                        new(bpTypeTag, $"{bpTypeNameShort} "),
                        new(TextTags.Class, owlcatDbTypeName),
                        new(bpTypeTag, $".{bpTypeNameShort}"),
                        new(TextTags.Property, $".{item.DisplayText}"),
                        new(TextTags.LineBreak, "\n"),
                        new(TextTags.Label, "Guid: "),
                        new(TextTags.Text, guid),
                    }.ToImmutableArray();

                    return CompletionDescription.Create(taggedText);
                }

                private static CompletionItem CreateCompletion(BlueprintInfo bp, INamedTypeSymbol owlcatDbType, SemanticModel sm)
                {
                    var owlcatDbTypeName = owlcatDbType.ToString();
                    var blueprintTypeName = bp.TypeName;
                    var guid = bp.GuidString;

                    var properties = new Dictionary<string, string>()
                    {
                        { nameof(owlcatDbTypeName), owlcatDbTypeName },
                        { nameof(blueprintTypeName), blueprintTypeName },
                        { nameof(guid), guid },
                    }
                    .ToImmutableDictionary();

                    var tags = new[] { WellKnownTags.Property, WellKnownTags.Internal }.ToImmutableArray();

                    var completionItem = CompletionItem.Create(bp.Name, properties: properties, tags: tags);

                    return completionItem;
                }

                private const string OwlcatDbTypeName = "Owlcat";

                private static readonly Regex OwlcatDbAccess =
                    new($@"\b{Constants.BlueprintsDbTypeName}\s*\.\s*{OwlcatDbTypeName}\s*\.\s*[A-Z][A-Za-z0-9_]+\s*\.\z",
                        RegexOptions.Compiled |
                        RegexOptions.RightToLeft |
                        RegexOptions.Multiline |
                        RegexOptions.CultureInvariant);

                public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
                {
                    if (trigger.Kind is not CompletionTriggerKind.Insertion || (trigger.Character != '.')) return false;

                    var prefixSpan = text.GetSubText(TextSpan.FromBounds(0, caretPosition));
                    return OwlcatDbAccess.IsMatch(prefixSpan.ToString());
                }

                public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
                {
                    //return await base.GetChangeAsync(document, item, commitKey, cancellationToken);

                    var span = item.Span;

                    var docText = await document.GetTextAsync();
                    var originalText = docText.ToString(span);

                    var textChange = new TextChange(new TextSpan(span.End, 0), originalText += item.DisplayText);

                    return CompletionChange.Create(textChange);
                }
            }
        }
    }
}
