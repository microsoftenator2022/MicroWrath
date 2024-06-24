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

namespace MicroWrath.Extensions
{
    internal static partial class BlueprintExtensions
    {
        public static void SetIcon(this BlueprintUnitFact fact, Sprite sprite) => fact.m_Icon = sprite;

        public static void SetIcon(this BlueprintUnitFact fact, string assetID, long fileID)
            => fact.SetIcon(AssetUtils.Direct.GetSprite(assetID, fileID));

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
        }

        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Func<TComponent, TComponent> init)
            where TComponent : BlueprintComponent, new()
        {
            if (init == default) init = Functional.Identity;

            var component = init(Construct.New.Component<TComponent>());

            AddComponent<TComponent>(blueprint, component);

            return component;
        }

        public static IEnumerable<TComponent> AddComponents<TComponent>(this BlueprintScriptableObject blueprint, IEnumerable<TComponent> components)
            where TComponent : BlueprintComponent
        {
            foreach (var component in components)
                blueprint.AddComponent(component);

            return components;
        }

        public static IEnumerable<BlueprintComponent> AddComponents(this BlueprintScriptableObject blueprint, params BlueprintComponent[] components) =>
            blueprint.AddComponents<BlueprintComponent>(components);

        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Action<TComponent>? init = default)
            where TComponent : BlueprintComponent, new() => AddComponent<TComponent>(blueprint, c => { init?.Invoke(c); return c; });

        //public static IEnumerable<TComponent> GetComponents<TComponent>(this BlueprintScriptableObject blueprint)
        //    where TComponent : BlueprintComponent =>
        //    blueprint.Components.OfType<TComponent>();

        public static TComponent EnsureComponent<TComponent>(this BlueprintScriptableObject blueprint)
            where TComponent : BlueprintComponent, new()
        {
            if (blueprint.ComponentsArray.OfType<TComponent>().FirstOrDefault() is not { } component)
                component = blueprint.AddComponent<TComponent>(Functional.Identity);

            return component;
        }

        public static void RemoveComponents(this BlueprintScriptableObject blueprint, Func<BlueprintComponent, bool> predicate) =>
            blueprint.ComponentsArray = blueprint.ComponentsArray.Where(predicate).ToArray();

        public static void RemoveComponent(this BlueprintScriptableObject blueprint, BlueprintComponent component) =>
            blueprint.RemoveComponents(c => c != component);
    }
}
