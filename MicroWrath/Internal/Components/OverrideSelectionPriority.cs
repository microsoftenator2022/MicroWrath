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
using Kingmaker.UnitLogic.Class.LevelUp.Actions;

namespace MicroWrath.Components
{
    [Obsolete]
    [AllowedOn(typeof(BlueprintFeatureSelection))]
    [AllowedOn(typeof(BlueprintParametrizedFeature))]
    internal class OverrideSelectionPriority : BlueprintComponent
    {
#pragma warning disable CS0649
        public CharGenPhaseBaseVM.ChargenPhasePriority Priority;
#pragma warning restore CS0649
    }

    [Obsolete]
    [HarmonyPatch(typeof(CharGenFeatureSelectorPhaseVM), nameof(CharGenFeatureSelectorPhaseVM.GetFeaturePriority))]
    internal static class CharGenFeatureSelectorPhaseVM_GetFeaturePriority_Patch
    {
        static CharGenPhaseBaseVM.ChargenPhasePriority Postfix(CharGenPhaseBaseVM.ChargenPhasePriority __result,
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

    [HarmonyPatch]
    [AllowedOn(typeof(IFeatureSelection))]
    [AllowedOn(typeof(BlueprintFeatureSelection))]
    [AllowedOn(typeof(BlueprintParametrizedFeature))]
    internal class SelectionPriority : BlueprintComponent
    {
        public CharGenPhaseBaseVM.ChargenPhasePriority? PhasePriority = null;
        public LevelUpActionPriority? ActionPriority = null;

        [HarmonyPatch(typeof(CharGenFeatureSelectorPhaseVM), nameof(CharGenFeatureSelectorPhaseVM.GetFeaturePriority))]
        [HarmonyPostfix]
        static CharGenPhaseBaseVM.ChargenPhasePriority GetFeaturePriority_Postfix(
            CharGenPhaseBaseVM.ChargenPhasePriority __result,
            FeatureSelectionState featureSelectionState)
        {
            if (featureSelectionState.Selection is BlueprintScriptableObject blueprint &&
                blueprint.GetComponent<SelectionPriority>()?.PhasePriority is { } phasePriority)
                return phasePriority;

            return __result;
        }

        [HarmonyPatch(typeof(SelectFeature), nameof(SelectFeature.CalculatePriority))]
        [HarmonyPostfix]
        static LevelUpActionPriority CalculatePriority_Postfix(LevelUpActionPriority __result, IFeatureSelection selection)
        {
            if (selection is BlueprintScriptableObject blueprint &&
                blueprint.GetComponent<SelectionPriority>()?.ActionPriority is { } actionPriority)
                return actionPriority;

            return __result;
        }
    }
}
