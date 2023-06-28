using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Converters;

using Microsoft.SqlServer.Server;

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

        public static Texture2D? GetTextureAssemblyResource(Assembly ass, string name, TextureFormat format = TextureFormat.RGBA32)
        {
            name = name.Replace('\\', '.');

            if (!ass.GetManifestResourceNames().Contains(name)) return null;

            using var s = ass.GetManifestResourceStream(name);
            using var bs = new BinaryReader(s);

            var imageData = bs.ReadBytes((int)s.Length);

            var t = new Texture2D(2, 2, format, false);
            t.LoadImage(imageData);
            t.Apply();

            return t;
        }

        public static Sprite CreateSprite(Texture2D texture, Rect? rect = null, Vector2? pivot = null)
        {
            pivot ??= new(0.5f, 0.5f);
            rect ??= new(0, 0, texture.width, texture.height);

            return Sprite.Create(texture, rect.Value, pivot.Value);
        }

        public static Sprite? GetSpriteAssemblyResource(Assembly ass, string name, Rect? rect = null, Vector2? pivot = null)
        {
            var t = GetTextureAssemblyResource(ass, name);
            
            if (t is null) return null;

            return CreateSprite(t, rect, pivot);
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
