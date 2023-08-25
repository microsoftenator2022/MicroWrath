using System;
using System.Collections.Generic;

using Kingmaker.Blueprints.Classes;
using Kingmaker.ElementsSystem;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace MicroWrath
{
    internal static partial class Default
    {
        public static Kingmaker.ElementsSystem.ActionList ActionList => new();
        public static Kingmaker.DialogSystem.CharacterSelection CharacterSelection => new();
        public static Kingmaker.ElementsSystem.ConditionsChecker ConditionsChecker => new() { Conditions = new Kingmaker.ElementsSystem.Condition[0] };
        public static Kingmaker.UnitLogic.Mechanics.ContextDiceValue ContextDiceValue => new()
        {
            DiceType = Kingmaker.RuleSystem.DiceType.Zero,
            DiceCountValue = 0,
            BonusValue = 0
        };
        public static Kingmaker.UnitLogic.Mechanics.ContextValue ContextValue => new();
        public static Kingmaker.DialogSystem.CueSelection CueSelection => new();
        public static Kingmaker.DialogSystem.DialogSpeaker DialogSpeaker => new() { NoSpeaker = true };
        public static Kingmaker.Localization.LocalizedString LocalizedString => new();
        public static Kingmaker.ResourceLinks.PrefabLink PrefabLink => new();
        public static Kingmaker.DialogSystem.Blueprints.ShowCheck ShowCheck => new();
        public static Kingmaker.RuleSystem.Rules.Damage.DamageTypeDescription DamageTypeDescription => new();
        public static Kingmaker.RuleSystem.Rules.Damage.DamageDescription DamageDescription => new()
        {
            Dice = new() { m_Dice = Kingmaker.RuleSystem.DiceType.Zero },
            m_DiceModifiers = new(Kingmaker.RuleSystem.DiceFormula.Zero),
            TypeDescription = DamageTypeDescription
        };
        public static Kingmaker.UnitLogic.Mechanics.ContextDurationValue ContextDurationValue => new()
        {
            DiceCountValue = new(),
            BonusValue = new()
        };
        public static Kingmaker.UnitLogic.Buffs.Polymorph.VisualTransitionSettings VisualTransitionSettings => new()
        {
            OldScaleCurve = new()
            {
                keys = new UnityEngine.Keyframe[]
                {
                    new() { outTangent = 1.0f },
                    new()
                    {
                        time = 1.0f,
                        value = 1.0f,
                        inTangent = 1.0f
                    }
                } 
            },
            NewScaleCurve = new()
            {
                keys = new UnityEngine.Keyframe[]
                {
                    new() { outTangent = 1.0f },
                    new()
                    {
                        time = 1.0f,
                        value = 1.0f,
                        inTangent = 1.0f
                    }
                }
            }
        };

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
