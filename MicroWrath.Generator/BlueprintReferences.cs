using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

using Microsoft.CodeAnalysis;

using MicroWrath.Generator.Common;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    [Generator]
    internal class BlueprintReferences : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;

            var referenceBaseType = compilation
                .Select((c, _) => c.GetTypeByMetadataName("Kingmaker.Blueprints.AnyBlueprintReference")
                    ?.BaseType
                    ?.ConstructUnboundGenericType());

            var blueprintReferenceTypes =
                Incremental.GetAssignableTypes(compilation)
                .Combine(compilation)
                .Combine(referenceBaseType)
                .SelectMany(static (tc, ct) =>
                {
                    var (referenceType, compilation, referenceBaseType) = tc.Expand();

                    if (referenceBaseType is null || referenceType.IsUnboundGenericType)
                        return Enumerable.Empty<(INamedTypeSymbol, INamedTypeSymbol)>();

                    if (referenceType.BaseType is not INamedTypeSymbol baseType)
                        return Enumerable.Empty<(INamedTypeSymbol, INamedTypeSymbol)>();

                    if (!baseType.IsGenericType ||
                        !referenceBaseType.Equals(baseType.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                        return Enumerable.Empty<(INamedTypeSymbol, INamedTypeSymbol)>();

                    if (baseType.TypeArguments.OfType<INamedTypeSymbol>().FirstOrDefault()
                        is not INamedTypeSymbol argumentType)
                        return Enumerable.Empty<(INamedTypeSymbol, INamedTypeSymbol)>();

                    return EnumerableExtensions.Singleton((referenceType, argumentType));

                    //var ts = type.GetBaseTypesAndSelf(ct)
                    //    .Take(1)
                    //    .Where(t =>
                    //        t.IsGenericType &&
                    //        referenceBaseType.Equals(t.ConstructUnboundGenericType(), SymbolEqualityComparer.Default))
                    //    .Select(t => (t, t.TypeArguments.OfType<INamedTypeSymbol>().FirstOrDefault()))
                    //    .SelectMany(ts =>
                    //    {
                    //        var (t, ta) = ts;
                    //        if (ta is null)
                    //            return Array.Empty<(INamedTypeSymbol, INamedTypeSymbol)>();

                    //        return EnumerableExtensions.Singleton(ts);
                    //    });

                    //return ts;
                })
                .Collect();

            //var blueprintTypes = Incremental.GetBlueprintTypes(compilation);

            context.RegisterSourceOutput(blueprintReferenceTypes, (spc, referenceTypes) =>
            {
                var matchedhNames = referenceTypes
                    .Where(ts =>
                    {
                        var (refType, bpType) = ts;

                        return refType.Name == $"{bpType.Name}Reference";
                    })
                    .Select(ts => (refTypeName: ts.Item1.ToString(), bpName: ts.Item2.ToString()))
                    .DistinctBy(ts => ts.bpName);

                var sb = new StringBuilder();

                sb.AppendLine("using Kingmaker;");
                sb.AppendLine("using Kingmaker.Blueprints;");
                sb.AppendLine($@"
namespace MicroWrath.Extensions
{{
    internal static partial class BlueprintExtensions
    {{");
                foreach (var (rt, ta) in matchedhNames)
                {
                    if (spc.CancellationToken.IsCancellationRequested)
                        break;

                    sb.AppendLine($@"
        public static {rt} ToReference(this {ta} blueprint) => blueprint.ToReference<{rt}>();");
                }

                sb.AppendLine($@"
    }}
}}");

                spc.AddSource("Blueprint.ToReference", sb.ToString());

                sb.Clear();

                sb.AppendLine("using Kingmaker;");
                sb.AppendLine("using Kingmaker.Blueprints;");
                sb.AppendLine("using MicroWrath;");

                sb.AppendLine($@"
namespace MicroWrath.Extensions
{{
    internal static partial class MicroBlueprintExtensions
    {{");

                foreach (var (rt, ta) in matchedhNames)
                {
                    if (spc.CancellationToken.IsCancellationRequested)
                        break;

                    sb.AppendLine($@"
        public static {rt} ToReference(this IMicroBlueprint<{ta}> microBlueprint) => microBlueprint.ToReference<{ta}, {rt}>();");
                }

                sb.AppendLine($@"
    }}
}}");

                spc.AddSource("IMicroBlueprint.ToReference", sb.ToString());
            });
        }
    }
}
