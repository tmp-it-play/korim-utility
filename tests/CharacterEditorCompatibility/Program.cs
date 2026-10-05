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
        var xml = XDocument.Load(Path.Combine(args[0], "Translations/CharacterEditor/Languages/Korean/Keyed/Buildings.xml"));
        foreach (var entry in xml.Root.Elements())
            Translator.Values[entry.Name.LocalName] = entry.Value;
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.CharacterEditor.CharacterEditorTranslation).TypeHandle);

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
        Console.WriteLine($"Character Editor Harmony integration: {checks} checks passed (Unity/game UI not exercised).");
    }
}

namespace Verse
{
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
