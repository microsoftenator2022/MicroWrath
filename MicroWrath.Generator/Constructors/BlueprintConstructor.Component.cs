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

            var typeParams = Incremental.GetTypeParameters(newComponentMethodInvocations.Select((m, _) => m.symbol), syntax);

            //context.RegisterSourceOutput(
            //    newComponentMethodInvocations
            //        .Collect()
            //        .Combine(typeParams.Collect())
            //        .Combine(syntax.Collect())
            //        .Combine(compilation),
            //    (spc, xs) =>
            //{
            //    var (((ncmis, tps), syntax), compilation) = xs;

            //    var sb = new StringBuilder();

            //    var methodInvocations = syntax
            //        .Where(sc => sc.Node is GenericNameSyntax &&
            //            sc.Node.GetText().ToString().Contains("AddNewComponent"));

            //    foreach (var mi in methodInvocations)
            //    {
            //        sb.AppendLine("// Text:");
            //        foreach (var line in mi.Node.GetText().Lines)
            //        {
            //            sb.AppendLine($" // {line}");
            //        }

            //        var node = mi.Node as GenericNameSyntax;

            //        var id = node?.Identifier;

            //        var symbol = node is not null ? mi.SemanticModel.GetSymbolInfo(node!).Symbol : null;

            //        sb.AppendLine($"// Symbol: {symbol?.ToString() ?? "<null>" }");

            //        var ancestors = mi.Node.Ancestors(true);

            //        var treeSemanticModel = compilation.GetSemanticModel(ancestors.Last().SyntaxTree, true);

            //        sb.AppendLine($" // Ancestors:");

            //        foreach (var ancestor in ancestors)
            //        {
            //            foreach (var line in ancestor.GetText().Lines)
            //            {
            //                sb.AppendLine($"   // {line}");
            //            }

            //            var ancestorSymbol = treeSemanticModel.GetSymbolInfo(ancestor).Symbol ?? treeSemanticModel.GetDeclaredSymbol(ancestor);
            //            if (ancestorSymbol is not null)
            //            {
            //                sb.AppendLine($"  // Non-null symbol: {ancestorSymbol}");

            //                if (ancestorSymbol is not IMethodSymbol ms)
            //                    continue;

            //                var returnType = ms.ReturnType;

            //                sb.AppendLine($"   // Method return type: {returnType?.ToString() ?? "<null>"}");

            //                if (ancestor is SimpleLambdaExpressionSyntax sles)
            //                {
            //                    var paramTypeSymbol = treeSemanticModel.GetSymbolInfo(sles.Parameter).Symbol ?? treeSemanticModel.GetDeclaredSymbol(sles.Parameter);

            //                    sb.AppendLine($"    //! {treeSemanticModel.GetSymbolInfo(sles.Parameter).CandidateReason}");

            //                    sb.AppendLine($"  // param type: {paramTypeSymbol?.ToString() ?? "<null>"}");

            //                    sb.AppendLine($"  // As parenthesized");

            //                    //var parameter = sles.Parameter.WithType();

            //                    var list = SyntaxFactory.SeparatedList(EnumerableExtensions.Singleton(sles.Parameter));
            //                    var parameterList = SyntaxFactory.ParameterList(list);
            //                    var parenthesized = SyntaxFactory.ParenthesizedLambdaExpression(
            //                        parameterList, sles.Body);

            //                    foreach (var line in parenthesized.GetText().Lines)
            //                    {
            //                        sb.AppendLine($"   // {line}");
            //                    }
            //                }
            //            }

            //            sb.AppendLine();
            //        }

            //        sb.AppendLine();
            //    }

            //    spc.AddSource("typeParams", sb.ToString());
            //});

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

                spc.AddSource("0DEBUG_componentInitTypes", sb.ToString());
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
