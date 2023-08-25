using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;

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
            context.RegisterPostInitializationOutput(pic =>
            {

            });

            var rootNamespace = context.AnalyzerConfigOptionsProvider.Select(static (c, _) => Incremental.GetConfig(c).RootNamespace);

            var compilation = context.CompilationProvider;

            var microModInterface = compilation.Select(static (c, _) => c.GetTypeByMetadataName("MicroWrath.IMicroMod"));

            var typeSymbols = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is TypeDeclarationSyntax,
                static (sc, _) => sc)
                .Combine(microModInterface)
                .SelectMany(static (scmi, _) =>
                {
                    var (sc, mmi) = scmi;

                    var typeSymbol = sc.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)sc.Node);

                    if (mmi is null || typeSymbol is null || typeSymbol.IsAbstract ||
                        !typeSymbol.AllInterfaces.Any(i => i.Equals(mmi, SymbolEqualityComparer.Default)))
                        return Enumerable.Empty<(GeneratorSyntaxContext sc, INamedTypeSymbol mi, INamedTypeSymbol type)>();

                    return EnumerableExtensions.Singleton((sc, mmi, type: typeSymbol));
                });

            var partialAndNonImplementing = typeSymbols
                .SelectMany(static (sct, _) =>
                {
                    var (sc, mmi, typeSymbol) = sct;

                    var interfaceMembers = mmi.GetMembers();

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

                var sb = new StringBuilder();

                sb.AppendLine($@"using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using UnityModManagerNet;

using MicroWrath;
using MicroWrath.Util;

using UniRx;

namespace {ns}
{{
    internal abstract class ModMain : IMicroMod
    {{
        private static ModMain? instance;
        protected internal static ModMain Instance
        {{
            get => instance!;
            protected set => instance = value;
        }}

        private UnityModManager.ModEntry? modEntry;
        protected internal UnityModManager.ModEntry? ModEntry
        {{
            get => modEntry;
            protected set => modEntry = value;
        }}

        private Harmony? harmony;
        protected internal Harmony Harmony
        {{
            get => harmony!;
            protected set => harmony = value;
        }}

        protected virtual void ApplyHarmonyPatches()
        {{
            Harmony.PatchAll();
        }}

        //public event Action<UnityModManager.ModEntry> Loaded = Functional.Ignore;

        protected virtual void DoInit()
        {{
            var initMethods = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(m => m.GetParameters().Length == 0 && m.GetCustomAttribute<InitAttribute>() is not null);

            foreach (var method in initMethods)
            {{
                method.Invoke(null, null);
            }}
        }}

        public virtual bool Load(UnityModManager.ModEntry modEntry)
        {{
            this.modEntry = modEntry;
            MicroLogger.ModEntry = modEntry;

            //DoInit();

            Triggers.BlueprintsCache_Init_Prefix.Subscribe(_ => DoInit());

            harmony = new Harmony(modEntry.Info.Id);
            ApplyHarmonyPatches();

            instance = this;

            //Loaded(modEntry);

            return true;
        }}

        public virtual bool Load(Kingmaker.Modding.OwlcatModification mod) => throw new NotImplementedException();
    }}
}}");

                spc.AddSource("MicroMod.Main", sb.ToString());
                
                sb.Clear();

                var name = maybeType.Value?.Name ?? "Main";

                if (!shouldGen) return;

                var props = maybeType.Map(t => new
                {
                    HasParameterlessConstructor = t.Constructors.Any(c => c.Parameters.Length == 0),
                });

                sb.AppendLine($@"using System;
using System.Linq;
using System.Reflection;

using HarmonyLib;

using UnityModManagerNet;

using MicroWrath;
using MicroWrath.Util;

namespace {ns}
{{
    internal partial class {name} : ModMain
    {{");
                if(!(props.Value?.HasParameterlessConstructor ?? false))
                sb.Append($@"
        private {name}() {{ }}");

                sb.Append($@"
    }}
}}
");

                spc.AddSource("Main", sb.ToString());
            });

        }
    }
}
