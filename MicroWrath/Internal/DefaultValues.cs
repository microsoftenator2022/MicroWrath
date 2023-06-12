using System;

using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace MicroWrath
{
    internal static partial class Default
    {
        public static Kingmaker.ElementsSystem.ActionList ActionList =>
            new Kingmaker.ElementsSystem.ActionList();
        public static Kingmaker.DialogSystem.CharacterSelection CharacterSelection => new Kingmaker.DialogSystem.CharacterSelection();
        public static Kingmaker.ElementsSystem.ConditionsChecker ConditionsChecker =>
            new Kingmaker.ElementsSystem.ConditionsChecker() { Conditions = new Kingmaker.ElementsSystem.Condition[0] };
        public static Kingmaker.UnitLogic.Mechanics.ContextDiceValue ContextDiceValue => new Kingmaker.UnitLogic.Mechanics.ContextDiceValue()
        {
            DiceType = Kingmaker.RuleSystem.DiceType.Zero,
            DiceCountValue = 0,
            BonusValue = 0
        };
        public static Kingmaker.DialogSystem.CueSelection CueSelection => new Kingmaker.DialogSystem.CueSelection();
        public static Kingmaker.DialogSystem.DialogSpeaker DialogSpeaker => new Kingmaker.DialogSystem.DialogSpeaker() { NoSpeaker = true };
        public static Kingmaker.Localization.LocalizedString LocalizedString => new Kingmaker.Localization.LocalizedString();
        public static Kingmaker.ResourceLinks.PrefabLink PrefabLink => new Kingmaker.ResourceLinks.PrefabLink();
        public static Kingmaker.DialogSystem.Blueprints.ShowCheck ShowCheck => new Kingmaker.DialogSystem.Blueprints.ShowCheck();

        public static BlueprintFeature IsClassFeature(BlueprintFeature feature)
        {
            feature.IsClassFeature = true;

            return feature;
        }

        public static BlueprintBuff IsClassFeature(BlueprintBuff buff)
        {
            buff.IsClassFeature = true;

            return buff;
        }

        public static Element Name(Element element)
        {
            element.name = $"${element.GetType().Name}${System.Guid.NewGuid():N}$";

            return element;
        }
    }
}
