using System;
using System.Collections.Generic;
using System.Linq;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

using MicroWrath.Constructors;

namespace MicroWrath
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

            blueprint.ComponentsArray = blueprint.ComponentsArray.Append(component);
        }

        public static TComponent AddNewComponent<TComponent>(this BlueprintScriptableObject blueprint, Func<TComponent, TComponent>? init = default)
            where TComponent : BlueprintComponent, new()
        {
            if (init == default) init = Functional.Identity;

            var component = init(Construct.New.Component<TComponent>());

            AddComponent<TComponent>(blueprint, component);

            return component;
        }
    }
}
