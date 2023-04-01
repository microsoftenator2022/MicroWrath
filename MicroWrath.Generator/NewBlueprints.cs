using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

using Newtonsoft.Json.Linq;

using MicroWrath.Util;
using MicroWrath.Generator.Common;

namespace MicroWrath.Generator
{
    [Generator]
    public class NewBlueprints : IIncrementalGenerator
    {
        private readonly record struct BlueprintInfo(string Name, string AssetId, INamedTypeSymbol TypeSymbol)
        {
            public override string ToString()
            {
                var fullTypeName = TypeSymbol.FullName();

                return $"        public static readonly NewBlueprint<{fullTypeName}> {Name} = " +
                    $"new NewBlueprint<{fullTypeName}>(guidString: \"{AssetId}\", nameof({Name}));";
            }
        }

        private readonly record struct BlueprintInfoFile(string Path, IEnumerable<BlueprintInfo> Blueprints)
        {
            public string[] GetNameParts(string projectPath) =>
                GeneratorUtil.PathToNamespaceParts(this.Path, projectPath).ToArray();

            public string GetNamespace(string projectPath)
            {
                var nsParts = GetNameParts(projectPath);
                return nsParts.Take(nsParts.Length - 1).Aggregate((a, b) => $"{a}.{b}");
            }

            public string GetClassName(string projectPath)
            {
                var nsParts = GetNameParts(projectPath);
                return nsParts.Skip(nsParts.Length - 1).Single();
            }

            public string GetOutputFileName(string projectPath) =>
                GetNameParts(projectPath).Aggregate((a, b) => $"{a}.{b}");

            public string ToString(string projectPath)
            {
                var ns = GetNamespace(projectPath);
                var className = GetClassName(projectPath);

                return $@"using Microsoftenator.Wotr.Common;
namespace {ns}
{{
    public static partial class {className}
    {{
{Blueprints.Select(bp => bp.ToString()).Aggregate((a, b) => $"{a}{Environment.NewLine}{b}")}
    }}
}}";
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var projectDir = Incremental.GetConfig(context.AnalyzerConfigOptionsProvider).Select((c, _) => c.ProjectPath.Value ?? "");

            var compilation = context.CompilationProvider;

            var allBpTypes = Incremental.GetBlueprintTypes(compilation).Collect();

            var newBpFiles = context.AdditionalTextsProvider
                .Where(static f => f.Path.ToLower().EndsWith("newblueprints.json"));

            var bpText = newBpFiles
                .SelectMany(static (f, _) => Option.OfObj(f.GetText()?.ToString()).Map(t => (f.Path, t)).ToEnumerable());

            var bpStrings = bpText
                .SelectMany(static (fileText, _) =>
                {
                    var (path, text) = fileText;
                    var json = JArray.Parse(text);

                    return json
                        .Select<JToken, Option<(string, string, string, string)>>(j =>
                        {
                            var name = j["name"]?.ToString();
                            var assetId = j["assetId"]?.ToString();
                            var typeName = j["type"]?.ToString();

                            assetId = Guid.TryParse(assetId, out var guid) ? guid.ToString("N") : null;

                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(assetId) || string.IsNullOrEmpty(typeName))
                                return Option.None<(string, string, string, string)>();

                            return Option.Some((path, name!, assetId!, typeName!));
                        })
                        .SelectMany(Option.ToEnumerable);
                });

            var bpTypes = bpStrings
                .Select(static ((string, string, string, string typeName) bp, CancellationToken _) => bp.typeName)
                .Combine(allBpTypes)
                .Select(static (typeNameAndTypes, _) =>
                {
                    var (typeName, types) = typeNameAndTypes;

                    return (typeName, types.TryGetTypeSymbolByName(typeName));
                })
                .Collect();

            var bps = bpStrings
                .Combine(bpTypes)
                .SelectMany(static (stringsAndTypes, _) =>
                {
                    var ((path, name, assetId, typeName), types) = stringsAndTypes;

                    var type = types
                        .SelectMany(t =>
                        {
                            var (tName, tType) = t;

                            if (typeName != tName) return Enumerable.Empty<INamedTypeSymbol>();

                            return tType.ToEnumerable();
                        })
                        .FirstOrDefault();

                    return Option.OfObj(type).Map(t => (path, bp: new BlueprintInfo(name, assetId, t))).ToEnumerable();
                })
                .Collect()
                .SelectMany(static (pathsAndBps, _) =>
                {
                    return pathsAndBps
                        .GroupBy(pbp => pbp.path)
                        .Select(g => new BlueprintInfoFile(g.Key, g.Select(b => b.bp)));
                });

            context.RegisterSourceOutput(bps.Combine(projectDir), static (spc, bpFile) =>
            {
                var (file, projectDir) = bpFile;

                spc.AddSource(file.GetOutputFileName(projectDir), file.ToString(projectDir));
            });
        }
    }
}
