using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Enums;
using Kingmaker.Enums.Damage;

using MicroWrath.Util.Linq;

namespace MicroWrath.Util
{
    /// <summary>
    /// Character alignment extension methods
    /// </summary>
    public static class AlignmentExtensions
    {
        /// <summary>
        /// Creates a single <see cref="Alignment"/> value from a collection of <see cref="AlignmentComponent"/> values
        /// </summary>
        public static Alignment ToAlignment(this IEnumerable<AlignmentComponent> components) =>
            components.Aggregate(Alignment.TrueNeutral, (acc, component) => (Alignment)((int)acc | (int)component));

        /// <returns>Collection of <see cref="AlignmentComponent"/> values from a provided <see cref="Alignment"/> value</returns>
        public static IEnumerable<AlignmentComponent> ToComponents(this Alignment alignment) =>
            EnumerableExtensions.Generate<Alignment, AlignmentComponent>(alignment, alignment =>
            {
                foreach (var c in Enum.GetValues(typeof(AlignmentComponent))
                    .OfType<AlignmentComponent>()
                    .Where(c => c is not AlignmentComponent.None))
                {
                    if (((int)c & (int)alignment) != 0)
                        return Option.Some((c, (Alignment)((int) alignment & ~(int)c)));
                }

                return Option<(AlignmentComponent, Alignment)>.None;
            });

        /// <returns>True if <paramref name="alignment"/> contains <see cref="AlignmentComponent.Good"/></returns>
        public static bool IsGood(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Good);

        /// <returns>True if <paramref name="alignment"/> contains <see cref="AlignmentComponent.Evil"/></returns>
        public static bool IsEvil(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Evil);

        /// <returns>True if <paramref name="alignment"/> contains <see cref="AlignmentComponent.Lawful"/></returns>
        public static bool IsLawful(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Lawful);

        /// <returns>True if <paramref name="alignment"/> contains <see cref="AlignmentComponent.Chaotic"/></returns>
        public static bool IsChaotic(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Chaotic);
        
        /// <returns>Equivalent <see cref="DamageAlignment"/> from provided <see cref="AlignmentComponent"/> value</returns>
        public static DamageAlignment ToDamageAlignment(this AlignmentComponent alignmentComponent) => alignmentComponent switch
        {
            AlignmentComponent.Good => DamageAlignment.Good,
            AlignmentComponent.Evil => DamageAlignment.Evil,
            AlignmentComponent.Lawful => DamageAlignment.Lawful,
            AlignmentComponent.Chaotic => DamageAlignment.Chaotic,
            _ => 0
        };

        /// <returns>Collection of <see cref="DamageAlignment"/> values from an <see cref="Alignment"/> value</returns>
        public static IEnumerable<DamageAlignment> ToDamageAlignments(this Alignment alignment) =>
            alignment.ToComponents().Where(c => c is not AlignmentComponent.Neutral).Select(ToDamageAlignment);
    }
}
