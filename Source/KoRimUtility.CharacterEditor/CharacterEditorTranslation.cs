using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    // This assembly is loaded only with void.charactereditor, which already requires Harmony.
    [StaticConstructorOnStartup]
    internal static class CharacterEditorTranslation
    {
        private static FieldInfo casketDescription;
        private static FieldInfo graveDescription;
        private static FieldInfo enterGrave;

        static CharacterEditorTranslation()
        {
            var thingTool = AccessTools.TypeByName("CharacterEditor.ThingTool");
            var labels = AccessTools.TypeByName("CharacterEditor.Label");
            var createBuilding = thingTool == null ? null : AccessTools.Method(thingTool, "CreateBuilding",
                new[] { typeof(string), typeof(string), typeof(string), typeof(Type), typeof(string) });
            var updateLabels = labels == null ? null : AccessTools.Method(labels, "UpdateLabels", Type.EmptyTypes);
            casketDescription = labels == null ? null : AccessTools.Field(labels, "DESC_CASCET");
            graveDescription = labels == null ? null : AccessTools.Field(labels, "DESC_GRAVE");
            enterGrave = labels == null ? null : AccessTools.Field(labels, "ENTER_ZOMBGRELLA");
            if (createBuilding == null || updateLabels == null ||
                !IsStringField(casketDescription) || !IsStringField(graveDescription) || !IsStringField(enterGrave))
            {
                Log.Warning("[KoRim Utility] Character Editor API changed; building translations were not installed.");
                return;
            }

            var harmony = new Harmony("snowykte0426.korimutility.charactereditor");
            try
            {
                harmony.Patch(createBuilding, prefix: new HarmonyMethod(typeof(CharacterEditorTranslation), nameof(BuildingPrefix)));
                harmony.Patch(updateLabels, postfix: new HarmonyMethod(typeof(CharacterEditorTranslation), nameof(LabelsPostfix)));
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Warning("[KoRim Utility] Could not install Character Editor translations: " + exception.Message);
            }
        }

        private static bool IsStringField(FieldInfo field)
        {
            return field != null && field.IsStatic && !field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string);
        }

        private static bool IsKorean => LanguageDatabase.activeLanguage?.folderName != null &&
            LanguageDatabase.activeLanguage.folderName.StartsWith("Korean", StringComparison.OrdinalIgnoreCase);

        // Translate before CE generates blueprint, reinstall blueprint and frame labels.
        // Only display arguments change; defName is also the save identifier and must stay intact.
        private static void BuildingPrefix(string __0, ref string __1, ref string __2)
        {
            if (!IsKorean || (__0 != "Zombrella" && __0 != "Zombgrella"))
                return;
            __1 = ("KoRimUtility.CE." + __0 + ".Label").Translate();
            __2 = ("KoRimUtility.CE." + __0 + ".Description").Translate();
        }

        private static void LabelsPostfix()
        {
            if (!IsKorean)
                return;
            casketDescription.SetValue(null, "KoRimUtility.CE.Zombrella.Description".Translate().ToString());
            graveDescription.SetValue(null, "KoRimUtility.CE.Zombgrella.Description".Translate().ToString());
            enterGrave.SetValue(null, "KoRimUtility.CE.EnterZombgrella".Translate().ToString());
        }
    }
}
