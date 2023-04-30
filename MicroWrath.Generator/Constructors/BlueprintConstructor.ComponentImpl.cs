using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

using static MicroWrath.Generator.Constants;

namespace MicroWrath.Generator
{
    internal partial class BlueprintConstructor
    {
        internal static string ComponentConstructorPart(
            INamedTypeSymbol componentType,
            ImmutableArray<(IFieldSymbol field, IFieldSymbol init)> initFields,
            ImmutableArray<(IPropertySymbol property, IFieldSymbol init)> initProperties,
            ImmutableArray<IMethodSymbol> initMethods)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using Kingmaker.Blueprints;");
            sb.AppendLine("using MicroWrath.Util;");

            var ns = componentType.ContainingNamespace;

            var name = componentType.Name;

            if (componentType.ContainingType != null)
            {
                name = componentType.GetContainingTypes().Reverse()
                    .Select(t => t.Name)
                    .Aggregate((acc, next) => $"{acc}.{next}") + "." + name;
            }

            if (ns.ToString() != "Kingmaker.Blueprints")
                sb.AppendLine($"using {ns};");

            sb.Append($@"
namespace {ConstructorNamespace}
{{
    internal static partial class {ConstructClassName}
    {{
        private partial class ComponentConstructor : IComponentConstructor<{name}>
        {{
            {name} IComponentConstructor<{name}>.New() =>
                new {name}()
                {{");

            foreach (var (f, init) in initFields)
            {
                sb.Append($@"
                    {f.Name} = {init},");
            }

            foreach (var (p, init) in initProperties)
            {
                sb.Append($@"
                    {p.Name} = {init},");
            }

            sb.Append($@"
                }}");

            foreach (var m in initMethods)
            {
                sb.Append($@"
                .Apply({m.ContainingType}.{m.Name}).Downcast<{m.ReturnType}, {name}>()");
            }

            sb.Append($@";
        }}
    }}
}}");

            return sb.ToString();
        }
    }

}
