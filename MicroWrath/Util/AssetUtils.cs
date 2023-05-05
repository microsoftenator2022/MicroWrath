using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints.JsonSystem.Converters;
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
    }
}
