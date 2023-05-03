using System;
using System.Collections.Generic;
using System.Linq;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

using MicroWrath.Constructors;

namespace MicroWrath.Extensions
{
    internal static class BlueprintExtensions
    {
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

            MicroLogger.Debug(() => $"Adding {typeof(TComponent)} {component.name} to {blueprint.AssetGuid} ({blueprint.name})");

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

        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Action<TComponent>? init = default)
            where TComponent : BlueprintComponent, new() => AddComponent<TComponent>(blueprint, c => { init?.Invoke(c); return c; });

        public static IEnumerable<TComponent> GetComponents<TComponent>(this BlueprintScriptableObject blueprint)
            where TComponent : BlueprintComponent =>
            blueprint.Components.OfType<TComponent>();

        public static void RemoveComponents(this BlueprintScriptableObject blueprint, Func<BlueprintComponent, bool> predicate) =>
            blueprint.ComponentsArray = blueprint.ComponentsArray.Where(predicate).ToArray();

        public static void RemoveComponent(this BlueprintScriptableObject blueprint, BlueprintComponent component) =>
            blueprint.RemoveComponents(c => c != component);
    }
}
