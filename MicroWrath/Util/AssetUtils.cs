using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Kingmaker.Blueprints;
using Kingmaker.Blueprints.JsonSystem.Converters;

using Microsoft.SqlServer.Server;

using TabletopTweaks.Core.Utilities;

using UnityEngine;
using UnityEngine.Rendering;

namespace MicroWrath.Util
{
    /// <summary>
    /// Mod asset utility functions
    /// </summary>
    public static class AssetUtils
    {
        /// <summary>
        /// Direct referenced assets (BlueprintReferencedAssets)
        /// </summary>
        public static class Direct
        {
            /// <summary>
            /// Gets a blueprint sprite
            /// </summary>
            public static Sprite GetSprite(string assetId, long fileId) =>
                (Sprite)UnityObjectConverter.AssetList.Get(assetId, fileId);
        }

        /// <summary>Generates a texture from a bitmap (eg. PNG) assembly resource</summary>
        /// <param name="ass">Assembly</param>
        /// <param name="name">Asset file path (in project directory structure)</param>
        /// <param name="format">Texture format</param>
        /// <param name="mipChain">Generate mip chain (see <see cref="Texture2D(int, int, TextureFormat, bool)"/> constructor)</param>
        /// <returns>New texture object</returns>
        public static Texture2D? GetTextureAssemblyResource(Assembly ass, string name, TextureFormat format = TextureFormat.RGBA32, bool mipChain = false)
        {
            name = name.Replace('\\', '.');

            if (!ass.GetManifestResourceNames().Contains(name)) return null;

            using var s = ass.GetManifestResourceStream(name);
            using var bs = new BinaryReader(s);

            var imageData = bs.ReadBytes((int)s.Length);

            var t = new Texture2D(2, 2, format, mipChain);
            t.LoadImage(imageData);
            t.Apply();

            return t;
        }

        /// <summary>
        /// Creates a sprite from a texture
        /// </summary>
        public static Sprite CreateSprite(Texture2D texture, Rect? rect = null, Vector2? pivot = null)
        {
            pivot ??= new(0.5f, 0.5f);
            rect ??= new(0, 0, texture.width, texture.height);

            return Sprite.Create(texture, rect.Value, pivot.Value);
        }

        /// <summary>
        /// Equivalent to <see cref="GetSpriteAssemblyResource(Assembly, string, Rect?, Vector2?)"/> and <see cref="CreateSprite"/>
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="name"></param>
        /// <param name="rect"></param>
        /// <param name="pivot"></param>
        /// <returns></returns>
        public static Sprite? GetSpriteAssemblyResource(Assembly ass, string name, Rect? rect = null, Vector2? pivot = null)
        {
            var t = GetTextureAssemblyResource(ass, name);
            
            if (t is null) return null;

            return CreateSprite(t, rect, pivot);
        }

        /// <summary>
        /// Clones a blueprint
        /// </summary>
        /// <typeparam name="TBlueprint">Blueprint type</typeparam>
        /// <param name="blueprint">Blueprint to clone</param>
        /// <param name="guid">New blueprint's guid</param>
        /// <param name="name">New blueprint's name</param>
        /// <param name="addToLibrary">Add to library immediately</param>
        /// <returns>Blueprint clone</returns>
        public static TBlueprint CloneBlueprint<TBlueprint>(TBlueprint blueprint, BlueprintGuid guid, string? name = null, bool addToLibrary = true) 
            where TBlueprint : SimpleBlueprint
        {
            blueprint = (ObjectDeepCopier.Clone(blueprint) as TBlueprint)!;

            if (blueprint is BlueprintScriptableObject bso)
            {
                foreach (var c in bso.Components)
                {
                    c.OwnerBlueprint = bso;
                }
            }

            blueprint.AssetGuid = guid;

            if (name is not null) blueprint.name = name;

            if (addToLibrary) ResourcesLibrary.BlueprintsCache.AddCachedBlueprint(blueprint.AssetGuid, blueprint);

            return blueprint;
        }

        static readonly Dictionary<string, AssetBundle> loadedBundles = new();
        static AssetBundle LoadBundleFromResource(string name)
        {
            if (!loadedBundles.ContainsKey(name))
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name);

                if (stream is null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"No resource with name {name}");
                    sb.AppendLine($"Assembly resource names:");

                    foreach (var n in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                    {
                        sb.AppendLine($"  {n}");
                    }

                    throw new ArgumentException(sb.ToString());
                }

                loadedBundles[name] = AssetBundle.LoadFromStream(stream);
            }

