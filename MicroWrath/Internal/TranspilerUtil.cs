using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HarmonyLib;

using MicroWrath.Util.Linq;

namespace MicroWrath
{
    internal static class TranspilerUtil
    {
        internal static IEnumerable<CodeInstruction> ReplaceInstructions(
            IEnumerable<CodeInstruction> source,
            IEnumerable<CodeInstruction> match,
            IEnumerable<CodeInstruction> replaceWith)
        {
            MicroLogger.Debug(() =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("Original:");
                foreach (var i in source)
                {
                    sb.AppendLine(i.ToString());
                }

                return sb.ToString();
            });

            MicroLogger.Debug(() =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("Looking for:");
                foreach (var i in match)
                {
                    sb.AppendLine(i.ToString());
                }

                return sb.ToString();
            });

            var matchIndexed = match.Select<CodeInstruction, Func<(int, CodeInstruction), bool>>(m =>
                ((int, CodeInstruction) ici) =>
                    m.opcode == ici.Item2.opcode &&
                    (m.operand is null || m.operand == ici.Item2.operand));

            (int index, CodeInstruction i)[] matchedInstructions = source.Indexed().FindSequence(matchIndexed).ToArray();

            if (!matchedInstructions.Any())
            {
                MicroLogger.Error("Match not found");
            }

            var index = matchedInstructions.First().index;
            MicroLogger.Debug(() => $"Match index: {index}");

            var iList = source.ToList();

            iList.RemoveRange(index, matchedInstructions.Length);
            iList.InsertRange(index, replaceWith);

            MicroLogger.Debug(() =>
            {
                var sb = new StringBuilder();

                sb.AppendLine("Transpiled:");
                foreach (var i in iList)
                {
                    sb.AppendLine(i.ToString());
                }

                return sb.ToString();
            });

            return iList;
        }
    }
}
