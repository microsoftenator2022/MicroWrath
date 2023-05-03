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
    internal static class ComponentExtensions
    {
        public static void AddFeatures(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IMicroBlueprint<BlueprintFeature> feature,
            params IMicroBlueprint<BlueprintFeature>[] features)
        {
            MicroLogger.Debug(() => $"Adding {feature.BlueprintGuid} to selection {selection.AssetGuid} ({selection.name})");

            var featuresList = selection.m_Features.ToList();
            var allFeaturesList = selection.m_AllFeatures.ToList();

            foreach (var f in features.Append(feature))
            {
                if (!featuresList.Contains(f.ToReference()) || allowDuplicates)
                    featuresList.Add(f.ToReference<BlueprintFeature, BlueprintFeatureReference>());

                if (!allFeaturesList.Contains(f.ToReference()) || allowDuplicates)
                    allFeaturesList.Add(f.ToReference<BlueprintFeature, BlueprintFeatureReference>());
            }

            selection.m_Features = featuresList.ToArray();
            selection.m_AllFeatures = allFeaturesList.ToArray();
        }

        public static void AddFeatures(this BlueprintFeatureSelection selection,
            IMicroBlueprint<BlueprintFeature> feature,
            params IMicroBlueprint<BlueprintFeature>[] features) =>
            AddFeatures(selection, false, feature, features);

        public static PrerequisiteFeature AddPrerequisiteFeature(
            this BlueprintFeature feature,
            IMicroBlueprint<BlueprintFeature> prerequisiteFeature,
            bool hideInUI = false,
            bool removeOnApply = false)
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
