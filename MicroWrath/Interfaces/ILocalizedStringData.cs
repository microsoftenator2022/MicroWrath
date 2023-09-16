using System;
using System.Collections.Generic;
using System.Text;

namespace MicroWrath
{
    /// <summary>
    /// Localized string metadata
    /// </summary>
    public interface ILocalizedStringData
    {
        /// <summary>
        /// Localized string key
        /// </summary>
        string? Key { get; }

        /// <summary>
        /// String name in generated LocalizedStrings class
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Localized string locale
        /// </summary>
        Kingmaker.Localization.Shared.Locale Locale { get; }
    }
}
