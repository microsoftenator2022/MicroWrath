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
    [HarmonyPatch(typeof(AbilityData), nameof(AbilityData.IsAffectedByArcaneSpellFailure), MethodType.Getter)]
    internal static class ArcaneSpellFailurePatch
    {
        internal static bool Postfix(bool __result, AbilityData __instance)
        {
            if(__instance.Blueprint.ComponentsArray.OfType<ArcaneSpellFailureComponent>().Any()) return true;

            return __result;
        }
    }

    internal class ArcaneSpellFailureComponent : BlueprintComponent
    {
    }
}
