using System;
using System.Collections.Generic;
using System.Text;

namespace MicroWrath.Interfaces
{
    public interface ILocalizedStringData
    {
        string? Key { get; }
        string? Name { get; }
        Kingmaker.Localization.Shared.Locale Locale { get; }
    }
}
