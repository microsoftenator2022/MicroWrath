﻿using System;
using MicroWrath;
using Kingmaker.Blueprints;
using System.Collections.Generic;
using System.Linq;

namespace MicroWrath.Constructors
{
    internal static partial class Construct
    {
        internal interface IBlueprintConstructor<TBlueprint> where TBlueprint : SimpleBlueprint, new()
        {
            TBlueprint New(string assetId, string name);
        }

        /// <exclude />
        private partial class BlueprintConstructor : IBlueprintConstructor<SimpleBlueprint>
        {
            internal BlueprintConstructor() { }

            SimpleBlueprint IBlueprintConstructor<SimpleBlueprint>.New(string assetId, string name) =>
                new() { AssetGuid = BlueprintGuid.Parse(assetId), name = name };

            public TBlueprint New<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint, new()
            {
                if (this is IBlueprintConstructor<TBlueprint> blueprintConstructor)
                {
                    MicroLogger.Debug(() => $"New blueprint {assetId} - {name}");
                    return blueprintConstructor.New(assetId, name);
                }

                MicroLogger.Debug(() => $"Missing initializer for {typeof(TBlueprint)}. Using reflection fallback.");

                // Reflection-based fallback
                if (!initializers.ContainsKey(typeof(TBlueprint)))
                    initializers.Add(typeof(TBlueprint), new BlueprintReflectionInitializer<TBlueprint>(typeof(Default)));

                return ((BlueprintReflectionInitializer<TBlueprint>)initializers[typeof(TBlueprint)]).New(assetId, name);
            }

            private static readonly Dictionary<Type, IReflectionInitializer> initializers = new();
        }

        internal interface IComponentConstructor<TComponent> where TComponent : BlueprintComponent, new()
        {
            TComponent New();
        }

        /// <exclude />
        private partial class ComponentConstructor : IComponentConstructor<BlueprintComponent>
        {
            internal ComponentConstructor() { }

            BlueprintComponent IComponentConstructor<BlueprintComponent>.New() =>
                new();

            public TComponent New<TComponent>() where TComponent : BlueprintComponent, new()
            {
                if (this is IComponentConstructor<TComponent> componentConstructor)
                {
                    return componentConstructor.New();
                }

                MicroLogger.Warning($"Missing initializer for {typeof(TComponent)}. Using fallback");

                // Reflection-based fallback
                if (!initializers.ContainsKey(typeof(TComponent)))
                    initializers.Add(typeof(TComponent), new ComponentReflectionInitializer<TComponent>(typeof(Default)));

                return ((ComponentReflectionInitializer<TComponent>)initializers[typeof(TComponent)]).New();
            }

            private static readonly Dictionary<Type, IReflectionInitializer> initializers = new();
        }

        /// <summary>
        /// Blueprint/Component constructors using <see cref="Default"/>.
        /// </summary>
        public static class New
        {
            /// <exclude />
            private static readonly Lazy<BlueprintConstructor> blueprintConstructor = new(() => new());

            /// <summary>
            /// Creates a new blueprint. Fields are initialized using <see cref="Default"/> values.
            /// </summary>
            /// <typeparam name="TBlueprint">Blueprint type.</typeparam>
            /// <param name="assetId">Asset ID for the new blueprint (<see cref="SimpleBlueprint.AssetGuid"/>).</param>
            /// <param name="name">Blueprint name (<see cref="SimpleBlueprint.name"/>).</param>
            /// <returns>New blueprint.</returns>
            public static TBlueprint Blueprint<TBlueprint>(string assetId, string name) where TBlueprint : SimpleBlueprint, new() =>
                blueprintConstructor.Value.New<TBlueprint>(assetId, name);

            /// <inheritdoc cref="Blueprint{TBlueprint}(string, string)"/>
            /// <param name="generatedGuid">Asset ID for the new blueprint (<see cref="SimpleBlueprint.AssetGuid"/>).<br/>
            /// Uses <see cref="GeneratedGuid.Key"/> as blueprint name (<see cref="SimpleBlueprint.name"/>).</param>
            /// <returns>New blueprint.</returns>
            public static TBlueprint Blueprint<TBlueprint>(GeneratedGuid generatedGuid) where TBlueprint : SimpleBlueprint, new() =>
                blueprintConstructor.Value.New<TBlueprint>(generatedGuid.Guid.ToString(), generatedGuid.Key);

            /// <exclude />
            private static readonly Lazy<ComponentConstructor> componentConstructor = new(() => new());

            /// <summary>
            /// Creates a new component. Fields are initialized using <see cref="Default"/> value.
            /// </summary>
            /// <typeparam name="TComponent">Component type.</typeparam>
            /// <returns>New component.</returns>
            public static TComponent Component<TComponent>() where TComponent : BlueprintComponent, new() =>
                componentConstructor.Value.New<TComponent>();
        }
    }
}
