using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Kingmaker.UnitLogic.ActivatableAbilities;
using Kingmaker.UnitLogic.Parts;

namespace MicroWrath.Extensions
{
    /// <summary>
    /// Extends <see cref="ActivatableAbilityGroup" /> to accept new values.
    /// This class is partial so you may place your new values as fields within.
    /// </summary>
    internal readonly partial struct ExtraActivatableAbilityGroup
    {
        public readonly int Value;

        public ExtraActivatableAbilityGroup(int value)
        {
            Value = value;

            if (!Groups.ContainsKey((ActivatableAbilityGroup)value))
                Add(value, 1);
        }

        public ExtraActivatableAbilityGroup(uint value) : this(unchecked((int)value)) { }

        internal static readonly Dictionary<ActivatableAbilityGroup, int> Groups = new();

        public static void Add(int group, int size)
        {
            if (Enum.GetValues(typeof(ActivatableAbilityGroup)).Cast<int>().Contains(group))
                throw new InvalidOperationException("Value exists in original enum");

            Groups.Add((ActivatableAbilityGroup)group, size);
        }

        public static implicit operator ActivatableAbilityGroup(ExtraActivatableAbilityGroup extraGroup) => (ActivatableAbilityGroup)extraGroup.Value;

        [HarmonyPatch(typeof(UnitPartActivatableAbility))]
        static class ActivatableAbilityGroupSize_Patch
        {
            [HarmonyPatch(nameof(UnitPartActivatableAbility.GetGroupSize))]
            [HarmonyPrefix]
            static bool GetGroupSize_Prefix(ActivatableAbilityGroup group, ref int __result)
            {
                if (Groups.TryGetValue(group, out var size))
                {
                    __result = size;
                    return false;
                }

                return true;
            }
        }
    }
}
