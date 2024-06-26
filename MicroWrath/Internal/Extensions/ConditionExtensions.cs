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
    internal static partial class ConditionExtensions
    {
        public static ConditionsChecker Add(this ConditionsChecker checker, params Condition[] conditions)
        {
            checker.Conditions = checker.Conditions.Concat(conditions);

            return checker;
        }

        public static Conditional AddCondition(this Conditional conditional, params Condition[] conditions)
        {
            conditional.ConditionsChecker.Add(conditions);

            return conditional;
        }

        public static OrAndLogic And(this Condition conditional, params Condition[] conditionals)
        {
            var oal = new OrAndLogic();
            oal.ConditionsChecker = Default.ConditionsChecker;

            oal.ConditionsChecker.Operation = Operation.And;

            oal.ConditionsChecker.Add(conditional);
            oal.ConditionsChecker.Add(conditionals);

            return oal;
        }

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
