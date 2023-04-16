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
using static MicroWrath.Generator.Constants;

namespace MicroWrath.Generator
{
    [Generator]
    public class LocalizedStrings : IIncrementalGenerator
    {
        private readonly record struct LocalizedStringData(string Name, string ValueMemberFullName, string Key, Option<string> Locale);

        private static LocalizedStringData CreateLocalizedStringData(ISymbol symbol, AttributeData attribute, string rootNamespace)
        {
            var dict = new Dictionary<string, TypedConstant>();

            foreach(var prop in attribute.NamedArguments)
                dict.Add(prop.Key, prop.Value);

            var fullName = symbol.ToString();
            var name = fullName.Replace(".", "_");
            if (name.StartsWith($"{rootNamespace}_"))
                name = name.Remove(0, rootNamespace.Length + 1);

            if (dict.TryGetValue("Name", out var n) || dict.TryGetValue("Key", out n))
                name = n.ToCSharpString().Replace("\"", "");

            var key = dict.TryGetValue("Key", out var k) ? k.ToCSharpString() : $"\"{fullName}\"";

            var localeString = dict.TryGetValue("Locale", out var l) ? Option.Some(l.ToCSharpString()) : (Option<string>)Option.None<string>();

            return new(name, fullName, key, localeString);
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var attributeNodes = context.SyntaxProvider.ForAttributeWithMetadataName(
                AttributeFullName,
                static (sn, _) => sn is VariableDeclaratorSyntax,
                static (ac, _) => ac);

            var localizedStringInstances = attributeNodes
                .Where(static ac => ac.TargetSymbol is
                {
                    DeclaredAccessibility:
                        Accessibility.Internal or
                        Accessibility.ProtectedOrInternal or
                        Accessibility.Public,
                    IsStatic: true,
                });

            var config = Incremental.GetConfig(context.AnalyzerConfigOptionsProvider);
            var rootNamespace = config.Select(static (c, _) => c.RootNamespace as Option<string>.Some ?? "");

            var localizedStrings = localizedStringInstances
                .Combine(rootNamespace)
                .SelectMany(static (ac, _) => ac.Left.Attributes.Select(a => CreateLocalizedStringData(ac.Left.TargetSymbol, a, ac.Right)));

            context.RegisterSourceOutput(rootNamespace.Combine(localizedStrings.Collect()), static (spc, lsAndConfig) =>
            {
                var (rootNamespace, localizedStrings) = lsAndConfig;

                var locales = localizedStrings.GroupBy(ls => ls.Locale);

                var uniqueStrings = localizedStrings.DistinctBy(s => s.Name);

                var sb = new StringBuilder();

                sb.Append($@"using System;
using System.Collections.Generic;

using UniRx;

using Kingmaker.Localization;

using MicroWrath;

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

        private static readonly IDisposable LocaleChangedHandler =
            Triggers.LocaleChanged.Subscribe(locale => LocalizationManager.CurrentPack.AddStrings(GetLocalizationPack(locale)));
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

                spc.AddSource("LocalizedStrings", sb.ToString());
            });
        }
    }
}
