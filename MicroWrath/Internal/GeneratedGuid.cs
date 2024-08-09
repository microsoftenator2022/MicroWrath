using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Newtonsoft.Json;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

using MonoMod.Utils;

namespace MicroWrath
{
    /// <summary>
    /// A source-generated named guid.
    /// </summary>
    internal partial class GeneratedGuid
    {
        /// <summary>
        /// Unique name for this guid.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// This guid value.
        /// </summary>
        public BlueprintGuid Guid { get; }

        internal GeneratedGuid(string key, BlueprintGuid guid)
        {
            Key = key;
            Guid = guid;
        }

        private static readonly Dictionary<string, Guid> guids = new();

        /// <summary>
        /// Collection of unique names.
        /// </summary>
        internal static IEnumerable<string> Keys => guids.Keys;

        /// <exclude />
        private static string ModDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <exclude />

        private static readonly Dictionary<string, Guid> runtimeGuids = new();

        /// <summary>
        /// Used to generate guids.json (to persist the values)
        /// </summary>
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

        /// <summary>
        /// Used at runtime to save generated guids from names that are not compile-time constant.
        /// </summary>
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
                    runtimeGuids[key] = GuidEx.CreateV5(typeof(GeneratedGuid).FullName, key);
                    MicroLogger.Debug(() => $"No guid found for {key}. Generated new guid {runtimeGuids[key]}.");

                    if (!TrySaveRuntimeGuids()) MicroLogger.Warning("Could not save runtime guids. New saves may not load after game restart");
                }

                guids[key] = runtimeGuids[key];
            }

            return new(key, new BlueprintGuid(guids[key]));
        }

        /// <summary>
        /// Creates a blueprint reference of type <typeparamref name="TRef"/> from this guid.
        /// </summary>
        public TRef ToBlueprintReference<TRef>() where TRef : BlueprintReferenceBase, new() =>
            new() { deserializedGuid = this.Guid };

        /// <summary>
        /// Creates a <see cref="IMicroBlueprint{TBlueprint}"/> from this guid.
        /// </summary>
        public IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>() where TBlueprint : SimpleBlueprint =>
            new MicroBlueprint<TBlueprint>(this.Guid);

        public override string ToString() => this.Guid.ToString();

        /// <summary>
        /// Implicit conversion to <see cref="BlueprintGuid"/>
        /// </summary>
        /// <param name="guid"></param>
        public static implicit operator BlueprintGuid(GeneratedGuid guid) => guid.Guid;
        //public static implicit operator string(GeneratedGuid guid) => guid.Guid.ToString();
    }
}
