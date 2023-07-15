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
using Kingmaker.UI.ServiceWindow.CharacterScreen;
using Kingmaker.UnitLogic;

namespace MicroWrath.Components
{
    [AllowedOn(typeof(BlueprintFeature))]
    [AllowedOn(typeof(BlueprintFeatureSelection))]
    [AllowedOn(typeof(BlueprintParametrizedFeature))]
    [AllowedOn(typeof(BlueprintProgression))]
    internal class OverrideUIFeatureGroup : BlueprintComponent
    {
        internal enum UIFeatureGroup
        {
            None,
            Ability,
            Feat,
            Trait
        }

        public UIFeatureGroup Group = UIFeatureGroup.None;

        [HarmonyPatch(typeof(UIUtilityUnit))]
        private static class Patches
        {
            private static IEnumerable<UIFeature> AddOverrideFeatures(
                IEnumerable<UIFeature> source, UnitDescriptor unit, UIFeatureGroup group = UIFeatureGroup.None)
            {
                var overrideFeatures = unit.Progression.Features.Visible
                    .Where(f => f.Blueprint.Components.OfType<OverrideUIFeatureGroup>().Any());

                if (!overrideFeatures.Any()) return source;

                foreach (var f in overrideFeatures)
                {
                    var component = f.Blueprint.Components
                        .OfType<OverrideUIFeatureGroup>()
                        .FirstOrDefault();

                    MicroLogger.Debug(() => $"Override {f.Blueprint.Name} as {group}? {component?.Group}");
                }

                source = source.Where(uif =>
                    !uif.Feature.Components
                        .OfType<OverrideUIFeatureGroup>()
                        .Any(c => c.Group == UIFeatureGroup.None || c.Group != group));

                if (group == UIFeatureGroup.None) return source;

                var extraFeatures = unit.Progression.Features.Visible
                    .Where(f => !source.Any(uif => uif.Feature == f.Blueprint))
                    .Where(f => !f.Blueprint.HideInCharacterSheetAndLevelUp);

                var step = 0;

                void LogFeatures()
                {
                    if (extraFeatures is null) return;

                    MicroLogger.Debug(() => $"STEP {step}:");

                    foreach (var f in extraFeatures)
                    {
                        MicroLogger.Debug(() => $"  {f}");
                    }

                    step++;
                }   

                LogFeatures();

                extraFeatures = extraFeatures
                    .Where(f => f.Blueprint.Components.OfType<OverrideUIFeatureGroup>().Any(c => c.Group == group));

                LogFeatures();

                var extraUIFeatures = extraFeatures
                    .Select(f => new UIFeature(
                        f.Blueprint, f.Param, f.Rank, UIUtilityUnit.TryGetSourceSelection(f.Blueprint, unit),
                        null, unit));

                return source.Concat(extraUIFeatures);
            }

            [HarmonyPatch(nameof(UIUtilityUnit.CollectAbilityFeatures))]
            [HarmonyPostfix]
            public static IEnumerable<UIFeature> CollectAbilityFeatures_Postfix(IEnumerable<UIFeature> __result, UnitDescriptor unit) =>
                AddOverrideFeatures(__result, unit, UIFeatureGroup.Ability);

            [HarmonyPatch(nameof(UIUtilityUnit.CollectFeats))]
            [HarmonyPostfix]
            public static IEnumerable<UIFeature> CollectFeats_Postfix(IEnumerable<UIFeature> __result, UnitDescriptor unit) =>
                AddOverrideFeatures(__result, unit, UIFeatureGroup.Feat);

            [HarmonyPatch(nameof(UIUtilityUnit.CollectTraits))]
            [HarmonyPostfix]
            public static IEnumerable<UIFeature> CollectTraits_Postfix(IEnumerable<UIFeature> __result, UnitDescriptor unit) =>
                AddOverrideFeatures(__result, unit, UIFeatureGroup.Trait);
        }
    }
}
