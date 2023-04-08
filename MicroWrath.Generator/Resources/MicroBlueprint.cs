using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath.Interfaces;

namespace MicroWrath
{
    internal readonly record struct MicroBlueprint<TBlueprint>(string AssetId) : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();
        public BlueprintReference<TBlueprint> ToReference() => ToReference<BlueprintReference<TBlueprint>>();

        public string Name => ToReference().NameSafe();

        public BlueprintGuid BlueprintGuid { get; } = BlueprintGuid.Parse(AssetId);
        TBlueprint? IMicroBlueprint<TBlueprint>.GetBlueprint() => ToReference<BlueprintReference<TBlueprint>>().Get();
    }
}
