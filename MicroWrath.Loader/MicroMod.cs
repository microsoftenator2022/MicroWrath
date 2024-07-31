using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Kingmaker;
using Kingmaker.Modding;

using MicroWrath.Loader;

using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MicroWrath
{
    public interface IMicroMod
    {
        bool Load(ModEntry modEntry);
        bool Load(OwlcatModification owlMod);
    }

    public static partial class MicroMod
    {
        internal static Assembly? MicroWrathAssembly;

        internal static string OwlcatModsDirectory =>
            (typeof(OwlcatModificationsManager)
                    .GetProperty("DefaultModificationsDirectory", BindingFlags.NonPublic | BindingFlags.Static)
                    .GetValue(null) as string)!;
            //return OwlcatModificationsManager.DefaultModificationsDirectory;

        private static IEnumerable<string> GetModDirectories(INanoLogger logger)
        {
            if (Directory.Exists(OwlcatModsDirectory)) yield return OwlcatModsDirectory;

            //if (modEntry is null) 
            //{
            //    yield break;
            //}

            //var modsDir = Path.GetDirectoryName(Path.GetDirectoryName(modEntry.Path));
            var modsDir = GameInfo.Load().ModsDirectory;

            logger.Log($"UMM mods directory: {modsDir}");

            yield return modsDir;
        }

        private static Version? GetFileVersion(string path)
        {
            if (Version.TryParse(FileVersionInfo.GetVersionInfo(path).FileVersion, out var version))
                return version;

            return null;
        }

        private static bool UpdateMicroWrathLoader(IEnumerable<string> modsDirectories, INanoLogger logger)
        {
            var loaderUpdated = false;

            var executingAssemblyPath = Assembly.GetExecutingAssembly().Location;
            var executingAssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

            //if (executingAssemblyVersion is null)
            //    return false;

            foreach (var file in modsDirectories
                .SelectMany(dir => Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories))
                .Distinct())
            {
                if (Path.GetFileName(file) != "MicroWrath.Loader.dll")
                    continue;

                var otherFileVersion = GetFileVersion(file);

                if (otherFileVersion is null)
                {
#if DEBUG
                    logger.Log($"{file} version is null");
#endif
                    continue;
                }

                if (executingAssemblyVersion > otherFileVersion)
                {
                    logger.Warn($"Updating loader at {file}");
                    File.Copy(executingAssemblyPath, file, true);
                    
                    loaderUpdated = false;
                }
                else if (otherFileVersion > executingAssemblyVersion)
                {
                    logger.Warn($"Updating loader at {executingAssemblyPath}");
                    File.Copy(file, executingAssemblyPath);

                    loaderUpdated = true;
                }
            }

            return loaderUpdated;
        }

        private static bool EnsureMicroWrath(IEnumerable<string> modsDirectories, INanoLogger logger)
        {
            if (MicroWrathAssembly != null)
            {
                logger.Log("MicroWrath already loaded");
                return true;
            }

            logger.Log($"MicroMod loader v{Assembly.GetExecutingAssembly().GetName().Version}");

            if (UpdateMicroWrathLoader(modsDirectories, logger))
            {
                logger.Error("Newer loader version was found. Restart for the change to take effect.");
                return false;
            }

            var monoRuntimeVersion = Type.GetType("Mono.Runtime")
                ?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static)
                ?.Invoke(null, null)
                ?.ToString();

            logger.Log($"Mono runtime version: {monoRuntimeVersion ?? "<null>"}");

            var candidates = modsDirectories
                .SelectMany(d => Directory.EnumerateFiles(d, "*.dll", SearchOption.AllDirectories))
                .Where(f => Path.GetFileName(f) == "MicroWrath.dll")
                .Select(f =>
                {
                    //if (Version.TryParse(FileVersionInfo.GetVersionInfo(f).FileVersion, out var version))
                    //    return (version, f);

                    return (version: GetFileVersion(f) ?? new Version(0, 0, 0, 0), f);
                })
                .OrderByDescending(f => f.version)
                .ToArray();

            if (candidates.Length > 1 && candidates.Select(f => f.version).Distinct().Count() > 1)
                logger.Warn($"Multiple MicroWrath versions found");

#if DEBUG
            var sb = new StringBuilder();

            sb.AppendLine("MicroWrath assemblies:");

            foreach (var (v, f) in candidates)
            {
                sb.AppendLine($"  Version {v} : {f}");
            }

            logger.Log(sb.ToString());
#endif
            var microWrath = candidates.Select(f => f.f).FirstOrDefault();

            if (microWrath is string filePath)
            {
                try
                {
                    logger.Log($"Loading MicroWrath from {filePath}");

#if DEBUG
                    logger.Log($"MicroWrath File Info:{Environment.NewLine}{FileVersionInfo.GetVersionInfo(filePath)}");
#endif

                    var vi = FileVersionInfo.GetVersionInfo(filePath);

                    logger.Log($"MicroWrath Version: {vi.ProductVersion}");

                    MicroWrathAssembly = Assembly.LoadFrom(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    logger.Error($"Exception occured while loading {filePath}");
                    logger.Exception(ex);
                }
            }

            logger.Error("Loading MicroWrath.dll failed");
            return false;
        }

        private static (bool, IMicroMod?) LoadMod(string assPath, INanoLogger logger)
        {
            if (!File.Exists(assPath))
            {
                logger.Error($"{assPath} does not exist");
                return (false, null);
            }

            try
            {
                logger.Log($"Loading mod from {assPath}");

#if DEBUG
                logger.Log($"Mod File Info:{Environment.NewLine}{FileVersionInfo.GetVersionInfo(assPath)}");
#endif

                var ModAssembly = Assembly.LoadFrom(assPath);

                Type? modType = ModAssembly?.DefinedTypes
                    .Where(t => typeof(IMicroMod).IsAssignableFrom(t) && !t.IsAbstract)
                    .Select(t => t.UnderlyingSystemType)
                    .FirstOrDefault();

                if (modType is null)
                {
                    logger.Error($"No type implementing {nameof(IMicroMod)} found in {assPath}");
                    return (false, null);
                }

                if (Activator.CreateInstance(modType, true) is not IMicroMod modMain)
                {
                    logger.Error($"Failed to initialize object of type {modType.FullName} from {assPath}");
                    return (false, null);
                }
#if DEBUG
                logger.Log($"Mod entry point: {modMain.GetType().FullName}.Load");
#endif
                return (true, modMain);
            }
            catch (Exception ex)
            {
                logger.Error($"Exception occured while loading {assPath}");
                logger.Exception(ex);
            }

            return (false, null);
        }
    }
}
