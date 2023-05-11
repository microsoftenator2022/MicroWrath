using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.ElementsSystem;

using MicroWrath.Util.Linq;

namespace MicroWrath
{
    internal static partial class ConditionExtensions
    {
        public static ConditionsChecker Add(this ConditionsChecker checker, params Condition[] conditions)
        {
            checker.Conditions = checker.Conditions.Concat(conditions);

            return checker;
        }
    }
}
