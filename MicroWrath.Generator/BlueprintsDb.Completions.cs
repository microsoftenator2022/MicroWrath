using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using Microsoft.CodeAnalysis.Tags;
using MicroWrath.Generator.Extensions;

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
                    var service = doc.Project.Services.GetService<CompletionService>();
                    if (service is null) return;

                    var semanticModel = await doc.GetSemanticModelAsync().ConfigureAwait(false);
                    if (semanticModel is null) return;

                    var owlcatDbType = TryGetOwlcatDbType(semanticModel).Value;
                    if (owlcatDbType is null) return;

                    var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                    if (root is null) return;

                    var span = service.GetDefaultCompletionListSpan(await doc.GetTextAsync(), context.Position);

                    var node = root.FindNode(span);

                    var maybeBp = TryGetBlueprintMemberAccessInChildren(node, context.Position, owlcatDbType, semanticModel);

                    if (maybeBp is null && !node.ChildThatContainsPosition(context.Position).IsNode)
                    {
                        maybeBp = TryGetBlueprintMemberAccessInChildren(node, context.Position - 1, owlcatDbType, semanticModel);
                        var newStart = span.Start;
                        var newEnd = span.End -1;

                        if (newEnd < newStart) newStart--;

                        span = new TextSpan(newStart, newEnd);
                    }

                    if (maybeBp is null) return;

                    var (typeName, bpName) = maybeBp.Value;

                    if (typeName is null || bpName is null) return;

                    var key = Blueprints.BlueprintList.Keys.FirstOrDefault(key => key.Name == typeName.Identifier.Text);
                    if (key is null) return;

                    var blueprints = Blueprints.BlueprintList[key];
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

                    var sm = await document.GetSemanticModelAsync();

                    var bpType = sm?.Compilation.GetTypeByMetadataName(blueprintTypeName);
                    var bpTypeNameShort = bpType?.Name ?? "";
                    
                    var bpTypeTag = bpType?.TypeKind is TypeKind.Struct ? TextTags.Struct : TextTags.Class;

                    var taggedText = new TaggedText[]
                    {
                        new(bpTypeTag, $"{bpTypeNameShort} "),
                        new(TextTags.Class, owlcatDbTypeName),
                        new(bpTypeTag, $".{bpTypeNameShort}"),
                        new(TextTags.Property, $".{item.DisplayText}")
                    }.ToImmutableArray();
                    
                    return CompletionDescription.Create(taggedText);
                }

                private static CompletionItem CreateCompletion(BlueprintInfo bp, INamedTypeSymbol owlcatDbType, SemanticModel sm)
                {
                    var owlcatDbTypeName = owlcatDbType.ToString();
                    var blueprintTypeName = bp.TypeName;
                    
                    var properties = new Dictionary<string, string>()
                    {
                        { nameof(owlcatDbTypeName), owlcatDbTypeName },
                        { nameof(blueprintTypeName), blueprintTypeName },
                    }
                    .ToImmutableDictionary();

                    var tags = new[] { WellKnownTags.Property, WellKnownTags.Internal }.ToImmutableArray();

                    var completionItem = CompletionItem.Create(bp.Name, properties: properties, tags: tags);
                    
                    return completionItem;
                }

                public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
                {
                    if (!char.IsLetterOrDigit(trigger.Character) && !(trigger.Character == '.' && trigger.Kind is CompletionTriggerKind.Insertion)) return false;

                    return true;
                }

                public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
                {
                    //return await base.GetChangeAsync(document, item, commitKey, cancellationToken);

                    var span = item.Span;

                    var docText = await document.GetTextAsync().ConfigureAwait(false);
                    var originalText = docText.ToString(span);

                    var textChange = new TextChange(new TextSpan(span.End, 0), originalText += item.DisplayText);

                    return CompletionChange.Create(textChange);
                }
            }
        }
    }
}
