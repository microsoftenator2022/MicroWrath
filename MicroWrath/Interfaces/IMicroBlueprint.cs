using System;

using Kingmaker.Blueprints;

using MicroWrath.Util;

namespace MicroWrath.Interfaces
{
    public interface IMicroBlueprint<out TBlueprint> where TBlueprint : SimpleBlueprint
    {
        BlueprintGuid BlueprintGuid { get; }
        TBlueprint? GetBlueprint();
    }
}
