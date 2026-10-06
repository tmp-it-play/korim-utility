using RimWorld;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    internal static class SuppressionUtility
    {
        internal static bool NeedsSuppression(Pawn slave)
        {
            return slave?.IsSlaveOfColony == true &&
                slave.guest?.slaveInteractionMode == SlaveInteractionModeDefOf.Suppress &&
                slave.needs?.TryGetNeed<Need_Suppression>()?.CanBeSuppressedNow == true;
        }

        internal static bool MeetsRequirements(Pawn pawn)
        {
            return pawn?.IsFreeColonist == true && !pawn.IsSlave && !pawn.IsPrisoner &&
                !pawn.Dead && !pawn.Downed && !pawn.InMentalState &&
                !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Warden) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                SuppressionRules.MeetsMinimum(SuppressionPower.Skill(pawn, SkillDefOf.Social),
                    pawn.GetStatValue(StatDefOf.SuppressionPower));
        }

        internal static bool IsAssignedWarden(Pawn pawn)
        {
            return pawn?.workSettings?.WorkIsActive(WorkTypeDefOf.Warden) == true && MeetsRequirements(pawn);
        }

        internal static void TrySuppress(Pawn warden, Pawn slave)
        {
            if (!NeedsSuppression(slave)) return;
            var need = slave.needs?.TryGetNeed<Need_Suppression>();
            if (!MeetsRequirements(warden)) return;
            var before = need.CurLevelPercentage;
            var power = warden.GetStatValue(StatDefOf.SuppressionPower);
            var chance = SuppressionRules.SuccessChance(SuppressionPower.Skill(warden, SkillDefOf.Social),
                power, SuppressionPower.Evaluate(slave, false).CombatTotal, before, SuppressionMod.Settings.difficulty);
            // Only a real interaction consumes randomness; previews and alert scans never roll.
            if (!Rand.Chance(chance)) return;
            var gain = SuppressionRules.Gain(power, before, SuppressionMod.Settings.gainMultiplier);
            if (gain <= 0f) return;
            SlaveRebellionUtility.IncrementSuppression(need, warden, slave, gain);
            if (need.CurLevelPercentage > before) SuppressionMemory.Apply(slave, before);
        }
    }
}
