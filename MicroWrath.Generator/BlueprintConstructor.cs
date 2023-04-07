using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Elfie.Model;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    [Generator]
    internal partial class BlueprintConstructor : IIncrementalGenerator
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
    internal static partial class {ConstructClassName}
    {{
        private interface IBlueprintConstructor<out TBlueprint> where TBlueprint : SimpleBlueprint
        {{
            TBlueprint New(string assetId, string name);
        }}

        private partial class BlueprintConstructor : IBlueprintConstructor<SimpleBlueprint>
        {{
            internal BlueprintConstructor() {{ }}

            SimpleBlueprint IBlueprintConstructor<SimpleBlueprint>.New(string assetId, string name) =>
                new() {{ AssetGuid = BlueprintGuid.Parse(assetId), name = name }};

            public TBlueprint New<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint =>
                ((IBlueprintConstructor<TBlueprint>)this).New(assetId, name);
        }}

        public static class {ConstructNewClassName}
        {{
            private static readonly Lazy<BlueprintConstructor> blueprintConstructor = new(() => new());
            public static TBlueprint {NewBlueprintMethodName}<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint =>
                blueprintConstructor.Value.New<TBlueprint>(assetId, name);
        }}
    }}
}}");
                pic.AddSource("blueprintConstructorBase", sb.ToString());
            });

            var compilation = context.CompilationProvider;

            var simpleBlueprintType = compilation.Select(static (c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.SimpleBlueprint"));

            var blueprintConstructorType = compilation
                .Select(static (c, _) => c.Assembly
                    .GetTypeByMetadataName($"{ConstructorClassFullName}+{ConstructNewClassName}")
                    .ToOption());

            var newBlueprintMethod = blueprintConstructorType
                .Select(static (t, _) =>
                    t.Bind(static t => t.GetMembers(NewBlueprintMethodName).TryHead()));

            var syntax = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is InvocationExpressionSyntax,
                static (sc, _) => sc);

            var invocations = syntax
                .Where(static sc => sc.Node is InvocationExpressionSyntax)
                .Select(static (sc, _) =>
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
                .Combine(syntax.Collect())
                .SelectMany(static (tai, ct) =>
                {
                    var (ta, invocationsSyntax) = tai;

                    if (ta is not ITypeParameterSymbol tps)
                        return EnumerableExtensions.Singleton(ta);

                    return Analyzers.GetAllGenericInstances(tps, invocationsSyntax, ct);
                })
                .Collect()
                .Combine(simpleBlueprintType)
                .SelectMany(static (tsbp, _) =>
                {
                    var (ts, simpleBlueprint) = tsbp;

                    return ts
                        .OfType<INamedTypeSymbol>()
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                        .Where(t => !t.Equals(simpleBlueprint, SymbolEqualityComparer.Default));
                });

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initMembers = GetBpMemberInitialValues(invocationTypeArguments, defaultValuesType, context);
//#if DEBUG
//            context.RegisterSourceOutput(defaultValuesType.Combine(initMembers.Collect()), (spc, defaultValues) =>
//            {
//                var (defaults, types) = defaultValues;

//                var sb = new StringBuilder();

//                sb.AppendLine($"// {defaults}");

//                foreach (var (bpType, fields, properties) in types)
//                {
//                    sb.AppendLine($"// {bpType}");

//                    if (fields.Length > 0)
//                    {
//                        sb.AppendLine($" // Fields:");
//                        foreach (var f in fields)
//                        {
//                            sb.AppendLine($"  // {f}");
//                        }
//                    }

//                    if (properties.Length > 0)
//                    {
//                        sb.AppendLine($" // Properties:");
//                        foreach (var p in properties)
//                        {
//                            sb.AppendLine($"  // {p}");
//                        }
//                    }
//                }

//                spc.AddSource("initTypes", sb.ToString());
//            });
//#endif

            context.RegisterImplementationSourceOutput(initMembers, (spc, bpInit) =>
            {
                var (bpType, fields, properties) = bpInit;

                spc.AddSource(bpType.Name, BlueprintConstructorPart(bpType, fields, properties));
            });
        }
    }
}
