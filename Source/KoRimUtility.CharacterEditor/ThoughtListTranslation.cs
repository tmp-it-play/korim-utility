using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    [StaticConstructorOnStartup]
    internal static class ThoughtListTranslation
    {
        private const string KeyPrefix = "KoRimUtility.CE.ThoughtList.";

        static ThoughtListTranslation()
        {
            var type = AccessTools.TypeByName("CharacterEditor.DialogAddThought");
            var method = type == null ? null : AccessTools.DeclaredMethod(type, "GetLabelForThought", new[] { typeof(ThoughtDef) });
            if (method == null || method.ReturnType != typeof(string))
            {
                Log.Warning("[KoRim Utility] Character Editor API changed; thought list translation skipped.");
                return;
            }

            var harmony = new Harmony("snowykte0426.korimutility.charactereditor.thoughtlist");
            try
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(ThoughtListTranslation), nameof(TranslateLabel)));
            }
            catch (Exception exception)
            {
                harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
                Log.Warning("[KoRim Utility] Could not translate Character Editor thought list: " + exception.Message);
            }
        }

        private static void TranslateLabel(ThoughtDef __0, ref string __result)
        {
            if (!KoreanTranslation.IsActive || __0 == null || string.IsNullOrEmpty(__0.defName))
                return;
            bool internalName = string.Equals(__result, __0.defName.CapitalizeFirst(), StringComparison.Ordinal);
            bool unresolvedName = __result != null && __result.IndexOf('{') >= 0 && __result.IndexOf('}') >= 0;
            if (!internalName && !unresolvedName)
                return;

            // These names describe the group without choosing a quality level or a specific weapon.
            var key = KeyPrefix + __0.defName;
            if (KoreanTranslation.CanTranslate(key))
            {
                __result = key.Translate().ToString();
                return;
            }

            // Some thoughts start with a hidden, unnamed stage. Only the list needs a fallback;
            // Character Editor's stage selection and the underlying ThoughtDef remain unchanged.
            if (!internalName || __0.stages == null)
                return;
            foreach (var stage in __0.stages)
            {
                if (stage == null || !stage.visible)
                    continue;
                var label = string.IsNullOrWhiteSpace(stage.labelSocial) ? stage.label : stage.labelSocial;
                if (string.IsNullOrWhiteSpace(label) || label.IndexOf('{') >= 0 || label.IndexOf('}') >= 0)
                    continue;
                __result = label.CapitalizeFirst();
                return;
            }
        }
    }
}
