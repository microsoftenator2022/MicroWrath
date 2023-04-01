using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Util;

namespace MicroWrath.Generator
{
    internal partial class BlueprintsDb
    {
        private static class Syntax
        {
            public static IncrementalValuesProvider<(string Name, string BlueprintTypeName)> GetBlueprintMemberSyntax(SyntaxValueProvider syntaxProvider)
            {
                var memberAccessExprs = syntaxProvider.CreateSyntaxProvider(
                    static (sn, _) => sn is MemberAccessExpressionSyntax,
                    static (sn, _) => ((MemberAccessExpressionSyntax)sn.Node, sn.SemanticModel));

                return memberAccessExprs
                    .SelectMany(static (sn, _) => TryGetOwlcatDbType(sn.SemanticModel)
                        .Map(owlcatDbType => (sn, owlcatDbType))
                        .ToEnumerable())
                    .SelectMany(static (sn, _) =>
                    {
                        var ((node, sm), owlcatDbType) = sn;

                        var bpTypeName = Option
                            .OfObj(node.GetParent())
                            .Bind(p => TryGetBlueprintTypeNameFromSyntaxNode(p, owlcatDbType, sm));

                        var bpName = bpTypeName
                            .Map(bpt => (node.Name.Identifier.ValueText, bpt));

                        return bpName.ToEnumerable();
                    });
            }
        }
    }
}
