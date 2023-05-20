using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Kingmaker.Modding;

using UnityModManagerNet;

using MicroWrath.Loader;

namespace MicroWrath
{
    internal static partial class MicroMod
    {
        internal static Assembly? MicroWrathAssembly;

        internal static string OwlcatModsDirectory => OwlcatModificationsManager.DefaultModificationsDirectory;

        private static IEnumerable<string> GetModDirectories(INanoLogger logger, UnityModManager.ModEntry? modEntry = null)
        {
            yield return OwlcatModsDirectory;

            if (modEntry is null) yield break;

            var modsDir = Path.GetDirectoryName(Path.GetDirectoryName(modEntry.Path));

            logger.Log($"UMM mods directory: {modsDir}");

            yield return modsDir;
        }

        private static bool EnsureMicroWrath(IEnumerable<string> modsDirectories, INanoLogger logger)
        {
            if (MicroWrathAssembly != null)
            {
                logger.Log("MicroWrath already loaded");
                return true;
            }

            logger.Log($"MicroMod loader v{Assembly.GetExecutingAssembly().GetName().Version}");

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
                    if (Version.TryParse(FileVersionInfo.GetVersionInfo(f).FileVersion, out var version))
                        return (version, f);

                    return (version: new Version(0,0,0,0), f);
                })
                .OrderByDescending(f => f.version)
                .ToArray();

            if (candidates.Length > 1 && candidates.Select(f => f.version).Distinct().Count() > 1)
                logger.Warn($"Multiple MicroWrath versions found");
#if DEBUG
            logger.Log("MicroWrath assemblies:");

            foreach (var (v, f) in candidates)
            {
                logger.Log($"Version {v} : {f}");
            }
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
    }
}
