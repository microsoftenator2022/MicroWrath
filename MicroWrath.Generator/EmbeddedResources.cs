﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;

using MicroWrath.Generator.Common;
using MicroWrath.Util;

namespace MicroWrath.Generator
{
    [Generator]
    internal class EmbeddedResources : IIncrementalGenerator
    {
        private static IEnumerable<string> GetGeneratorResourceNames(Assembly assembly) =>
            assembly.GetManifestResourceNames().Where(n => n.EndsWith(".cs"));

        private static string ProcessSource(StreamReader reader)
        {
            var sb = new StringBuilder();

            sb.AppendLine("#nullable enable");
            //sb.AppendLine("#pragma warning disable CS0649");
            sb.Append(reader.ReadToEnd());

            return sb.ToString();
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static pic => 
            {
                foreach (var name in GetGeneratorResourceNames(Assembly.GetExecutingAssembly())
                    .Where(name => name.StartsWith($"{nameof(MicroWrath)}.{nameof(Generator)}.Resources.")))
                {
                    using var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name));
                    var str = ProcessSource(sr);

                    var sourceName = name
                        .Replace($"{nameof(Generator)}.", "")
                        .Replace("Resources.", "")
                        .Replace(".cs", ".generated.cs");

                    pic.AddSource(sourceName, str);
                }
            });

            var config = context.AnalyzerConfigOptionsProvider.Select(static (config, _) => Incremental.GetConfig(config));
            var rootNamespace = config.Select(static (config, _) => config.RootNamespace);

            context.RegisterSourceOutput(rootNamespace, (spc, ns) =>
            {
                if (ns.IsNone) return;

                foreach (var name in GetGeneratorResourceNames(Assembly.GetExecutingAssembly())
                    .Where(name => name.StartsWith($"{nameof(MicroWrath)}.{nameof(Generator)}.ModResources.")))
                {
                    using var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name));

                    var str = ProcessSource(sr).Replace($"namespace {nameof(MicroWrath)}", $"namespace {ns.Value!}");

                    var sourceName = name
                        .Replace($"{nameof(MicroWrath)}.{nameof(MicroWrath.Generator)}.", "")
                        .Replace("ModResources.", "")
                        .Replace(".cs", ".generated.cs");

                    spc.AddSource(sourceName, str);
                }
            });
        }
    }
}
