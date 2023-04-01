using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using System.Threading;

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

                    var blueprintNames = Blueprints.BlueprintList[key];
                    var completionItems = blueprintNames
                        .Select(bp => CompletionItem.Create(bp.Name));

                    context.AddItems(completionItems);
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

                    return CompletionChange.Create(new TextChange(new TextSpan(span.End, 1), originalText += item.DisplayText));
                }
            }
        }
    }
}
