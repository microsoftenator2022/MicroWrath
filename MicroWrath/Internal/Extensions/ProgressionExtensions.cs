using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

namespace MicroWrath.Extensions
{
    internal static partial class BlueprintExtensions
    {
        /// <summary>
        /// Add features to a <see cref="BlueprintProgression"/>.
        /// </summary>
        /// <param name="levelEntry"><see cref="LevelEntry"/> to add features to.</param>
        /// <param name="features">Features to add.</param>
        /// <returns><see cref="LevelEntry"/> from <paramref name="levelEntry"/> with the added features.</returns>
        public static LevelEntry AddFeatures(this LevelEntry levelEntry, params BlueprintFeatureBase[] features)
        {
            levelEntry.SetFeatures(levelEntry.Features.Concat(features));

            return levelEntry;
        }

        /// <inheritdoc cref="AddFeatures(LevelEntry, BlueprintFeatureBase[])"/>
        /// <param name="progression"><see cref="BlueprintProgression"/> to add to.</param>
        /// <param name="level">Level to add to.</param>
        /// <param name="features">Features to add.</param>
        public static void AddFeatures(this BlueprintProgression progression, int level, params BlueprintFeatureBase[] features)
        {
            var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == level);

            if (entry is null)
            {
                entry = new LevelEntry { Level = level };
                progression.LevelEntries = progression.LevelEntries.Append(entry).ToArray();
            }

            entry.AddFeatures(features);
        }
    }
}
