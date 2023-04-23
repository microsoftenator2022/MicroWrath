using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath;

namespace MicroWrath
{
    internal readonly record struct MicroBlueprint<TBlueprint>(string AssetId) : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();

        public string Name => ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        public BlueprintGuid BlueprintGuid { get; } = BlueprintGuid.Parse(AssetId);
        public TBlueprint? GetBlueprint() => ToReference<BlueprintReference<TBlueprint>>().Get();
    }
}
