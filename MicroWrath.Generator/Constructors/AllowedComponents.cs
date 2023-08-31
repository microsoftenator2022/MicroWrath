using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

using MicroWrath.Generator.Common;

namespace MicroWrath.Generator
{
    internal partial class BlueprintConstructor
    {
        private static IncrementalValuesProvider<(INamedTypeSymbol blueprintType, ImmutableArray<INamedTypeSymbol> componentTypes)> GetAllowedComponents(
            IncrementalValueProvider<Compilation> compilation)
        {
            var allowedOnAttribute = compilation
                .Select(static (c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.AllowedOnAttribute"));

            var componentTypes = Incremental.GetComponentTypes(compilation)
                .Where(static c => !c.IsAbstract)
                .Combine(allowedOnAttribute)
                .Select(static (componentType, _) => (
                    componentType: componentType.Left,
                    allowedOnAttributes: componentType.Left.GetAttributes()
                        .Where(attr => attr.AttributeClass?.Equals(componentType.Right, SymbolEqualityComparer.Default) ?? false)))
                .Where(static ct => ct.allowedOnAttributes.Any());

            var componentsAllowedOn = componentTypes
                .Select(static (cs, _) =>
                {
                    var (componentType, attributes) = cs;

                    var allowedOn = attributes
                        .Select(static attr => attr.ConstructorArguments
                            .FirstOrDefault(static arg => arg.Kind == TypedConstantKind.Type))
                        .Select(static arg => arg.Value);

                    return (componentType, allowedOn: allowedOn.OfType<INamedTypeSymbol>());
                });

            var byBlueprintType = componentsAllowedOn
                .Collect()
                .Select(static (cs, ct) =>
                {
                    var dict = new Dictionary<INamedTypeSymbol, ImmutableArray<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

                    foreach (var (c, bpts) in cs)
                    {
                        if (ct.IsCancellationRequested) break;

                        foreach (var bpt in bpts)
                        {
                            if (ct.IsCancellationRequested) break;

                            if (!dict.ContainsKey(bpt))
                                dict[bpt] = ImmutableArray.Create<INamedTypeSymbol>();

                            dict[bpt] = dict[bpt].Add(c);
                        }
                    }

                    return dict;
                })
                .SelectMany(static (dict, _) => dict.Select(static kvp => (blueprintType: kvp.Key, componentTypes: kvp.Value)));
            return byBlueprintType;
        }

        private void GenerateAllowedComponentsConstructors(IncrementalGeneratorInitializationContext context,
            IncrementalValuesProvider<(INamedTypeSymbol blueprintType, ImmutableArray<INamedTypeSymbol> componentTypes)> byBlueprintType)
        {
#if DEBUG
            context.RegisterImplementationSourceOutput(byBlueprintType.Collect(), static (spc, bpcs) =>
            {
                var sb = new StringBuilder();

                sb.AppendLine($"// Blueprint types: {bpcs.Length}");

                foreach (var (bpt, cs) in bpcs)
                {
                    if (spc.CancellationToken.IsCancellationRequested) return;

                    sb.AppendLine($"// Blueprint type: {bpt}");
                    sb.AppendLine("// Allowed components:");

                    foreach (var c in cs)
                    {
                        if (spc.CancellationToken.IsCancellationRequested) return;

                        sb.AppendLine($"    // {c}");
                    }
                }

                if (spc.CancellationToken.IsCancellationRequested) return;

                spc.AddSource("allowedComponents", sb.ToString());
            });
#endif

            context.RegisterSourceOutput(byBlueprintType, static (spc, bpt) =>
            {
                var sb = new StringBuilder();

                var methods = new List<string>();

                var namespaces = new List<string>
                {
                    bpt.blueprintType.ContainingNamespace.ToString()
                };

                foreach (var c in bpt.componentTypes)
                {
                    if (c.IsGenericType) continue;

                    var ns = c.ContainingNamespace.ToString();
                    if (!namespaces.Contains(ns)) namespaces.Add(ns);

                    methods.Add($"internal static {c} Add{c.Name}(this {bpt.blueprintType} blueprint, Action<{c}>? init = null) => blueprint.AddComponent<{c}>(init);");
                }

                if (methods.Count == 0) return;

                sb.AppendLine($"using System;");
                foreach (var ns in namespaces)
                {
                    sb.AppendLine($"using {ns};");
                }

                sb.Append($@"
namespace MicroWrath.Extensions.Components
{{
    internal static class {bpt.blueprintType.Name}Components
    {{
");
                foreach (var methodLine in methods)
                {
                    sb.AppendLine($@"
        {methodLine}");
                }
                sb.Append($@"
    }}
}}");

                spc.AddSource($"{bpt.blueprintType.Name}.Components", sb.ToString());
            });
        }

        
    }
}
