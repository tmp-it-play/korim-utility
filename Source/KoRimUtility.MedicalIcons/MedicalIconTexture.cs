using UnityEngine;
using Verse;

namespace KoRimUtility.MedicalIcons
{
    internal static class MedicalIconTexture
    {
        // CutoutComplex multiplies two independently masked tint factors.
        // A linear blend between color/colorTwo does not match the game shader.
        internal static Color Shade(Color pixel, Color mask, Color primary, Color secondary)
        {
            return pixel * Color.Lerp(Color.white, primary, mask.r)
                         * Color.Lerp(Color.white, secondary, mask.g);
        }

        internal static Texture2D Bake(Material material)
        {
            var source = (Texture2D)material.mainTexture;
            var mask = material.GetTexture(ShaderPropertyIDs.MaskTex) as Texture2D;
            if (source == null || mask == null)
                return null;
            Texture2D readable = null, readableMask = null;
            try
            {
                readable = source.isReadable ? source : TextureAtlasHelper.MakeReadableTextureInstance(source);
                readableMask = mask.isReadable ? mask : TextureAtlasHelper.MakeReadableTextureInstance(mask);
                var pixels = readable.GetPixels();
                var primary = material.color;
                var secondary = material.GetColor(ShaderPropertyIDs.ColorTwo);
                var sameSize = source.width == mask.width && source.height == mask.height;
                var maskPixels = sameSize ? readableMask.GetPixels() : null;
                for (var y = 0; y < source.height; y++)
                    for (var x = 0; x < source.width; x++)
                    {
                        var index = y * source.width + x;
                        var weight = sameSize ? maskPixels[index] : readableMask.GetPixelBilinear(
                            (x + 0.5f) / source.width, (y + 0.5f) / source.height);
                        pixels[index] = Shade(pixels[index], weight, primary, secondary);
                    }
                var baked = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false)
                {
                    name = "KoRimMedicalUI_" + source.name + "_" + mask.name,
                    filterMode = source.filterMode,
                    wrapMode = TextureWrapMode.Clamp
                };
                baked.SetPixels(pixels);
                baked.Apply(false, false);
                return baked;
            }
            finally
            {
                if (readable != null && readable != source)
                    Object.Destroy(readable);
                if (readableMask != null && readableMask != mask)
                    Object.Destroy(readableMask);
            }
        }
    }
}
