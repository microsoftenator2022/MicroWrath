using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.Constructors;
using MicroWrath.Util;
using MicroWrath.Util.Linq;
using HarmonyLib;
using Kingmaker.SharedTypes;

namespace MicroWrath.Extensions
{
    /// <summary>
    /// Blueprint extension methods
    /// </summary>
    internal static partial class BlueprintExtensions
    {
        /// <summary>
        /// Set <see cref="BlueprintUnitFact.Icon"/>.
        /// </summary>
        /// <param name="fact">Fact to set icon for.</param>
        /// <param name="sprite">Sprite to use as icon.</param>
        public static void SetIcon(this BlueprintUnitFact fact, Sprite sprite) => fact.m_Icon = sprite;

        /// <summary>
        /// Set <see cref="BlueprintUnitFact.Icon"/> using asset ID and file ID (<see cref="BlueprintReferencedAssets"/>)
        /// </summary>
        /// <param name="fact">Fact to set icon for.</param>
        /// <param name="assetID">Asset ID of sprite.</param>
        /// <param name="fileID">File ID of sprits.</param>
        public static void SetIcon(this BlueprintUnitFact fact, string assetID, long fileID)
            => fact.SetIcon(AssetUtils.Direct.GetSprite(assetID, fileID));

        /// <summary>
        /// Add a component to this blueprint.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="blueprint">Blueprint to add component to.</param>
        /// <param name="component">Component to add to blueprint.</param>
        public static void AddComponent<TComponent>(this BlueprintScriptableObject blueprint, TComponent component)
            where TComponent : BlueprintComponent
        {
            var name =
                string.IsNullOrEmpty(component.name) ?
                $"${blueprint.name ?? blueprint.GetType().Name}${component.GetType().Name}" :
                component.name;

            component.name = name;

            for (var i = 2; blueprint.ComponentsArray.Select(c => c.name).Contains(component.name); i++)
                component.name = $"{name}${i}";

            MicroLogger.Debug(() => $"Adding {component.GetType()} ({typeof(TComponent)}) to {blueprint.name}", blueprint.ToMicroBlueprint());

            blueprint.ComponentsArray = blueprint.ComponentsArray.Append(component);

            component.OwnerBlueprint = blueprint;
        }

        /// <summary>
        /// Add component to this blueprint, providing an initialization function.
        /// A default initializer (see: <see cref="Default"/>) is applied first.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="blueprint">Blueprint to add component to.</param>
        /// <param name="init">Initialization function.</param>
        /// <returns>New component.</returns>
        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Func<TComponent, TComponent> init)
            where TComponent : BlueprintComponent, new()
        {
            if (init == default) init = Functional.Identity;

            var component = init(Construct.New.Component<TComponent>());

            AddComponent<TComponent>(blueprint, component);

            return component;
        }

        /// <summary>
        /// Add a sequence of components to this blueprint.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="blueprint">Blueprint to add components to.</param>
        /// <param name="components">Components to add.</param>
        /// <returns>Sequence of added components.</returns>
        public static IEnumerable<TComponent> AddComponents<TComponent>(this BlueprintScriptableObject blueprint, IEnumerable<TComponent> components)
            where TComponent : BlueprintComponent
        {
            foreach (var component in components)
                blueprint.AddComponent(component);

            return components;
        }

        /// <summary>
        /// Add components to this blueprint.
        /// </summary>
        /// <param name="blueprint">Blueprint to add components to.</param>
        /// <param name="components">Components to add.</param>
        /// <returns>Sequence of added components.</returns>
        public static IEnumerable<BlueprintComponent> AddComponents(this BlueprintScriptableObject blueprint, params BlueprintComponent[] components) =>
            blueprint.AddComponents<BlueprintComponent>(components);

        /// <summary>
        /// Add a component to this bluprint, optionally providing an intialization action (<see cref="Action{TComponent}"/>).
        /// A default initializer (see: <see cref="Default"/>) is applied first.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="blueprint">Blueprint to add component to.</param>
        /// <param name="init">Initialization action.</param>
        /// <returns>Added component.</returns>
        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Action<TComponent>? init = default)
            where TComponent : BlueprintComponent, new() => AddComponent<TComponent>(blueprint, c => { init?.Invoke(c); return c; });

        //public static IEnumerable<TComponent> GetComponents<TComponent>(this BlueprintScriptableObject blueprint)
        //    where TComponent : BlueprintComponent =>
        //    blueprint.Components.OfType<TComponent>();

        /// <summary>
        /// Get a component or create one if it does not exist.
        /// A default initializer (see: <see cref="Default"/>) is applied to a new component.
        /// </summary>
        /// <typeparam name="TComponent">Component type.</typeparam>
        /// <param name="blueprint">Blueprint to get component from.</param>
        /// <returns>Existing or new component.</returns>
        public static TComponent EnsureComponent<TComponent>(this BlueprintScriptableObject blueprint)
            where TComponent : BlueprintComponent, new()
        {
            if (blueprint.ComponentsArray.OfType<TComponent>().FirstOrDefault() is not { } component)
                component = blueprint.AddComponent<TComponent>(Functional.Identity);

            return component;
        }

        /// <summary>
        /// Remove components from a blueprint matching a provided predicate.
        /// </summary>
        /// <param name="blueprint">Blueprint to remove components from.</param>
        /// <param name="predicate">Component selector predicate.</param>
        public static void RemoveComponents(this BlueprintScriptableObject blueprint, Func<BlueprintComponent, bool> predicate) =>
            blueprint.ComponentsArray = blueprint.ComponentsArray.Where(predicate).ToArray();

        /// <summary>
        /// Remove a specific component from a blueprint.
        /// </summary>
        /// <param name="blueprint">Blueprint to remove component from.</param>
        /// <param name="component">Component to remove.</param>
        public static void RemoveComponent(this BlueprintScriptableObject blueprint, BlueprintComponent component) =>
            blueprint.RemoveComponents(c => c != component);
    }
}
