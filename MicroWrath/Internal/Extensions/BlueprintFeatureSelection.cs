using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints;
using MicroWrath.Util.Linq;

namespace MicroWrath.Extensions
{
    internal static partial class BlueprintExtensions
    {
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<BlueprintReference<TBlueprint>> features)
            where TBlueprint : BlueprintFeature
        {
            var featuresList = selection.m_Features.ToList();
            var allFeaturesList = selection.m_AllFeatures.ToList();

            foreach (var bpRef in features)
            {
                MicroLogger.Debug(() => $"Adding {bpRef} to selection {selection.name}", selection.ToMicroBlueprint());
                if (allowDuplicates || !featuresList.Any(f => f.deserializedGuid == bpRef.deserializedGuid))
                    featuresList.Add(new BlueprintFeatureReference() { deserializedGuid = bpRef.deserializedGuid });

                if (allowDuplicates || !allFeaturesList.Any(f => f.deserializedGuid == bpRef.deserializedGuid))
                    allFeaturesList.Add(new BlueprintFeatureReference() { deserializedGuid = bpRef.deserializedGuid });
            }

            selection.m_Features = featuresList.ToArray();
            selection.m_AllFeatures = allFeaturesList.ToArray();
        }

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<BlueprintReference<TBlueprint>> blueprints)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, blueprints);

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            BlueprintReference<TBlueprint> feature,
            params BlueprintReference<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new [] { feature }.Concat(features));

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            BlueprintReference<TBlueprint> feature,
            params BlueprintReference<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<TBlueprint> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, features.Select(bp => bp.ToReference<BlueprintReference<TBlueprint>>()));

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<TBlueprint> blueprints)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, blueprints);

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            TBlueprint feature,
            params TBlueprint[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new[] { feature }.Concat(features));

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            TBlueprint feature,
            params TBlueprint[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<IMicroBlueprint<TBlueprint>> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, features.Select(bp => bp.ToReference<TBlueprint, BlueprintReference<TBlueprint>>()));

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<IMicroBlueprint<TBlueprint>> blueprints)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, blueprints);

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IMicroBlueprint<TBlueprint> feature,
            params IMicroBlueprint<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new[] { feature }.Concat(features));

        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IMicroBlueprint<TBlueprint> feature,
            params IMicroBlueprint<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);
    }
}
