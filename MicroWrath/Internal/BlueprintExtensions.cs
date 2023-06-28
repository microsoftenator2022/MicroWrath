using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Selection;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Facts;
using Kingmaker.UnitLogic.Abilities.Blueprints;

using MicroWrath.Constructors;
using MicroWrath.Util;
using MicroWrath.Util.Linq;
using HarmonyLib;

namespace MicroWrath.Extensions
{
    internal static partial class BlueprintExtensions
    {
        public static void SetIcon(this BlueprintUnitFact fact, Sprite sprite) => fact.m_Icon = sprite;

        public static void SetIcon(this BlueprintUnitFact fact, string assetID, long fileID)
            => fact.SetIcon(AssetUtils.Direct.GetSprite(assetID, fileID));

        public static void AddComponent<TComponent>(this BlueprintScriptableObject blueprint, TComponent component)
            where TComponent : BlueprintComponent
        {
            var name =
                string.IsNullOrEmpty(component.name) ?
                $"${blueprint.name ?? blueprint.GetType().Name}${component.GetType().Name}" :
                component.name;

            component.name = name;

            for (var i = 2; blueprint.ComponentsArray.Select(c => c.name).Contains(component.name); i++)
                component.name = $"{name}${i}";

            MicroLogger.Debug(() => $"Adding {typeof(TComponent)} to {blueprint.name}", blueprint.ToMicroBlueprint());

            blueprint.ComponentsArray = blueprint.ComponentsArray.Append(component);
        }

        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Func<TComponent, TComponent> init)
            where TComponent : BlueprintComponent, new()
        {
            if (init == default) init = Functional.Identity;

            var component = init(Construct.New.Component<TComponent>());

            AddComponent<TComponent>(blueprint, component);

            return component;
        }

        public static TComponent AddComponent<TComponent>(this BlueprintScriptableObject blueprint, Action<TComponent>? init = default)
            where TComponent : BlueprintComponent, new() => AddComponent<TComponent>(blueprint, c => { init?.Invoke(c); return c; });

        //public static IEnumerable<TComponent> GetComponents<TComponent>(this BlueprintScriptableObject blueprint)
        //    where TComponent : BlueprintComponent =>
        //    blueprint.Components.OfType<TComponent>();

        public static void RemoveComponents(this BlueprintScriptableObject blueprint, Func<BlueprintComponent, bool> predicate) =>
            blueprint.ComponentsArray = blueprint.ComponentsArray.Where(predicate).ToArray();

        public static void RemoveComponent(this BlueprintScriptableObject blueprint, BlueprintComponent component) =>
            blueprint.RemoveComponents(c => c != component);

        public static void AddFeatures(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IEnumerable<IMicroBlueprint<BlueprintFeature>> features)
        {
            var featuresList = selection.m_Features.ToList();
            var allFeaturesList = selection.m_AllFeatures.ToList();

            foreach (var f in features)
            {
                MicroLogger.Debug(() => $"Adding {f} to selection {selection.name}", selection.ToMicroBlueprint());

                if (!featuresList.Contains(f.ToReference()) || allowDuplicates)
                    featuresList.Add(f.ToReference<BlueprintFeature, BlueprintFeatureReference>());

                if (!allFeaturesList.Contains(f.ToReference()) || allowDuplicates)
                    allFeaturesList.Add(f.ToReference<BlueprintFeature, BlueprintFeatureReference>());
            }

            selection.m_Features = featuresList.ToArray();
            selection.m_AllFeatures = allFeaturesList.ToArray();
        }

        public static void AddFeatures(
            this BlueprintFeatureSelection selection,
            IEnumerable<IMicroBlueprint<BlueprintFeature>> features) =>
            AddFeatures(selection, false, features);

        public static void AddFeatures(
            this BlueprintFeatureSelection selection,
            bool allowDuplicates,
            IMicroBlueprint<BlueprintFeature> feature,
            params IMicroBlueprint<BlueprintFeature>[] features) =>
            AddFeatures(selection, allowDuplicates, new[] { feature }.Concat(features));

        public static void AddFeatures(this BlueprintFeatureSelection selection,
            IMicroBlueprint<BlueprintFeature> feature,
            params IMicroBlueprint<BlueprintFeature>[] features) =>
            AddFeatures(selection, false, feature, features);

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

                spellList.SpellsByLevel = spellList.SpellsByLevel.AddToArray(spellListForLevel);
            }

            spellListForLevel.m_Spells.Add(spell.ToReference<BlueprintAbilityReference>());
        }

        public static void AddToSpellLists(this BlueprintAbility spell, IEnumerable<(BlueprintSpellList, int)> spellLists)
        {
            foreach (var (sl, level) in spellLists)
                spell.AddToSpellList(sl, level);
        }

        public static void AddToSpellLists(this BlueprintAbility spell) =>
            spell.AddToSpellLists(spell.Components
                .OfType<SpellListComponent>()
                .Select(c => (c.SpellList, c.SpellLevel)));
    }
}
