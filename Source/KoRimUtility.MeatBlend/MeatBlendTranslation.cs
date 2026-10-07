using System;
using Verse;

namespace KoRimUtility.MeatBlend
{
    [StaticConstructorOnStartup]
    internal static class MeatBlendTranslation
    {
        private const string Prefix = "KoRimUtility.MeatBlend.";

        static MeatBlendTranslation()
        {
            if (KoreanTranslation.IsActive)
                LongEventHandler.ExecuteWhenFinished(Apply);
        }

        private static bool IsFrom(Def def, string packageId) =>
            string.Equals(def.modContentPack?.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase);

        private static void Apply()
        {
            TranslateVariant("sneaks.meatblend", "MeatblendRaw", false);
            TranslateVariant("sneaks.meatblendcheat", "MeatBlendCheatRaw", true);
        }

        private static void TranslateVariant(string packageId, string itemName, bool cheat)
        {
            var item = DefDatabase<ThingDef>.GetNamedSilentFail(itemName);
            if (item == null || !IsFrom(item, packageId)) return;

            item.label = KoreanTranslation.TranslateOrOriginal(Prefix + (cheat ? "CheatLabel" : "Label"), item.label);
            item.description = KoreanTranslation.TranslateOrOriginal(Prefix + "Description", item.description);
            item.ClearCachedData();

            // Older releases have fewer bills. Match recipes by their product so
            // translation does not depend on a fixed list of recipe names.
            foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                if (!IsFrom(recipe, packageId) || recipe.products == null || recipe.products.Count != 1 ||
                    recipe.products[0].thingDef != item) continue;

                int count = recipe.products[0].count;
                string labelKey = Prefix + (cheat ? "CheatRecipeLabel" : "RecipeLabel");
                if (KoreanTranslation.CanTranslate(labelKey))
                    recipe.label = labelKey.Translate(count).ToString();
                if (KoreanTranslation.CanTranslate(Prefix + "RecipeDescription"))
                    recipe.description = (Prefix + "RecipeDescription").Translate(count).ToString();
                recipe.jobString = KoreanTranslation.TranslateOrOriginal(Prefix + "Job", recipe.jobString);
                recipe.ClearCachedData();
            }
        }
    }
}
