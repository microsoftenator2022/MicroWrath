using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    [Generator]
    internal class BlueprintConstructor : IIncrementalGenerator
    {
        private const string ConstructorNamespace = "MicroWrath.Constructors";
        private const string ConstructClassName = "Construct";
        private const string ConstructNewClassName = "New";

        private static readonly string ConstructorClassFullName = $"{ConstructorNamespace}.{ConstructClassName}";

        private const string NewBlueprintMethodName = "Blueprint";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static pic =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("using System;");
                sb.AppendLine("using MicroWrath;");
                sb.AppendLine("using Kingmaker.Blueprints;");
                sb.Append($@"
namespace {ConstructorNamespace}
{{
    public static class {ConstructClassName}
    {{
        private interface IBlueprintConstructor<out TBlueprint> where TBlueprint : SimpleBlueprint
        {{
            TBlueprint New(string guid, string name);
        }}

        private partial class BlueprintConstructor : IBlueprintConstructor<SimpleBlueprint>
        {{
            internal BlueprintConstructor() {{ }}
            SimpleBlueprint IBlueprintConstructor<SimpleBlueprint>.New(string guid, string name) =>
                new() {{ AssetGuid = BlueprintGuid.Parse(name), name = name }} ;

            public TBlueprint New<TBlueprint>(string guid, string name) where TBlueprint : SimpleBlueprint =>
                ((IBlueprintConstructor<TBlueprint>)this).New(guid, name);
        }}

        public static partial class {ConstructNewClassName}
        {{
            private static readonly Lazy<BlueprintConstructor> blueprintConstructor = new(() => new());
            public static TBlueprint {NewBlueprintMethodName}<TBlueprint>(string guid, string name) where TBlueprint : SimpleBlueprint => blueprintConstructor.Value.New<TBlueprint>(guid, name);
        }}
    }}
}}");
                pic.AddSource("blueprintConstructors", sb.ToString());
            });

            var compilation = context.CompilationProvider;

            var simpleBlueprintType = compilation.Select((c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.SimpleBlueprint"));

            var blueprintConstructorType = compilation
                .Select(static (c, _) =>
                    Option.OfObj(c.Assembly
                        .GetTypeByMetadataName(
                            // CLR names for nested classes are separated by '+', not '.'
                            $"{ConstructorClassFullName}+{ConstructNewClassName}")));

            var newBlueprintMethod = blueprintConstructorType
                .Select(static (t, _) =>
                    t.Bind(t => Option.OfObj(t.GetMembers(NewBlueprintMethodName).FirstOrDefault())));

            var invocations = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is InvocationExpressionSyntax,
                static (sc, _) =>
                  ( node: (InvocationExpressionSyntax)sc.Node,
                    symbol: sc.SemanticModel.GetSymbolInfo(sc.Node),
                    sm: sc.SemanticModel ));

            var newBlueprintMethodInvocations = invocations
                .Combine(newBlueprintMethod)
                .SelectMany(static (ns, _) =>
                {
                    var ((node, si, sm), methodSymbol) = ns;

                    if (si.Symbol is not IMethodSymbol symbol)
                        return Enumerable.Empty<(InvocationExpressionSyntax node, IMethodSymbol symbol, SemanticModel sm)>();

                    return methodSymbol.Bind<ISymbol, (InvocationExpressionSyntax node, IMethodSymbol symbol, SemanticModel sm)>(ms =>
                    {
                        if (!ms.Equals(symbol.ConstructedFrom, SymbolEqualityComparer.Default))
                            return Option.None<(InvocationExpressionSyntax, IMethodSymbol, SemanticModel)>();

                        return Option.Some((node, symbol, sm));
                    })
                    .ToEnumerable();
                });

            var invocationTypeArguments = newBlueprintMethodInvocations
                .SelectMany(static (m, _) => m.symbol.TypeArguments)
                .Collect()
                .Combine(simpleBlueprintType)
                .SelectMany(static (tsbp, _) =>
                {
                    var (ts, simpleBlueprint) = tsbp;
                    return ts.OfType<INamedTypeSymbol>().Where(t => !t.Equals(simpleBlueprint, SymbolEqualityComparer.Default));
                });
                
            context.RegisterSourceOutput(invocationTypeArguments.Collect(), static (spc, ts) =>
            {
                var sb = new StringBuilder();

                foreach (var t in ts)
                {
                    sb.AppendLine($"// Node: {t}");
                }

                spc.AddSource("debug", sb.ToString());
            });
        }
    }
}
