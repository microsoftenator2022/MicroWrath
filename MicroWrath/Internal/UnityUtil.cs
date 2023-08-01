using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Owlcat.Runtime.Core.Utils;

using UnityEngine;
using UnityEngine.Rendering;

namespace MicroWrath.Util.Unity
{
    internal static class UnityUtil
    {
        public static bool SupportsSetPixel(this TextureFormat tFormat)
        {
            return tFormat is
                TextureFormat.Alpha8 or
                TextureFormat.ARGB32 or
                TextureFormat.ARGB4444 or
                TextureFormat.BGRA32 or
                TextureFormat.R16 or
                TextureFormat.R8 or
                TextureFormat.RFloat or
                TextureFormat.RG16 or
                TextureFormat.RG32 or
                TextureFormat.RGB24 or
                TextureFormat.RGB48 or
                TextureFormat.RGB565 or
                TextureFormat.RGB9e5Float or
                TextureFormat.RGBA32 or
                TextureFormat.RGBA4444 or
                TextureFormat.RGBA64 or
                TextureFormat.RGBAFloat or
                TextureFormat.RGBAHalf or
                TextureFormat.RGFloat or
                TextureFormat.RGHalf or
                TextureFormat.RHalf;
        }

        public readonly record struct ColorHSV(double h, double s, double v);

        public static Color ModifyHSV(this Color c, Func<ColorHSV, ColorHSV> f)
        {
            var a = c.a;

            Color.RGBToHSV(c, out var h, out var s, out var v);
            var hsv = f(new ColorHSV(h, s, v));
            
            c = Color.HSVToRGB((float)hsv.h, (float)hsv.s, (float)hsv.v);
            c.a = a;

            return c;
        }

        /// <summary>
        /// Rotate Hue (as float) buy a an angle in degrees. Result is normalized to be
        /// in the range [0..1]
        /// </summary>
        /// <param name="hue">Hue as float - degrees/380 or radians/pi</param>
        /// <param name="degrees">Angle in degrees</param>
        public static float RotateHueN(float hue, double degrees)
        {
            var h = (double)hue;

            h += degrees / 360.0;

            // x.y -> (x.y - x.0) = 0.y
            if (h > 1) h -= ((int)h);

            // -x.y -> (-x.y + (-(-x.0) + 1) = (-x.y + (x.0 + 1)) = -0.y + 1 = (1 - 0.y)
            if (h < 0) h += (-(int)h) + 1;

            return (float)h;
        }

        public static Color RotateColorHue(Color color, double degrees)
        {
            if (color.r == color.g && color.g == color.b)
                return color;

            var oldColor = color;

            //Color.RGBToHSV(color, out var h, out var s, out var v);

            //var oldH = h;

            //var hF64 = (double)h;

            //hF64 += degrees / 360.0;

            //// x.y -> (x.y - x.0) = 0.y
            //if (hF64 > 1) hF64 -= ((int)hF64);

            //// -x.y -> (-x.y + (-(-x.0) + 1) = (-x.y + (x.0 + 1)) = -0.y + 1 = (1 - 0.y)
            //if (hF64 < 0) hF64 += (-(int)hF64) + 1;

            //h = (float)hF64;

            //color = Color.HSVToRGB(h, s, v);

            //MicroLogger.Debug(() => $"{(oldH * 360)}\u00b0 -> {(h * 360)}\u00b0");

            color = color.ModifyHSV(hsv =>
            {
                var oldH = hsv.h;

                //var hue = hsv.h;

                //hue += degrees / 360.0;

                //// x.y -> (x.y - x.0) = 0.y
                //if (hue > 1) hue -= ((int)hue);

                //// -x.y -> (-x.y + (-(-x.0) + 1) = (-x.y + (x.0 + 1)) = -0.y + 1 = (1 - 0.y)
                //if (hue < 0) hue += (-(int)hue) + 1;

                //hsv = hsv with { h = hue };

                hsv = hsv with { h = RotateHueN((float)hsv.h, degrees) };

                MicroLogger.Debug(() => $"{(oldH * 360)}\u00b0 -> {(hsv.h * 360)}\u00b0");

                return hsv;
            });

            MicroLogger.Debug(() => $"{oldColor} -> {color}");

            return color;
        }

