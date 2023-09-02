using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;

namespace MicroWrath
{
    public static class MicroBlueprintExtensions
    {
        public static Option<TBlueprint> TryGetBlueprint<TBlueprint>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint =>
            bpRef.GetBlueprint().ToOption();

        public static TReference ToReference<TBlueprint, TReference>(this IMicroBlueprint<TBlueprint> bpRef)
            where TBlueprint : SimpleBlueprint
            where TReference : BlueprintReference<TBlueprint>, new() =>
            new() { deserializedGuid = bpRef.BlueprintGuid };

        //public static BlueprintReference<TBlueprint> ToReference<TBlueprint>(this IMicroBlueprint<TBlueprint> bpRef)
        //    where TBlueprint : SimpleBlueprint =>
        //    bpRef.ToReference<TBlueprint, BlueprintReference<TBlueprint>>();
    }

    public interface IMicroBlueprint<out TBlueprint> where TBlueprint : SimpleBlueprint
    {
        BlueprintGuid BlueprintGuid { get; }
        TBlueprint? GetBlueprint();
    }
}
