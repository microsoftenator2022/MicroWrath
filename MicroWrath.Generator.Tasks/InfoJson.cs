using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MicroWrath.Generator
{
    public class InfoJson : Task
    {
        static readonly JsonSerializerOptions SerializerOptions =
            new() { WriteIndented = true };

        [Required]
        public string Id { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public string AssemblyName { get; set; } = "MicroWrath.Loader.dll";
        public string EntryMethod { get; set; } = "MicroWrath.MicroMod.Load";
        public string DisplayName { get; set; }
        public string Author { get; set; }
        public ITaskItem[] Requirements { get; set; }
        public ITaskItem[] LoadAfter { get; set; }
        public string GameVersion { get; set; }
        public string ManagerVersion { get; set; }
        public string HomePage { get; set; }
        public string Repository { get; set; }

        public override bool Execute()
        {
            if (!OutputPath.ToLower().EndsWith("info.json") && Directory.Exists(OutputPath))
            {
                OutputPath = Path.Combine(Path.GetFullPath(OutputPath), "Info.json");
            }
            else
            {
                Log.LogError("OutputPath is not 'Info.json' path or directory");
                return false;
            }

            var requirements = Requirements?.Select(ti => ti.ItemSpec)?.ToArray();
            var loadAfter = LoadAfter?.Select(ti => ti.ItemSpec)?.ToArray();

            File.WriteAllText(OutputPath,
                JsonSerializer.Serialize(new {
                    Id,
                    Version,
                    AssemblyName,
                    EntryMethod,
                    DisplayName,
                    Author,
                    GameVersion,
                    ManagerVersion,
                    HomePage,
                    Repository,
                    Requirements = requirements,
                    LoadAfter = loadAfter
                }, SerializerOptions));

            return true;
        }
    }
}
