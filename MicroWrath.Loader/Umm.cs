using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Modding;

using MicroWrath.Loader;

using static UnityModManagerNet.UnityModManager;

namespace MicroWrath
{
    public static partial class MicroMod
    {
        public static bool Load(ModEntry modEntry)
        {
            var logger = new UmmLogger(modEntry);

            if (!EnsureMicroWrath(GetModDirectories(logger), logger))
                return false;

            var assPath = Path.Combine(modEntry.Path, $"{modEntry.Info.Id}.dll");

            var (succ, mod) = LoadMod(assPath, logger);

            if (succ)
                return mod!.Load(modEntry);

            return false;
        }
    }
}
