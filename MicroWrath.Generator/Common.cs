using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator.Common
{
    internal static class GeneratorUtil
    {
        internal static string ParentDir(string path, int count = 1)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                path = path[..^1];

            while (count > 0)
            {
                path = path[0..(path.LastIndexOf(Path.DirectorySeparatorChar))];
                count--;
            }
                
            return path + Path.DirectorySeparatorChar;
        }

        internal static IEnumerable<string> PathToNamespaceParts(string path, string projectPath, int depth = 0)
        {
            if (File.Exists(path))
                path = path.Replace(Path.GetFileName(path), "");

            var currentDepth = depth < 0 ? depth : 0;

            path = path.Replace(ParentDir(projectPath, 1 - currentDepth), "");

            var parts = path.Split(new[] { Path.DirectorySeparatorChar })
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);

            if (depth > 0 && parts.Count() > depth)
                parts = parts.Skip(depth);

            return parts;
        }

        internal static string PathToNamespace(string path, string projectPath, int depth = 0) =>
            PathToNamespaceParts(path, projectPath, depth).Aggregate((a, b) => $"{a}.{b}");
    }

    internal static class Analyzers
    {
        internal static IEnumerable<INamedTypeSymbol> GetAllGenericInstances(ITypeParameterSymbol type, ImmutableArray<GeneratorSyntaxContext> nodes, CancellationToken ct)
        {
            //Debugger.Launch();

            // Generic method or type containing this type parameter
            var containingSymbol = type.ContainingSymbol;

            var references = Enumerable.Empty<ISymbol>();

            if (type.TypeParameterKind is TypeParameterKind.Method)
                // Get all method invocation symbols
                references = nodes
                    .Where(static sc => sc.Node is InvocationExpressionSyntax)
                    .Select(static sc => sc.SemanticModel.GetSymbolInfo(sc.Node).Symbol!)
                    .OfType<IMethodSymbol>()
                    // Where the method's unbound generic symbol is the same as the containing method
                    .Where(m =>
                    {
                        if (m is null) return false;

                        // Extension methods are not reduced by comparer
                        if (m.MethodKind == MethodKind.ReducedExtension)
                            m = m.ReducedFrom!;

                        return containingSymbol.Equals(m.ConstructedFrom, SymbolEqualityComparer.Default);
                    })
                    // Return the type parameters
                    .SelectMany(static m => m.TypeArguments);

            else if (type.TypeParameterKind is TypeParameterKind.Type)
                // Find all type declaration and generic type name symbols
                references = nodes
                    .Where(static sc => sc.Node is TypeDeclarationSyntax or GenericNameSyntax)
                    .Select(static sc => sc.SemanticModel.GetSymbolInfo(sc.Node).Symbol!)
                    .OfType<INamedTypeSymbol>()
                    // Where the type's unbound generic symbol is the same as the containing type
                    .Where(t =>
                    {
                        if (t is null) return false;

                        return containingSymbol.Equals(t.ConstructedFrom, SymbolEqualityComparer.Default);
                    })
                    // Return the type parameters
                    .SelectMany(static t => t.TypeArguments);

            foreach (var tpr in references)
            {
                if (ct.IsCancellationRequested) yield break;

                if (tpr.Equals(type, SymbolEqualityComparer.Default)) continue;

                if (tpr is INamedTypeSymbol nt)
                {
                    if (nt.IsGenericType)
                    {
                        foreach (var ta in  nt.TypeArguments)
                        {
                            if (ct.IsCancellationRequested) yield break;

                            if (ta is ITypeParameterSymbol ttp)
                            {
                                foreach (var t in GetAllGenericInstances(ttp, nodes, ct))
                                {
                                    if (ct.IsCancellationRequested) yield break;

                                    yield return t;
                                }
                            }
                            else yield return nt;
                        }
                    }
                    else yield return nt;
                }

                if (tpr is ITypeParameterSymbol tp)
                    foreach (var t in GetAllGenericInstances(tp, nodes, ct))
                    {
                        if (ct.IsCancellationRequested) yield break;

                        yield return t;
                    }
            }

        }

        internal static IEnumerable<INamedTypeSymbol> GetBaseTypesAndSelf(this INamedTypeSymbol type)
        {
            yield return type;

            var baseType = type.BaseType;
            if (baseType is not null)
            { 
                foreach (var t in GetBaseTypesAndSelf(baseType))
                    yield return t;
            }
        }

        internal static IEnumerable<INamedTypeSymbol> AllNestedTypesAndSelf(this INamedTypeSymbol type)
        {
            yield return type;
            foreach (var typeMember in type.GetTypeMembers())
            {
                foreach (var nestedType in typeMember.AllNestedTypesAndSelf())
                {
                    yield return nestedType;
                }
            }
        }

        internal static IEnumerable<INamespaceSymbol> GetAllNamespaces(this INamespaceSymbol root)
        {
            yield return root;
            foreach (var child in root.GetNamespaceMembers())
                foreach (var next in GetAllNamespaces(child))
                    yield return next;
        }

        internal static IEnumerable<INamespaceSymbol> GetAllNamespaces(this IAssemblySymbol assembly) =>
            assembly.GlobalNamespace.GetAllNamespaces();

        internal static IEnumerable<INamedTypeSymbol> GetAllTypes(this IEnumerable<INamedTypeSymbol> types, bool includeNested = false)
        {
            if(includeNested) types = types.SelectMany(t => t.AllNestedTypesAndSelf());

            return types.Where(t => t.CanBeReferencedByName);
        }

        internal static IEnumerable<INamedTypeSymbol> GetAssignableTypes(this IEnumerable<INamedTypeSymbol> types, bool includeNested = false)
        {
            return GetAllTypes(types, includeNested)
                .Where((INamedTypeSymbol t) => t is
                {
                    IsStatic: false,
                    IsImplicitClass: false,
                    IsScriptClass: false,
                    TypeKind: TypeKind.Class or TypeKind.Struct,
                    DeclaredAccessibility: Accessibility.Public,
                });
        }

        internal static IEnumerable<INamedTypeSymbol> GetAssignableTypes(this INamespaceSymbol ns, bool includeNested = false) =>
            ns.GetTypeMembers().GetAssignableTypes(includeNested);

        internal static Option<INamedTypeSymbol> TryGetTypeSymbolByName(this IEnumerable<INamedTypeSymbol> types, string name) =>
            types.TryFind(t => t.Name == name);

        internal static Option<INamedTypeSymbol> TryGetTypeSymbolByName(string name, IAssemblySymbol assembly) => 
            assembly.GetAllNamespaces()
                .SelectMany(ns => ns.GetAssignableTypes()).TryGetTypeSymbolByName(name);

        internal static bool HasGenericConstraints(ITypeParameterSymbol tp) =>
            tp.HasConstructorConstraint ||
            tp.HasNotNullConstraint ||
            tp.HasReferenceTypeConstraint ||
            tp.HasUnmanagedTypeConstraint ||
            tp.HasValueTypeConstraint ||
            tp.ConstraintTypes.Any();

        internal static readonly SymbolDisplayFormat FullNameTypeDisplayFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
        internal static string FullName(this ITypeSymbol symbol) => symbol.ToDisplayString(FullNameTypeDisplayFormat);

        internal static readonly SymbolDisplayFormat ShortNameNoGenericsDisplayFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.None);
        internal static string ShortNameNoGenerics(this ISymbol t) =>
            t.ToDisplayString(ShortNameNoGenericsDisplayFormat);

        internal static readonly SymbolDisplayFormat FullNameNoGenericsDisplayFormat =
            new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.None);
        internal static string DisplayStringNoGenerics(this ITypeSymbol t) =>
            t.ToDisplayString(FullNameNoGenericsDisplayFormat);

        internal static string EscapedTypeName(this INamedTypeSymbol symbol) => symbol.DisplayStringNoGenerics().Replace('.', '_');

        //internal static readonly SymbolDisplayFormat FullGenericsDisplayFormat =
        //    new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeConstraints | SymbolDisplayGenericsOptions.IncludeTypeParameters);
        internal static string GenericParametersPart(this INamedTypeSymbol symbol) =>
            symbol.ToDisplayString(new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                SymbolDisplayGenericsOptions.IncludeTypeParameters))
                .Remove(0, symbol.ToDisplayString(ShortNameNoGenericsDisplayFormat).Length);

        internal static string GenericParametersPartNoConstraints(this INamedTypeSymbol symbol) =>
            symbol.ToDisplayString(new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters))
                .Remove(0, symbol.ToDisplayString(ShortNameNoGenericsDisplayFormat).Length);

        internal static string GenericConstraintsPart(this INamedTypeSymbol symbol) =>
            GenericParametersPart(symbol).Remove(0, GenericParametersPartNoConstraints(symbol).Length);
    }

    internal static class Incremental
    {
        internal static IncrementalValuesProvider<ITypeSymbol> GetTypeParameters(
            IncrementalValuesProvider<IMethodSymbol> methodSymbols,
            IncrementalValuesProvider<GeneratorSyntaxContext> syntax) =>
            methodSymbols
                .SelectMany(static (m, _) => m.TypeArguments)
                .Combine(syntax.Collect())
                .SelectMany(static (tai, ct) =>
                {
                    var (ta, invocationsSyntax) = tai;

                    if (ta is not ITypeParameterSymbol tps)
                        return EnumerableExtensions.Singleton(ta);

                    return Analyzers.GetAllGenericInstances(tps, invocationsSyntax, ct);
                });

        internal static IncrementalValuesProvider<IMethodSymbol> GetMethodInvocations(
            string typeName,
            string methodName,
            IncrementalValueProvider<Compilation> compilation,
            IncrementalValuesProvider<(InvocationExpressionSyntax node, IMethodSymbol symbol, SemanticModel sm)> nodesAndSymbols)
        {
            var typeSymbol = compilation
                .Select((c, _) => c.GetTypeByMetadataName(typeName).ToOption());

            var methodSymbol = typeSymbol
                .Select((t, _) =>
                    t.Bind(t => t.GetMembers(methodName).TryHead()
                        .Bind(static m => (m as IMethodSymbol).ToOption())));

            return nodesAndSymbols
                .Combine(methodSymbol)
                .SelectMany(static (nssm, _) =>
                {
                    var ((node, symbol, sm), method) = nssm;

                    return method
                        .Bind<IMethodSymbol, IMethodSymbol>(m =>
                        {
                            if (m.Equals(symbol.ConstructedFrom, SymbolEqualityComparer.Default)) return Option.Some(symbol);

                            return Option.None<IMethodSymbol>();
                        })
                        .ToEnumerable();
                });
        }

        private static IEnumerable<INamedTypeSymbol> GetAssignableTo(
            Compilation compilation,
            IEnumerable<INamedTypeSymbol> types,
            INamedTypeSymbol destination) =>
            types.Where(t => compilation.ClassifyConversion(t, destination).Exists);
        
        private static IEnumerable<INamedTypeSymbol> GetCompilationBlueprintTypes(
            Compilation compilation,
            IEnumerable<INamedTypeSymbol> types) =>
            types.TryFind(t => t.Name == "SimpleBlueprint").ToEnumerable()
                .SelectMany(simpleBlueprint => GetAssignableTo(compilation, types, simpleBlueprint));

        //private static IEnumerable<INamedTypeSymbol> GetCompilationBlueprintComponentTypes(
        //    Compilation compilation,
        //    IEnumerable<INamedTypeSymbol> types) =>
        //    types.TryFind(t => t.Name == "BlueprintComponent").ToEnumerable()
        //        .SelectMany(blueprintComponent => GetAssignableTo(compilation, types, blueprintComponent));

        internal static IncrementalValuesProvider<INamedTypeSymbol> GetAssignableTypes(IncrementalValuesProvider<IAssemblySymbol> assemblies) =>
            assemblies
                .SelectMany(static (a, _) => a.GlobalNamespace.GetAllNamespaces())
                .SelectMany(static (ns, _) => ns.GetAssignableTypes());

        internal static IncrementalValuesProvider<INamedTypeSymbol> GetBlueprintTypes(IncrementalValueProvider<Compilation> compilation)
        {
            var assemblies = compilation
                .SelectMany(static (c, _) => c.SourceModule.ReferencedAssemblySymbols)
                .Where(static a => a.Name == "Assembly-CSharp");

            var types = GetAssignableTypes(assemblies);

            return types
                .Collect()
                .Combine(compilation)
                .SelectMany(static (typesAndCompilation, _) =>
                {
                    var (types, compilation) = typesAndCompilation;

                    return GetCompilationBlueprintTypes(compilation, types);
                });
        }

        internal readonly record struct Config(Option<string> RootNamespace, Option<string> ProjectPath);

        internal static Config GetConfig(AnalyzerConfigOptionsProvider analyzerConfig)
        {
            var options = analyzerConfig.GlobalOptions;

            options.TryGetValue("build_property.projectdir", out string? ppValue);

            var projectPath = ppValue.ToOption();

            options.TryGetValue("build_property.rootnamespace", out string? gnsValue);

            var rootNamespace = gnsValue.ToOption();

            return new Config(rootNamespace, projectPath);
        }

        internal static IncrementalValueProvider<Config> GetConfig(IncrementalValueProvider<AnalyzerConfigOptionsProvider> analyzerConfig) =>
            analyzerConfig.Select((c, _) => GetConfig(c));
    }
}
