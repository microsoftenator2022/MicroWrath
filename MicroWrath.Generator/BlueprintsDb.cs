using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MicroWrath.Generator
{
    internal static class SyntaxExtensions
    {
        internal static MemberAccessExpressionSyntax? GetParent(this MemberAccessExpressionSyntax ma) =>
            ma.Expression as MemberAccessExpressionSyntax;
    }

    [Generator]
    internal class BlueprintsDb : IIncrementalGenerator
    {
        private readonly record struct BlueprintInfo(string GuidString, string Name, string TypeName);

        private const string blueprintsDbNamespace = "MicroWrath.BlueprintsDb";
        private const string blueprintsDbTypeName = "BlueprintsDb";
        private static string blueprintsDbTypeFullName = $"{blueprintsDbNamespace}.{blueprintsDbTypeName}";

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

            var blueprintData = cheatdata
                .SelectMany(static (at, _) =>
                {
                    if (at.GetText()?.ToString() is not string text)
                        return Enumerable.Empty<BlueprintInfo>();

                    var entries = JValue.Parse(text)["Entries"].ToArray();

                    return entries.Choose<JToken, BlueprintInfo>(static entry =>
                    {
                        if (entry["Guid"]?.ToString() is string guid &&
                            entry["Name"]?.ToString() is string name &&
                            entry["TypeFullName"]?.ToString() is string typeName)
                        {
                            var nameChars = new List<char>();
                            string? escapedName = null;

                            if (!SyntaxFacts.IsValidIdentifier(name))
                            {
                                nameChars = name.Select(static c =>
                                    SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_').ToList();

                                if (!SyntaxFacts.IsIdentifierStartCharacter(name[0]))
                                    nameChars.Insert(0, '_');

                                escapedName = new string(nameChars.ToArray());
                            }
                            else escapedName = name;

                            return Option.Some(new BlueprintInfo(GuidString: guid, Name: escapedName, TypeName: typeName));
                        }

                        return Option.None<BlueprintInfo>();
                    });
                })
                .Collect()
                .Combine(compilation)
                .SelectMany(static (bpsc, _) =>
                {
                    var (bps, compilation) = bpsc;

                    var types = new Dictionary<string, ISymbol?>();

                    foreach (var typeName in bps.Select(static bp => bp.TypeName).Distinct())
                    {
                        types[typeName] = compilation.GetTypeByMetadataName(typeName);
                    }

                    return bps
                        .Select(
                            bp => types[bp.TypeName]?.Name == bp.Name ?
                            new BlueprintInfo(
                                GuidString: bp.GuidString,
                                TypeName: bp.TypeName,
                                Name: bp.Name + "_blueprint") :
                            bp)
                        .GroupBy(bp => types[bp.TypeName]!, SymbolEqualityComparer.Default);
                })
                .Where(static g => g.Key is not null && g.Key.DeclaredAccessibility == Accessibility.Public)
                .Select(static (bpType, _) =>
                {
                    static IEnumerable<BlueprintInfo> renameDuplicates(IEnumerable<BlueprintInfo> source)
                    {
                        if (source.Count() == 1)
                        {
                            yield return source.First();
                        }
                        else
                        { 
                            var i = 0;
                            foreach (var sourceItem in source)
                            {
                                yield return new BlueprintInfo(
                                    GuidString: sourceItem.GuidString,
                                    TypeName: sourceItem.TypeName,
                                    Name: sourceItem.Name + $"_blueprint{++i}");
                            }
                        }
                    }

                    return (key: bpType.Key, bps: bpType.GroupBy(static bp => bp.Name).SelectMany(renameDuplicates));
                });

            var config = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => Incremental.GetConfig(c));

            var invocations = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is InvocationExpressionSyntax ies,
                static (sc, _) => (Node: sc.Node as InvocationExpressionSyntax, SemanticModel: sc.SemanticModel))
                .Where(static snsm => snsm.Node is not null)
                .Select(static (snsm, _) => (Node: snsm.Node!, SemanticModel: snsm.SemanticModel))
                .Where(static snsm =>
                {
                    var sm = snsm.SemanticModel;
                    var ass = snsm.SemanticModel.Compilation.Assembly;
                    var symbol = sm.GetSymbolInfo(snsm.Node).Symbol;

                    if (symbol is null) return true;

                    return ass.Equals(symbol.ContainingAssembly, SymbolEqualityComparer.Default);
                });

            var owlcatDbType = invocations
                .SelectMany(static (invocation, _) =>
                {
                    var blueprintsDbType = invocation.SemanticModel.Compilation.Assembly.GetTypeByMetadataName(blueprintsDbTypeFullName);

                    return Option.OfObj((blueprintsDbType?.GetTypeMembers()
                            is ImmutableArray<INamedTypeSymbol> dbTypeMembers ? dbTypeMembers : default)
                            .FirstOrDefault(static m => m.Name == "Owlcat"))
                        .ToEnumerable();
                })
                .Collect()
                .SelectMany(static (ts, _) => Option.OfObj(ts.FirstOrDefault()).ToEnumerable())
                .Collect();

            var blueprintNamesToLoad = invocations
                .Combine(owlcatDbType)
                .SelectMany(static (invocationsAndOcType, _) =>
                {
                    var (invocation, owlcatDbTypeSeq) = invocationsAndOcType;
                    var owlcatDbType = owlcatDbTypeSeq.FirstOrDefault();
                    if (owlcatDbType is null) return Enumerable.Empty<string>();

                    var owlcatDbInvocations =
                        Option.OfObj((invocation.Node.Expression as MemberAccessExpressionSyntax)?.GetParent())
                        .Bind(static bpType =>
                            Option.OfObj(bpType.GetParent())
                            .Map(maybeOwlcat => (maybeOwlcat, bpType)));

                    return owlcatDbInvocations
                        .Bind<(MemberAccessExpressionSyntax maybeOwlcat, MemberAccessExpressionSyntax bpType), string>(i =>
                        {
                            var exprType = invocation.SemanticModel.GetTypeInfo(i.maybeOwlcat).Type;
                            if (owlcatDbType.Equals(exprType, SymbolEqualityComparer.Default))
                                return Option.Some(i.bpType.Name.ToString());

                            return Option.None<string>();
                        }).ToEnumerable();
                });
            
#if DEBUG
            context.RegisterSourceOutput(blueprintNamesToLoad.Collect(), (spc, names) =>
            {
                var sb = new StringBuilder();

                foreach (var name in names)
                {
                    sb.AppendLine($"// {name}");
                }
                
                spc.AddSource("invocations", sb.ToString());
            });
#endif

            var usedBlueprintTypes = blueprintData
                .Combine(blueprintNamesToLoad.Collect())
                .Select(static (bpdt, _) =>
                {
                    var (bpd, typeNames) = bpdt;

                    if (bpd.key is ISymbol s && typeNames.Contains(s.Name))
                        return bpd;
                    
                    return (bpd.key, bps: Enumerable.Empty<BlueprintInfo>());
                });

            context.RegisterSourceOutput(usedBlueprintTypes.Collect().Combine(config), static (spc, bpsAndConfig) =>
            {
                var (bps, config) = bpsAndConfig;

                var sb = new StringBuilder();

#if DEBUG
                sb.AppendLine($"// {bps.Length} blueprint types");

                foreach (var bpType in bps)
                {
                    sb.AppendLine($"// {bpType.key}: {bpType.bps.Count()}");
                }

                spc.AddSource("summary", sb.ToString());
#endif

                foreach (var bpType in bps)
                {
                    if (bpType.key is not INamedTypeSymbol type)
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
            {{
                /// <summary>Does nothing at runtime. Forces the source generator to output members for this type.</summary>
                internal static void LoadBlueprints() {{ }}
");
                    foreach (var bp in bpType.bps)
                    {
                        sb.Append($@"
                internal static {type} {bp.Name} => ResourcesLibrary.TryGetBlueprint<{type}>(""{bp.GuidString}"");");
                    }

                    sb.Append($@"
            }}
        }}
    }}
}}");
                    spc.AddSource(bpType.key.ToDisplayString(), sb.ToString());
                }
            });
        }
    }
}
