using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KoRimUtility.FoodAlert
{
    [StaticConstructorOnStartup]
    internal static class PreferabilityTranslation
    {
        static PreferabilityTranslation()
        {
            var harmony = new Harmony("snowykte0426.korimutility.foodalert");
            Patch(harmony, "FoodAlert.FoodAlertMod", "DoSettingsWindowContents");
            Patch(harmony, "FoodAlert.HarmonyPatches", "FoodCounter_NearDatePostfix");
        }

        private static void Patch(Harmony harmony, string typeName, string methodName)
        {
            var type = AccessTools.TypeByName(typeName);
            var method = type == null ? null : AccessTools.DeclaredMethod(type, methodName);
            if (method == null)
            {
                Log.Warning("[KoRim Utility] Food Alert API changed; food category translation skipped for " + methodName);
                return;
            }
            try
            {
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(PreferabilityTranslation), nameof(Transpiler)));
            }
            catch (Exception exception)
            {
                harmony.Unpatch(method, HarmonyPatchType.Transpiler, harmony.Id);
                Log.Warning("[KoRim Utility] Could not translate Food Alert categories: " + exception.Message);
            }
        }

        private static string TranslateName(string original)
        {
            var folder = LanguageDatabase.activeLanguage?.folderName;
            var key = "KoRimUtility.FoodAlert.Preferability." + original;
            return folder != null && folder.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) && key.CanTranslate()
                ? key.Translate().ToString() : original;
        }

        // Only display conversions in Food Alert's two UI methods are changed.
        // Enum values, saved settings and nutrition calculations remain upstream's.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = instructions.ToList();
            var enumName = AccessTools.Method(typeof(Enum), nameof(Enum.GetName), new[] { typeof(Type), typeof(object) });
            var toString = AccessTools.Method(typeof(object), nameof(ToString), Type.EmptyTypes);
            var translate = AccessTools.Method(typeof(PreferabilityTranslation), nameof(TranslateName));
            var matches = new List<int>();
            for (int i = 0; i < code.Count; i++)
            {
                if ((__originalMethod.Name == "DoSettingsWindowContents" && code[i].Calls(enumName)) ||
                    (__originalMethod.Name == "FoodCounter_NearDatePostfix" && i > 0 && code[i].Calls(toString) &&
                     code[i - 1].opcode == OpCodes.Constrained && Equals(code[i - 1].operand, typeof(FoodPreferability))))
                    matches.Add(i);
            }
            if (matches.Count != 1)
                throw new InvalidOperationException("Unexpected food category display code in " + __originalMethod.Name);
            code.Insert(matches[0] + 1, new CodeInstruction(OpCodes.Call, translate));
            return code;
        }
    }
}
