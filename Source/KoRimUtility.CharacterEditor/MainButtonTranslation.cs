using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    [StaticConstructorOnStartup]
    internal static class MainButtonTranslation
    {
        private const string ButtonName = "HotkeyEditor";

        static MainButtonTranslation()
        {
            var defTool = AccessTools.TypeByName("CharacterEditor.DefTool");
            var createButton = defTool == null ? null : AccessTools.Method(defTool, "GetCreateMainButton",
                new[] { typeof(string), typeof(string), typeof(string), typeof(Type),
                    typeof(ModContentPack), typeof(string), typeof(bool) });
            if (createButton == null)
            {
                Log.Warning("[KoRim Utility] Character Editor main button API changed; menu translation was not installed.");
                return;
            }
            var harmony = new Harmony("snowykte0426.korimutility.charactereditor.mainbutton");
            try
            {
                harmony.Patch(createButton,
                    prefix: new HarmonyMethod(typeof(MainButtonTranslation), nameof(ButtonPrefix)),
                    postfix: new HarmonyMethod(typeof(MainButtonTranslation), nameof(ButtonPostfix)));
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Warning("[KoRim Utility] Could not install Character Editor menu translation: " + exception.Message);
            }
        }

        private static void ButtonPrefix(string __0, ref string __1, ref string __2)
        {
            if (__0 != ButtonName || !KoreanTranslation.IsActive)
                return;
            __1 = "KoRimUtility.CE.MainButton.Label".Translate();
            __2 = "KoRimUtility.CE.MainButton.Description".Translate();
        }

        private static void ButtonPostfix(string __0, string __1, string __2, MainButtonDef __result)
        {
            if (__0 != ButtonName || !KoreanTranslation.IsActive || __result == null)
                return;
            // CE returns an existing Def without updating its text. Also clear
            // LabelCap if another mod or an earlier initialization already read it.
            UpdateLabel(__result, __1, __2);
            if (__result.hotKey?.defName == ButtonName)
                UpdateLabel(__result.hotKey, __1, __2);
        }

        private static void UpdateLabel(Def def, string label, string description)
        {
            def.label = label;
            def.description = description;
            def.ClearCachedData();
        }
    }
}
