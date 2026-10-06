using UnityEngine;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    public sealed class SuppressionSettings : ModSettings
    {
        public float gainMultiplier = 1f;
        public float difficulty = 1f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref gainMultiplier, "suppressionGainMultiplier", 1f);
            Scribe_Values.Look(ref difficulty, "suppressionDifficulty", 1f);
            gainMultiplier = SuppressionRules.Clamp(gainMultiplier,
                SuppressionRules.MinimumGainMultiplier, SuppressionRules.MaximumGainMultiplier, 1f);
            difficulty = SuppressionRules.Clamp(difficulty,
                SuppressionRules.MinimumDifficulty, SuppressionRules.MaximumDifficulty, 1f);
        }
    }

    public sealed class SuppressionMod : Mod
    {
        internal static SuppressionSettings Settings = new SuppressionSettings();

        public SuppressionMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SuppressionSettings>();
        }

        public override string SettingsCategory() => "KoRim Utility";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("KoRimUtility.Suppression.SettingsTitle".Translate());
            listing.Gap();
            listing.Label("KoRimUtility.Suppression.GainSetting".Translate(Settings.gainMultiplier.ToString("0.00")));
            Settings.gainMultiplier = RoundSlider(listing.Slider(Settings.gainMultiplier,
                SuppressionRules.MinimumGainMultiplier, SuppressionRules.MaximumGainMultiplier));
            listing.Label("KoRimUtility.Suppression.GainHelp".Translate());
            listing.Gap();
            listing.Label("KoRimUtility.Suppression.DifficultySetting".Translate(Settings.difficulty.ToString("0.00")));
            Settings.difficulty = RoundSlider(listing.Slider(Settings.difficulty,
                SuppressionRules.MinimumDifficulty, SuppressionRules.MaximumDifficulty));
            listing.Label("KoRimUtility.Suppression.DifficultyHelp".Translate(
                SuppressionRules.MinimumSocial, SuppressionRules.MinimumPower.ToStringPercent()));
            listing.Gap();
            listing.Label("KoRimUtility.Suppression.MoodHelp".Translate());
            if (listing.ButtonText("KoRimUtility.Suppression.ResetSettings".Translate()))
            {
                Settings.gainMultiplier = 1f;
                Settings.difficulty = 1f;
            }
            listing.End();
        }

        private static float RoundSlider(float value) => Mathf.Round(value * 20f) / 20f;
    }
}
