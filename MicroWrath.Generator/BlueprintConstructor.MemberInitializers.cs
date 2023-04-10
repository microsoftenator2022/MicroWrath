using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    internal partial class BlueprintConstructor
    {
        internal record class InitMembers(INamedTypeSymbol BlueprintType, ImmutableArray<(IFieldSymbol f, IFieldSymbol d)> InitFields,
            ImmutableArray<(IPropertySymbol p, IFieldSymbol d)> InitProperties,
            ImmutableArray<IMethodSymbol> InitMethods);

        internal static IncrementalValuesProvider<InitMembers> GetBpMemberInitialValues(IncrementalValuesProvider<INamedTypeSymbol> bpTypes, IncrementalValueProvider<Option<INamedTypeSymbol>> defaults)
        {

            var defaultValues = defaults
                .SelectMany(static (defaults, _) => defaults.ToEnumerable().SelectMany(t => t.GetMembers().OfType<IFieldSymbol>()))
                .Collect();

            var withMembers = bpTypes.Select(static (bp, _) => (bp, bp.GetBaseTypesAndSelf().SelectMany(t => t.GetMembers())));

            var allFields = withMembers
                .Select(static (bpms, _) =>
                {
                    var (bp, ms) = bpms;

                    return (bp, ms.OfType<IFieldSymbol>());
                });

            var withFields = allFields
                .Combine(defaultValues)
                .Select(static (fds, _) =>
                {
                    var ((bp, fields), defaults) = fds;

                    return (bp, fields: fields
                        .SelectMany(f => defaults
                            .TryFind(d => d.Type.Equals(f.Type, SymbolEqualityComparer.Default))
                            .Map(d => (f, d))
                            .ToEnumerable())
                        .ToImmutableArray());
                })
                .Where(static bpfds => bpfds.fields.Length > 0)
                .Collect()
                .Select(static (bpfds, _) => bpfds
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));

            var allProperties = withMembers
                .Select(static (bpms, _) =>
                {
                    var (bp, ms) = bpms;

                    return (bp, ms.OfType<IPropertySymbol>().Where(static p => !p.IsReadOnly));
                });

            var withProperties = allProperties
                .Combine(defaultValues)
                .Select(static (pds, _) =>
                {
                    var ((bp, properties), defaults) = pds;

                    return (bp, properties: properties
                        .SelectMany(p => defaults
                            .TryFind(d => d.Type.Equals(p.Type, SymbolEqualityComparer.Default))
                            .Map(d => (p, d))
                            .ToEnumerable())
                        .ToImmutableArray());
                })
                .Where(static bpfds => bpfds.properties.Length > 0)
                .Collect()
                .Select(static (bppds, _) => bppds
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));

            var initMethods = defaults
                .SelectMany((d, _) => d.ToEnumerable())
                .SelectMany((d, _) => d.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Parameters.Length == 1 &&
                        m.ReturnType.Equals(m.Parameters[0].Type, SymbolEqualityComparer.Default)));
               
            var withInitMethods = bpTypes
                .Combine(initMethods.Collect())
                .Select((bptm, _) =>
                {
                    var (bpType, initMethods) =  bptm;

                    return (bpType, initMethods
                        .Where(m => bpType.GetBaseTypesAndSelf()
                            .Any(bpType => m.ReturnType.Equals(bpType, SymbolEqualityComparer.Default)))
                        .ToImmutableArray());
                })
                .Collect()
                .Select(static (bppds, _) => bppds
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));
            
            return bpTypes
                .Combine(withFields)
                .Combine(withProperties)
                .Combine(withInitMethods)
                .Select((bpWithInit, _) =>
                {
                    var (((bp, fields), properties), methods) = bpWithInit;

                    var hasInitFields = fields.TryGetValue(bp, out var initFields);
                    if (!hasInitFields) initFields = ImmutableArray.Create<(IFieldSymbol, IFieldSymbol)>();

                    var hasInitProperties = properties.TryGetValue(bp, out var initProperties);
                    if (!hasInitProperties) initProperties = ImmutableArray.Create<(IPropertySymbol, IFieldSymbol)>();

                    var hasInitMethods = methods.TryGetValue(bp, out var initMethods);
                    if (!hasInitMethods) initMethods = ImmutableArray.Create<IMethodSymbol>();

                    return new InitMembers(bp, initFields, initProperties, initMethods);
                })
                .Where(bpWithInit => bpWithInit.InitFields.Length > 0 ||
                    bpWithInit.InitProperties.Length > 0 ||
                    bpWithInit.InitMethods.Length > 0);
        }
    }
}
