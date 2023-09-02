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
        internal static void CreateComponentConstructors(
            IncrementalValueProvider<Compilation> compilation,
            IncrementalValuesProvider<GeneratorSyntaxContext> syntax,
            IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<INamedTypeSymbol> generatedComponentReferences)
        {
            var blueprintComponentType = compilation
                .Select(static (c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.BlueprintComponent"));

            var invocations = syntax
                .Where(static sc => sc.Node is InvocationExpressionSyntax)
                .Select(static (sc, ct) =>
                   (node: (InvocationExpressionSyntax)sc.Node,
                    symbol: (sc.SemanticModel.GetSymbolInfo(sc.Node, ct).Symbol as IMethodSymbol)!,
                    sm: sc.SemanticModel))
                .Where(static ns => ns.symbol is not null);

            var newComponentMethodInvocations = Incremental.GetMethodInvocations(
                $"{ConstructorClassFullName}+{ConstructNewClassName}",
                NewComponentMethodName,
                compilation,
                invocations);

            var typeParams = Incremental.GetTypeParameters(newComponentMethodInvocations.Select((m, _) => m.symbol), syntax);

            var invocationTypeArguments = typeParams
                .Collect()
                .Combine(blueprintComponentType)
                .Combine(generatedComponentReferences.Collect())
                .Combine(compilation)
                .SelectMany(static (tsbp, _) =>
                {
                    var (((ts, blueprintComponent), generatedRefs), compilation) = tsbp;

                    return ts
                        .OfType<INamedTypeSymbol>()
                        .Concat(generatedRefs)
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                        .Where(t => blueprintComponent is not null &&
                            compilation.ClassifyConversion(t, blueprintComponent).Exists &&
                            !t.Equals(blueprintComponent, SymbolEqualityComparer.Default));
                });

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initMembers = GetTypeMemberInitialValues(invocationTypeArguments, defaultValuesType);

            #region DebugOutput
#if DEBUG
            context.RegisterImplementationSourceOutput(defaultValuesType.Combine(initMembers.Collect()), (spc, defaultValues) =>
            {
                var (defaults, types) = defaultValues;

                var sb = new StringBuilder();

                foreach (var (type, fields, properties, methods) in types)
                {
                    sb.AppendLine($"// {type}");

                    if (fields.Length > 0)
                    {
                        sb.AppendLine(" // Fields:");
                        foreach (var f in fields)
                        {
                            sb.AppendLine($"  // {f}");
                        }
                    }

                    if (properties.Length > 0)
                    {
                        sb.AppendLine(" // Properties:");
                        foreach (var p in properties)
                        {
                            sb.AppendLine($"  // {p}");
                        }
                    }

                    if (methods.Length > 0)
                    {
                        sb.AppendLine(" // Methods:");
                        foreach (var m in methods)
                        {
                            sb.AppendLine($"  // {m}");
                        }
                    }
                }

                spc.AddSource("0DEBUG_componentInitTypes", sb.ToString());
            });
#endif
            #endregion

            context.RegisterImplementationSourceOutput(initMembers, (spc, componentInit) =>
            {
                var (componentType, fields, properties, methods) = componentInit;

                if (spc.CancellationToken.IsCancellationRequested)
                    return;

                AddSource(spc, componentType, ComponentConstructorPart(componentType, fields, properties, methods, spc.CancellationToken));
            });
        }
    }
}
