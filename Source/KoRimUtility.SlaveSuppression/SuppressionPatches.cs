using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace KoRimUtility.SlaveSuppression
{
    [StaticConstructorOnStartup]
    internal static class SuppressionPatches
    {
        internal static bool Installed
        {
            get; private set;
        }

        static SuppressionPatches()
        {
            if (!ModsConfig.IdeologyActive)
                return;
            var harmony = new Harmony("snowykte0426.korimutility.slavesuppression");
            try
            {
                harmony.Patch(AccessTools.Method(typeof(WorkGiver_Warden_SuppressSlave), "JobOnThing"),
                    postfix: new HarmonyMethod(typeof(SuppressionPatches), nameof(JobPostfix)));
                harmony.Patch(AccessTools.Method(typeof(SlaveRebellionUtility), "IncrementInteractionSuppression"),
                    prefix: new HarmonyMethod(typeof(SuppressionPatches), nameof(InteractionPrefix)));
                Installed = true;
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Error("[KoRim Utility] Slave suppression could not be enabled: " + exception);
            }
        }

        private static void JobPostfix(Pawn pawn, Thing t, ref Job __result)
        {
            if (__result == null || !(t is Pawn slave) || !slave.IsSlaveOfColony)
                return;
            if (!SuppressionUtility.NeedsSuppression(slave))
            {
                __result = null;
                return;
            }
            if (!SuppressionUtility.MeetsRequirements(pawn))
            {
                __result = null;
                JobFailReason.Is("KoRimUtility.Suppression.Requirements".Translate(
                    SuppressionRules.MinimumSocial, SuppressionRules.MinimumPower.ToStringPercent()));
            }
        }

        private static bool InteractionPrefix(Pawn initiator, Pawn recipient)
        {
            if (initiator?.Faction?.IsPlayer != true || recipient?.IsSlaveOfColony != true)
                return true;
            SuppressionUtility.TrySuppress(initiator, recipient);
            return false;
        }
    }
}
