﻿using System;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace MicroWrath.Generator
{
    [Generator]
    internal partial class BlueprintConstructor : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var compilation = context.CompilationProvider;

            var sp = context.SyntaxProvider;

            var syntax = context.SyntaxProvider.CreateSyntaxProvider(
                static (sn, _) => sn is InvocationExpressionSyntax or GenericNameSyntax or TypeDeclarationSyntax or MethodDeclarationSyntax,
                static (sc, _) => sc);

            var allowedComponents = GetAllowedComponents(compilation);

            GenerateAllowedComponentsConstructors(context, allowedComponents);

            CreateBlueprintConstructors(compilation, syntax, context);

            CreateComponentConstructors(compilation, syntax, context, 
                allowedComponents
                    .Collect()
                    .SelectMany((bpts, _) => bpts
                        .SelectMany(bpt => bpt.componentTypes)
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)));
        }

        internal static void AddSource(SourceProductionContext spc, INamedTypeSymbol typeSymbol, string content)
        {
            var name = typeSymbol.Name;

            if (spc.CancellationToken.IsCancellationRequested)
                return;

            while (typeSymbol.ContainingType != null)
            {
                if (spc.CancellationToken.IsCancellationRequested)
                    return;

                name = $" {typeSymbol.ContainingType.Name}.{name}";
                typeSymbol = typeSymbol.ContainingType;
            }

            spc.AddSource(name, content);
        }
    }
}
