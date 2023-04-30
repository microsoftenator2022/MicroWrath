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
        public string WrathAssembliesPath { get; set; }

        [Required]
        public string AssemblyPath { get; set; }

        [Required]
        public string GuidsFile { get; set; }

        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Generating guids file");

            Log.LogMessage($"WrathAssembliesPath: {WrathAssembliesPath}");
            Log.LogMessage($"AssemblyPath: {AssemblyPath}");
            Log.LogMessage($"GuidsFile: {GuidsFile}");

            var guids = new Dictionary<string, Guid>();

            if (File.Exists(GuidsFile))
            {
                Log.LogMessage(MessageImportance.High, $"Loading guids from file {GuidsFile}");

                using var file = File.OpenText(GuidsFile);

                guids = JsonConvert.DeserializeObject<Dictionary<string, Guid>>(file.ReadToEnd());
            }

            if (!File.Exists(AssemblyPath))
            {
                Log.LogError("Assembly file does not exist");
                return false;
            }

            AssemblyPath = Path.GetFullPath(AssemblyPath);

            Log.LogMessage(MessageImportance.High, $"Loading assembly {AssemblyPath}");
            
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name);

                var assemblyPath = $"{name.Name}.dll";

                if (!File.Exists(assemblyPath))
                {
                    foreach (var f in Directory.EnumerateFiles(Path.GetDirectoryName(AssemblyPath), "*.dll", SearchOption.AllDirectories))
                    {
                        if (Path.GetFileNameWithoutExtension(f) == name.Name)
                        {
                            Log.LogMessage($"Loading {args.Name} from {f}");
                            return Assembly.LoadFrom(f);
                        }
                    }

                    foreach (var f in Directory.EnumerateFiles(WrathAssembliesPath, "*.dll", SearchOption.AllDirectories))
                    {
                        if (Path.GetFileNameWithoutExtension(f) == name.Name)
                        {
                            Log.LogMessage($"Loading {args.Name} from {f}");
                            return Assembly.LoadFrom(f);
                        }
                    }

                    throw new FileNotFoundException();
                }

                return null;
            };

            var ass = Assembly.LoadFrom(AssemblyPath);

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

            Log.LogMessage(MessageImportance.High, $"{guids.Count} total entries");

            if (guids is null || guids.Count == 0) return true;

            Log.LogMessage(MessageImportance.High, $"Writing guids to file {GuidsFile}");

            var json = JsonConvert.SerializeObject(guids);

            using var writer = new StreamWriter(File.OpenWrite(GuidsFile));

            writer.Write(json);

            return true;
        }
    }
}
