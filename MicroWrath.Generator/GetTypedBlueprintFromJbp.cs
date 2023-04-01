using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MicroWrath.Util;

using MicroWrath.Generator.Common;

namespace MicroWrath.Generator
{
    //[Generator]
    public class TypedBlueprintFromJbp : IIncrementalGenerator
    {
        private static string FormatLine((string _, string name, string assetId, INamedTypeSymbol type) b)
        {
            var typeName = b.type.FullName();

            return $"        public static readonly OwlcatBlueprint<{typeName}> {b.name} = " +
                $"new OwlcatBlueprint<{typeName}>(guidString: \"{b.assetId}\");";
        }

        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            var analyzerConfig = initContext.AnalyzerConfigOptionsProvider;

            var config = Incremental.GetConfig(analyzerConfig);

            var compilation = initContext.CompilationProvider;

            var blueprintTypes = Incremental.GetBlueprintTypes(compilation).Collect();

            var textFiles =
                initContext.AdditionalTextsProvider
                    .Where(static file => file.Path.ToLower().EndsWith(".jbp"));

            var bpInfos =
                textFiles
                .Combine(config)
                .SelectMany(static (fileAndConfig, cancellationToken) =>
                {
                    var (file, config) = fileAndConfig;

                    return Option.OfObj(file.GetText()?.ToString())
                        .Bind<string, (string, string, string, string)>(fileText =>
                        {
                            JObject jObject = JObject.Parse(fileText);
                            var assetId = jObject["AssetId"]?.Value<string>();
                            var typeName =
                                jObject["Data"]
                                    ?["$type"]?.Value<string>()
                                    ?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)?[1];

                            if (assetId is null || typeName is null)
                                return Option.None<(string, string, string, string)>();

                            var ns = GeneratorUtil.PathToNamespace(file.Path, config.ProjectPath.Value ?? "");

                            return Option.Some((ns, Path.GetFileNameWithoutExtension(file.Path), assetId, typeName));
                        })
                        .ToEnumerable();
                })
                .Combine(blueprintTypes)
                .SelectMany(static (bpAndCompilation, _) =>
                {
                    var ((ns, name, assetId, typeName), bpTypes) = bpAndCompilation;

                    var type = bpTypes.TryGetTypeSymbolByName(typeName);

                    return type.Map(t => (ns, name, assetId, t)).ToEnumerable();
                })
                .Collect()
                .SelectMany(static (files, cancellationToken) => files
                    .GroupBy(((string ns, string, string, INamedTypeSymbol) bp) => bp.ns))
                .Where(static bps => bps.Any());
            
            initContext.RegisterSourceOutput(bpInfos.Combine(compilation), static (spc, bpsc) =>
            {
                var (bps, c) = bpsc;

                if (!c.SourceModule.ReferencedAssemblySymbols.Any(a => a.Name == "Microsoftenator.Wotr.Common"))
                    return;

                var ns = bps.Key;
                
                var splitIndex = ns.LastIndexOf('.');

                var className = ns[(splitIndex + 1)..];
                ns = ns[..(splitIndex)];

                spc.AddSource($"{ns}.{className}", $@"
using Microsoftenator.Wotr.Common;

namespace {ns}
{{
    public static partial class {className}
    {{
{bps.Select(FormatLine).Aggregate((a, b) => $"{a}{Environment.NewLine}{b}")}
    }}
}}
");
            });
        }
    }
}
