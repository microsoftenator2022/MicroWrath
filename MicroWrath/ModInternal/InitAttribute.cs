using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MicroWrath
{
    /// <summary>
    /// Run this static void method on mod init. <br/> Note: Runs before harmony patches are applied.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class InitAttribute : Attribute
    {
#pragma warning disable CS0649
        public readonly int Priority;
#pragma warning restore CS0649
    }
}
