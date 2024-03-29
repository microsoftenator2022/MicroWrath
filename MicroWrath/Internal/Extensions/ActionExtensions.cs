﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.ElementsSystem;

using MicroWrath.Util.Linq;

namespace MicroWrath.Extensions
{
    internal static partial class ActionExtensions
    {
        public static ActionList Add(this ActionList aList, params GameAction[] actions)
        {
            aList.Actions = aList.Actions.Concat(actions);

            return aList;
        }
    }
}
