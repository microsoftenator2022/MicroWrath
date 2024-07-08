using System;

using Kingmaker.Localization;

using MicroWrath;

namespace MicroWrath.Localization
{
    /// <summary>
    /// This string should should be treated as a <see cref="LocalizedString"/>
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    internal sealed class LocalizedStringAttribute : Attribute, ILocalizedStringData
    {
        public LocalizedStringAttribute() { }

        /// <summary>
        /// <see cref="LocalizedString.Key"/> for this string.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Name for this string.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// <see cref="Kingmaker.Localization.Shared.Locale"/> for this string.
        /// </summary>
        public Kingmaker.Localization.Shared.Locale Locale { get; set; }
    }
}