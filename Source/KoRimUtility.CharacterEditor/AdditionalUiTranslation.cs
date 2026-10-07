using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace KoRimUtility.CharacterEditor
{
    [StaticConstructorOnStartup]
    internal static class AdditionalUiTranslation
    {
        private const string KeyPrefix = "KoRimUtility.CE.Extra.";
        private static readonly Dictionary<string, string> Literals = new Dictionary<string, string>
        {
            { "mood", "Mood" }, { "mood [", "MoodValue" },
            { "opinion", "Opinion" }, { "opinion [", "OpinionValue" },
            { "Modify", "Modify" },
            { "changes will be applied after some passed time", "ModifyTip" }
        };
        private static readonly Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            { "Do not click on back button or you will need to restart your game. This is not a bug from the Editor, but because of the fluid ideo.", "FluidIdeology" },
            { "health conditions are malformed -> consider to use 'full heal'", "InvalidHealth" },
            { "failed to heal", "HealFailed" },
            { "can't delete below the minimum number", "MinimumPawns" },
            { "can't create zombies in space!", "ZombieInSpace" },
            { "default preset not found!", "MissingPreset" },
            { "no data or not readable", "UnreadableData" },
            { "error while parsing scenario file. reason -> wrong or invalid data format", "InvalidScenario" },
            { "couldn't add scenario parts", "ScenarioPartsFailed" },
            { "failed to add thought. reason = this thought or thought stage cause an exception. action was rolled back.", "ThoughtFailed" }
        };
        private static readonly Dictionary<string, string> MessagePrefixes = new Dictionary<string, string>
        {
            { "export successful to ", "ExportedTo" },
            { "import successful from ", "ImportedFrom" },
            { "Missing Mods: ", "MissingMods" },
            { "Hair not found:", "MissingHair" },
            { "BodyTypeDef not found: ", "MissingBody" },
            { "Missing Texture=", "MissingTexture" },
            { "can't load scenario file. reason-> no data in file=", "EmptyScenario" }
        };

        static AdditionalUiTranslation()
        {
            var harmony = new Harmony("snowykte0426.korimutility.charactereditor.extra");
            Patch(harmony, "CharacterEditor.DialogAddThought", "DrawSlider", nameof(LiteralTranspiler), true);
            Patch(harmony, "CharacterEditor.DialogPsychology", "DoWindowContents", nameof(LiteralTranspiler), true);
            Patch(harmony, "CharacterEditor.MessageTool", "Show", nameof(MessagePrefix), false);
            Patch(harmony, "CharacterEditor.MessageTool", "ShowCustomDialog", nameof(DialogPrefix), false);
        }

        private static void Patch(Harmony harmony, string typeName, string methodName, string hook, bool transpiler)
        {
            var type = AccessTools.TypeByName(typeName);
            var method = type == null ? null : AccessTools.DeclaredMethod(type, methodName);
            if (method == null || (!transpiler &&
                (method.GetParameters().Length == 0 || method.GetParameters()[0].ParameterType != typeof(string))))
            {
                Log.Warning("[KoRim Utility] Character Editor API changed; translation skipped for " + typeName + "." + methodName);
                return;
            }
            try
            {
                var patch = new HarmonyMethod(typeof(AdditionalUiTranslation), hook);
                if (transpiler)
                    harmony.Patch(method, transpiler: patch);
                else
                    harmony.Patch(method, prefix: patch);
            }
            catch (Exception exception)
            {
                harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
                Log.Warning("[KoRim Utility] Could not translate " + typeName + "." + methodName + ": " + exception.Message);
            }
        }

        private static string TranslateOrOriginal(string key, string original)
        {
            return KoreanTranslation.TranslateOrOriginal(KeyPrefix + key, original);
        }

        private static IEnumerable<CodeInstruction> LiteralTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var translate = AccessTools.Method(typeof(AdditionalUiTranslation), nameof(TranslateLiteral));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string value && Literals.ContainsKey(value))
                    yield return new CodeInstruction(OpCodes.Call, translate);
            }
        }

        private static string TranslateLiteral(string value)
        {
            return Literals.TryGetValue(value, out var key) ? TranslateOrOriginal(key, value) : value;
        }

        private static void MessagePrefix(ref string __0)
        {
            if (!KoreanTranslation.IsActive || __0 == null)
                return;
            if (Messages.TryGetValue(__0, out var key))
            {
                __0 = TranslateOrOriginal(key, __0);
                return;
            }
            foreach (var pair in MessagePrefixes)
                if (__0.StartsWith(pair.Key, StringComparison.Ordinal))
                {
                    __0 = TranslateOrOriginal(pair.Value, pair.Key) + __0.Substring(pair.Key.Length);
                    return;
                }
        }

        private static void DialogPrefix(ref string __0)
        {
            const string prefix = "Data: ";
            if (__0 != null && __0.StartsWith(prefix, StringComparison.Ordinal))
                __0 = TranslateOrOriginal("SlotData", prefix) + __0.Substring(prefix.Length);
        }
    }
}
