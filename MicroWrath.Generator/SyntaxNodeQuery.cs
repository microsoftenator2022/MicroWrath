using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;

using static MicroWrath.Generator.Extensions.SyntaxNodeQuery;

namespace MicroWrath.Generator.Extensions
{
    internal static class SyntaxNodeQuery
    {
        public static T? TryFindInChildren<T>(this SyntaxNode node, int position, Func<T, bool>? predicate = null) where T : SyntaxNode
        {
            predicate ??= _ => true;

            if (position < 0) return null;

            if (node.ChildThatContainsPosition(position).AsNode() is SyntaxNode sn)
            {
                if (sn is T t && predicate(t))
                    return t;
                
                return TryFindInChildren(sn, position, predicate);
            }

            if (node is T n && predicate(n))
                return n;

            return null;
        }

        public static U? TryPickInChildren<T, U>(this SyntaxNode node, int position, Func<T, U?>? predicate = null) where T : SyntaxNode
        {
            predicate ??= _ => default;

            if (position < 0) return default;

            if (node.ChildThatContainsPosition(position).AsNode() is SyntaxNode sn)
            {
                if (sn is T t && predicate(t) is U u)
                    return u;

                return TryPickInChildren(sn, position, predicate);
            }

            if (node is T n && predicate(n) is U nu)
                return nu;

            return default;
        }
    }
}
