using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HarmonyLib;

using MicroWrath.Util.Linq;

namespace MicroWrath.Util
{
    internal static class TranspilerUtil
    {
        /// <summary>
        /// Tries to locate a range of instructions matching a sequence of predicates.
        /// May return a subset (TODO: verify this) so you should check the length.
        /// </summary>
        /// <param name="instructions">Sequence of <see cref="CodeInstruction"/>s to search.</param>
        /// <param name="matchFuncs">Sequence of predicates to match.</param>
        /// <param name="start">Start index.</param>
        public static IEnumerable<(int index, CodeInstruction instruction)> FindInstructionsIndexed(
            this IEnumerable<CodeInstruction> instructions,
            IEnumerable<Func<CodeInstruction, bool>> matchFuncs,
            int start = 0)
        {
            var matched = instructions
                .Indexed()
                .Skip(start)
                .FindSequence(matchFuncs
                    .Select<Func<CodeInstruction, bool>, Func<(int index, CodeInstruction item), bool>>(f =>
                        i => f(i.item)));

            if (!matched.Any()) return Enumerable.Empty<(int, CodeInstruction)>();

            return matched;
        }

        /// <summary>
        /// Replaces a matched sequence of instructions.
        /// </summary>
        /// <param name="source">Sequence of <see cref="CodeInstruction"/>s to search.</param>
        /// <param name="match">Sequence of instructions to match (by opcode and operand).</param>
        /// <param name="replaceWith"></param>
        /// <returns></returns>
        public static IEnumerable<CodeInstruction> ReplaceInstructions(
            IEnumerable<CodeInstruction> source,
            IEnumerable<CodeInstruction> match,
            IEnumerable<CodeInstruction> replaceWith)
        {
            var matchIndexed = match.Select<CodeInstruction, Func<(int, CodeInstruction), bool>>(m =>
                ((int, CodeInstruction instruction) ici) =>
                    m.opcode == ici.instruction.opcode &&
                    (m.operand is null || m.operand == ici.instruction.operand));

            (int index, CodeInstruction i)[] matchedInstructions = source.Indexed().FindSequence(matchIndexed).ToArray();

            if (!matchedInstructions.Any())
            {
                return Enumerable.Empty<CodeInstruction>();
            }

            var index = matchedInstructions.First().index;

            var iList = source.ToList();

            iList.RemoveRange(index, matchedInstructions.Length);
            iList.InsertRange(index, replaceWith);


            return iList;
        }
    }
}
