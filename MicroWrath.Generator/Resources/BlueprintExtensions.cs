using System;
using System.Collections.Generic;
using System.Linq;

using Kingmaker.Blueprints;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath
{
    internal static class BlueprintExtensions
    {
        public static void AddComponent(this BlueprintScriptableObject blueprint, BlueprintComponent component)
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
    }
}
