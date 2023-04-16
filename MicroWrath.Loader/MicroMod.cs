using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityModManagerNet;

using static UnityModManagerNet.UnityModManager;

namespace MicroWrath
{
    public interface IMicroMod
    {
        bool Load(ModEntry modEntry);
    }

    internal static class MicroMod
    {
        internal static Assembly? MicroWrathAssembly;

        private static bool EnsureMicroWrath(string modsDirectory, ModEntry modEntry)
        {
            if (MicroWrathAssembly != null)
            {
                modEntry.Logger.Log("MicroWrath already loaded");
                return true;
            }

            var candidates = Directory.EnumerateFiles(modsDirectory, "*.dll", SearchOption.AllDirectories)
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
                modEntry.Logger.Warning($"Multiple MicroWrath versions found");
#if DEBUG
            modEntry.Logger.Log("MicroWrath assemblies:");

            foreach (var (v, f) in candidates)
            {
                modEntry.Logger.Log($"Version {v} : {f}");
            }
#endif
            var microWrath = candidates.Select(f => f.f).FirstOrDefault();

            if (microWrath is string filePath)
            {
                try
                {
                    modEntry.Logger.Log($"Loading MicroWrath from {filePath}");

#if DEBUG
                    modEntry.Logger.Log($"File Info:{Environment.NewLine}{FileVersionInfo.GetVersionInfo(filePath)}");
#endif

                    var vi = FileVersionInfo.GetVersionInfo(filePath);

                    modEntry.Logger.Log($"Version: {vi.ProductVersion}");

                    MicroWrathAssembly = Assembly.LoadFrom(filePath);
                    return true;
                }
                catch (Exception ex)
                {
                    modEntry.Logger.Critical($"Exception occured while loading {filePath}");
                    modEntry.Logger.LogException(ex);
                }
            }

            modEntry.Logger.Critical("Loading MicroWrath.dll failed");
            return false;
        }

        private static bool LoadMod(ModEntry modEntry)
        {
            var assPath = Path.Combine(modEntry.Path, $"{modEntry.Info.Id}.dll");

            if (!File.Exists(assPath))
            {
                modEntry.Logger.Critical($"{assPath} does not exist");
                return false;
            }

            try
            {
                modEntry.Logger.Log($"Loading mod from {assPath}");

#if DEBUG
                modEntry.Logger.Log($"Mod Info:{Environment.NewLine}{FileVersionInfo.GetVersionInfo(assPath)}");
#endif

                var ModAssembly = Assembly.LoadFrom(assPath);
            
                Type? modType = ModAssembly?.DefinedTypes
                    .Where(t => typeof(IMicroMod).IsAssignableFrom(t))
                    .Select(t => t.UnderlyingSystemType)
                    .FirstOrDefault();

                if (modType is null)
                {
                    modEntry.Logger.Critical($"No type implementing {nameof(IMicroMod)} found in {assPath}");
                    return false;
                }

                if (modType.GetConstructors().FirstOrDefault() is not ConstructorInfo constructor)
                {
                    modEntry.Logger.Critical($"Could not get constructor for {modType.FullName}");
                    return false;
                }

                if (constructor.Invoke(new object[0]) is not IMicroMod modMain)
                {
                    modEntry.Logger.Critical($"Failed to initialize object of type {modType.FullName} from {assPath}");
                    return false;
                }
                
                modEntry.Logger.Log($"Mod entry point: {modMain.GetType().FullName}.Load");

                return modMain.Load(modEntry);
            }
            catch (Exception ex)
            {
                modEntry.Logger.Critical($"Exception occured while loading {assPath}");
                modEntry.Logger.LogException(ex);
            }

            return false;
        }

        public static bool Load(ModEntry modEntry)
        {
            modEntry.Logger.Log($"MicroMod loader v{Assembly.GetExecutingAssembly().GetName().Version}");

            var assemblyPath = Assembly.GetExecutingAssembly().Location;

            var directory = Path.GetDirectoryName(assemblyPath);

            var modsDirectory = Path.GetDirectoryName(directory);

            return EnsureMicroWrath(modsDirectory, modEntry) && LoadMod(modEntry);
        }
    }
}
