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
    internal class GameActions : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;
            
            var gameActionType = compilation.Select((c, _) =>
                    c.SourceModule.ReferencedAssemblySymbols
                        .FirstOrDefault(a => a.Name == "Assembly-CSharp")
                        ?.GetTypeByMetadataName("Kingmaker.ElementsSystem.GameAction"));

            var gameActionTypes = Incremental.GetAssignableTypes(compilation)
                .Combine(compilation)
                .Combine(gameActionType)
                //.Combine(compilation.Select((c, _) => c.GetTypeByMetadataName("System.Object")))
                .Where(typeCompilationGameActionType =>
                {
                    var (type, compilation, gameActionType) = typeCompilationGameActionType.Flatten();

                    if (gameActionType == null) return false;

                    if (type.IsAbstract) return false;
                    if (!type.Constructors.Any(m => m.Parameters.Length == 0)) return false;

                    //if (type.Equals(objectType, SymbolEqualityComparer.Default)) return false;
                    
                    return type.GetBaseTypesAndSelf().Contains(gameActionType, SymbolEqualityComparer.Default);
                })
                .Select((tuple, _) => tuple.Flatten().Item1);

            var defaultValuesType = compilation
                .Select(static (c, _) => c.Assembly.GetTypeByMetadataName("MicroWrath.Default").ToOption());

            var initializers = BlueprintConstructor.GetTypeMemberInitialValues(gameActionTypes, defaultValuesType);

            context.RegisterSourceOutput(initializers.Collect().Combine(compilation), (spc, types) =>
            {
                var (initializers, compilation) = types;

                var sb = new StringBuilder();

                //sb.AppendLine($"// {type}");

                sb.AppendLine($@"using System;
using Kingmaker.ElementsSystem;
using MicroWrath.Util;

namespace MicroWrath
{{
    internal static partial class GameActions
    {{");

                foreach (var i in initializers)
                {
                    if (spc.CancellationToken.IsCancellationRequested)
                        break;

                    var t = i.ContainingType;

                    sb.AppendLine(@$"
        public static {t} {t.Name}(Action<{t}>? init = null)
        {{
            var gameAction = {BlueprintConstructor.GetInitializerExpression(i)};

            init?.Invoke(gameAction);

            return gameAction;
        }}");
                }

                sb.AppendLine($@"
    }}
}}");

                spc.AddSource("GameActions", sb.ToString());
            });
        }
    }
}
