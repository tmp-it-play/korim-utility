using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    [StaticConstructorOnStartup]
    internal static class UiTranslation
    {
        private const string KeyPrefix = "KoRimUtility.CE.UI.";
        private static readonly FieldInfo[] Fields;

        static UiTranslation()
        {
            var labels = AccessTools.TypeByName("CharacterEditor.Label");
            var english = labels == null ? null : AccessTools.DeclaredMethod(labels, "LangEN", Type.EmptyTypes);
            if (english == null || !english.IsStatic || english.ReturnType != typeof(void))
            {
                Log.Warning("[KoRim Utility] Character Editor API changed; UI translations were not installed.");
                return;
            }

            Fields = labels.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var harmony = new Harmony("snowykte0426.korimutility.charactereditor.ui");
            try
            {
                harmony.Patch(english, postfix: new HarmonyMethod(typeof(UiTranslation), nameof(EnglishPostfix)));
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Warning("[KoRim Utility] Could not install Character Editor UI translations: " + exception.Message);
            }
        }

        // CE falls back to LangEN for Korean. Translate here, before UpdateLabels copies
        // COLONISTS into ListName: the faction filters compare those strings directly.
        private static void EnglishPostfix()
        {
            var language = LanguageDatabase.activeLanguage?.folderName;
            if (language == null || !language.StartsWith("Korean", StringComparison.OrdinalIgnoreCase))
                return;

            foreach (var field in Fields)
            {
                if (field.FieldType != typeof(string) || field.IsLiteral || field.IsInitOnly)
                    continue;
                var key = KeyPrefix + field.Name;
                if (key.CanTranslate())
                    field.SetValue(null, key.Translate().ToString());
            }
        }
    }
}
