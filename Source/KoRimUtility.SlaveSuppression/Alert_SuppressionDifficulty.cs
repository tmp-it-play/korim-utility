using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    public sealed class Alert_SuppressionDifficulty : Alert
    {
        public Alert_SuppressionDifficulty()
        {
            defaultLabel = "KoRimUtility.Suppression.AlertLabel".Translate();
            defaultPriority = AlertPriority.High;
            requireIdeology = true;
        }

        private static List<Pawn> UncoveredSlaves()
        {
            var result = new List<Pawn>();
            if (!SuppressionPatches.Installed)
                return result;
            foreach (var map in Find.Maps)
            {
                var targets = new List<Pawn>();
                foreach (var slave in map.mapPawns.SlavesOfColonySpawned)
                    if (SuppressionUtility.NeedsSuppression(slave))
                        targets.Add(slave);
                if (targets.Count == 0)
                    continue;
                var covered = false;
                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (!SuppressionUtility.IsAssignedWarden(pawn))
                        continue;
                    covered = true;
                    break;
                }
                if (!covered)
                    result.AddRange(targets);
            }
            return result;
        }

        public override AlertReport GetReport() => AlertReport.CulpritsAre(UncoveredSlaves());

        public override TaggedString GetExplanation()
        {
            var text = new StringBuilder("KoRimUtility.Suppression.AlertExplanation".Translate(
                SuppressionRules.MinimumSocial, SuppressionRules.MinimumPower.ToStringPercent()));
            foreach (var slave in UncoveredSlaves())
                text.AppendLine().Append("  - ").Append(slave.LabelShortCap);
            return text.ToString();
        }
    }
}
