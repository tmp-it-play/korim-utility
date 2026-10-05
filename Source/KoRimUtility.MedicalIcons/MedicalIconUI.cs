using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace KoRimUtility.MedicalIcons
{
    // Loaded only with RJW; Harmony is already an RJW dependency.
    [StaticConstructorOnStartup]
    internal static class MedicalIconUI
    {
        private static readonly Dictionary<ThingDef, Texture2D> Icons = new Dictionary<ThingDef, Texture2D>();
        private static readonly Dictionary<Material, Texture2D> Baked = new Dictionary<Material, Texture2D>();

        static MedicalIconUI()
        {
            var harmony = new Harmony("snowykte0426.korimutility.medicalicons");
            var defIcon = AccessTools.Method(typeof(Widgets), nameof(Widgets.GetIconFor), new[] {
                typeof(ThingDef), typeof(Material).MakeByRefType(), typeof(ThingDef), typeof(ThingStyleDef), typeof(int?) });
            var thingIcon = AccessTools.Method(typeof(Widgets), nameof(Widgets.GetIconFor), new[] {
                typeof(Thing), typeof(Vector2), typeof(Rot4?), typeof(bool), typeof(float).MakeByRefType(),
                typeof(float).MakeByRefType(), typeof(Vector2).MakeByRefType(), typeof(Color).MakeByRefType(),
                typeof(Material).MakeByRefType() });
            if (defIcon == null || thingIcon == null)
            {
                Log.Error("[KoRim Utility] Medical icon UI API changed; icon compatibility was not installed.");
                return;
            }
            harmony.Patch(defIcon, postfix: new HarmonyMethod(typeof(MedicalIconUI), nameof(DefIconPostfix)));
            harmony.Patch(thingIcon, postfix: new HarmonyMethod(typeof(MedicalIconUI), nameof(ThingIconPostfix)));
            // CE's large item preview reads graphicData.texPath directly,
            // bypassing both uiIcon and Widgets.GetIconFor.
            var editorTextures = AccessTools.TypeByName("CharacterEditor.TextureTool");
            var editorTexture = editorTextures == null ? null : AccessTools.Method(editorTextures, "GetTexture",
                new[] { typeof(ThingDef), typeof(int), typeof(ThingStyleDef), typeof(Rot4) });
            if (editorTexture != null)
                harmony.Patch(editorTexture, prefix: new HarmonyMethod(typeof(MedicalIconUI), nameof(EditorTexturePrefix)));
            // Bionic icons changes graphics in its startup constructor, after
            // BuildableDef.ResolveIcon. Read those final materials after all constructors.
            LongEventHandler.ExecuteWhenFinished(Refresh);
        }

        internal static void Refresh()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!string.IsNullOrEmpty(def.uiIconPath) || def.graphic == null)
                    continue;
                var ours = def.graphic is Graphic_MedicalIcon;
                var bionic = def.graphicData?.texPath?.StartsWith("BionicIcons/Boxes/", StringComparison.Ordinal) == true;
                if (!ours && !bionic)
                    continue;
                var material = def.graphic.MatSingle;
                if (material == null || !material.HasProperty(ShaderPropertyIDs.MaskTex)
                    || material.GetTexture(ShaderPropertyIDs.MaskTex) == null)
                    continue;
                try
                {
                    if (!Baked.TryGetValue(material, out var texture))
                    {
                        texture = MedicalIconTexture.Bake(material);
                        if (texture == null) continue;
                        Baked.Add(material, texture);
                    }
                    Icons[def] = texture;
                    def.uiIcon = texture;
                    def.uiIconColor = Color.white;
                    def.uiIconMaterial = null;
                }
                catch (Exception exception)
                {
                    Log.Error("[KoRim Utility] Could not prepare medical UI icon for " + def.defName + ": " + exception.Message);
                }
            }
        }

        private static void DefIconPostfix(ThingDef thingDef, ThingStyleDef thingStyleDef,
            ref Material material, ref Texture2D __result)
        {
            if (thingStyleDef?.UIIcon != null || !Icons.TryGetValue(thingDef, out var texture))
                return;
            __result = texture;
            material = null;
        }

        private static bool EditorTexturePrefix(ThingDef __0, ThingStyleDef __2, ref Texture2D __result)
        {
            if (__0 == null || __2 != null || !Icons.TryGetValue(__0, out var texture))
                return true;
            __result = texture;
            return false;
        }

        private static void ThingIconPostfix(Thing thing, ref Color color, ref Material material, ref Texture __result)
        {
            if (thing == null || thing.UIIconOverride != null || thing.StyleDef?.UIIcon != null
                || !Icons.TryGetValue(thing.def, out var texture))
                return;
            __result = texture;
            color = Color.white;
            material = null;
        }
    }
}
