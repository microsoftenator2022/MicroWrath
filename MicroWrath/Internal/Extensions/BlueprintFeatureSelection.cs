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
        /// <summary>
        /// Add features to a <see cref="BlueprintFeatureSelection"/>.
        /// </summary>
        /// <typeparam name="TBlueprint">Feature blueprint type.</typeparam>
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="features">Features to add.</param>
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

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<BlueprintReference<TBlueprint>> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, features);

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="feature">First feature to add.</param>
        /// <param name="features">Additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            BlueprintReference<TBlueprint> feature,
            params BlueprintReference<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new [] { feature }.Concat(features));

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="feature">First feature to add.</param>
        /// <param name="features">Additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            BlueprintReference<TBlueprint> feature,
            params BlueprintReference<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<TBlueprint> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, features.Select(bp => bp.ToReference<BlueprintReference<TBlueprint>>()));

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<TBlueprint> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, features);

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="feature">First blueprint to add.</param>
        /// <param name="features">Sequence of additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            TBlueprint feature,
            params TBlueprint[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new[] { feature }.Concat(features));

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="feature">First feature to add.</param>
        /// <param name="features">Additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            TBlueprint feature,
            params TBlueprint[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<IMicroBlueprint<TBlueprint>> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, features.Select(bp => bp.ToReference<TBlueprint, BlueprintReference<TBlueprint>>()));

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IEnumerable<IMicroBlueprint<TBlueprint>> features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, features);

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="allowDuplicates">Are duplicates allowed?</param>
        /// <param name="feature">First feature to add.</param>
        /// <param name="features">Additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IMicroBlueprint<TBlueprint> feature,
            params IMicroBlueprint<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, allowDuplicates, new[] { feature }.Concat(features));

        /// <inheritdoc cref="AddFeatures{TBlueprint}(BlueprintFeatureSelection, bool, IEnumerable{BlueprintReference{TBlueprint}})" />
        /// <param name="selection">Selection to add to.</param>
        /// <param name="feature">First feature to add.</param>
        /// <param name="features">Additional features to add.</param>
        public static void AddFeatures<TBlueprint>(
            this BlueprintFeatureSelection selection,
            IMicroBlueprint<TBlueprint> feature,
            params IMicroBlueprint<TBlueprint>[] features)
            where TBlueprint : BlueprintFeature =>
            AddFeatures(selection, false, feature, features);
    }
}
