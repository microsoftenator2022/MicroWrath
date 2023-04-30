using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;

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

        // TODO: Ensure param is compile-time constant
        public static GeneratedGuid Get(string key) => new(key, new BlueprintGuid(guids[key]));

        public override string ToString() => this.Guid.ToString();

        public static implicit operator BlueprintGuid(GeneratedGuid guid) => guid.Guid;
        public static implicit operator string(GeneratedGuid guid) => guid.Guid.ToString();
    }
}
