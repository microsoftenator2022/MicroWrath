#pragma warning disable CS0649

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Parts;

using Newtonsoft.Json;

namespace MicroWrath.Extensions
{
#if false
    [TypeId("bcdeed45-bf79-4a02-b66c-41a125dbe44e")]
#endif
    internal partial class AddActivatableAbilityGroup : UnitFactComponentDelegate
    {
        public int Group;

        public override void OnFactAttached()
        {
            var group = new ExtraActivatableAbilityGroup(this.Group);

            if (base.Fact?.Blueprint is BlueprintActivatableAbility aa)
            {
                aa.Group = group;
            }
        }
    }

    /// <summary>
    /// Extends <see cref="ActivatableAbilityGroup" /> to accept new values.
    /// </summary>
    internal readonly partial struct ExtraActivatableAbilityGroup
    {
        /// <summary>
        /// Base group sizes
        /// </summary>
        internal static readonly Dictionary<uint, int> Groups = new();

        public readonly uint GroupId;

        internal class UnitPart : OldStyleUnitPart
        {
            [JsonProperty]
            public Dictionary<uint, int> GroupSizeIncreases = [];
        }

        static IEnumerable<ActivatableAbilityGroup> EnumValues =>
            Enum.GetValues(typeof(ActivatableAbilityGroup)).Cast<ActivatableAbilityGroup>();

        static void CheckEnumValues(uint groupId)
        {
            if (EnumValues.Select(g => (uint)g).Contains(groupId))
                throw new ArgumentException($"{nameof(ActivatableAbilityGroup)} already contains id {groupId} ({(ActivatableAbilityGroup)groupId})");
        }

        public ExtraActivatableAbilityGroup(uint groupId, int size = 1)
        {
            CheckEnumValues(groupId);
            
            this.GroupId = groupId;

            if (!Groups.ContainsKey(groupId))
                Add(groupId, size);
            else
                Groups[groupId] = Math.Max(size, Groups[groupId]);
        }
        
        public ExtraActivatableAbilityGroup(int groupId, int size = 1) : this(unchecked((uint)groupId), size) { }

        public static void Add(uint groupId, int size)
        {
            CheckEnumValues(groupId);

            Groups.Add(groupId, size);
        }

        public static implicit operator ActivatableAbilityGroup(ExtraActivatableAbilityGroup extraGroup) =>
            (ActivatableAbilityGroup)extraGroup.GroupId;

        /// <exclude />
        static int GetGroupSizeIncrease(uint groupId, UnitDescriptor owner)
        {
            if (owner.Get<ExtraActivatableAbilityGroup.UnitPart>() is { } unitPart &&
                unitPart.GroupSizeIncreases.TryGetValue((uint)groupId, out var increase))
                return increase;

            return 0;
        }

        /// <exclude />
        static void SetGroupSizeIncrease(uint groupId, UnitDescriptor owner, int increase)
        {
            var unitPart = owner.Ensure<ExtraActivatableAbilityGroup.UnitPart>();

            if (increase == 0)
            {
                _ = unitPart.GroupSizeIncreases.Remove(groupId);

                if (unitPart.GroupSizeIncreases.Count == 0)
                    owner.Remove<ExtraActivatableAbilityGroup.UnitPart>();

                return;
            }

            unitPart.GroupSizeIncreases[groupId] = increase;
        }

        /// <exclude />
        static bool TryGetGroupSize(ActivatableAbilityGroup groupId, UnitDescriptor owner , out int value)
        {
            var result = Groups.TryGetValue((uint)groupId, out value);

            if (result)
            {
                value += GetGroupSizeIncrease((uint)groupId, owner);
            }

            return result;
        }

        [HarmonyPatch(typeof(UnitPartActivatableAbility))]
        static class ActivatableAbilityGroupSize_Patch
        {
            [HarmonyPatch(nameof(UnitPartActivatableAbility.GetGroupSize))]
            [HarmonyPrefix]
            static bool GetGroupSize_Prefix(ActivatableAbilityGroup group, UnitPartActivatableAbility __instance, ref int __result)
            {
                if (EnumValues.Contains(group))
                    return true;

                if (TryGetGroupSize(group, __instance.Owner, out var value))
                {
                    __result = value;

                    return false;
                }

                return true;
            }

            [HarmonyPatch(nameof(UnitPartActivatableAbility.IncreaseGroupSize))]
            [HarmonyPrefix]
            static bool IncreaseGroupSize_Prefix(ActivatableAbilityGroup group, UnitPartActivatableAbility __instance)
            {
                if (EnumValues.Contains(group) || !Groups.ContainsKey((uint)group))
                    return true;

                SetGroupSizeIncrease((uint)group, __instance.Owner,
                    GetGroupSizeIncrease((uint)group, __instance.Owner) + 1);

                return false;
            }

            [HarmonyPatch(nameof(UnitPartActivatableAbility.DecreaseGroupSize))]
            [HarmonyPrefix]
            static bool DecreaseGroupSize_Prefix(ActivatableAbilityGroup group, UnitPartActivatableAbility __instance)
            {
                if (EnumValues.Contains(group) || !Groups.ContainsKey((uint)group))
                    return true;

                SetGroupSizeIncrease((uint)group, __instance.Owner,
                    GetGroupSizeIncrease((uint)group, __instance.Owner) - 1);

                return false;
            }
        }
    }
}