            return loadedBundles[name];
        }

        struct AlphaBlendConfig
        {
            public float x;
            public float y;
            public Rect foregroundRect;
        }

        /// <summary>
        /// Alpha blend textures. Uses a compute shader to not make bubbles sad
        /// </summary>
        /// <param name="background">Background texture.</param>
        /// <param name="foreground">Foreground texture.</param>
        /// <param name="position">Coordinates of the foreground texture on the background.</param>
        /// <param name="foregroundRect">Subset of foreground texture. Clamped to background dimensions. Default: Use full foreground.</param>
        /// <param name="format">Output texture format.</param>
        /// <param name="mips">Autogenerate mip maps for output texture.</param>
        /// <param name="renderFormat">RenderTexture format.</param>
        /// <param name="filterMode">Output texture filter mode.</param>
        /// <returns>New texture containing blended textures.</returns>
        public static Texture2D AlphaBlend(
            Texture2D background,
            Texture2D foreground,
            Vector2 position = default,
            Rect foregroundRect = default,
            TextureFormat format = TextureFormat.RGBA32,
            bool mips = false,
            RenderTextureFormat renderFormat = RenderTextureFormat.Default,
            FilterMode filterMode = default)
        {
            var shader = LoadBundleFromResource("MicroWrath.Resources.UnityAssets")
                .LoadAsset<ComputeShader>("4a82214c994c2964a8a3d1c02f6e4644");

            int kernelIndex = shader.FindKernel("CSMain");

            var rt =
                RenderTexture.GetTemporary(
                    background.width,
                    background.height,
                    0,
                    renderFormat,
                    RenderTextureReadWrite.Linear);

            rt.autoGenerateMips = mips;
            rt.enableRandomWrite = true;

            var buffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(AlphaBlendConfig)));

            if (foregroundRect.width == 0)
            {
                foregroundRect.width = Math.Min(background.width - foregroundRect.x, foreground.width);
            }

            if (foregroundRect.height == 0)
            {
                foregroundRect.height = Math.Min(background.height - foregroundRect.y, foreground.height);
            }

            buffer.SetData(new[] { new AlphaBlendConfig() { x = position.x, y = position.y, foregroundRect = foregroundRect } });

            shader.SetBuffer(kernelIndex, "RectPosition", buffer);

            shader.SetTexture(kernelIndex, "Background", background);
            shader.SetTexture(kernelIndex, "Foreground", foreground);
            shader.SetTexture(kernelIndex, "Output", rt);

            shader.Dispatch(kernelIndex, background.width, background.height, 1);

            var output = new Texture2D(background.width, background.height, format, mips)
            {
                filterMode = filterMode
            };

            AsyncGPUReadback.Request(buffer).WaitForCompletion();

            Graphics.ConvertTexture(rt, output);
            
            buffer.Dispose();
            RenderTexture.ReleaseTemporary(rt);

            return output;
        }

        struct DiagonalCutConfig
        {
            public float offset;
            public float width;
            public float height;
            public float invertX;
            public float invertY;
        }

        /// <summary>
        /// Diagonal cut blend two textures with compute shader.
        /// </summary>
        /// <param name="textureA">The first texture.</param>
        /// <param name="textureB">The second texture.</param>
        /// <param name="offset">Cut start X offset.</param>
        /// <param name="invertX">Invert cut along X axis.</param>
        /// <param name="invertY">Invert cut along Y axis.</param>
        /// <param name="width">Output width. Default is the smaller of two input textures.</param>
        /// <param name="height">Output height. Default is the smaller of two input textures.</param>
        /// <param name="format">Output texture format.</param>
        /// <param name="mips">Autogenerate mip maps for output texture.</param>
        /// <param name="renderFormat">RenderTexture format.</param>
        /// <param name="filterMode">Output texture filter mode.</param>
        /// <returns></returns>
        public static Texture2D DiagonalCutBlend(
            Texture2D textureA,
            Texture2D textureB,
            float offset = 0,
            bool invertX = false,
            bool invertY = false,
            int width = 0,
            int height = 0,
            TextureFormat format = TextureFormat.RGBA32,
            bool mips = false,
            RenderTextureFormat renderFormat = RenderTextureFormat.Default,
            FilterMode filterMode = default)
        {
            var shader = LoadBundleFromResource("MicroWrath.Resources.UnityAssets")
                .LoadAsset<ComputeShader>("59d5c474bf3270c47ba11d9b07e7e063");

            int kernelIndex = shader.FindKernel("CSMain");

            if (width < 1)
                width = Mathf.Min(textureA.width, textureB.width);

            if (height < 1)
                height = Mathf.Min(textureA.height, textureB.height);

            var rt =
                RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    renderFormat,
                    RenderTextureReadWrite.Linear);

            rt.autoGenerateMips = mips;
            rt.enableRandomWrite = true;

            var buffer = new ComputeBuffer(1, Marshal.SizeOf(typeof(DiagonalCutConfig)));

            buffer.SetData(new[] { new DiagonalCutConfig() { offset = offset, width = width, height = height, invertX = invertX ? 1f : 0f, invertY = invertY ? 1f : 0f } });

            shader.SetBuffer(kernelIndex, "Params", buffer);

            shader.SetTexture(kernelIndex, "InputA", textureA);
            shader.SetTexture(kernelIndex, "InputB", textureB);
            shader.SetTexture(kernelIndex, "Output", rt);

            shader.Dispatch(kernelIndex, width, height, 1);

            var output = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filterMode
            };

            AsyncGPUReadback.Request(buffer).WaitForCompletion();

            Graphics.ConvertTexture(rt, output);

            buffer.Dispose();
            RenderTexture.ReleaseTemporary(rt);

            return output;
        }
    }
}
