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

                    var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);
                    if (root is null) return;

                    var defaultSpan = service.GetDefaultCompletionListSpan(await doc.GetTextAsync().ConfigureAwait(false), context.Position);

                    var node = root.FindNode(defaultSpan);

                    if (node is not MemberAccessExpressionSyntax thisExpr ||
                        thisExpr.Expression is not MemberAccessExpressionSyntax memberAccess) return;

                    var owlcatDbType = TryGetOwlcatDbType(semanticModel).Value;
                    if (owlcatDbType is null) return;

                    var typeName = TryGetBlueprintTypeNameFromSyntaxNode(memberAccess, owlcatDbType, semanticModel).Value;
                    if (typeName is null) return;

                    var key = Blueprints.BlueprintList.Keys.FirstOrDefault(key => key.Name == typeName);
                    if (key is null) return;

                    var blueprints = Blueprints.BlueprintList[key];
                    var completionItems = blueprints
                        .Select(bp => CreateCompletion(bp, owlcatDbType, semanticModel));

                    context.AddItems(completionItems);
                }

                public override async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
                {
                    var properties = item.Properties;
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
                    if (trigger.Character != '.' || trigger.Kind is not CompletionTriggerKind.Insertion) return false;

                    return true;
                }

                public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
                {
                    var span = item.Span;

                    var docText = await document.GetTextAsync().ConfigureAwait(false);
                    var originalText = docText.ToString(span);

                    var textChange = new TextChange(new TextSpan(span.End, 1), originalText += item.DisplayText);

                    return CompletionChange.Create(textChange);
                }
            }
        }
    }
}
