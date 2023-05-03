using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Newtonsoft.Json;

using MicroWrath.Util;

using MicroWrath.Generator.Common;
using System.IO;
using System.Collections.Immutable;

namespace MicroWrath.Generator
{
    [Generator]
    internal class GeneratedGuids : IIncrementalGenerator
    {
        //private static ImmutableDictionary<string, Guid> guids = ImmutableDictionary.Create<string, Guid>();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var config = Incremental.GetConfig(context.AnalyzerConfigOptionsProvider);

            var compilation = context.CompilationProvider;

            var generatedGuidsType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName(Constants.GeneratedGuidFullName).ToOption());

            var getGuidMethod = generatedGuidsType
                .SelectMany(static (t, _) => t.ToEnumerable()
                    .SelectMany(static t => t.GetMembers("Get"))
                    .OfType<IMethodSymbol>())
                .Where(static m =>
                    m.IsStatic &&
                    m.Parameters.Length == 1)
                .Collect()
                .Select(static (m, _) => m.FirstOrDefault());

            var invocations = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is InvocationExpressionSyntax,
                static (sc, _) => sc)
                .Combine(getGuidMethod)
                .Where(static scm =>
                {
                    var (sc, m) = scm;
                    if (m is null) return false;

                    return m.Equals(sc.SemanticModel.GetSymbolInfo(sc.Node).Symbol, SymbolEqualityComparer.Default);
                })
                .Select(static (mi, _) => mi.Left);

            var constantKeys = invocations
                .SelectMany(static (sc, _) =>
                {
                    var node = (sc.Node as InvocationExpressionSyntax).ToOption();

                    return node
                        .Bind(static n => n.ArgumentList.Arguments.TryHead())
                        .Bind(static arg => arg.ChildNodes().TryHead())
                        .Bind(literal => sc.SemanticModel.GetConstantValue(literal).ToOption())
                        .ToEnumerable()
                        .OfType<string>()
                        .Where(static s => !string.IsNullOrEmpty(s));
                });

            var guidsFile = context.AdditionalTextsProvider
                .Where(static at => at.Path.ToLower().EndsWith("guids.json"))
                .Collect()
                .SelectMany(static (ats, _) => ats.Reverse().TryHead().ToEnumerable())
                .Select(static (at, _) => (at.Path, at.GetText()?.ToString() ?? ""));

            context.RegisterSourceOutput(config.Combine(guidsFile.Collect()).Combine(constantKeys.Collect()), (spc, fileAndKeys) =>
            {
                var ((config, filePaths), keys) = fileAndKeys;

                var (filePath, fileText) = filePaths.FirstOrDefault();

                var fileGuids = fileText is not null ? JsonConvert.DeserializeObject<Dictionary<string, Guid>>(fileText) : null; 

                var guids = ImmutableDictionary<string, Guid>.Empty;
                
                if (fileGuids is not null) guids = guids.AddRange(fileGuids);

                foreach (var key in keys)
                    if (!guids.ContainsKey(key))
                        guids = guids.Add(key, Guid.NewGuid());

                var sb = new StringBuilder();
                
                sb.Append($@"using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;

using {config.RootNamespace.Value};

namespace MicroWrath
{{
    internal partial class {Constants.GeneratedGuidClassName}
    {{
        static {Constants.GeneratedGuidClassName}()
        {{");
                foreach (var entry in guids)
                {
                    sb.Append($@"
            guids[""{entry.Key}""] = System.Guid.Parse(""{entry.Value}"");");
                }
                
                sb.Append($@"
        }}");

                foreach (var entry in guids)
                {
                    sb.Append($@"
        public static GeneratedGuid {Analyzers.EscapeIdentifierString(entry.Key)} => new(""{entry.Key}"", BlueprintGuid.Parse(guids[""{entry.Key}""].ToString()));");
                }

                sb.Append($@"
    }}
}}
");

                spc.AddSource("gg", sb.ToString());
            });
        }
    }
}
