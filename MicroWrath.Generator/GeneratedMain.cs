using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Generator;
using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    [Generator]
    internal class GeneratedMain : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => Incremental.GetConfig(c).RootNamespace);

            var compilation = context.CompilationProvider;

            var microModInterface = compilation.Select(static (c, _) => c.GetTypeByMetadataName("MicroWrath.IMicroMod"));

            var typeSymbols = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is TypeDeclarationSyntax,
                static (sc, _) => sc)
                .Combine(microModInterface)
                .SelectMany(static (scmi, _) =>
                {
                    var (sc, mi) = scmi;

                    var typeSymbol = sc.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)sc.Node);

                    if (mi is null || typeSymbol is null ||
                        !typeSymbol.AllInterfaces.Any(i => i.Equals(mi, SymbolEqualityComparer.Default)))
                        return Enumerable.Empty<(GeneratorSyntaxContext sc, INamedTypeSymbol mi, INamedTypeSymbol type)>();

                    return EnumerableExtensions.Singleton((sc, mi, type: typeSymbol));
                });

            var partialAndNonImplementing = typeSymbols
                .SelectMany(static (sct, _) =>
                {
                    var (sc, mi, typeSymbol) = sct;

                    var interfaceMembers = mi.GetMembers();

                    if (interfaceMembers.Any(m => typeSymbol.FindImplementationForInterfaceMember(m) is not null))
                        return Enumerable.Empty<(GeneratorSyntaxContext sc, INamedTypeSymbol type)>();

                    return EnumerableExtensions.Singleton((sc, type: typeSymbol));
                })
                .Where(static scs => ((TypeDeclarationSyntax)scs.sc.Node).Modifiers
                    .Any(static m => m.IsKind(SyntaxKind.PartialKeyword)))
                .Select(static (scs, _) => scs.type);
#if DEBUG
            context.RegisterSourceOutput(typeSymbols.Collect(), (spc, types) =>
            {
                var sb = new StringBuilder();

                foreach (var t in types)
                {
                    sb.AppendLine($"// {(t.ToString() ?? "null")}");
                }

                spc.AddSource("types", sb.ToString());
            });
#endif
            var mainType = partialAndNonImplementing
                .Collect()
                .Combine(rootNamespace)
                .Select(static (ts, _) =>
                {
                    var rootNs = ts.Right.Value;
                    var type = ts.Left.FirstOrDefault();

                    if (rootNs is null) return Option.None<(string, Option<INamedTypeSymbol>)>();

                    return Option.OfObj((rootNs, Option.OfObj(type)));
                });
            
            var shouldGenerate =
                typeSymbols
                .Collect()
                .Combine(partialAndNonImplementing.Collect())
                .Select(static (tp, _) => tp.Left.Length == tp.Right.Length);

            context.RegisterSourceOutput(mainType.Combine(shouldGenerate), static (spc, microModMainThings) =>
            {
                var (maybeTypeAndNs, shouldGen) = microModMainThings;

                if (maybeTypeAndNs.IsNone) return;

                var (ns, maybeType) = maybeTypeAndNs.Value;

                var name = maybeType.Value?.Name ?? "Main";

                if (!shouldGen) return;

                var props = maybeType.Map(t => new
                {
                    HasParameterlessConstructor = t.Constructors.Any(c => c.Parameters.Length == 0),
                });

                var sb = new StringBuilder();

                sb.AppendLine($@"using System;
using HarmonyLib;
using UnityModManagerNet;
using MicroWrath;
using MicroWrath.Util;

namespace {ns}
{{
    public partial class {name} : IMicroMod
    {{");
                if(!(props.Value?.HasParameterlessConstructor ?? false))
                sb.Append($@"
        private {name}() {{ }}");

                sb.Append($@"
        private static {name}? instance;
        internal static {name} Instance => instance!;

        private UnityModManager.ModEntry? modEntry;
        internal UnityModManager.ModEntry ModEntry => modEntry!;

        private Harmony? harmony;
        internal Harmony Harmony => harmony!;

        public event Action<UnityModManager.ModEntry> Loaded = Functional.Ignore;

        public bool Load(UnityModManager.ModEntry modEntry)
        {{
            this.modEntry = modEntry;
            MicroLogger.ModEntry = modEntry;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            instance = this;

            Loaded(modEntry);

            return true;
        }}
    }}
}}
");

                spc.AddSource("Main", sb.ToString());
            });

        }
    }
}
