// Runs the real Harmony hooks against a small CE/Verse stand-in, without launching Unity.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Verse;

internal static class Program
{
    private static int checks;

    private static void Equal(string expected, string actual)
    {
        if (expected != actual)
            throw new Exception($"Expected '{expected}', got '{actual}'");
        checks++;
    }

    public static void Main(string[] args)
    {
        foreach (var path in Directory.GetFiles(Path.Combine(args[0], "Translations/CharacterEditor/Languages/Korean/Keyed"), "*.xml"))
            foreach (var entry in XDocument.Load(path).Root.Elements())
                Translator.Values[entry.Name.LocalName] = entry.Value;
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.CharacterEditor.CharacterEditorTranslation).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.CharacterEditor.MainButtonTranslation).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.CharacterEditor.UiTranslation).TypeHandle);
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.CharacterEditor.AdditionalUiTranslation).TypeHandle);

        // Both current and legacy Korean folder names are supported.
        foreach (var language in new[] { "Korean", "Korean (한국어)" })
        {
            LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = language };
            foreach (var name in new[] { "Zombrella", "Zombgrella" })
            {
                CharacterEditor.ThingTool.CreateBuilding(name, name, "original description", typeof(object), "original texture");
                var expected = Translator.Values["KoRimUtility.CE." + name + ".Label"];
                Equal(name, CharacterEditor.ThingTool.DefName);
                Equal(expected, CharacterEditor.ThingTool.Label);
                Equal(expected + " (blueprint)", CharacterEditor.ThingTool.Blueprint);
                Equal(expected + " (reinstall)", CharacterEditor.ThingTool.ReinstallBlueprint);
                Equal(expected + " (frame)", CharacterEditor.ThingTool.Frame);
                Equal(Translator.Values["KoRimUtility.CE." + name + ".Description"], CharacterEditor.ThingTool.Description);
                Equal("original texture", CharacterEditor.ThingTool.Texture);
            }
            CharacterEditor.Label.UpdateLabels();
            Equal(Translator.Values["KoRimUtility.CE.EnterZombgrella"], CharacterEditor.Label.ENTER_ZOMBGRELLA);
            Equal(Translator.Values["KoRimUtility.CE.Zombrella.Description"], CharacterEditor.Label.DESC_CASCET);
            Equal(Translator.Values["KoRimUtility.CE.Zombgrella.Description"], CharacterEditor.Label.DESC_GRAVE);

            // Repeated settings initialization must not restore the English label.
            CharacterEditor.Label.UpdateLabels();
            Equal(Translator.Values["KoRimUtility.CE.EnterZombgrella"], CharacterEditor.Label.ENTER_ZOMBGRELLA);
            CharacterEditor.ThingTool.CreateBuilding("OtherBuilding", "untouched", "untouched description", typeof(object), "texture");
            Equal("untouched", CharacterEditor.ThingTool.Label);
            Equal("untouched description", CharacterEditor.ThingTool.Description);
        }

        // Switching away from Korean restores CE's own labels and leaves other languages alone.
        foreach (var language in new[] { "English", "Japanese", null })
        {
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            CharacterEditor.Label.UpdateLabels();
            Equal("original entry", CharacterEditor.Label.ENTER_ZOMBGRELLA);
            Equal("original casket", CharacterEditor.Label.DESC_CASCET);
            Equal("original grave", CharacterEditor.Label.DESC_GRAVE);
            CharacterEditor.ThingTool.CreateBuilding("Zombrella", "original label", "original description", typeof(object), "texture");
            Equal("original label", CharacterEditor.ThingTool.Label);
            Equal("original description", CharacterEditor.ThingTool.Description);
        }
        CheckMainButton();
        CheckUi();
        CheckAdditionalUi();
        Console.WriteLine($"Character Editor Harmony integration: {checks} checks passed (Unity/game UI not exercised).");
    }

    private static void CheckUi()
    {
        foreach (var name in new[] { "READONLY_LABEL", "CONSTANT_LABEL", "NUMBER" })
            Translator.Values["KoRimUtility.CE.UI." + name] = "must not replace immutable or non-string fields";
        foreach (var language in new[] { "Korean", "Korean (한국어)", "korean", "English", "Japanese", null })
        {
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            var korean = language?.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) == true;
            for (var repeat = 0; repeat < 2; repeat++)
            {
                CharacterEditor.Label.UpdateLabels();
                Equal(korean ? "정착민" : "Colonists", CharacterEditor.Label.COLONISTS);
                Equal(korean ? "인간형" : "Humanoid", CharacterEditor.Label.HUMANOID);
                Equal(korean ? "의료 처치" : "Medicate", CharacterEditor.Label.MEDICATE);
                Equal(korean ? "무기 교체" : "Arm", CharacterEditor.Label.REEQUIP);
                Equal(korean ? "머리카락" : "Hair", CharacterEditor.Label.HAIR);
                Equal(korean ? "머리색" : "Hair", CharacterEditor.Label.HAIRCOLOR);
                Equal(korean ? "머리 모양" : "Hairstyle", CharacterEditor.Label.FRISUR);
                // UpdateLabels must copy the translated category, not the English fallback.
                Equal(CharacterEditor.Label.COLONISTS, CharacterEditor.CEditor.ListName);
                Equal("english", CharacterEditor.Label.currentLanguage);
                Equal("Unrecognized upstream field", CharacterEditor.Label.NEW_LABEL);
                Equal("∞", CharacterEditor.Label.INFINITE);
                Equal("constant", CharacterEditor.Label.READONLY_LABEL);
                Equal("literal", CharacterEditor.Label.CONSTANT_LABEL);
                Equal("17", CharacterEditor.Label.NUMBER.ToString());
            }
        }
        LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = "Korean" };
        const string key = "KoRimUtility.CE.UI.HAIR";
        var translation = Translator.Values[key];
        Translator.Values.Remove(key);
        CharacterEditor.Label.UpdateLabels();
        Equal("Hair", CharacterEditor.Label.HAIR);
        Equal("정착민", CharacterEditor.Label.COLONISTS);
        Translator.Values[key] = translation;
        CharacterEditor.Label.UpdateLabels();
        Equal(translation, CharacterEditor.Label.HAIR);
    }

    private static void CheckAdditionalUi()
    {
        foreach (var language in new[] { "Korean", "Korean (한국어)", "English", "Japanese", null })
        {
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            var korean = language?.StartsWith("Korean") == true;
            Equal(korean ? "무드|무드 [|호감도|호감도 [|int" : "mood|mood [|opinion|opinion [|int",
                new CharacterEditor.DialogAddThought().DrawSlider());
            Equal(korean ? "수정|변경 사항은 시간이 조금 지난 뒤 적용됩니다.|DrawDebugOptions" :
                "Modify|changes will be applied after some passed time|DrawDebugOptions",
                new CharacterEditor.DialogPsychology().DoWindowContents());
            var type = new object();
            CharacterEditor.MessageTool.Show("failed to heal", type);
            Equal(korean ? "치료하지 못했습니다." : "failed to heal", CharacterEditor.MessageTool.Text);
            Equal("True", ReferenceEquals(type, CharacterEditor.MessageTool.MessageType).ToString());
            const string path = "C:\\Users\\Test\\my English name.pawn";
            CharacterEditor.MessageTool.Show("export successful to " + path);
            Equal((korean ? "내보내기 완료: " : "export successful to ") + path, CharacterEditor.MessageTool.Text);
            CharacterEditor.MessageTool.Show("unknown message: failed to heal");
            Equal("unknown message: failed to heal", CharacterEditor.MessageTool.Text);
            CharacterEditor.MessageTool.Show(null);
            Equal(null, CharacterEditor.MessageTool.Text);
            var called = false;
            Action confirm = () => called = true;
            CharacterEditor.MessageTool.ShowCustomDialog("Data: Hair|Colonists|Custom", "unchanged", null, confirm, null);
            Equal((korean ? "저장 데이터: " : "Data: ") + "Hair|Colonists|Custom", CharacterEditor.MessageTool.Text);
            Equal("unchanged", CharacterEditor.MessageTool.Title);
            Equal("True", ReferenceEquals(confirm, CharacterEditor.MessageTool.Confirm).ToString());
            CharacterEditor.MessageTool.Confirm();
            Equal("True", called.ToString());
        }
        LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = "Korean" };
        const string key = "KoRimUtility.CE.Extra.HealFailed";
        var saved = Translator.Values[key];
        Translator.Values.Remove(key);
        CharacterEditor.MessageTool.Show("failed to heal");
        Equal("failed to heal", CharacterEditor.MessageTool.Text);
        Translator.Values[key] = saved;
    }

    private static void CheckMainButton()
    {
        foreach (var language in new[] { "Korean", "Korean (한국어)", "English", "Japanese", null })
        {
            CharacterEditor.DefTool.Buttons.Clear();
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            var korean = language?.StartsWith("Korean") == true;
            var label = korean ? "캐릭터" : "Character";
            var desc = korean ? "캐릭터 편집기를 엽니다." : "Start Character Editor";
            var pack = new ModContentPack();
            var button = CharacterEditor.DefTool.GetCreateMainButton("HotkeyEditor", "Character", "Start Character Editor", typeof(Program), pack, "F8", false);
            Equal(label, button.LabelCap);
            Equal(desc, button.description);
            Equal(label, button.hotKey.LabelCap);
            Equal(desc, button.hotKey.description);
            Equal("HotkeyEditor", button.defName);
            Equal("HotkeyEditor", button.hotKey.defName);
            Equal("F8", button.hotKey.keyCode);
            Equal("False", button.buttonVisible.ToString());
            Equal("beditoricon", button.iconPath);
            Equal("True", (button.tabWindowClass == typeof(Program) && button.modContentPack == pack).ToString());
            var teleport = CharacterEditor.DefTool.GetCreateMainButton("HotkeyTeleport", "Teleport", "quick teleport", typeof(Program), pack, "F9", false);
            Equal("Teleport", teleport.label);
            Equal("quick teleport", teleport.description);
            Equal("F9", teleport.hotKey.keyCode);
        }
        // An English Def may already exist before our Korean hook is called.
        CharacterEditor.DefTool.Buttons.Clear();
        LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = "English" };
        var cached = CharacterEditor.DefTool.GetCreateMainButton("HotkeyEditor", "Character", "Start Character Editor", typeof(Program), null, "F8", true);
        Equal("Character", cached.LabelCap);
        Equal("Character", cached.hotKey.LabelCap);
        LanguageDatabase.activeLanguage.folderName = "Korean";
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var result = CharacterEditor.DefTool.GetCreateMainButton("HotkeyEditor", "Character", "Start Character Editor", typeof(Program), null, "F9", false);
            Equal("True", ReferenceEquals(cached, result).ToString());
            Equal("캐릭터", result.LabelCap);
            Equal("캐릭터 편집기를 엽니다.", result.description);
            Equal("캐릭터", result.hotKey.LabelCap);
            Equal("캐릭터 편집기를 엽니다.", result.hotKey.description);
            Equal("F8", result.hotKey.keyCode);
            Equal("True", result.buttonVisible.ToString());
        }
    }
}

