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
        internal static IncrementalValuesProvider<(INamedTypeSymbol bp, ImmutableArray<(IFieldSymbol f, IFieldSymbol d)> initFields, ImmutableArray<(IPropertySymbol p, IFieldSymbol d)> initProperties)>
            GetBpMemberInitialValues(IncrementalValuesProvider<INamedTypeSymbol> bpTypes, IncrementalValueProvider<Option<INamedTypeSymbol>> defaults)
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
            
            return bpTypes
                .Combine(withFields)
                .Combine(withProperties)
                .Select((bpfps, _) =>
                {
                    var ((bp, fields), properties) = bpfps;

                    var hasInitFields = fields.TryGetValue(bp, out var initFields);
                    if (!hasInitFields) initFields = ImmutableArray.Create<(IFieldSymbol, IFieldSymbol)>();

                    var hasInitProperties = properties.TryGetValue(bp, out var initProperties);
                    if (!hasInitProperties) initProperties = ImmutableArray.Create<(IPropertySymbol, IFieldSymbol)>();

                    return (bp, initFields, initProperties);
                })
                .Where(bpfps => bpfps.initFields.Length > 0 || bpfps.initProperties.Length > 0);
        }
    }
}
