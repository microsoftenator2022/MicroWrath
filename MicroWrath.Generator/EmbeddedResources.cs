using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;

namespace MicroWrath.Generator
{
    [Generator]
    internal class EmbeddedResources : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(pic => 
            {
                foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(n => n.EndsWith(".cs")))
                {
                    using var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name));
                    var str = sr.ReadToEnd();

                    pic.AddSource(name, str);
                }
            });
        }
    }
}
