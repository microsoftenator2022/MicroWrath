using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using static MicroWrath.Generator.Constants;
using System.Threading;

namespace MicroWrath.Generator
{
    internal static class SyntaxExtensions
    {
        internal static MemberAccessExpressionSyntax? GetExpression(this MemberAccessExpressionSyntax ma) =>
            ma.Expression as MemberAccessExpressionSyntax;
    }

    [Generator]
    internal sealed partial class BlueprintsDb : IIncrementalGenerator
    {
        private static Option<INamedTypeSymbol> TryGetOwlcatDbType(SemanticModel sm)
        {
            var blueprintsDbType = sm.Compilation.Assembly.GetTypeByMetadataName(BlueprintsDbTypeFullName);

            var typeMembers = blueprintsDbType?.GetTypeMembers() ?? default;

            return typeMembers.TryFind(static m => m.Name == "Owlcat");
        }

        private static Option<string> TryGetBlueprintTypeNameFromSyntaxNode(
            MemberAccessExpressionSyntax bpTypeExpr,
            INamedTypeSymbol owlcatDbType,
            SemanticModel sm,
            CancellationToken ct)
        {
            var owlcatDbExpr = bpTypeExpr.GetExpression().ToOption();

            return owlcatDbExpr
                .Bind<MemberAccessExpressionSyntax, string>(maybeOwlcat =>
                {
                    var exprType = sm.GetTypeInfo(maybeOwlcat, ct).Type;
                    if (owlcatDbType.Equals(exprType, SymbolEqualityComparer.Default))
                        return Option.Some(bpTypeExpr.Name.ToString());

                    return Option.None<string>();
                });
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var isDesignTime = context.AnalyzerConfigOptionsProvider.Select((aco, _) =>
            {
                aco.GlobalOptions.TryGetValue("build_property.DesignTimeBuild", out var designTime);

                return designTime;
            });

            var compilation = context.CompilationProvider;
            
            var cheatdata = context.AdditionalTextsProvider.Where(static at => Path.GetFileName(at.Path).ToLower() == "cheatdata.json");

            var blueprintData = Blueprints.GetBlueprintData(cheatdata, compilation);

            var config = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => Incremental.GetConfig(c));

            var blueprintMemberSyntax = Syntax.GetBlueprintMemberSyntax(context.SyntaxProvider);

            var blueprintsAccessorsToGenerate = blueprintData
                .Combine(blueprintMemberSyntax.Collect())
                .Combine(isDesignTime)
                .Select(static (bpsAndMembers, _) =>
                {
                    var (((blueprintType, blueprints), memberAccesses), designTime) = bpsAndMembers;

                    if (designTime is not null)
                        return (blueprintType, blueprints);

                    if (!memberAccesses.Any(member => member.BlueprintTypeName == blueprintType.Name))
                        return (blueprintType, Enumerable.Empty<BlueprintInfo>());

                    return (blueprintType, blueprints.Where(bp => memberAccesses.Any(member => member.Name == bp.Name)));
                });

            //var blueprintsAccessorsToGenerate = blueprintData
            //    .Select(static (bps, _) =>
            //    {
            //        var (blueprintType, blueprints) = bps;

            //        return (blueprintType, blueprints.Select(Functional.Identity));
            //    });

            context.RegisterSourceOutput(blueprintsAccessorsToGenerate, static (spc, bps) =>
            {
                var (symbol, blueprints) = bps;

                var sb = new StringBuilder();

                if (symbol is not INamedTypeSymbol type) return;

                var ns = type.ContainingNamespace;

                sb.Append($"using {ns};");
                    
                if (ns.ToString() != "Kingmaker.Blueprints")
                    sb.Append($@"
using Kingmaker.Blueprints;");

                    sb.Append($@"
namespace MicroWrath.BlueprintsDb
{{
    internal static partial class BlueprintsDb
    {{
        internal static partial class Owlcat
        {{
            internal static partial class {type.Name}
            {{");

                    foreach (var bp in blueprints)
                    {
                        if (spc.CancellationToken.IsCancellationRequested) break;

                        sb.Append($@"
                internal static OwlcatBlueprint<{type}> {bp.Name} => new OwlcatBlueprint<{type}>(""{bp.GuidString}"");");
                    }

                    sb.Append($@"
            }}
        }}
    }}
}}");
                    spc.AddSource(type.ToDisplayString(), sb.ToString());
            });
        }
    }
}
