using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using RimWorld;
using Verse;

internal static class Program
{
    private static int checks;
    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
        checks++;
    }
    private static void Main(string[] args)
    {
        foreach (var node in XDocument.Load(Path.Combine(args[0], "Translations/FoodAlertContinued/Languages/Korean/Keyed/FoodAlertContinued.xml")).Root.Elements())
            Translator.Values[node.Name.LocalName] = node.Value;
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.FoodAlert.PreferabilityTranslation).TypeHandle);
        foreach (var language in new[] { "Korean", "Korean (한국어)", "korean", "English", "Japanese", null })
        {
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            var korean = language?.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) == true;
            foreach (FoodPreferability preference in Enum.GetValues(typeof(FoodPreferability)))
            {
                FoodAlert.FoodAlertMod.Selected = preference;
                var name = preference.ToString();
                var key = "KoRimUtility.FoodAlert.Preferability." + name;
                var expected = korean && Translator.Values.ContainsKey(key) ? Translator.Values[key] : name;
                var label = FoodAlert.FoodAlertMod.DoSettingsWindowContents();
                Equal(expected, label);
                Equal(preference, FoodAlert.FoodAlertMod.Selected);
                Equal(preference, FoodAlert.FoodAlertMod.AfterSelection);
                Equal((int)preference, FoodAlert.FoodAlertMod.SavedValue);
                float y = 100;
                Equal("60 / 32 / 1 : " + expected, FoodAlert.HarmonyPatches.FoodCounter_NearDatePostfix(ref y));
                Equal(76f, y);
                Equal(preference, FoodAlert.FoodAlertMod.Selected);
            }
        }
        LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = "Korean" };
        FoodAlert.FoodAlertMod.Selected = FoodPreferability.RawBad;
        Translator.Values.Remove("KoRimUtility.FoodAlert.Preferability.RawBad");
        Equal("RawBad", FoodAlert.FoodAlertMod.DoSettingsWindowContents());
        Console.WriteLine($"Food Alert Harmony integration: {checks} checks passed.");
    }
}

namespace FoodAlert
{
    public static class FoodAlertMod
    {
        public static FoodPreferability Selected;
        public static FoodPreferability AfterSelection;
        public static int SavedValue;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string DoSettingsWindowContents()
        {
            var name = Enum.GetName(typeof(FoodPreferability), Selected);
            // The real UI compares and saves the enum, not the translated label.
            AfterSelection = Selected;
            SavedValue = (int)Selected;
            return name;
        }
    }
    public static class HarmonyPatches
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string FoodCounter_NearDatePostfix(ref float curBaseY)
        {
            var preference = FoodAlertMod.Selected;
            var label = preference.ToString();
            curBaseY -= 24f;
            return "60 / 32 / 1 : " + label;
        }
    }
}
namespace RimWorld
{
    public enum FoodPreferability { NeverForNutrition, DesperateOnly, RawBad, RawTasty, MealAwful, MealSimple, MealFine, MealLavish, Unknown }
}
namespace Verse
{
    [AttributeUsage(AttributeTargets.Class)] public class StaticConstructorOnStartup : Attribute { }
    public class LoadedLanguage { public string folderName; }
    public static class LanguageDatabase { public static LoadedLanguage activeLanguage; }
    public static class Log { public static void Warning(string message) { throw new Exception(message); } }
    public static class Translator
    {
        public static readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public static bool CanTranslate(this string key) => Values.ContainsKey(key);
        public static string Translate(this string key) => Values[key];
    }
}
