using System;
using MicroWrath;
using Kingmaker.Blueprints;

namespace MicroWrath.Constructors
{
    internal static partial class Construct
    {
        private interface IBlueprintConstructor<TBlueprint> where TBlueprint : SimpleBlueprint, new()
        {
            TBlueprint New(string assetId, string name);
        }

        private partial class BlueprintConstructor : IBlueprintConstructor<SimpleBlueprint>
        {
            internal BlueprintConstructor() { }

            SimpleBlueprint IBlueprintConstructor<SimpleBlueprint>.New(string assetId, string name) =>
                new() { AssetGuid = BlueprintGuid.Parse(assetId), name = name };

            public TBlueprint New<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint, new() =>
                ((IBlueprintConstructor<TBlueprint>)this).New(assetId, name);
        }

        public static class New
        {
            private static readonly Lazy<BlueprintConstructor> blueprintConstructor = new(() => new());
            public static TBlueprint Blueprint<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint, new() =>
                blueprintConstructor.Value.New<TBlueprint>(assetId, name);

            public static TBlueprint Blueprint<TBlueprint> (string assetId, string name) where TBlueprint : SimpleBlueprint =>
                blueprintConstructor.Value.New<TBlueprint>(assetId, name);
        }
    }
}
