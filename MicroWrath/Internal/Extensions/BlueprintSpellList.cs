using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.Util.Linq;

namespace MicroWrath.Extensions
{
    internal static partial class BlueprintExtensions
    {
        /// <summary>
        /// Add a spell to a spell list.
        /// </summary>
        /// <param name="spell">Spell to add.</param>
        /// <param name="spellList">Spell list to add to.</param>
        /// <param name="level">Spell level.</param>
        public static void AddToSpellList(this BlueprintAbility spell, BlueprintSpellList spellList, int level)
        {
            var slc = spell.Components.OfType<SpellListComponent>().FirstOrDefault();

            MicroLogger.Debug(() => $"Adding {spell} to {spellList} level {level}");

            slc ??= spell.AddComponent<SpellListComponent>(c =>
            {
                c.m_SpellList = spellList.ToReference<BlueprintSpellListReference>();
                c.SpellLevel = level;
            });

            if (slc.SpellLevel != level)
            {
                MicroLogger.Warning($"{spell} level for {spellList} is {slc.SpellLevel}, but added to {level}");
            }

            var spellListForLevel = spellList.SpellsByLevel.FirstOrDefault(sl => sl.SpellLevel == level);
            if (spellListForLevel is null)
            {
                spellListForLevel = new SpellLevelList(level);

                spellList.SpellsByLevel = spellList.SpellsByLevel.Append(spellListForLevel);
            }

            spellListForLevel.m_Spells.Add(spell.ToReference<BlueprintAbilityReference>());
        }

        /// <summary>
        /// Add a spell to multiple spell lists.
        /// </summary>
        /// <param name="spell">Spell to add.</param>
        /// <param name="spellLists">Sequence of (spell list, spell level) pairs.</param>
        public static void AddToSpellLists(this BlueprintAbility spell, IEnumerable<(BlueprintSpellList, int)> spellLists)
        {
            foreach (var (sl, level) in spellLists)
                spell.AddToSpellList(sl, level);
        }

        /// <summary>
        /// Add spell to spell lists based from existing <see cref="SpellListComponent"/>s.
        /// </summary>
        /// <param name="spell">Spell to add.</param>
        public static void AddToSpellLists(this BlueprintAbility spell) =>
            spell.AddToSpellLists(spell.Components
                .OfType<SpellListComponent>()
                .Select(c => (c.SpellList, c.SpellLevel)));
    }
}
