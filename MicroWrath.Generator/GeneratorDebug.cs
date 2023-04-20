using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MicroWrath.Util;
using MicroWrath.Generator.Common;

using Microsoft.CodeAnalysis;

namespace MicroWrath.Generator
{
#if DEBUG
    [Generator]
#endif
    public class GeneratorDebug : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var analyzerConfig = context.AnalyzerConfigOptionsProvider;
            var compilation = context.CompilationProvider;
            var additionalText = context.AdditionalTextsProvider;

            context.RegisterSourceOutput(analyzerConfig, (spc, config) =>
            {
                var configEntries = config.GlobalOptions.Keys.Select(k =>
                    (k, v: config.GlobalOptions.TryGetValue(k, out string? v) ? v.ToOption() : Option<string>.None));

                spc.AddSource("debug.analyzerConfig", $"{configEntries.Select(c => $"//{c.k}: {c.v}").Aggregate((a, b) => $"{a}{Environment.NewLine}{b}")}");
            });

            var blueprintTypes = Incremental.GetBlueprintTypes(compilation).Collect();

            context.RegisterSourceOutput(blueprintTypes, (spc, types) =>
            {
                spc.AddSource("debug.blueprintTypes", $"//{types.Select(t => t.FullName()).Aggregate((a, b) => $"{a}{Environment.NewLine}//{b}")}");
            });

            //context.RegisterSourceOutput(additionalText.Select((t, _) => (path: t.Path, text: t.GetText())).Collect(), (spc, files) =>
            //{
            //    IEnumerable<string> commentLines(string input)
            //    {
            //        var reader = new StringReader(input);

            //        while (reader.ReadLine() is string line)
            //        {
            //            yield return $"//{line}";
            //        }
            //    }

            //    var lines = files.SelectMany(file => (new[] { $"//File: {file.path}" }).Concat(commentLines(file.text?.ToString() ?? Environment.NewLine)));

            //    spc.AddSource("debug.files", lines.Aggregate((a, b) => $"{a}{Environment.NewLine}{b}"));
            //});
        }
    }
}
