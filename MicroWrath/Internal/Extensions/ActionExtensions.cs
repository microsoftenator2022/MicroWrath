using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.ElementsSystem;

using MicroWrath.Util.Linq;

namespace MicroWrath.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ActionList"/>
    /// </summary>
    internal static partial class ActionExtensions
    {
        /// <summary>
        /// Add any number of <see cref="GameAction"/>s to an <see cref="ActionList"/>
        /// </summary>
        /// <param name="aList"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static ActionList Add(this ActionList aList, params GameAction[] actions)
        {
            aList.Actions = aList.Actions.Concat(actions);

            return aList;
        }
    }
}
