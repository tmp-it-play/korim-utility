using RimWorld;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    [DefOf]
    public static class SuppressionDefOf
    {
        public static ThoughtDef KoRimUtility_Suppressed;
        public static TraitDef Masochist;
        public static PreceptDef Slavery_Honorable;

        static SuppressionDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SuppressionDefOf));
    }

    internal static class SuppressionMemory
    {
        internal static void Apply(Pawn slave, float previousSuppression)
        {
            var memories = slave.needs?.mood?.thoughts?.memories;
            if (memories == null) return;
            var masochist = slave.story?.traits?.HasTrait(SuppressionDefOf.Masochist) == true;
            var approvesSlavery = slave.Ideo?.HasPrecept(SuppressionDefOf.Slavery_Honorable) == true;
            var stage = SuppressionRules.MoodStage(previousSuppression, masochist, approvesSlavery);
            // One definition for every outcome; changing ideology/trait cannot leave both buff and debuff.
            memories.TryGainMemoryFast(SuppressionDefOf.KoRimUtility_Suppressed, stage);
        }
    }
}
