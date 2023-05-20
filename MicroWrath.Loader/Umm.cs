﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MicroWrath.Loader;

using static UnityModManagerNet.UnityModManager;

namespace MicroWrath
{
    public interface IMicroMod
    {
        bool Load(ModEntry modEntry);
    }

    internal static partial class MicroMod
    {
        private static bool LoadUmmMod(ModEntry modEntry, INanoLogger logger)
        {
            var assPath = Path.Combine(modEntry.Path, $"{modEntry.Info.Id}.dll");

            if (!File.Exists(assPath))
            {
                logger.Error($"{assPath} does not exist");
                return false;
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
                    return false;
                }

                if (Activator.CreateInstance(modType, true) is not IMicroMod modMain)
                {
                    logger.Error($"Failed to initialize object of type {modType.FullName} from {assPath}");
                    return false;
                }
#if DEBUG
                logger.Log($"Mod entry point: {modMain.GetType().FullName}.Load");
#endif
                return modMain.Load(modEntry);
            }
            catch (Exception ex)
            {
                logger.Error($"Exception occured while loading {assPath}");
                logger.Exception(ex);
            }

            return false;
        }

        public static bool Load(ModEntry modEntry)
        {
            //var assemblyPath = Assembly.GetExecutingAssembly().Location;

            //var directory = Path.GetDirectoryName(assemblyPath);

            //var modsDirectory = Path.GetDirectoryName(directory);

            var logger = new UmmLogger(modEntry);

            return EnsureMicroWrath(GetModDirectories(modEntry), logger) && LoadUmmMod(modEntry, logger);
        }
    }
}
