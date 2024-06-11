using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.UI.Common;
using Kingmaker.UI.MVVM._VM.CharGen.Phases;
using Kingmaker.UI.MVVM._VM.CharGen.Phases.FeatureSelector;
using Kingmaker.UnitLogic.Class.LevelUp;

namespace MicroWrath.Components
{
    [AllowedOn(typeof(BlueprintFeatureSelection))]
    [AllowedOn(typeof(BlueprintParametrizedFeature))]
    internal class OverrideSelectionPriority : BlueprintComponent
    {
#pragma warning disable CS0649
        public CharGenPhaseBaseVM.ChargenPhasePriority Priority;
#pragma warning restore CS0649
    }

    [HarmonyPatch(typeof(CharGenFeatureSelectorPhaseVM), nameof(CharGenFeatureSelectorPhaseVM.GetFeaturePriority))]
    internal static class CharGenFeatureSelectorPhaseVM_GetFeaturePriority_Patch
    {
        public static CharGenPhaseBaseVM.ChargenPhasePriority Postfix(CharGenPhaseBaseVM.ChargenPhasePriority __result,
            FeatureSelectionState featureSelectionState, CharGenFeatureSelectorPhaseVM __instance)
        {
            if (featureSelectionState.Selection is BlueprintScriptableObject blueprint &&
                blueprint.Components.OfType<OverrideSelectionPriority>().FirstOrDefault() is OverrideSelectionPriority osp)
            {
                return osp.Priority;
            }

            return __result;
        }
    }
}