namespace Verse
{
    public sealed class ModContentPack { }
    public class Def
    {
        public string defName, label, description;
        private string cachedLabel;
        public string LabelCap => cachedLabel ??= label;
        public void ClearCachedData() { cachedLabel = null; }
    }
    public sealed class StaticConstructorOnStartupAttribute : Attribute { }
    public sealed class LoadedLanguage { public string folderName; }
    public static class LanguageDatabase { public static LoadedLanguage activeLanguage; }
    public static class Log
    {
        public static void Warning(string message) => throw new Exception(message);
    }
    public static class Translator
    {
        public static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public static string Translate(this string key) => Values[key];
        public static bool CanTranslate(this string key) => Values.ContainsKey(key);
    }
}

namespace CharacterEditor
{
    internal static class DefTool
    {
        internal static readonly Dictionary<string, RimWorld.MainButtonDef> Buttons = new();

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static RimWorld.MainButtonDef GetCreateMainButton(string defName, string label, string desc,
            Type typeClass, ModContentPack pack, string keyCode, bool isVisible)
        {
            if (Buttons.TryGetValue(defName, out var existing)) return existing;
            var result = new RimWorld.MainButtonDef { defName = defName, label = label, description = desc,
                tabWindowClass = typeClass, modContentPack = pack, buttonVisible = isVisible, iconPath = "beditoricon",
                hotKey = new RimWorld.KeyBindingDef { defName = defName, label = label, description = desc, keyCode = keyCode } };
            Buttons.Add(defName, result);
            return result;
        }
    }
    internal static class ThingTool
    {
        internal static string DefName, Label, Description, Blueprint, ReinstallBlueprint, Frame, Texture;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void CreateBuilding(string defName, string label, string dsc, Type thingClass, string textur)
        {
            DefName = defName;
            Label = label;
            Description = dsc;
            Blueprint = label + " (blueprint)";
            ReinstallBlueprint = label + " (reinstall)";
            Frame = label + " (frame)";
            Texture = textur;
        }
    }

