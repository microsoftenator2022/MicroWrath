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
        private const string blueprintsDbNamespace = "MicroWrath.BlueprintsDb";
        private const string blueprintsDbTypeName = "BlueprintsDb";
        private static readonly string blueprintsDbTypeFullName = $"{blueprintsDbNamespace}.{blueprintsDbTypeName}";

        private static Option<INamedTypeSymbol> TryGetOwlcatDbType(SemanticModel sm)
        {
            var blueprintsDbType = sm.Compilation.Assembly.GetTypeByMetadataName(blueprintsDbTypeFullName);

            return Option.OfObj(blueprintsDbType?.GetTypeMembers().FirstOrDefault(static m => m.Name == "Owlcat"));
        }

        private static Option<string> TryGetBlueprintTypeNameFromSyntaxNode(MemberAccessExpressionSyntax bpTypeExpr, INamedTypeSymbol owlcatDbType, SemanticModel sm)
        {
            var owlcatDbExpr = Option.OfObj(bpTypeExpr.GetExpression());

            return owlcatDbExpr
                .Bind<MemberAccessExpressionSyntax, string>(maybeOwlcat =>
                {
                    var exprType = sm.GetTypeInfo(maybeOwlcat).Type;
                    if (owlcatDbType.Equals(exprType, SymbolEqualityComparer.Default))
                        return Option.Some(bpTypeExpr.Name.ToString());

                    return Option.None<string>();
                });
        }

        private void DefineBlueprintsDbClass(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(pic =>
            {
                pic.AddSource("BlueprintsDb", $@"namespace {blueprintsDbNamespace}
{{
    internal static partial class {blueprintsDbTypeName}
    {{
        internal static partial class Owlcat {{ }}
    }}
}}");
            });
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            DefineBlueprintsDbClass(context);

            var compilation = context.CompilationProvider;
            
            var cheatdata = context.AdditionalTextsProvider.Where(static at => Path.GetFileName(at.Path).ToLower() == "cheatdata.json");

            var blueprintData = Blueprints.GetBlueprintData(cheatdata, compilation);

            var config = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => Incremental.GetConfig(c));

            var blueprintMemberSyntax = Syntax.GetBlueprintMemberSyntax(context.SyntaxProvider);

            var blueprintsAccessorsToGenerate = blueprintData
                .Combine(blueprintMemberSyntax.Collect())
                .Select(static (bpsAndMembers, _) =>
                {
                    var ((blueprintType, blueprints), memberAccesses) = bpsAndMembers;

                    if (!memberAccesses.Any(member => member.BlueprintTypeName == blueprintType.Name))
                        return (blueprintType, Enumerable.Empty<BlueprintInfo>());

                    return (blueprintType, blueprints.Where(bp => memberAccesses.Any(member => member.Name == bp.Name)));
                });

            context.RegisterSourceOutput(blueprintsAccessorsToGenerate.Collect().Combine(config), static (spc, bpsAndConfig) =>
            {
                var (bps, config) = bpsAndConfig;
                
                var sb = new StringBuilder();

//#if DEBUG
//                sb.AppendLine($"// {bps.Length} blueprint types");

//                foreach (var (type, blueprints) in bps)
//                {
//                    sb.AppendLine($"// {type}: {blueprints.Count()}");
//                }

//                spc.AddSource("summary", sb.ToString());
//#endif

                foreach (var (symbol, blueprints) in bps)
                {
                    if (spc.CancellationToken.IsCancellationRequested) break;

                    if (symbol is not INamedTypeSymbol type)
                        continue;

                    sb.Clear();
                    
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
                internal static {type} {bp.Name} => ResourcesLibrary.TryGetBlueprint<{type}>(""{bp.GuidString}"");");
                    }

                    sb.Append($@"
            }}
        }}
    }}
}}");
                    spc.AddSource(type.ToDisplayString(), sb.ToString());
                }
            });
        }
    }
}
