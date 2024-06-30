using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace TTT.ReplacementComponents.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public partial class TTTReplacementAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(this.AnalyeObjectCreation, OperationKind.ObjectCreation);
            context.RegisterOperationAction(this.AnalyzeBPCoreConfigurators, OperationKind.Invocation);
        }

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
#pragma warning disable RS2008 // Enable analyzer release tracking
            nameof(TTTReplacementAnalyzer),
#pragma warning restore RS2008 // Enable analyzer release tracking
            "Possible TTT Replacement Component",
            "Component type '{0}' has possible TTT replacement '{1}'",
            "TabletopTweaks",
            DiagnosticSeverity.Info,
            true);

        private static string[] TTTComponentNames = [];

        public static IEnumerable<INamedTypeSymbol> GetOwlcatReplacementTypes(Compilation compilation, CancellationToken? ct = null)
        {
            IAssemblySymbol? ass = null;

            foreach (var refAss in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                try
                {
                    if (refAss.Name == "TabletopTweaks-Core")
                        ass = refAss;
                }
                catch
                {
                    continue;
                }
            }

            if (ass is null)
                yield break;

            var namespaces = Util.GetAllNamespaces(ass.GlobalNamespace, ct)
                .Where(ns => ns.ToString().StartsWith("TabletopTweaks.Core.NewComponents.OwlcatReplacements"))
                .ToArray();

            var assignableTypes = namespaces
                .SelectMany(ns => ns.GetTypeMembers())
                .Where(Util.IsAssignableType)
                .ToArray();

            foreach (var t in assignableTypes)
            {
                if (ct?.IsCancellationRequested ?? false)
                    break;

                yield return t;
            }
        }

        public static INamedTypeSymbol? TryGetTTTReplacement(
            INamedTypeSymbol typeSymbol,
            Compilation compilation,
            CancellationToken? ct = null)
        {
            if (TTTComponentNames.Length == 0)
                TTTComponentNames = GetOwlcatReplacementTypes(compilation, ct).Select(t => t.Name.ToString()).ToArray();
            
            var name = TTTComponentNames.FirstOrDefault(tName => tName == $"{typeSymbol.Name}TTT" || tName == $"TT{typeSymbol.Name}");

            if (name is null)
                return null;

            return GetOwlcatReplacementTypes(compilation, ct).First(t => t.Name == name);
        }

        public static INamedTypeSymbol? TryGetTTTReplacement(
            string typeName,
            Compilation compilation,
            CancellationToken? ct = null)
        {
            if (TTTComponentNames.Length == 0)
                TTTComponentNames = GetOwlcatReplacementTypes(compilation, ct).Select(t => t.Name.ToString()).ToArray();

            var name = TTTComponentNames.FirstOrDefault(tName => tName == $"{typeName}TTT" || tName == $"TT{typeName}");

            if (name is null)
                return null;

            return GetOwlcatReplacementTypes(compilation, ct).First(t => t.Name == name);
        }

        private void AnalyeObjectCreation(OperationAnalysisContext context)
        {
            var sm = context.Operation.SemanticModel;

            if (sm is null)
                return;

            if (context.Operation is not IObjectCreationOperation operation)
                return;

            if (operation.Type is not INamedTypeSymbol typeSymbol)
                return;

            if (TryGetTTTReplacement(typeSymbol, context.Compilation, context.CancellationToken) is not { } replacementType)
                return;

            context.ReportDiagnostic(Diagnostic.Create(Descriptor, context.Operation.Syntax.GetLocation(), typeSymbol, replacementType));
        }
    }
}
