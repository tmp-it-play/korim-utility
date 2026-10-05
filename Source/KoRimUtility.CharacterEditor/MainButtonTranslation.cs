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

        private static bool IsKorean => LanguageDatabase.activeLanguage?.folderName != null &&
            LanguageDatabase.activeLanguage.folderName.StartsWith("Korean", StringComparison.OrdinalIgnoreCase);

        private static void ButtonPrefix(string __0, ref string __1, ref string __2)
        {
            if (__0 != ButtonName || !IsKorean) return;
            __1 = "KoRimUtility.CE.MainButton.Label".Translate();
            __2 = "KoRimUtility.CE.MainButton.Description".Translate();
        }

        private static void ButtonPostfix(string __0, string __1, string __2, MainButtonDef __result)
        {
            if (__0 != ButtonName || !IsKorean || __result == null) return;
            // CE returns an existing Def without updating its text. Also clear
            // LabelCap if another mod or an earlier initialization already read it.
            __result.label = __1;
            __result.description = __2;
            __result.ClearCachedData();
            if (__result.hotKey?.defName == ButtonName)
            {
                __result.hotKey.label = __1;
                __result.hotKey.description = __2;
                __result.hotKey.ClearCachedData();
            }
        }
    }
}
