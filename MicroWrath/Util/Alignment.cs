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
    public static class AlignmentExtensions
    {
        public static Alignment ToAlignment(this IEnumerable<AlignmentComponent> components) =>
            components.Aggregate(Alignment.TrueNeutral, (acc, component) => (Alignment)((int)acc | (int)component));

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

public static bool IsGood(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Good);
        public static bool IsEvil(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Evil);
        public static bool IsLawful(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Lawful);
        public static bool IsChaotic(this Alignment alignment) => alignment.ToComponents().Contains(AlignmentComponent.Chaotic);
        
        public static DamageAlignment ToDamageAlignment(this AlignmentComponent alignmentComponent) => alignmentComponent switch
        {
            AlignmentComponent.Good => DamageAlignment.Good,
            AlignmentComponent.Evil => DamageAlignment.Evil,
            AlignmentComponent.Lawful => DamageAlignment.Lawful,
            AlignmentComponent.Chaotic => DamageAlignment.Chaotic,
            _ => 0
        };

        public static IEnumerable<DamageAlignment> ToDamageAlignments(this Alignment alignment) =>
            alignment.ToComponents().Where(c => c is not AlignmentComponent.Neutral).Select(ToDamageAlignment);
    }
}
