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
        Console.WriteLine($"Character Editor Harmony integration: {checks} checks passed (Unity/game UI not exercised).");
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UpdateLabels()
        {
            DESC_CASCET = "original casket";
            DESC_GRAVE = "original grave";
            ENTER_ZOMBGRELLA = "original entry";
        }
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
