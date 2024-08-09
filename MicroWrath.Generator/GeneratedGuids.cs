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
                .SelectMany(static (t, _) => t
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
                    if (sc.Node is not InvocationExpressionSyntax node)
                        return Enumerable.Empty<string>();

                    var arg0 = node.ArgumentList.Arguments.TryHead();
                    
                    var child0 = arg0.Bind(static arg => arg.ChildNodes().TryHead());

                    // :owlcat_suspecting:
                    var constValue = child0.Bind(literal =>
                    {
                        var constValue = sc.SemanticModel.GetConstantValue(literal);

                        if (!constValue.HasValue || constValue.Value is not string s)
                            return Option.None<string>();
                        
                        return Option.Some(s);
                    });

                    return constValue.Where(static s => !string.IsNullOrEmpty(s));

                    //return node.ArgumentList.Arguments.TryHead()
                    //    .Bind(static arg => arg.ChildNodes().TryHead())
                    //    .Bind(literal => 
                    //    {
                    //        var constValue = sc.SemanticModel.GetConstantValue(literal);
                    //        return constValue.HasValue ? (constValue.Value as string).ToOption() : Option.None<string>();
                    //    })
                    //    .Where(static s => !string.IsNullOrEmpty(s));
                });

            var guidsFile = context.AdditionalTextsProvider
                .Where(static at => at.Path.ToLower().EndsWith("guids.json"))
                .Collect()
                .SelectMany(static (ats, _) => ats.Reverse().TryHead())
                .Select(static (at, _) => (at.Path, at.GetText()?.ToString() ?? ""));

            #if DEBUG
            context.RegisterSourceOutput(generatedGuidsType.Combine(getGuidMethod.Combine(invocations.Collect().Combine(constantKeys.Collect()))), (spc, items) =>
            {
                var (guidType, guidMethod, invocations, keys) = items.Flatten();

                var sb = new StringBuilder();

                sb.AppendLine($"// Type: {guidType}");
                sb.AppendLine($"// Method: {guidMethod}");
                sb.AppendLine("// Invocations:");
                foreach (var invocation in invocations)
                {
                    sb.AppendLine($"//   {invocation.Node}");

                    if (invocation.Node is InvocationExpressionSyntax ies)
                    {
                        var a0 = ies.ArgumentList.Arguments.TryHead();

                        sb.AppendLine($"//    First argument: {a0}");

                        var c0 = a0.Bind(arg => arg.ChildNodes().TryHead());

                        sb.AppendLine($"//     First child: {a0}");

                        var s = c0.Map(literal => invocation.SemanticModel.GetConstantValue(literal));

                        sb.AppendLine($"//      Literal value: {(s.IsSome ? s.MaybeValue.Value as string : "")}");
                        
                        if (keys.Contains(s.MaybeValue.Value))
                            sb.AppendLine($"//       Matching key!");
                    }
                }

                spc.AddSource("generatedGuidsDebug", sb.ToString());
            });
            #endif

            context.RegisterSourceOutput(config.Combine(guidsFile.Collect()).Combine(constantKeys.Collect()), (spc, fileAndKeys) =>
            {
                var sb = new StringBuilder();

                var ((config, filePaths), keys) = fileAndKeys;

                var (filePath, fileText) = filePaths.FirstOrDefault();

                var fileGuids = fileText is not null ? JsonConvert.DeserializeObject<Dictionary<string, Guid>>(fileText) : null; 

                var guids = ImmutableDictionary<string, Guid>.Empty;
                
                if (fileGuids is not null) guids = guids.AddRange(fileGuids);

                foreach (var key in keys)
                    if (!guids.ContainsKey(key))
                        guids = guids.Add(key, GuidEx.CreateV5(Constants.GeneratedGuidFullName, key));

                sb.Append($@"using System;
using System.Collections.Generic;
using Kingmaker.Blueprints;

using {config.RootNamespace.MaybeValue};

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

                spc.AddSource("Guids", sb.ToString());
            });
        }
    }
}
