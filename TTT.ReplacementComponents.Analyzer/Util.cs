using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace TTT.ReplacementComponents.Analyzer;
internal static class Util
{
    internal static IEnumerable<INamespaceSymbol> GetAllNamespaces(INamespaceSymbol root, CancellationToken? ct)
    {
        yield return root;
        foreach (var child in root.GetNamespaceMembers())
            foreach (var next in GetAllNamespaces(child, ct))
            {
                if (ct?.IsCancellationRequested ?? false)
                    yield break;

                yield return next;
            }
    }

    internal static bool IsAssignableType(this INamedTypeSymbol type)
    {
        return type is
        {
            IsStatic: false,
            IsImplicitClass: false,
            IsScriptClass: false,
            TypeKind: TypeKind.Class or TypeKind.Struct,
            DeclaredAccessibility: Accessibility.Public,
        };
    }

    internal static IEnumerable<INamedTypeSymbol> GetAllBaseTypesAndSelf(INamedTypeSymbol type, CancellationToken? ct)
    {
        yield return type;

        if (type.BaseType is INamedTypeSymbol baseType)
            foreach (var t in GetAllBaseTypesAndSelf(baseType, ct))
            {
                if (ct?.IsCancellationRequested ?? false)
                    break;

                yield return t;
            }
    }
}
