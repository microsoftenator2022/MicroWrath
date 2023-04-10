using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;

using MicroWrath.Generator.Common;

using static MicroWrath.Generator.Constants;

namespace MicroWrath.Generator
{
    internal partial class BlueprintConstructor
    {
        internal static string BlueprintConstructorPart(
            INamedTypeSymbol bpType,
            ImmutableArray<(IFieldSymbol field, IFieldSymbol init)> initFields,
            ImmutableArray<(IPropertySymbol property, IFieldSymbol init)> initProperties,
            ImmutableArray<IMethodSymbol> initMethods)
        {
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using Kingmaker.Blueprints;");
            sb.AppendLine("using MicroWrath.Util;");

            var ns = bpType.ContainingNamespace;

            sb.AppendLine($"using {ns};");

            sb.Append($@"
namespace {ConstructorNamespace}
{{
    internal static partial class {ConstructClassName}
    {{
        private partial class BlueprintConstructor : IBlueprintConstructor<{bpType.Name}>
        {{
            {bpType.Name} IBlueprintConstructor<{bpType.Name}>.New(string assetId, string name) =>
                new {bpType.Name}()
                {{
                    AssetGuid = BlueprintGuid.Parse(assetId),
                    name = name,");
            
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
                .Apply({m.ContainingType}.{m.Name}).Downcast<{m.ReturnType},{bpType.Name}>()");
            }

            sb.Append($@";
        }}
    }}
}}");

            return sb.ToString();
        }
    }
}
