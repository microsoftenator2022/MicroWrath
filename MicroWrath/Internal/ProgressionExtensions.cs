using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes;

namespace MicroWrath.Extensions
{
    internal static class ProgressionExtensions
    {
        public static void AddEntry(this BlueprintProgression progression, int level, params BlueprintFeatureBase[] features)
        {
            var entry = progression.LevelEntries.FirstOrDefault(e => e.Level == level);

            if (entry is null)
            {
                entry = new LevelEntry { Level = level };
                progression.LevelEntries = progression.LevelEntries.Append(entry).ToArray();
            }

            entry.SetFeatures(features);
        }
    }
}
