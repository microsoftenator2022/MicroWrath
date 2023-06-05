using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Converters;

using TabletopTweaks.Core.Utilities;

using UnityEngine;

namespace MicroWrath.Util
{
    public static class AssetUtils
    {
        public static class Direct
        {
            public static Sprite GetSprite(string assetId, long fileId) =>
                (Sprite)UnityObjectConverter.AssetList.Get(assetId, fileId);
        }

        public static TBlueprint CloneBlueprint<TBlueprint>(TBlueprint blueprint, BlueprintGuid guid, string? name = null, bool addToLibrary = true) where TBlueprint : SimpleBlueprint
        {
            blueprint = (ObjectDeepCopier.Clone(blueprint) as TBlueprint)!;

            blueprint.AssetGuid = guid;

            if (name is not null) blueprint.name = name;

            if (addToLibrary) ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(blueprint.AssetGuid, blueprint);

            return blueprint;
        }
    }
}
