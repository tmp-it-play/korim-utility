using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace KoRimUtility.RimJobWorld
{
    [StaticConstructorOnStartup]
    internal static class NotificationTranslation
    {
        private const string CorpseText = " is trying to rape a corpse of ";
        private const string AttackText = " is being attacked by ";
        private const string CorpseKey = "KoRimUtility.RJW.CorpseAttempt";
        private const string AttackKey = "KoRimUtility.RJW.AnimalAttack";
        private static readonly MethodInfo ConcatThree = AccessTools.Method(typeof(string), nameof(string.Concat),
            new[] { typeof(string), typeof(string), typeof(string) });
        private static readonly MethodInfo ConcatFour = AccessTools.Method(typeof(string), nameof(string.Concat),
            new[] { typeof(string), typeof(string), typeof(string), typeof(string) });

        static NotificationTranslation()
        {
            var harmony = new Harmony("snowykte0426.korimutility.rjw.notifications");
            Patch(harmony, "rjw.JobDriver_ViolateCorpse", CorpseText);
            Patch(harmony, "rjw.JobDriver_BestialityForMale", AttackText);
        }

        private static IEnumerable<MethodInfo> Methods(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var method in type.GetMethods(flags))
                if (method.GetMethodBody() != null) yield return method;
            // Notifications live in an iterator and a captured callback. Do not depend
            // on compiler-generated type/method numbering or search unrelated RJW code.
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                foreach (var method in Methods(nested)) yield return method;
        }

        private static void Patch(Harmony harmony, string typeName, string marker)
        {
            MethodInfo target = null;
            try
            {
                var type = AccessTools.TypeByName(typeName);
                var matches = type == null ? new List<MethodInfo>() : Methods(type)
                    .Where(m => PatchProcessor.GetOriginalInstructions(m).Any(c => c.LoadsConstant(marker))).ToList();
                if (matches.Count != 1) throw new InvalidOperationException("Expected one notification in " + typeName);
                target = matches[0];
                harmony.Patch(target, transpiler: new HarmonyMethod(typeof(NotificationTranslation), nameof(Transpiler)));
            }
            catch (Exception exception)
            {
                if (target != null) harmony.Unpatch(target, HarmonyPatchType.Transpiler, harmony.Id);
                Log.Warning("[KoRim Utility] RJW notification translation skipped: " + exception.Message);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var matches = new List<int>();
            MethodInfo replacement = null;
            for (int i = 0; i < code.Count; i++)
            {
                bool corpse = code[i].LoadsConstant(CorpseText);
                if (!corpse && !code[i].LoadsConstant(AttackText)) continue;
                var concat = corpse ? ConcatThree : ConcatFour;
                // Only replace the concatenation belonging to the marked notification.
                // Message targets, severity, sound and all job control flow stay intact.
                int end = code.FindIndex(i + 1, c => c.Calls(concat));
                if (end < 0 || end - i > 8) throw new InvalidOperationException("RJW notification format changed");
                matches.Add(end);
                replacement = AccessTools.Method(typeof(NotificationTranslation), corpse ? nameof(CorpseAttempt) : nameof(AnimalAttack));
            }
            if (matches.Count != 1) throw new InvalidOperationException("Ambiguous RJW notification format");
            code[matches[0]].operand = replacement;
            return code;
        }

        private static bool CanTranslate(string key) =>
            LanguageDatabase.activeLanguage?.folderName?.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) == true && key.CanTranslate();

        private static string CorpseAttempt(string actor, string separator, string target) =>
            separator == CorpseText && CanTranslate(CorpseKey)
                ? CorpseKey.Translate(actor.Named("ACTOR"), target.Named("TARGET")).ToString()
                : string.Concat(actor, separator, target);

        private static string AnimalAttack(string target, string separator, string attacker, string suffix) =>
            separator == AttackText && suffix == "." && CanTranslate(AttackKey)
                ? AttackKey.Translate(target.Named("TARGET"), attacker.Named("ATTACKER")).ToString()
                : string.Concat(target, separator, attacker, suffix);
    }
}