    internal static class Label
    {
        internal static string DESC_CASCET, DESC_GRAVE, ENTER_ZOMBGRELLA;
        internal static string COLONISTS, HUMANOID, MEDICATE, REEQUIP, HAIR, HAIRCOLOR, FRISUR;
        internal static string currentLanguage = "english", NEW_LABEL, INFINITE;
        internal static readonly string READONLY_LABEL = "constant";
        internal const string CONSTANT_LABEL = "literal";
        internal static int NUMBER = 17;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void LangEN()
        {
            COLONISTS = "Colonists";
            HUMANOID = "Humanoid";
            MEDICATE = "Medicate";
            REEQUIP = "Arm";
            HAIR = HAIRCOLOR = "Hair";
            FRISUR = "Hairstyle";
            NEW_LABEL = "Unrecognized upstream field";
            INFINITE = "∞";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UpdateLabels()
        {
            LangEN();
            CEditor.ListName = COLONISTS;
            DESC_CASCET = "original casket";
            DESC_GRAVE = "original grave";
            ENTER_ZOMBGRELLA = "original entry";
        }
    }
    internal static class CEditor { internal static string ListName; }
    internal sealed class DialogAddThought
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal string DrawSlider() => string.Join("|", new[] { "mood", "mood [", "opinion", "opinion [", "int" });
    }
    internal sealed class DialogPsychology
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal string DoWindowContents() => string.Join("|", new[] { "Modify", "changes will be applied after some passed time", "DrawDebugOptions" });
    }
    internal static class MessageTool
    {
        internal static string Text, Title;
        internal static object MessageType;
        internal static Action Confirm;
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Show(string info, object mt = null) { Text = info; MessageType = mt; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ShowCustomDialog(string s, string title, Action onAbort, Action onConfirm, Action onNext)
        { Text = s; Title = title; Confirm = onConfirm; }
    }
}

namespace RimWorld
{
    public sealed class MainButtonDef : Def
    {
        public bool buttonVisible;
        public string iconPath;
        public Type tabWindowClass;
        public ModContentPack modContentPack;
        public KeyBindingDef hotKey;
    }
    public sealed class KeyBindingDef : Def { public string keyCode; }
}
