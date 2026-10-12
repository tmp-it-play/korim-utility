using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace KoRimUtility.RimJobWorld
{
    [StaticConstructorOnStartup]
    internal static class SexualityCardTranslation
    {
        private const string LegacyKey = "KoRimUtility.RJW.LegacySexuality";

        static SexualityCardTranslation()
        {
            // The old card formats the orientation into one label, while the new card
            // draws a heading and value separately. They cannot share a format string.
            var type = AccessTools.TypeByName("rjw.Dialog_Sexcard");
            if (type == null)
                return;
            var method = AccessTools.DeclaredMethod(type, "DrawSexuality", new[] { typeof(Pawn), typeof(Rect) });
            if (method == null)
            {
                Log.Warning("[KoRim Utility] RJW legacy sexuality card API changed; translation skipped.");
                return;
            }
            var harmony = new Harmony("snowykte0426.korimutility.rjw.sexualitycard");
            try
            {
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(SexualityCardTranslation), nameof(Transpiler)));
            }
            catch (Exception exception)
            {
                harmony.Unpatch(method, HarmonyPatchType.Transpiler, harmony.Id);
                Log.Warning("[KoRim Utility] Could not translate RJW legacy sexuality card: " + exception.Message);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            if (code.Count(c => c.LoadsConstant("SC_Sexuality")) != 2)
                throw new InvalidOperationException("Expected two legacy orientation labels");
            var helper = AccessTools.Method(typeof(SexualityCardTranslation), nameof(OrientationKey));
            foreach (var instruction in code)
            {
                yield return instruction;
                if (instruction.LoadsConstant("SC_Sexuality"))
                    yield return new CodeInstruction(OpCodes.Call, helper);
            }
        }

        private static string OrientationKey(string original)
        {
            return KoreanTranslation.CanTranslate(LegacyKey) ? LegacyKey : original;
        }
    }
}
