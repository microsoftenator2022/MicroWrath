using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MicroWrath.Generator.Tasks;
public class OwlcatMod : Task
{
    [Required]
    public string OwlcatTemplateModPath { get; set; }

    /// <summary>
    /// Owlcat Template Mod directory (eg. &lt;templatePath&gt;\Build\&lt;modName&gt;)
    /// </summary>
    [Required]
    public string BuildOutputPath { get; set; }

    [Required]
    public string WrathData { get; set; }

    public bool RemoveExistingModZip { get; set; } = false;

    const string ManifestJsonFilename = "OwlcatModificationManifest.json";
    const string SettingsJsonFilename = "OwlcatModificationSettings.json";
    const string MicroLoaderFilename = "MicroWrath.Loader.dll";

    public override bool Execute()
    {
        if (!(
            Directory.Exists(this.OwlcatTemplateModPath) &&
            File.Exists(Path.Combine(this.OwlcatTemplateModPath, ManifestJsonFilename)) &&
            File.Exists(Path.Combine(this.OwlcatTemplateModPath, SettingsJsonFilename))))
        {
            base.Log.LogError($"\"{this.OwlcatTemplateModPath}\" does not appear to be an Owlcat Mod directory");
            return false;
        }

        var manifestJson = JsonObject.Parse(File.ReadAllText(Path.Combine(this.OwlcatTemplateModPath, ManifestJsonFilename)));
        var uniqueName = manifestJson["UniqueName"].ToString();

        var modAssemblyPath = Path.GetFullPath(Path.Combine(this.BuildOutputPath, $"{uniqueName}.dll"));

        if (!File.Exists(modAssemblyPath))
        {
            base.Log.LogWarning($"\"{modAssemblyPath}\" was not found. This mod will likely fail to load.");
        }

        var zipPath = Path.Combine(Path.GetDirectoryName(this.OwlcatTemplateModPath), $"{uniqueName}.zip");

        if (this.RemoveExistingModZip && File.Exists(zipPath))
        {
            base.Log.LogMessage(MessageImportance.High, "Removing existing mod zip");
            File.Delete(zipPath);
        }

        var assemblies = Directory.EnumerateFiles(Path.Combine(this.OwlcatTemplateModPath, "Assemblies"));
        var bundles = Directory.EnumerateFiles(Path.Combine(this.OwlcatTemplateModPath, "Bundles"));
        var blueprints = Directory.EnumerateFiles(Path.Combine(this.OwlcatTemplateModPath, "Blueprints"));

        var targetDir = Path.Combine(this.WrathData, "Modifications", uniqueName);
        var assembliesTargetDir = Path.Combine(targetDir, "Assemblies");
        var bundlesTargetDir = Path.Combine(targetDir, "Bundles");
        var blueprintsTargetDir = Path.Combine(targetDir, "Blueprints");

        Directory.CreateDirectory(assembliesTargetDir);
        Directory.CreateDirectory(bundlesTargetDir);
        Directory.CreateDirectory(blueprintsTargetDir);

        //foreach (var ass in Directory.EnumerateFiles(this.BuildOutputPath))
        //{
        //    var filename = Path.GetFileName(ass);

        //    var copyTarget = Path.GetFileName(ass) == MicroLoaderFilename ?
        //        Path.Combine(assembliesTargetDir, filename) :
        //        Path.Combine(targetDir, filename);

        //    base.Log.LogMessage($"Copying file: {filename} -> {copyTarget}");

        //    File.Copy(ass, copyTarget, true);
        //}

        return true;
    }
}
