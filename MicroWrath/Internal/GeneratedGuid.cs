using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Kingmaker.Blueprints;

using MonoMod.Utils;
using System.Reflection;

namespace MicroWrath
{
    internal partial class GeneratedGuid
    {
        public string Key { get; }
        public BlueprintGuid Guid { get; }

        internal GeneratedGuid(string key, BlueprintGuid guid)
        {
            Key = key;
            Guid = guid;
        }

        private static readonly Dictionary<string, Guid> guids = new();

        internal static IEnumerable<string> Keys => guids.Keys;

        private static string ModDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly Dictionary<string, Guid> runtimeGuids = new();

        internal static bool TryLoadRuntimeGuids()
        {
            var path = Path.Combine(ModDirectory, "runtimeGuids.json");

            if (!File.Exists(path)) return false;

            try
            {
                runtimeGuids.AddRange(JsonConvert.DeserializeObject<Dictionary<string, Guid>>(File.ReadAllText(path)));
            }
            catch (Exception e)
            {
                MicroLogger.Error("Failed to load runtime guids with exception", e);
                return false;
            }

            return true;
        }

        internal static bool TrySaveRuntimeGuids()
        {
            try
            {
                File.WriteAllText(Path.Combine(ModDirectory, "runtimeGuids.json"), JsonConvert.SerializeObject(runtimeGuids, Formatting.Indented));
            }
            catch (Exception e)
            {
                MicroLogger.Error("Failed to save runtime guids with exception", e);
                return false;
            }

            return true;
        }

        public static GeneratedGuid Get(string key)
        {
            if (!guids.ContainsKey(key))
            {
                if (runtimeGuids.Count == 0) TryLoadRuntimeGuids();
                if (!runtimeGuids.ContainsKey(key))
                {
                    runtimeGuids[key] = System.Guid.NewGuid();
                    MicroLogger.Debug(() => $"No guid found for {key}. Generated new guid {runtimeGuids[key]}.");

                    if (!TrySaveRuntimeGuids()) MicroLogger.Warning("Could not save runtime guids. New saves may not load after game restart");
                }

                guids[key] = runtimeGuids[key];
            }

            return new(key, new BlueprintGuid(guids[key]));
        }

        public override string ToString() => this.Guid.ToString();

        public static implicit operator BlueprintGuid(GeneratedGuid guid) => guid.Guid;
        public static implicit operator string(GeneratedGuid guid) => guid.Guid.ToString();
    }
}
