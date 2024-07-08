using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Facts;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Abilities.Components;

namespace MicroWrath.Extensions
{
    /// <summary>
    /// <see cref="BlueprintComponent"/> extension methods.
    /// </summary>
    internal static partial class ComponentExtensions
    {
        /// <summary>
        /// Add prerequisite to feature.
        /// </summary>
        /// <param name="feature">Feature to add to.</param>
        /// <param name="prerequisiteFeature">Prerequisite feature</param>
        /// <param name="removeOnApply">Also add a <see cref="RemoveFeatureOnApply"/> component to that removes 
        /// the prerequisite feature (ie. this feature replaces its prerequisite).</param>
        /// <param name="hideInUI">Hide this prerequisite in the UI.</param>
        /// <returns>The added <see cref="PrerequisiteFeature"/>.</returns>
        public static PrerequisiteFeature AddPrerequisiteFeature(
            this BlueprintFeature feature,
            IMicroBlueprint<BlueprintFeature> prerequisiteFeature,
            bool removeOnApply = false,
            bool hideInUI = false)
        {
            MicroLogger.Debug(() => $"Adding {prerequisiteFeature} as prerequisite for {feature.AssetGuid} ({feature.name})",
                feature.ToMicroBlueprint());

            var prerequisite = feature.AddComponent<PrerequisiteFeature>();
            prerequisite.HideInUI = hideInUI;
            prerequisite.m_Feature = prerequisiteFeature.ToReference<BlueprintFeature, BlueprintFeatureReference>();
            
            if (removeOnApply)
            {
                feature.AddComponent<RemoveFeatureOnApply>(component =>
                    component.m_Feature = prerequisiteFeature.ToReference<BlueprintUnitFact, BlueprintUnitFactReference>());
            }

            return prerequisite;
        }

        /// <summary>
        /// Add <see cref="GameAction"/>s to a <see cref="AbilityEffectRunAction"/> component's <see cref="AbilityEffectRunAction.Actions"/>.
        /// </summary>
        /// <param name="component"><see cref="AbilityEffectRunAction"/> component to add to.</param>
        /// <param name="actions"><see cref="GameAction"/>s to add.</param>
        /// <returns>The modified <see cref="AbilityEffectRunAction"/>.</returns>
        public static AbilityEffectRunAction AddActions(
            this AbilityEffectRunAction component,
            params GameAction[] actions)
        {
            component.Actions.Add(actions);
            return component;
        }
    }
}
