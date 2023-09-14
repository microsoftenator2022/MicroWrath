using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath;

namespace MicroWrath
{
    internal static class MicroBlueprint
    {
        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this TBlueprint blueprint)
            where TBlueprint : SimpleBlueprint => new MicroBlueprint<TBlueprint>(blueprint.AssetGuid);

        public static IMicroBlueprint<TBlueprint> ToMicroBlueprint<TBlueprint>(this BlueprintReference<TBlueprint> reference)
            where TBlueprint : SimpleBlueprint => new MicroBlueprint<TBlueprint>(reference.guid);
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

        private TBlueprint? MaybeBlueprint { get; init; } = null;

        public string Name => MaybeBlueprint?.name ?? ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        public BlueprintGuid BlueprintGuid { get; }
        public TBlueprint? GetBlueprint() => ToReference<BlueprintReference<TBlueprint>>().Get();

        public override string ToString()
        {
            if (MaybeBlueprint is not null)
                return $"{typeof(TBlueprint)} {BlueprintGuid} ({Name})";

            return $"{typeof(TBlueprint)} {BlueprintGuid}";
        }

        //public static implicit operator MicroBlueprint<TBlueprint>(TBlueprint blueprint) =>
        //    new(blueprint.AssetGuid) { MaybeBlueprint = blueprint };

        public static implicit operator MicroBlueprint<TBlueprint>(BlueprintReference<TBlueprint> blueprintReference) =>
            new(blueprintReference.Guid);
    }

    internal readonly record struct OwlcatBlueprint<TBlueprint> : IMicroBlueprint<TBlueprint> where TBlueprint : SimpleBlueprint
    {
        public OwlcatBlueprint(string guidString)
        {
            BlueprintGuid = BlueprintGuid.Parse(guidString);
        }

        public BlueprintGuid BlueprintGuid { get; init; }

        public TReference ToReference<TReference>() where TReference : BlueprintReference<TBlueprint>, new() =>
            this.ToReference<TBlueprint, TReference>();

        public TBlueprint Blueprint => ToReference<BlueprintReference<TBlueprint>>().Get();

        public string Name => this.ToReference<BlueprintReference<TBlueprint>>().NameSafe();

        TBlueprint? IMicroBlueprint<TBlueprint>.GetBlueprint() => Blueprint;

        public override string ToString() => $"{typeof(TBlueprint)} {BlueprintGuid} ({Name})";
    }
}
