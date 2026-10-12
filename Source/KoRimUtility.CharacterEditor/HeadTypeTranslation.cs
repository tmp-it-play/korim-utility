using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    [StaticConstructorOnStartup]
    internal static class HeadTypeTranslation
    {
        private const string KeyPrefix = "KoRimUtility.CE.HeadType.";

        static HeadTypeTranslation()
        {
            var harmony = new Harmony("snowykte0426.korimutility.charactereditor.headtype");
            MethodInfo menuLabel = null;
            MethodInfo selectedLabel = null;
            try
            {
                var block = AccessTools.TypeByName("CharacterEditor.CEditor+EditorUI+BlockPerson");
                var choose = block == null ? null : AccessTools.DeclaredMethod(block, "AChooseHeadCustom");
                if (choose == null)
                    throw new InvalidOperationException("Head selector not found");

                // Find this selector's label delegate without depending on compiler-generated
                // numbering or patching the generic menu shared by unrelated definition types.
                menuLabel = PatchProcessor.GetOriginalInstructions(choose)
                    .Where(c => c.opcode == OpCodes.Ldftn)
                    .Select(c => c.operand as MethodInfo)
                    .Where(m => m != null && m.ReturnType == typeof(string) &&
                        m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(HeadTypeDef))
                    .Distinct().Single();
                var headTool = AccessTools.TypeByName("CharacterEditor.HeadTool");
                selectedLabel = headTool == null ? null : AccessTools.DeclaredMethod(headTool, "GetHeadName", new[] { typeof(Pawn), typeof(string) });
                if (selectedLabel == null || selectedLabel.ReturnType != typeof(string))
                    throw new InvalidOperationException("Head name formatter not found");

                harmony.Patch(menuLabel, postfix: new HarmonyMethod(typeof(HeadTypeTranslation), nameof(TranslateMenuLabel)));
                harmony.Patch(selectedLabel, postfix: new HarmonyMethod(typeof(HeadTypeTranslation), nameof(TranslateSelectedLabel)));
            }
            catch (Exception exception)
            {
                if (menuLabel != null)
                    harmony.Unpatch(menuLabel, HarmonyPatchType.All, harmony.Id);
                if (selectedLabel != null)
                    harmony.Unpatch(selectedLabel, HarmonyPatchType.All, harmony.Id);
                Log.Warning("[KoRim Utility] Character Editor head translation skipped: " + exception.Message);
            }
        }

        private static void TranslateMenuLabel(HeadTypeDef __0, ref string __result)
        {
            // Keep labels supplied by the head's own mod or another translation.
            if (__0 != null && string.IsNullOrEmpty(__0.label))
                __result = TranslateHead(__0, __result);
        }

        private static void TranslateSelectedLabel(Pawn __0, string __1, ref string __result)
        {
            if (!KoreanTranslation.IsActive || __0 == null)
                return;
            var head = __0.story?.headType;
            if (__1 != null && (head == null || !string.Equals(head.graphicPath, __1, StringComparison.Ordinal)))
                head = DefDatabase<HeadTypeDef>.AllDefsListForReading.Find(d => string.Equals(d.graphicPath, __1, StringComparison.Ordinal));
            if (head != null)
                __result = string.IsNullOrEmpty(head.label) ? TranslateHead(head, __result) : head.label;
        }

        private static string TranslateHead(HeadTypeDef head, string original)
        {
            return KoreanTranslation.TranslateOrOriginal(KeyPrefix + head.defName, original);
        }
    }
}
