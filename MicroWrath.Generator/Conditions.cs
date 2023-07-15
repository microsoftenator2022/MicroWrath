using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using MicroWrath.Generator.Common;
using MicroWrath.Util;

namespace MicroWrath.Generator
{
    [Generator]
    internal class Conditions : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;

            var conditionType = compilation.Select((c, _) =>
                    c.SourceModule.ReferencedAssemblySymbols
                        .FirstOrDefault(a => a.Name == "Assembly-CSharp")
                        ?.GetTypeByMetadataName("Kingmaker.ElementsSystem.Condition"));

            var conditionTypes = Incremental.GetAssignableTypes(compilation)
                .Combine(compilation)
                .Combine(conditionType)
                //.Combine(compilation.Select((c, _) => c.GetTypeByMetadataName("System.Object")))
                .Where(typeCompilationConditionType =>
                {
                    var (type, compilation, conditionType) = typeCompilationConditionType.Flatten();

                    if (conditionType == null) return false;

                    if (type.IsAbstract) return false;
                    if (!type.Constructors.Any(m => m.Parameters.Length == 0)) return false;

                    //if (type.Equals(objectType, SymbolEqualityComparer.Default)) return false;

                    return type.GetBaseTypesAndSelf().Contains(conditionType, SymbolEqualityComparer.Default);
                })
                .Select((tuple, _) => tuple.Flatten().Item1);

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initializers = BlueprintConstructor.GetTypeMemberInitialValues(conditionTypes, defaultValuesType);

            context.RegisterSourceOutput(initializers.Collect().Combine(compilation), (spc, types) =>
            {
                var (initializers, compilation) = types;

                var sb = new StringBuilder();

                sb.AppendLine($@"using System;
using Kingmaker.ElementsSystem;
using MicroWrath.Util;

namespace MicroWrath
{{
    internal static class Conditions
    {{");
        //public static T New<T>(Action<T>? init = null) where T : Condition, new()
        //{{
        //    var condition = new T();
            
        //    init?.Invoke(condition);

        //    return condition;
        //}}");

                foreach (var i in initializers)
                {
                    var t = i.ContainingType;

                    sb.AppendLine(@$"
        public static {t} {t.Name}(Action<{t}>? init = null)
        {{
            var condition = {BlueprintConstructor.GetInitializerExpression(i)};

            init?.Invoke(condition);

            return condition;
        }}");
                }

                sb.AppendLine($@"
    }}
}}");

                spc.AddSource("Conditions", sb.ToString());
            });
        }
    }
}
