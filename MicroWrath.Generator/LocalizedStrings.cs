using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using MicroWrath.Generator.Common;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using MicroWrath.Util;
using MicroWrath.Util.Linq;

namespace MicroWrath.Generator
{
    [Generator]
    public class LocalizedStrings : IIncrementalGenerator
    {
        private void GenerateAttribute(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static spc =>
            {
                spc.AddSource("localizedStringAttribute", """
using System;

namespace MicroWrath.Localization
{

    [AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    internal sealed class LocalizedStringAttribute : Attribute
    {
        public LocalizedStringAttribute() {}

        public string? Key { get; set; }

        public string? Name { get; set; }

        public Kingmaker.Localization.Shared.Locale Locale { get; set; }
    }
}
""");
            });
        }

        internal const string AttributeFullName = "MicroWrath.Localization.LocalizedStringAttribute";

        private readonly record struct LocalizedStringData(string Name, string ValueMemberFullName, string Key, Option<string> Locale);

        private static LocalizedStringData CreateLocalizedStringData(ISymbol symbol, AttributeData attribute)
        {
            var dict = new Dictionary<string, TypedConstant>();

            foreach(var prop in attribute.NamedArguments)
                dict.Add(prop.Key, prop.Value);

            var fullName = symbol.ToString();
            var name = symbol.Name;

            if (dict.TryGetValue("Name", out var n) || dict.TryGetValue("Key", out n))
                name = n.ToCSharpString().Replace("\"", "");

            var key = dict.TryGetValue("Key", out var k) ? k.ToCSharpString() : $"\"{name}\"";

            var localeString = dict.TryGetValue("Locale", out var l) ? Option.Some(l.ToCSharpString()) : (Option<string>)Option.None<string>();

            return new(name, fullName, key, localeString);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            GenerateAttribute(context);

            var attributeNodes = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeFullName,
                (sn, _) => sn is VariableDeclaratorSyntax,
                (ac, _) => ac);

            var localizedStrings = attributeNodes
                .Where(ac => ac.TargetSymbol is
                {
                    DeclaredAccessibility:
                            Accessibility.Internal or
                            Accessibility.ProtectedOrInternal or
                            Accessibility.Public,
                    IsStatic: true,
                })
                .SelectMany((ac, _) => ac.Attributes.Select(a => CreateLocalizedStringData(ac.TargetSymbol, a)));

            var config = Incremental.GetConfig(context.AnalyzerConfigOptionsProvider);
            var rootNamespace = config.Select(static (c, _) => c.RootNamespace as Option<string>.Some ?? "");
                
            context.RegisterSourceOutput(localizedStrings.Collect().Combine(rootNamespace), static (spc, lsAndConfig) =>
            {
                var (localizedStrings, rootNamespace) = lsAndConfig;

                var locales = localizedStrings.GroupBy(ls => ls.Locale);

                var uniqueStrings = localizedStrings.DistinctBy(s => s.Name);

                var sb = new StringBuilder();

                sb.Append($@"
using System;
using System.Collections.Generic;
using Kingmaker.Localization;
using MicroWrath.Localization;

namespace {rootNamespace}
{{
    internal static class LocalizedStrings
    {{
        public static readonly Dictionary<Kingmaker.Localization.Shared.Locale, Dictionary<string, string>> LocalizedStringEntries =
            new Dictionary<Kingmaker.Localization.Shared.Locale, Dictionary<string, string>>
        {{
");
                foreach (var group in locales)
                {
                    if (group.Key.IsNone) continue;

                    var locale = group.Key.Value!;

                    sb.Append($@"
            {{ {locale}, new Dictionary<string, string>
                {{
");
                    foreach (var ls in group)
                    {
                        sb.Append($@"
                    {{ {ls.Key}, {ls.ValueMemberFullName} }},");
                    }

                    sb.Append($@"
                }}
            }},
");
                }

                sb.Append($@"
        }};
");

                sb.Append($@"
        public static readonly Dictionary<string, string> DefaultStringEntries = new Dictionary<string, string>
        {{
");

                if (locales.FirstOrDefault(g => g.Key.IsNone) is var defaultStrings)
                {
                    foreach (var ls in defaultStrings)
                    {
                        sb.Append($@"
            {{ {ls.Key}, {ls.ValueMemberFullName} }},");
                    }
                }

                sb.Append($@"
        }};
");
                sb.Append($@"
        public static LocalizationPack GetLocalizationPack(Kingmaker.Localization.Shared.Locale locale)
        {{
            var pack = new LocalizationPack();

            foreach(var kvp in DefaultStringEntries)
                pack.PutString(kvp.Key, kvp.Value);

            if (LocalizedStringEntries.ContainsKey(locale))
                foreach (var kvp in LocalizedStringEntries[locale])
                    pack.PutString(kvp.Key, kvp.Value);

            return pack;
        }}
");
                

                foreach (var ls in uniqueStrings)
                {
                    if (string.IsNullOrEmpty(ls.Name)) continue;

                    sb.AppendLine($"        public static LocalizedString {ls.Name} => new LocalizedString {{ Key = {ls.Key} }};");
                }

                sb.Append($@"
    }}
}}
");

                spc.AddSource("localizedStrings", sb.ToString());
            });
        }
    }
}
