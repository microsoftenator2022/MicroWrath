﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MicroWrath
{
    /// <summary>
    /// Encyclopedia Utils
    /// </summary>
    public static class Encyclopedia
    {
        /// <summary>
        /// Link to an encyclopedia page. See: <see cref="Page"/>
        /// </summary>
        public readonly struct Link
        {
            /// <summary>
            /// Linked page
            /// </summary>
            public readonly Page Page;

            /// <summary>
            /// Link text
            /// </summary>
            public readonly string LinkText;

#pragma warning disable CS1591
            public Link(Page page, string linkText)
            {
                Page = page;
                LinkText = linkText;
            }

            public override string ToString()
                => $"{{g|Encyclopedia:{Enum.GetName(typeof(Page), this.Page)}}}{LinkText}{{/g}}";
#pragma warning restore
        }

#pragma warning disable CS1591
        public enum Page
        {
            AbilityDamage,
            Ability_Scores,
            Abjuration,
            Adapted_rules,
            Alignment,
            Armor_Check_Penalty,
            Armor_Class,
            Athletics,
            Attack,
            Attack_Of_Opportunity,
            BAB,
            Bonus,
            Buffs,
            Camera,
            Cantrips_Orisons,
            Caster_Level,
            Casting_Time,
            CA_Types,
            Character_Class,
            Character_Creation,
            Character_Level,
            Charge,
            Charisma,
            Check,
            Class,
            Class_Level,
            CMB,
            CMD,
            Combat,
            Combat_Maneuvers,
            Combat_Modifiers,
            Combat_Round,
            Combat_Splash,
            Companions,
            Concealment,
            Concentration,
            Conjuration,
            Constitution,
            Core_rules,
            Critical,
            Damage,
            Damage_Reduction,
            Damage_Type,
            DC,
            Dexterity,
            Dialogue_Skill_Checks,
            Dice,
            Divination,
            Enchantment,
            Energy_Damage,
            Energy_Immunity,
            Energy_Resistance,
            Energy_Vulnerability,
            Equip,
            Evocation,
            Fast_Healing,
            Feat,
            Flanking,
            Flat_Footed,
            Flat_Footed_AC,
            Formations,
            Free_Action,
            Full_Round_Action,
            Healing,
            Health_and_Death,
            Helpless,
            Hit_Dice,
            HP,
            Illusion,
            Incorporeal_Touch_Attack,
            Initiative,
            Injury_Death,
            Inspect,
            Intelligence,
            Journal,
            Knowledge_Arcana,
            Knowledge_World,
            Level_Up,
            Light_Weapon,
            Lore_Nature,
            Lore_Religion,
            Magic_School,
            Map_Movement,
            Max_Dex_Bonus,
            MeleeAttack,
            Mobility,
            Moral_Choices,
            Movement,
            Move_Action,
            NaturalAttack,
            Necromancy,
            Penalty,
            Perception,
            Persuasion,
            Physical_Damage,
            Race,
            Range,
            RangedAttack,
            Reach,
            Regeneration,
            Rest,
            Safe_Location,
            Saving_Throw,
            Saving_Throws_Results,
            Scrolls,
            Shooting_into_Melee,
            Size,
            Skills,
            Special_Abilities,
            Special_Attacks,
            Special_Movement,
            Speed,
            Spell,
            Spells,
            Spell_Descriptions,
            Spell_Descriptor,
            Spell_Fail_Chance,
            Spell_Resistance,
            Spell_Target,
            Standard_Actions,
            Stealth,
            Stories,
            Strength,
            Surprise,
            Swift_Action,
            Tactical_Movement,
            Temporary_HP,
            Terrain_Obstacles,
            Threatened_Area,
            TouchAttack,
            Touch_AC,
            Trait,
            Transmutation,
            Traps,
            Trickery,
            Trophies,
            //Tutorial_10_AbilityUse,
            //Tutorial_11_Battle,
            //Tutorial_12_HiddenObj,
            //Tutorial_13_SkillCheckObj,
            //Tutorial_14_DiceRoll,
            //Tutorial_15_Log,
            //Tutorial_16_Attack,
            //Tutorial_17_Exit,
            //Tutorial_18_LevelUp,
            //Tutorial_19_Trader,
            //Tutorial_1_Move,
            //Tutorial_20_Rest,
            //Tutorial_21_Potions,
            //Tutorial_22_BreakChest,
            //Tutorial_22_ConsumableLockpick,
            //Tutorial_22_Traps,
            //Tutorial_23_Inspect,
            //Tutorial_24_DamageReduction,
            //Tutorial_25_Camping,
            //Tutorial_26_Alignment,
            //Tutorial_2_Camera,
            //Tutorial_30_GM_Kenabres,
            //Tutorial_3_Interactions,
            //Tutorial_40_CameraDemonCity,
            //Tutorial_4_Journal,
            //Tutorial_5_DialogSkillcheck,
            //Tutorial_6_Party,
            //Tutorial_7_LootCrates,
            //Tutorial_8_Loot,
            //Tutorial_9_1_Light,
            //Tutorial_9_Equip,
            //Tutorial_Crusade_01_Chapter2Intro,
            //Tutorial_Crusade_02_Battle,
            //Tutorial_Crusade_03_General,
            //Tutorial_Crusade_04_GeneralInCombat,
            //Tutorial_Crusade_05_Hiring,
            //Tutorial_Crusade_06_ArmyManagement,
            //Tutorial_Crusade_07_Morale,
            //Tutorial_Crusade_08_Chapter3Intro,
            //Tutorial_Crusade_09_Kingdom,
            //Tutorial_Crusade_10_Edicts,
            //Tutorial_Crusade_11_Mercs,
            //Tutorial_Crusade_12_Chapter3Morale,
            //Tutorial_Crusade_13_Regions,
            //Tutorial_Crusade_14_Buildings,
            //Tutorial_Finnean,
            //Tutorial_OptimalFormation,
            TwoWeapon_Fighting,
            UnarmedAttack,
            Use_Magic_Device,
            Weapon_Proficiency,
            Weapon_Range,
            Wisdom,
            XP,
        }
#pragma warning restore
    }
}
