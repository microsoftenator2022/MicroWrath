using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
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
            IncrementalGeneratorInitializationContext context)
        {
            var blueprintComponentType = compilation
                .Select(static (c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.BlueprintComponent"));

            var invocations = syntax
                .Select(static (sc, _) =>
                   (node: (InvocationExpressionSyntax)sc.Node,
                    symbol: (sc.SemanticModel.GetSymbolInfo(sc.Node).Symbol as IMethodSymbol)!,
                    sm: sc.SemanticModel))
                .Where(static ns => ns.symbol is not null);

            var newComponentMethodInvocations = Incremental.GetMethodInvocations(
                $"{ConstructorClassFullName}+{ConstructNewClassName}",
                NewComponentMethodName,
                compilation,
                invocations);

            var typeParams = Incremental.GetTypeParameters(newComponentMethodInvocations, syntax);

            context.RegisterSourceOutput(typeParams.Collect(), (spc, types) =>
            {
                var sb = new StringBuilder();

                foreach (var t in types)
                {
                    sb.Append($"// {t}");
                }

                spc.AddSource("componentTypeParams", sb.ToString());
            });

            var invocationTypeArguments = typeParams
                .Collect()
                .Combine(blueprintComponentType)
                .SelectMany(static (tsbp, _) =>
                {
                    var (ts, blueprintComponent) = tsbp;

                    return ts
                        .OfType<INamedTypeSymbol>()
                        .Distinct((IEqualityComparer<INamedTypeSymbol>)SymbolEqualityComparer.Default)
                        .Where(t => !t.Equals(blueprintComponent, SymbolEqualityComparer.Default));
                });

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initMembers = GetTypeMemberInitialValues(invocationTypeArguments, defaultValuesType);

#region DebugOutput
#if DEBUG
            context.RegisterSourceOutput(defaultValuesType.Combine(initMembers.Collect()), (spc, defaultValues) =>
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

                spc.AddSource("componentInitTypes", sb.ToString());
            });
#endif
#endregion
            context.RegisterImplementationSourceOutput(initMembers, (spc, componentInit) =>
            {
                var (componentType, fields, properties, methods) = componentInit;

                spc.AddSource(componentType.Name, ComponentConstructorPart(componentType, fields, properties, methods));
            });
        }
    }
}
