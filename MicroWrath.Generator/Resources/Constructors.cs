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

        private interface IComponentConstructor<TComponent> where TComponent : BlueprintComponent, new()
        {
            TComponent New();
        }

        private partial class ComponentConstructor : IComponentConstructor<BlueprintComponent>
        {
            internal ComponentConstructor() { }

            BlueprintComponent IComponentConstructor<BlueprintComponent>.New() =>
                new();

            public TComponent New<TComponent>() where TComponent : BlueprintComponent, new() =>
                ((IComponentConstructor<TComponent>)this).New();
        }

        public static class New
        {
            private static readonly Lazy<BlueprintConstructor> blueprintConstructor = new(() => new());
            public static TBlueprint Blueprint<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint, new() =>
                blueprintConstructor.Value.New<TBlueprint>(assetId, name);

            private static readonly Lazy<ComponentConstructor> componentConstructor = new(() => new());
            public static TComponent Component<TComponent>() where TComponent : BlueprintComponent, new() =>
                componentConstructor.Value.New<TComponent>();
        }
    }
}
