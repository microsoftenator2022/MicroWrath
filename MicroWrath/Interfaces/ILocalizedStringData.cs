using System;
using System.Collections.Generic;
using System.Text;

namespace MicroWrath
{
    public interface ILocalizedStringData
    {
        string? Key { get; }
        string? Name { get; }
        Kingmaker.Localization.Shared.Locale Locale { get; }
    }
}
