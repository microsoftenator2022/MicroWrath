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

namespace MicroWrath.Extensions
{
    internal static partial class ComponentExtensions
    {
        public static PrerequisiteFeature AddPrerequisiteFeature(
            this BlueprintFeature feature,
            IMicroBlueprint<BlueprintFeature> prerequisiteFeature,
            bool removeOnApply = false,
            bool hideInUI = false)
        {
            MicroLogger.Debug(() => $"Adding {prerequisiteFeature.BlueprintGuid} as prerequisite for {feature.AssetGuid} ({feature.name})");

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
    }
}
