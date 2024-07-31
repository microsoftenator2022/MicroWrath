using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Modding;

using MicroWrath.Loader;

namespace MicroWrath
{
    public static partial class MicroMod
    {
        //static readonly string ModAssemblyDirectory = 
        //    "..";
        //    //Path.Combine("..", "MicroWrathMod");

        [OwlcatModificationEnterPoint]
        public static void Init(OwlcatModification owlMod)
        {
            var logger = new OwlLogger(owlMod.Logger);

            if (!EnsureMicroWrath(GetModDirectories(logger), logger))
                throw new Exception("Failed to load MicroWrath");

            var assPath = Path.GetFullPath(Path.Combine(owlMod.Path, $"{owlMod.Manifest.UniqueName}.dll"));

            var (succ, mod) = LoadMod(assPath, logger);

            if (!succ || !mod!.Load(owlMod))
                throw new Exception("Failed to load mod");
        }
    }
}
