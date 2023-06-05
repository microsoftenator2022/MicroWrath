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

            context.RegisterSourceOutput(gameActionTypes.Collect().Combine(gameActionType).Combine(compilation), (spc, types) =>
            {
                var (gaTypes, type, compilation) = types.Flatten();

                var sb = new StringBuilder();

                sb.AppendLine($"// {type}");

                sb.AppendLine($@"using System;
using Kingmaker.ElementsSystem;

namespace MicroWrath.GameActions
{{
    internal static class GameActions
    {{
        public static T New<T>(Action<T>? init = null) where T : GameAction, new()
        {{
            var gameAction = new T();
            
            init?.Invoke(gameAction);

            return gameAction;
        }}");

                foreach (var t in gaTypes)
                {
                    sb.AppendLine($"public static {t} {t.Name}(Action<{t}>? init = null) => New<{t}>(init);");
                }

                sb.AppendLine($@"
    }}
}}");

                spc.AddSource("gameActions", sb.ToString());
            });
        }
    }
}
