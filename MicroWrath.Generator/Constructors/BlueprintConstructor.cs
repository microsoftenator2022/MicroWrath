﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Generator.Common;
using MicroWrath.Util;
using MicroWrath.Util.Linq;

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

            CreateBlueprintConstructors(compilation, syntax, context);

            CreateComponentConstructors(compilation, syntax, context, 
                allowedComponents
                    .Collect()
                    .SelectMany((bpts, _) => bpts
                        .SelectMany(bpt => bpt.componentTypes)
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)));

            GenerateAllowedComponentsConstructors(context, allowedComponents);
        }
    }
}
