using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MicroWrath.Generator
{
    public class InfoJson : Task
    {
        static readonly JsonSerializerOptions SerializerOptions =
            new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

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
            if (!Path.GetFileName(this.OutputPath).Equals("info.json", StringComparison.InvariantCultureIgnoreCase))
                if (Directory.Exists(this.OutputPath))
                {
                    this.OutputPath = Path.Combine(Path.GetFullPath(this.OutputPath), "Info.json");
                }
                else
                {
                    base.Log.LogError($"OutputPath {this.OutputPath} is not 'Info.json' path or directory");
                    return false;
                }

            var requirements = this.Requirements?.Select(ti => ti.ItemSpec)?.ToArray();
            var loadAfter = this.LoadAfter?.Select(ti => ti.ItemSpec)?.ToArray();

            File.WriteAllText(OutputPath,
                JsonSerializer.Serialize(new {
                    this.Id,
                    this.Version,
                    this.AssemblyName,
                    this.EntryMethod,
                    DisplayName = this.DisplayName ?? this.Id,
                    this.Author,
                    this.GameVersion,
                    this.ManagerVersion,
                    this.HomePage,
                    this.Repository,
                    Requirements = requirements,
                    LoadAfter = loadAfter
                }, SerializerOptions));

            return true;
        }
    }
}
