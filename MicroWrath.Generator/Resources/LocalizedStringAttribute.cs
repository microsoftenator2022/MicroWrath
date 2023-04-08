using System;

using MicroWrath.Interfaces;

namespace MicroWrath.Localization
{
    [AttributeUsage(validOn: AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    internal sealed class LocalizedStringAttribute : Attribute, ILocalizedStringData
    {
        public LocalizedStringAttribute() { }

        public string? Key { get; set; }

        public string? Name { get; set; }

        public Kingmaker.Localization.Shared.Locale Locale { get; set; }
    }
}