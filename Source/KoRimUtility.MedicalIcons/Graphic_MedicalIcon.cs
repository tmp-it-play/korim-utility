using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace KoRimUtility.MedicalIcons
{
    public sealed class Graphic_MedicalIcon : Graphic_Single
    {
        private static readonly Dictionary<Texture2D, Dictionary<string, Texture2D>> Bases =
            new Dictionary<Texture2D, Dictionary<string, Texture2D>>();

        public override void Init(GraphicRequest req)
        {
            var source = req.texture ?? ContentFinder<Texture2D>.Get(req.path);
            // RimWorld's static atlas is keyed by the base texture, not by
            // (base, mask). Never register another mask on a shared mod texture.
            if (!Bases.TryGetValue(source, out var masks))
                Bases.Add(source, masks = new Dictionary<string, Texture2D>());
            var key = req.maskPath ?? "";
            if (!masks.TryGetValue(key, out var isolated))
            {
                // Unity can crash while instantiating a DDS-backed Texture2D.
                // Its readable-copy helper also handles compressed/unreadable textures.
                isolated = TextureAtlasHelper.MakeReadableTextureInstance(source);
                isolated.filterMode = source.filterMode;
                isolated.wrapMode = source.wrapMode;
                isolated.name = "KoRimMedical_" + source.name + "_" + key;
                masks.Add(key, isolated);
            }
            req.texture = isolated;
            base.Init(req);
        }

        public override void TryInsertIntoAtlas(TextureAtlasGroup groupKey)
        {
            var mask = mat.GetTexture(ShaderPropertyIDs.MaskTex);
            // HD texture packs can replace the case with a different size.
            // Render those isolated materials directly instead of an invalid atlas tile.
            if (mask != null && (mask.width != mat.mainTexture.width || mask.height != mat.mainTexture.height))
                return;
            base.TryInsertIntoAtlas(groupKey);
        }

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            return GraphicDatabase.Get<Graphic_MedicalIcon>(path, newShader, drawSize,
                newColor, newColorTwo, data, maskPath);
        }
    }
}
