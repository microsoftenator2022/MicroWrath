using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.EventConditionActionSystem.Conditions;
using Kingmaker.ElementsSystem;

using MicroWrath.Util.Linq;

namespace MicroWrath.Extensions
{
    /// <summary>
    /// <see cref="Condition"/> extension methods.
    /// </summary>
    internal static partial class ConditionExtensions
    {
        /// <summary>
        /// Add a <see cref="Condition"/> to a <see cref="ConditionsChecker"/>.
        /// </summary>
        /// <param name="checker"><see cref="ConditionsChecker"/> to add to.</param>
        /// <param name="conditions"><see cref="Condition"/>s to add.</param>
        /// <returns>Modified <see cref="ConditionsChecker"/>.</returns>
        public static ConditionsChecker Add(this ConditionsChecker checker, params Condition[] conditions)
        {
            checker.Conditions = checker.Conditions.Concat(conditions);

            return checker;
        }

        /// <summary>
        /// Add <see cref="Condition"/>s to a <see cref="Conditional"/> action.
        /// </summary>
        /// <param name="conditional"><see cref="Conditional"/> action to add to.</param>
        /// <param name="conditions"><see cref="Condition"/>s to add.</param>
        /// <returns>Modified <see cref="Conditional"/> action.</returns>
        public static Conditional AddCondition(this Conditional conditional, params Condition[] conditions)
        {
            conditional.ConditionsChecker.Add(conditions);

            return conditional;
        }

        /// <summary>
        /// Create an <see cref="OrAndLogic"/> condition requiring all conditions be met.
        /// </summary>
        /// <param name="conditional">First <see cref="Condition"/> to add.</param>
        /// <param name="conditionals">Additional <see cref="Condition"/>s.</param>
        /// <returns><see cref="OrAndLogic"/> that checks the provided <see cref="Condition"/>s.<br/>
        /// <see cref="OrAndLogic"/>.<see cref="ConditionsChecker.Operation"/> = <see cref="Operation.And"/>.</returns>
        public static OrAndLogic And(this Condition conditional, params Condition[] conditionals)
        {
            var oal = new OrAndLogic();
            oal.ConditionsChecker = Default.ConditionsChecker;

            oal.ConditionsChecker.Operation = Operation.And;

            oal.ConditionsChecker.Add(conditional);
            oal.ConditionsChecker.Add(conditionals);

            return oal;
        }

        /// <summary>
        /// Create an <see cref="OrAndLogic"/> condition requiring at least one condition be met.
        /// </summary>
        /// <param name="conditional">First <see cref="Condition"/> to add.</param>
        /// <param name="conditionals">Additional <see cref="Condition"/>s.</param>
        /// <returns><see cref="OrAndLogic"/> that checks the provided <see cref="Condition"/>s.<br/>
        /// <see cref="OrAndLogic"/>.<see cref="ConditionsChecker.Operation"/> = <see cref="Operation.Or"/>.</returns>
        public static OrAndLogic Or(this Condition conditional, params Condition[] conditionals)
        {
            var oal = new OrAndLogic();
            oal.ConditionsChecker = Default.ConditionsChecker;

            oal.ConditionsChecker.Operation = Operation.Or;

            oal.ConditionsChecker.Add(conditional);
            oal.ConditionsChecker.Add(conditionals);

            return oal;
        }
    }
}
