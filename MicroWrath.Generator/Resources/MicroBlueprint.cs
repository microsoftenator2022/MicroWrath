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

    internal static class MicroBlueprintExtensions
    {
        public static Option<TBlueprint> TryGetBlueprint<TBlueprint>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint =>
            bpRef.GetBlueprint().ToOption();

        public static TReference ToReference<TBlueprint, TReference>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint
            where TReference : BlueprintReference<TBlueprint>, new() =>
            new() { deserializedGuid = bpRef.BlueprintGuid };
    }
}