        public static Gradient? ChangeGradientColors(Gradient? g, Func<Color, Color> f)
        {
            if (g is null || g.colorKeys is null) return g;

            var colors = g.colorKeys;

            for (var i = 0; i < colors.Length; i++)
            {
                var ck = colors[i];

                ck.color = f(ck.color);

                colors[i] = ck;
            }

            g.colorKeys = colors;

            return g;
        }

        public static ParticleSystem.MinMaxGradient ChangeMinMaxGradientColors(ParticleSystem.MinMaxGradient mmg, Func<Color, Color> f) =>
            mmg.mode switch
            {
                ParticleSystemGradientMode.Color =>
                    new ParticleSystem.MinMaxGradient(f(mmg.color)),

                ParticleSystemGradientMode.TwoColors =>
                    new ParticleSystem.MinMaxGradient(
                        f(mmg.colorMin),
                        f(mmg.colorMax)),

                ParticleSystemGradientMode.Gradient => new
                    ParticleSystem.MinMaxGradient(ChangeGradientColors(mmg.gradient, f)),

                ParticleSystemGradientMode.TwoGradients =>
                    new ParticleSystem.MinMaxGradient(
                        ChangeGradientColors(mmg.gradientMin, f),
                        ChangeGradientColors(mmg.gradientMax, f)),

                _ => mmg,
            };

        public static Color AlphaBlend(Color c1, Color c2)
        {
            var output = Color.Lerp(c1, c2, 1f - (c1.a - c2.a));

            output.a = c1.a;

            return output;
        }

        public static Color AlphaBlend(Color c, params Color[] cs) =>
            cs.Aggregate(c, AlphaBlend);

        public static Texture2D AlphaBlend(Texture2D t1, Texture2D t2, int x = 0, int y = 0)
        {
            var output = new Texture2D(t1.width, t1.height);

            for (var i = 0; i < t1.width; i++)
            {
                var i2 = i - x;

                for (var j = 0; j < t1.height; j++)
                {
                    var j2 = j - y;

                    var p = t1.GetPixel(i, j);
                    
                    if (i2 >= 0 && i2 < t2.width &&
                        j2 >= 0 && j2 < t2.height)
                        p = AlphaBlend(p, t2.GetPixel(i2, j2));

                    output.SetPixel(i, j, p);
                }
            }
            output.Apply();

            return output;
        }

        public static Texture2D CopyReadable<TData>(Texture2D texture, TextureFormat format = TextureFormat.RGBA32)
            where TData : struct
        {
            var copy = new Texture2D(texture.width, texture.height, format, false);

            Graphics.ConvertTexture(texture, copy);

            var request = AsyncGPUReadback.Request(copy, 0, format);

            request.WaitForCompletion();

            var data = request.GetData<TData>(0);

            var newTexture = new Texture2D(texture.width, texture.height, format, false);

            newTexture.LoadRawTextureData(data);
            newTexture.Apply();

            UnityEngine.Object.Destroy(copy);

            return newTexture;
        }

        public static Texture2D CopyReadable(Texture2D texture, TextureFormat format = TextureFormat.RGBA32) =>
            CopyReadable<Color32>(texture, format);

        public static class Debug
        {
            public static string DumpGameObject(GameObject gameObject)
            {
                static IEnumerable<string> DumpGameObjectInner(GameObject obj, int depth = 0)
                {
                    var indent = new string(Enumerable.Repeat("    ", depth).SelectMany(Functional.Identity).ToArray());

                    yield return $"{indent}{obj.name}";

                    var components = obj.GetComponents<Component>();

                    yield return $"{indent}  {components.Length} Components:";

                    foreach (var c in components)
                    {
                        yield return $"{indent}    {c.GetType()}";
                        //if (c is ParticleSystemRenderer psr)
                        //{
                        //    yield return $"{indent}      Material: {psr.material.name}";
                        //    yield return $"{indent}      Shader: {psr.material.shader.name}";
                        //}
                    }

                    yield return $"{indent}  {obj.transform.childCount} Child GameObjects:";

                    for (var i = 0; i < obj.transform.childCount; i++)
                    {
                        foreach (var line in DumpGameObjectInner(obj.transform.GetChild(i).gameObject, depth + 1))
                            yield return line;
                    }
                }
                var sb = new StringBuilder();

                foreach (var line in DumpGameObjectInner(gameObject))
                    sb.AppendLine(line);

                return sb.ToString();
            }
        }
    }
}
