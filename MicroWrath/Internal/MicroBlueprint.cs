using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath;

namespace MicroWrath
{
    internal static class MicroBlueprint
    {
        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this TBlueprint blueprint)
            where TBlueprint : SimpleBlueprint => (MicroBlueprint<TBlueprint>)blueprint;

        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this BlueprintReference<TBlueprint> reference)
            where TBlueprint : SimpleBlueprint => (MicroBlueprint<TBlueprint>)reference;
    }

    internal readonly record struct MicroBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public MicroBlueprint(string assetId)
        {
            AssetId = assetId;
            BlueprintGuid = BlueprintGuid.Parse(AssetId);
        }

        public MicroBlueprint(BlueprintGuid guid)
        {
            BlueprintGuid = guid;
            AssetId = guid.ToString();
        }

        public readonly string AssetId;

        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();

        public string Name => ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        public BlueprintGuid BlueprintGuid { get; }
        public TBlueprint? GetBlueprint() => ToReference<BlueprintReference<TBlueprint>>().Get();

        public static implicit operator MicroBlueprint<TBlueprint>(TBlueprint blueprint) =>
            new(blueprint.AssetGuid);
        public static implicit operator MicroBlueprint<TBlueprint>(BlueprintReference<TBlueprint> blueprintReference) =>
            new(blueprintReference.Guid);
    }
}
