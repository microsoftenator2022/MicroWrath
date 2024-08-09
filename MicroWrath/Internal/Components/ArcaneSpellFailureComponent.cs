using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities;

namespace MicroWrath.Components
{
    /// <summary>
    /// Adds arcane spell failure chance to this ability.
    /// </summary>
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.IsAffectedByArcaneSpellFailure), MethodType.Getter)]
    internal class ArcaneSpellFailureComponent : BlueprintComponent
    {
        /// <exclude />
        private static bool Postfix(bool __result, AbilityData __instance)
        {
            if (__instance.Blueprint.ComponentsArray.OfType<ArcaneSpellFailureComponent>().Any()) return true;

            return __result;
        }
    }
}
