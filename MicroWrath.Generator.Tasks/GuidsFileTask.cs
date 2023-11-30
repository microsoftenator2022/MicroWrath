using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

//using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;

using TinyJson;

namespace MicroWrath.Generator
{
    public class GenerateGuidsFile : AppDomainIsolatedTask
    {
        static string ToJson(Dictionary<string, Guid> guids) => guids
            .ToDictionary(p => p.Key, p => p.Value.ToString())
            .ToJson();

        static Dictionary<string, Guid> FromJson(string json) => json
            .FromJson<Dictionary<string, string>>()
            .ToDictionary(p => p.Key, p => Guid.Parse(p.Value));

        [Required]
        public string WrathPath { get; set; }

        [Required]
        public string Assembly { get; set; }

        [Required]
        public string GuidsFile { get; set; }

        public ITaskItem[] References { get; set; }
        public override bool Execute()
        {
            References ??= new ITaskItem[0];

            Log.LogMessage(MessageImportance.High, "Generating guids file");

            Log.LogMessage($"WrathPath: {WrathPath}");
            Log.LogMessage($"AssemblyPath: {Assembly}");
            Log.LogMessage($"GuidsFile: {GuidsFile}");

            var sb = new StringBuilder();
            sb.Append("References:");

            if (!References.Any())
                sb.Append(" []");

            sb.AppendLine();

            foreach (var ti in References)
            {
                sb.AppendLine($"  {ti.ItemSpec}");
            }

            Log.LogMessage(sb.ToString());

            var guids = new Dictionary<string, Guid>();

            if (File.Exists(GuidsFile))
            {
                Log.LogMessage(MessageImportance.High, $"Loading guids from file {GuidsFile}");

                guids = FromJson(File.ReadAllText(GuidsFile)) ?? guids;
            }

            if (!File.Exists(Assembly))
            {
                Log.LogError("Assembly file does not exist");
                return false;
            }

            var modDirectory = Path.Combine(WrathPath, "Mods", Path.GetFileNameWithoutExtension(Assembly));
            
            Assembly = Path.GetFullPath(Assembly);

            Log.LogMessage(MessageImportance.High, $"Loading assembly {Assembly}");
            
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name);

                var assemblyPath = $"{name.Name}.dll";

                if (!File.Exists(assemblyPath))
                {
                    foreach (var f in
                        References.Select(r => r.ItemSpec)
                        .Concat(Directory.EnumerateFiles(
                            Path.GetDirectoryName(Assembly),
                            "*.dll",
                            SearchOption.AllDirectories))
                        .Concat(Directory.EnumerateFiles(
                            Path.Combine(WrathPath, "Wrath_Data", "Managed"),
                            "*.dll",
                            SearchOption.AllDirectories)))
                    {
                        if (Path.GetFileNameWithoutExtension(f) == name.Name)
                        {
                            Log.LogMessage($"Loading {args.Name} from {f}");
                            return System.Reflection.Assembly.LoadFrom(f);
                        }
                    }

                    throw new FileNotFoundException();
                }

                return null;
            };

            var ass = System.Reflection.Assembly.LoadFrom(Assembly);

            Type guidsType = null;

            try
            {
                Log.LogMessage("Getting guids type");

                guidsType = ass.GetTypes().First(t => t.FullName == "MicroWrath.GeneratedGuid");

                Log.LogMessage("Getting guids field");
                var guidsField = guidsType.GetField("guids", BindingFlags.NonPublic | BindingFlags.Static);
            
                foreach (var entry in (Dictionary<string, Guid>)guidsField.GetValue(null))
                {
                    if (guids.ContainsKey(entry.Key)) 
                    {
                        Log.LogMessage(MessageImportance.High, $"{entry.Value}: {entry.Key}");

                        if (entry.Value != guids[entry.Key])
                        {
                            Log.LogWarning($"Replacing existing non-matching guid {guids[entry.Key]}");
                            guids[entry.Key] = entry.Value;
                        }

                        continue;
                    }

                    Log.LogMessage(MessageImportance.High, $"New guid {entry.Value}: {entry.Key}");

                    guids.Add(entry.Key, entry.Value);
                }
            }
            catch (ReflectionTypeLoadException rtle)
            {
                foreach (var e in rtle.LoaderExceptions)
                    Log.LogErrorFromException(e);

                throw;
            }

            var runtimeGuidsFilePath = Path.Combine(modDirectory, "runtimeGuids.json");
            if (File.Exists(runtimeGuidsFilePath))
            {
                Log.LogMessage(MessageImportance.High, $"Found runtimeGuids.json in {modDirectory}");

                foreach (var entry in FromJson(File.ReadAllText(runtimeGuidsFilePath)))
                {
                    Log.LogMessage(MessageImportance.High, $"{entry.Key}: {entry.Value}");

                    if (guids.ContainsKey(entry.Key))
                    {
                        if (entry.Value != guids[entry.Key])
                            Log.LogWarning("Runtime guid for key {entry.Key} does not match existing entry {guids[entry]}. Ignored");

                        continue;
                    }

                    guids.Add(entry.Key, entry.Value);
                }
            }

            Log.LogMessage(MessageImportance.High, $"{guids.Count} total entries");

            if (guids is null || guids.Count == 0) return true;

            Log.LogMessage(MessageImportance.High, $"Writing guids to file {GuidsFile}");

            var json = ToJson(guids);

            File.WriteAllText(GuidsFile, json);

            return true;
        }
    }
}
