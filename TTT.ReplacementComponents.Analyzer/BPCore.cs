using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TTT.ReplacementComponents.Analyzer;

internal static class BPCore
{
    public static IEnumerable<INamedTypeSymbol> GetConfiguratorTypes(Compilation compilation, CancellationToken? ct = null)
    {
        var namespaces = Util.GetAllNamespaces(compilation.GlobalNamespace, ct).Where(ns => ns.ToString().StartsWith("BlueprintCore"));

        var baseType = namespaces.FirstOrDefault(ns => ns.ToString() == "BlueprintCore.Blueprints.Configurators")
            ?.GetTypeMembers()
            .FirstOrDefault(t =>
                t.IsGenericType &&
                t.Name == "BaseBlueprintConfigurator"
            );

        if (baseType is null)
            yield break;

        foreach (var t in namespaces.SelectMany(ns => ns.GetTypeMembers()))
        {
            if (ct?.IsCancellationRequested ?? false)
                break;

            if (t.BaseType is null)
                continue;

            if (!Util.GetAllBaseTypesAndSelf(t, ct).Any(t => t.IsGenericType))
                continue;

            if (Util.GetAllBaseTypesAndSelf(t, ct).Any(t => t.Equals(baseType, SymbolEqualityComparer.Default) || t.ConstructedFrom.Equals(baseType, SymbolEqualityComparer.Default)))
                yield return t;
        }
    }
}

public partial class TTTReplacementAnalyzer
{
    private void AnalyzeBPCoreConfigurators(OperationAnalysisContext context)
    {
        var sm = context.Operation.SemanticModel;

        if (sm is null)
            return;

        if (sm.GetSymbolInfo(context.Operation.Syntax, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            return;

        var configuratorTypes = BPCore.GetConfiguratorTypes(context.Compilation, context.CancellationToken);

        var methods = configuratorTypes
            .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>())
            .Where(m => !m.IsStatic)
            .ToArray();

        if (!methods.Any(m => methodSymbol.Name == m.Name))
            return;

        var componentTypeName = methodSymbol.Name;

        INamedTypeSymbol? tryGetReplacementType(string typeName) =>
            TryGetTTTReplacement(typeName, context.Compilation, context.CancellationToken);

        INamedTypeSymbol? replacementType = tryGetReplacementType(componentTypeName);

        if (replacementType is null && componentTypeName.StartsWith("Add"))
        {
            componentTypeName = componentTypeName.Remove(0, 3);
            replacementType = tryGetReplacementType(componentTypeName);
        }

        if (replacementType is not null)
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Operation.Syntax.GetLocation(), componentTypeName, replacementType));
    }
}
