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
        internal record class InitMembers(INamedTypeSymbol Type, ImmutableArray<(IFieldSymbol f, IFieldSymbol d)> InitFields,
            ImmutableArray<(IPropertySymbol p, IFieldSymbol d)> InitProperties,
            ImmutableArray<IMethodSymbol> InitMethods);

        internal static IncrementalValuesProvider<InitMembers> GetTypeMemberInitialValues(IncrementalValuesProvider<INamedTypeSymbol> types, IncrementalValueProvider<Option<INamedTypeSymbol>> defaults)
        {

            var defaultValues = defaults
                .SelectMany(static (defaults, _) => defaults.ToEnumerable().SelectMany(t => t.GetMembers().OfType<IFieldSymbol>()))
                .Collect();

            var withMembers = types.Select(static (t, _) => (t, t.GetBaseTypesAndSelf().SelectMany(t => t.GetMembers())));

            var allFields = withMembers
                .Select(static (tms, _) =>
                {
                    var (t, ms) = tms;

                    return (t, ms.OfType<IFieldSymbol>());
                });

            var withFields = allFields
                .Combine(defaultValues)
                .Select(static (fds, _) =>
                {
                    var ((t, fields), defaults) = fds;

                    return (t, fields: fields
                        .SelectMany(f => defaults
                            .TryFind(d => d.Type.Equals(f.Type, SymbolEqualityComparer.Default))
                            .Map(d => (f, d))
                            .ToEnumerable())
                        .ToImmutableArray());
                })
                .Where(tfds => tfds.fields.Length > 0)
                .Collect()
                .Select(static (tfds, _) => tfds
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));

            var allProperties = withMembers
                .Select(static (tms, _) =>
                {
                    var (t, ms) = tms;

                    return (t, ms.OfType<IPropertySymbol>().Where(p => !p.IsReadOnly));
                });

            var withProperties = allProperties
                .Combine(defaultValues)
                .Select(static (pds, _) =>
                {
                    var ((t, properties), defaults) = pds;

                    return (t, properties: properties
                        .SelectMany(p => defaults
                            .TryFind(d => d.Type.Equals(p.Type, SymbolEqualityComparer.Default))
                            .Map(d => (p, d))
                            .ToEnumerable())
                        .ToImmutableArray());
                })
                .Where(tpds => tpds.properties.Length > 0)
                .Collect()
                .Select(static (tpds, _) => tpds
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));

            var initMethods = defaults
                .SelectMany((d, _) => d.ToEnumerable())
                .SelectMany((d, _) => d.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Parameters.Length == 1 &&
                        m.ReturnType.Equals(m.Parameters[0].Type, SymbolEqualityComparer.Default)));
               
            var withInitMethods = types
                .Combine(initMethods.Collect())
                .Select((tm, _) =>
                {
                    var (type, initMethods) =  tm;

                    return (type, initMethods
                        .Where(m => type.GetBaseTypesAndSelf()
                            .Any(bpType => m.ReturnType.Equals(bpType, SymbolEqualityComparer.Default)))
                        .ToImmutableArray());
                })
                .Collect()
                .Select(static (tms, _) => tms
                    .ToDictionary(SymbolEqualityComparer.Default)
                    .ToImmutableDictionary(SymbolEqualityComparer.Default));
            
            return types
                .Combine(withFields)
                .Combine(withProperties)
                .Combine(withInitMethods)
                .Select((bpWithInit, _) =>
                {
                    var (((t, fields), properties), methods) = bpWithInit;

                    var hasInitFields = fields.TryGetValue(t, out var initFields);
                    if (!hasInitFields) initFields = ImmutableArray.Create<(IFieldSymbol, IFieldSymbol)>();

                    var hasInitProperties = properties.TryGetValue(t, out var initProperties);
                    if (!hasInitProperties) initProperties = ImmutableArray.Create<(IPropertySymbol, IFieldSymbol)>();

                    var hasInitMethods = methods.TryGetValue(t, out var initMethods);
                    if (!hasInitMethods) initMethods = ImmutableArray.Create<IMethodSymbol>();

                    return new InitMembers(t, initFields, initProperties, initMethods);
                });
        }
    }
}
