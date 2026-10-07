using UnityEngine;
using Verse;

namespace KoRimUtility
{
    public sealed class KoRimUtilityMod : Mod
    {
        public KoRimUtilityMod(ModContentPack content) : base(content) { }

        public override string SettingsCategory() => "KoRimUtility.SettingsTitle".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("KoRimUtility.SettingsIntro".Translate());
            listing.Gap();
            listing.Label("KoRimUtility.SettingsPlaceholder".Translate());
            listing.End();
        }
    }
}
