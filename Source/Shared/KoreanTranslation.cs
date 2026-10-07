using System;
using Verse;

namespace KoRimUtility
{
    internal static class KoreanTranslation
    {
        internal static bool IsActive =>
            LanguageDatabase.activeLanguage?.folderName?.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) == true;

        internal static bool CanTranslate(string key) => IsActive && key.CanTranslate();

        internal static string TranslateOrOriginal(string key, string original) =>
            CanTranslate(key) ? key.Translate().ToString() : original;
    }
}
