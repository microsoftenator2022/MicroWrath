using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Newtonsoft.Json;
using System.Reflection;

namespace MicroWrath.Generator
{
    public class GenerateGuidsFile : AppDomainIsolatedTask
    {
        [Required]
        public string WrathPath { get; set; }

        [Required]
        public string Assembly { get; set; }

        [Required]
        public string GuidsFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Generating guids file");

            Log.LogMessage($"WrathPath: {WrathPath}");
            Log.LogMessage($"AssemblyPath: {Assembly}");
            Log.LogMessage($"GuidsFile: {GuidsFile}");

            var guids = new Dictionary<string, Guid>();

            if (File.Exists(GuidsFile))
            {
                Log.LogMessage(MessageImportance.High, $"Loading guids from file {GuidsFile}");

                guids = JsonConvert.DeserializeObject<Dictionary<string, Guid>>(File.ReadAllText(GuidsFile)) ?? guids;
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
                    foreach (var f in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly), "*.dll", SearchOption.AllDirectories))
                    {
                        if (Path.GetFileNameWithoutExtension(f) == name.Name)
                        {
                            Log.LogMessage($"Loading {args.Name} from {f}");
                            return System.Reflection.Assembly.LoadFrom(f);
                        }
                    }

                    foreach (var f in Directory.EnumerateFiles(Path.Combine(WrathPath, "Wrath_Data", "Managed"), "*.dll", SearchOption.AllDirectories))
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
                        Log.LogMessage(MessageImportance.High, $"Existing guid with key {entry.Key}: {entry.Value}");

                        if (entry.Value != guids[entry.Key])
                        {
                            Log.LogWarning($"Replacing existing non-matching guid {guids[entry.Key]}");
                            guids[entry.Key] = entry.Value;
                        }

                        continue;
                    }

                    Log.LogMessage(MessageImportance.High, $"New guid with key {entry.Key}: {entry.Value}");

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

                foreach (var entry in JsonConvert.DeserializeObject<Dictionary<string, Guid>>(File.ReadAllText(runtimeGuidsFilePath)))
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

            var json = JsonConvert.SerializeObject(guids, Formatting.Indented);

            File.WriteAllText(GuidsFile, json);

            return true;
        }
    }
}
