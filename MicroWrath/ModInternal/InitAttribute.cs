﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroWrath
{
    [AttributeUsage(validOn: AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class InitAttribute : Attribute
    {
        public readonly int Priority;
    }
}
