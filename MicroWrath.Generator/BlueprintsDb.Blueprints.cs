using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MicroWrath.Util;
using MicroWrath.Generator.Common;
using System.Collections.Immutable;

namespace MicroWrath.Generator
{
    internal partial class BlueprintsDb
    {
        private readonly record struct BlueprintInfo(string GuidString, string Name, string TypeName)
        {
            public INamedTypeSymbol? GetBlueprintType(SemanticModel sm) => sm.Compilation.GetTypeByMetadataName(this.TypeName);
        }

        private static class Blueprints
        {
            public static Dictionary<ISymbol, ImmutableArray<BlueprintInfo>> BlueprintList { get; private set; } =
                new(SymbolEqualityComparer.Default);

            public static IncrementalValuesProvider<(ISymbol type, ImmutableArray<BlueprintInfo> blueprints)>
                GetBlueprintData(IncrementalValuesProvider<AdditionalText> cheatdataJson, IncrementalValueProvider<Compilation> compilation)
            {
                var blueprints = cheatdataJson
                .SelectMany(static (at, _) =>
                {
                    if (at.GetText()?.ToString() is not string text)
                        return Enumerable.Empty<BlueprintInfo>();

                    var entries = JValue.Parse(text)["Entries"].ToArray();

                    return entries.Choose<JToken, BlueprintInfo>(static entry =>
                    {
                        if (entry["Guid"]?.ToString() is string guid &&
                            entry["Name"]?.ToString() is string name &&
                            entry["TypeFullName"]?.ToString() is string typeName)
                        {
                            var nameChars = new List<char>();
                            string? escapedName = null;

                            if (!SyntaxFacts.IsValidIdentifier(name))
                            {
                                nameChars = name.Select(static c =>
                                    SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_').ToList();

                                if (!SyntaxFacts.IsIdentifierStartCharacter(name[0]))
                                    nameChars.Insert(0, '_');

                                escapedName = new string(nameChars.ToArray());
                            }
                            else escapedName = name;

                            return Option.Some(new BlueprintInfo(GuidString: guid, Name: escapedName, TypeName: typeName));
                        }

                        return Option.None<BlueprintInfo>();
                    });
                })
                .Collect()
                .Combine(compilation)
                .SelectMany(static (bpsc, _) =>
                {
                    var (bps, compilation) = bpsc;

                    var types = new Dictionary<string, ISymbol?>();

                    foreach (var typeName in bps.Select(static bp => bp.TypeName).Distinct())
                    {
                        types[typeName] = compilation.GetTypeByMetadataName(typeName);
                    }

                    return bps
                        .Select(
                            bp => types[bp.TypeName]?.Name == bp.Name ?
                            new BlueprintInfo(
                                GuidString: bp.GuidString,
                                TypeName: bp.TypeName,
                                Name: bp.Name + "_blueprint") :
                            bp)
                        .GroupBy(bp => types[bp.TypeName]!, SymbolEqualityComparer.Default);
                })
                .Where(static g => g.Key is not null && g.Key.DeclaredAccessibility == Accessibility.Public)
                .Select(static (bpType, ct) =>
                {
                    BlueprintList.Remove(bpType.Key);

                    IEnumerable<BlueprintInfo> renameDuplicates(IEnumerable<BlueprintInfo> source)
                    {
                        if (source.Count() == 1)
                        {
                            yield return source.First();
                        }
                        else
                        {
                            //var i = 0;
                            foreach (var sourceItem in source)
                            {
                                if (ct.IsCancellationRequested) break;

                                yield return new BlueprintInfo(
                                    GuidString: sourceItem.GuidString,
                                    TypeName: sourceItem.TypeName,
                                    Name: sourceItem.Name + $"_{sourceItem.GuidString}");
                            }
                        }
                    }
                    var bps = (key: bpType.Key, bps: bpType.GroupBy(static bp => bp.Name).SelectMany(renameDuplicates).ToImmutableArray());

                    BlueprintList[bpType.Key] = bps.bps;

                    return bps;
                });

                return blueprints;
            }
        }
    }
}
