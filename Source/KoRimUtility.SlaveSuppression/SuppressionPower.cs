using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace KoRimUtility.SlaveSuppression
{
    internal struct PowerBreakdown
    {
        internal float combat, weapon, armor, augmentation, status, wealth;
        internal float CombatTotal => combat + weapon + armor + augmentation;
        internal float Total => CombatTotal + status + wealth;
    }

    internal static class SuppressionPower
    {
        internal static int Skill(Pawn pawn, SkillDef skill)
        {
            var record = pawn.skills?.GetSkill(skill);
            return record == null || record.TotallyDisabled ? 0 : record.Level;
        }

        internal static PowerBreakdown Evaluate(Pawn pawn, bool includeAuthority = true)
        {
            var result = new PowerBreakdown();
            if (pawn?.RaceProps?.Humanlike != true) return result;
            var manipulation = Capacity(pawn, PawnCapacityDefOf.Manipulation);
            var moving = Capacity(pawn, PawnCapacityDefOf.Moving);
            var sight = Capacity(pawn, PawnCapacityDefOf.Sight);
            var readiness = Mathf.Clamp01(Mathf.Min(Capacity(pawn, PawnCapacityDefOf.Consciousness),
                Mathf.Min(manipulation, moving)));
            if (!pawn.WorkTagIsDisabled(WorkTags.Violent))
            {
                result.combat = 0.12f * Mathf.Clamp01(Mathf.Max(Skill(pawn, SkillDefOf.Shooting),
                    Skill(pawn, SkillDefOf.Melee)) / 20f) * readiness;
                result.weapon = 0.12f * Mathf.Clamp01(WeaponDps(pawn) / 20f) * readiness;
            }
            result.armor = 0.06f * Mathf.Clamp01(Armor(pawn) / 1.5f);
            // Count the actual capability improvement, not the price of the implant a second time.
            result.augmentation = 0.06f * Mathf.Clamp01((Mathf.Max(0f, manipulation - 1f) +
                Mathf.Max(0f, moving - 1f) + Mathf.Max(0f, sight - 1f)) / 1.5f) * readiness;
            if (!includeAuthority) return result;
            if (ModsConfig.RoyaltyActive && pawn.royalty?.MostSeniorTitle != null)
                result.status = SuppressionRules.TitleBonus(pawn.royalty.MostSeniorTitle.def.seniority);
            if (pawn.Ideo != null)
            {
                foreach (var precept in pawn.Ideo.PreceptsListForReading)
                {
                    if (precept is Precept_Role role && role.Active && role.IsAssigned(pawn))
                        result.status = Mathf.Max(result.status, role.def.leaderRole ? 0.06f : 0.03f);
                }
            }
            // Use the game's cached colony wealth; it cannot substitute for the social minimum.
            if (pawn.IsFreeColonist && pawn.Spawned && pawn.Map.IsPlayerHome)
                result.wealth = 0.03f * Mathf.Clamp01(Mathf.Log10(1f +
                    Mathf.Max(0f, pawn.Map.wealthWatcher.WealthTotal) / 10000f) / 2f);
            return result;
        }

        private static float Capacity(Pawn pawn, PawnCapacityDef capacity)
        {
            return pawn.health?.capacities == null ? 0f :
                SuppressionRules.Clamp(pawn.health.capacities.GetLevel(capacity), 0f, 3f);
        }

        private static float WeaponDps(Pawn pawn)
        {
            var best = pawn.GetStatValue(StatDefOf.MeleeDPS);
            var weapon = pawn.equipment?.Primary;
            if (weapon?.def.Verbs == null) return best;
            foreach (var verb in weapon.def.Verbs)
            {
                var projectile = verb.defaultProjectile?.projectile;
                if (projectile == null || projectile.damageDef?.harmsHealth != true) continue;
                var burst = Mathf.Max(1, verb.burstShotCount);
                var cycle = verb.warmupTime * weapon.GetStatValue(StatDefOf.RangedWeapon_WarmupMultiplier) *
                    pawn.GetStatValue(StatDefOf.AimingDelayFactor) + weapon.GetStatValue(StatDefOf.RangedWeapon_Cooldown) +
                    (burst - 1) * verb.ticksBetweenBurstShots / 60f;
                var accuracy = Mathf.Clamp01((weapon.GetStatValue(StatDefOf.AccuracyShort) +
                    weapon.GetStatValue(StatDefOf.AccuracyMedium)) / 2f) *
                    Mathf.Clamp01(pawn.GetStatValue(StatDefOf.ShootingAccuracyPawn));
                best = Mathf.Max(best, projectile.GetDamageAmount(weapon) * burst * accuracy / Mathf.Max(0.1f, cycle));
            }
            return SuppressionRules.Clamp(best, 0f, 1000f);
        }

        private static float Armor(Pawn pawn)
        {
            var naturalArmor = pawn.GetStatValue(StatDefOf.ArmorRating_Sharp);
            if (pawn.apparel == null) return naturalArmor;
            var weighted = 0f;
            var coverage = 0f;
            foreach (var part in pawn.RaceProps.body.AllParts)
            {
                if (part.depth != BodyPartDepth.Outside || part.coverageAbs <= 0f) continue;
                var best = naturalArmor;
                foreach (var apparel in pawn.apparel.WornApparel)
                    if (apparel.def.apparel.CoversBodyPart(part))
                        best = Mathf.Max(best, apparel.GetStatValue(StatDefOf.ArmorRating_Sharp));
                weighted += best * part.coverageAbs;
                coverage += part.coverageAbs;
            }
            return coverage > 0f ? weighted / coverage : naturalArmor;
        }
    }

    public sealed class StatPart_SuppressionAuthority : StatPart
    {
        public override void TransformValue(StatRequest req, ref float val)
        {
            if (SuppressionPatches.Installed && req.Thing is Pawn pawn)
                val += SuppressionPower.Evaluate(pawn).Total;
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (!SuppressionPatches.Installed || !(req.Thing is Pawn pawn)) return null;
            var values = SuppressionPower.Evaluate(pawn);
            var text = new StringBuilder();
            Add(text, "Combat", values.combat);
            Add(text, "Weapon", values.weapon);
            Add(text, "Armor", values.armor);
            Add(text, "Augmentation", values.augmentation);
            Add(text, "Status", values.status);
            Add(text, "Wealth", values.wealth);
            text.Append("KoRimUtility.Suppression.Requirements".Translate(
                SuppressionRules.MinimumSocial, SuppressionRules.MinimumPower.ToStringPercent()));
            return text.ToString();
        }

        private static void Add(StringBuilder text, string key, float value)
        {
            text.AppendLine("KoRimUtility.Suppression.Power".Translate(
                ("KoRimUtility.Suppression." + key).Translate(), value.ToStringPercent()));
        }
    }
}
