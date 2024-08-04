#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
    public string? OwlcatTemplateModPath { get; set; }

    /// <summary>
    /// Owlcat Template Mod directory (eg. &lt;templatePath&gt;\Build\&lt;modName&gt;)
    /// </summary>
    [Required]
    public string BinPath { get; set; } = null!;

    public string? BuildOutput { get; set; }

    public string? ModZipFile { get; set; }

    public bool ReplaceOwlcatModZip { get; set; } = false;

    public string? DeployPath { get; set; }

    const string ManifestJsonFilename = "OwlcatModificationManifest.json";
    const string SettingsJsonFilename = "OwlcatModificationSettings.json";
    const string MicroLoaderFilename = "MicroWrath.Loader.dll";

    private void CopyFiles()
    {
        var assembliesDir = Path.Combine(this.OwlcatTemplateModPath, "Assemblies");

        File.Copy(Path.Combine(this.BinPath, MicroLoaderFilename), Path.Combine(assembliesDir, MicroLoaderFilename), true);

        foreach (var f in Directory.EnumerateFiles(this.BinPath, "*", SearchOption.AllDirectories).Where(f => Path.GetFileName(f) != MicroLoaderFilename))
        {
            var relativePath = f.Replace(this.BinPath, "");

            File.Copy(f, Path.Combine(this.OwlcatTemplateModPath, relativePath), true);
        }
    }

    private void ZipMod(string zipPath)
    {
        base.Log.LogMessage(MessageImportance.High, $"Create zip: {this.OwlcatTemplateModPath} -> {zipPath}");

        ZipFile.CreateFromDirectory(this.OwlcatTemplateModPath, zipPath);

        if (this.ReplaceOwlcatModZip)
        {
            var owlcatZipPath = Path.Combine(new DirectoryInfo(this.OwlcatTemplateModPath).Parent.FullName, Path.GetFileName(zipPath));

            if (File.Exists(owlcatZipPath))
            {
                base.Log.LogMessage(MessageImportance.High, "Removing existing mod zip");
                File.Delete(owlcatZipPath);
            }

            base.Log.LogMessage(MessageImportance.High, $"Copy file: {zipPath} -> {owlcatZipPath}");
            File.Copy(zipPath, owlcatZipPath);
        }
    }

    private string GetBuildOutput(string uniqueName) => Path.Combine(Path.GetFullPath(this.BuildOutput ?? new DirectoryInfo(this.BinPath).Parent.FullName), uniqueName);

    private void ExtractToOutput(string uniqueName, string zipPath)
    {
        var buildOutput = this.GetBuildOutput(uniqueName);

        base.Log.LogMessage(MessageImportance.High, $"Build output: {buildOutput}");

        if (!Directory.Exists(buildOutput))
            _ = Directory.CreateDirectory(buildOutput);

        base.Log.LogMessage(MessageImportance.High, $"Extract zip: {zipPath} -> {buildOutput}");
        ZipFile.ExtractToDirectory(zipPath, buildOutput);

        if (this.ModZipFile is not null)
        {
            File.Copy(zipPath, this.ModZipFile, true);
        }

        File.Delete(zipPath);
    }

    private void Deploy(string uniqueName, string zipPath, string targetPath)
    {
        targetPath = new DirectoryInfo(targetPath).Name == uniqueName ? targetPath : Path.Combine(targetPath, uniqueName);

        if (!Directory.Exists(targetPath))
            _ = Directory.CreateDirectory(targetPath);

        base.Log.LogMessage(MessageImportance.High, $"Extract zip: {zipPath} -> {targetPath}");
        ZipFile.ExtractToDirectory(zipPath, targetPath);
    }

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

        if (manifestJson is null)
            return false;

        var uniqueName = manifestJson["UniqueName"]?.ToString();

        if (uniqueName is null)
            return false;

        var modAssemblyPath = Path.GetFullPath(Path.Combine(this.BinPath, $"{uniqueName}.dll"));

        if (!File.Exists(modAssemblyPath))
        {
            base.Log.LogWarning($"\"{modAssemblyPath}\" was not found. This mod will likely fail to load.");
        }

        base.Log.LogMessage(MessageImportance.High, $"{nameof(this.BinPath)}: {this.BinPath}");
        base.Log.LogMessage(MessageImportance.High, $"{nameof(this.BinPath)}: {this.BinPath}");

        var zipPath = Path.Combine(this.BinPath, $"{uniqueName}.zip");

        if (File.Exists(zipPath))
            File.Delete(zipPath);
        
        base.Log.LogMessage(MessageImportance.High, nameof(this.CopyFiles));
        this.CopyFiles();

        base.Log.LogMessage(MessageImportance.High, nameof(this.ZipMod));
        this.ZipMod(zipPath);

        base.Log.LogMessage(MessageImportance.High, nameof(this.ExtractToOutput));
        this.ExtractToOutput(uniqueName, zipPath);

        //if (this.DeployPath is not null)
        //    this.Deploy(uniqueName, zipPath, this.DeployPath);

        return true;
    }
}
