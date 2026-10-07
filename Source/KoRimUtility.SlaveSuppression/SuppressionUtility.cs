using RimWorld;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    internal static class SuppressionUtility
    {
        internal static bool NeedsSuppression(Pawn slave) => SuppressionNeed(slave) != null;

        private static Need_Suppression SuppressionNeed(Pawn slave)
        {
            if (slave?.IsSlaveOfColony != true ||
                slave.guest?.slaveInteractionMode != SlaveInteractionModeDefOf.Suppress)
                return null;
            var need = slave.needs?.TryGetNeed<Need_Suppression>();
            return need?.CanBeSuppressedNow == true ? need : null;
        }

        internal static bool MeetsRequirements(Pawn pawn) => TryGetWardenStats(pawn, out _, out _);

        private static bool TryGetWardenStats(Pawn pawn, out int social, out float power)
        {
            social = 0;
            power = 0f;
            if (pawn?.IsFreeColonist != true || pawn.IsSlave || pawn.IsPrisoner ||
                pawn.Dead || pawn.Downed || pawn.InMentalState ||
                pawn.WorkTypeIsDisabled(WorkTypeDefOf.Warden) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking))
                return false;
            social = SuppressionPower.Skill(pawn, SkillDefOf.Social);
            power = pawn.GetStatValue(StatDefOf.SuppressionPower);
            return SuppressionRules.MeetsMinimum(social, power);
        }

        internal static bool IsAssignedWarden(Pawn pawn)
        {
            return pawn?.workSettings?.WorkIsActive(WorkTypeDefOf.Warden) == true && MeetsRequirements(pawn);
        }

        internal static void TrySuppress(Pawn warden, Pawn slave)
        {
            var need = SuppressionNeed(slave);
            if (need == null || !TryGetWardenStats(warden, out var social, out var power))
                return;
            var before = need.CurLevelPercentage;
            var chance = SuppressionRules.SuccessChance(social,
                power, SuppressionPower.Evaluate(slave, false).CombatTotal, before, SuppressionMod.Settings.difficulty);
            if (!Rand.Chance(chance))
                return;
            var gain = SuppressionRules.Gain(power, before, SuppressionMod.Settings.gainMultiplier);
            if (gain <= 0f)
                return;
            SlaveRebellionUtility.IncrementSuppression(need, warden, slave, gain);
            if (need.CurLevelPercentage > before)
                SuppressionMemory.Apply(slave, before);
        }
    }
}
