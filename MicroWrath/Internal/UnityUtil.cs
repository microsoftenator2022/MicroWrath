using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace MicroWrath.Util.Unity
{
    internal static class UnityUtil
    {
        public static Color RotateColorHue(Color color, double degrees)
        {
            if (color.r == color.g && color.g == color.b)
                return color;

            var oldColor = color;

            Color.RGBToHSV(color, out var h, out var s, out var v);

            var oldH = h;

            var hF64 = (double)h;

            hF64 += degrees / 360.0;

            // x.y -> (x.y - x.0) = 0.y
            if (hF64 > 1) hF64 -= ((int)hF64);

            // -x.y -> (-x.y + (-(-x.0) + 1) = (-x.y + (x.0 + 1)) = -0.y + 1 = (1 - 0.y)
            if (hF64 < 0) hF64 += (-(int)hF64) + 1;

            h = (float)hF64;

            color = Color.HSVToRGB(h, s, v);

            MicroLogger.Debug(() => $"{(oldH * 360)}\u00b0 -> {(h * 360)}\u00b0");
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
