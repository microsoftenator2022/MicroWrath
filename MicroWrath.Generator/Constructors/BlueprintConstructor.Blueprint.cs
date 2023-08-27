using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using static MicroWrath.Generator.Constants;

namespace MicroWrath.Generator
{
    internal partial class BlueprintConstructor
    {
        internal static void CreateBlueprintConstructors(
            IncrementalValueProvider<Compilation> compilation,
            IncrementalValuesProvider<GeneratorSyntaxContext> syntax,
            IncrementalGeneratorInitializationContext context)
        {
            var simpleBlueprintType = compilation
                .Select(static (c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.SimpleBlueprint"));

            var invocations = syntax
                .Where(static sc => sc.Node is InvocationExpressionSyntax)
                .Select(static (sc, _) =>
                  (node: (InvocationExpressionSyntax)sc.Node,
                    symbol: (sc.SemanticModel.GetSymbolInfo(sc.Node).Symbol as IMethodSymbol)!,
                    sm: sc.SemanticModel))
                .Where(static ns => ns.symbol is not null);

            var newBlueprintMethodInvocations = Incremental.GetMethodInvocations(
                $"{ConstructorClassFullName}+{ConstructNewClassName}",
                NewBlueprintMethodName,
                compilation,
                invocations);

            var invocationTypeArguments = Incremental.GetTypeParameters(newBlueprintMethodInvocations.Select((m, _) => m.symbol), syntax)
                .Collect()
                .Combine(simpleBlueprintType)
                .Combine(compilation)
                .SelectMany(static (tsbp, _) =>
                {
                    var ((ts, simpleBlueprint), compilation) = tsbp;

                    return ts
                        .OfType<INamedTypeSymbol>()
                        .Distinct((IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
                        .Where(t => simpleBlueprint is not null &&
                            compilation.ClassifyConversion(t, simpleBlueprint).Exists &&
                            !t.Equals(simpleBlueprint, SymbolEqualityComparer.Default));
                });

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initMembers = GetTypeMemberInitialValues(invocationTypeArguments, defaultValuesType);

            #region DebugOutput
//#if DEBUG
//            context.RegisterImplementationSourceOutput(initMembers.Collect(), (spc, initMemberTypes) =>
//            {
//                var types = initMemberTypes;

//                var sb = new StringBuilder();

//                foreach (var (bpType, fields, properties, methods) in types)
//                {
//                    sb.AppendLine($"// {bpType}");

//                    if (fields.Length > 0)
//                    {
//                        sb.AppendLine(" // Fields:");
//                        foreach (var f in fields)
//                        {
//                            sb.AppendLine($"  // {f}");
//                        }
//                    }

//                    if (properties.Length > 0)
//                    {
//                        sb.AppendLine(" // Properties:");
//                        foreach (var p in properties)
//                        {
//                            sb.AppendLine($"  // {p}");
//                        }
//                    }

//                    if (methods.Length > 0)
//                    {
//                        sb.AppendLine(" // Methods:");
//                        foreach (var m in methods)
//                        {
//                            sb.AppendLine($"  // {m}");
//                        }
//                    }
//                }

//                spc.AddSource("0DEBUG_blueprintInitTypes", sb.ToString());
//            });
//#endif
            #endregion
            context.RegisterImplementationSourceOutput(initMembers, (spc, bpInit) =>
            {
                var (bpType, fields, properties, methods) = bpInit;

                spc.AddSource(bpType.Name, BlueprintConstructorPart(bpType, fields, properties, methods));
            });
        }
    }
}
