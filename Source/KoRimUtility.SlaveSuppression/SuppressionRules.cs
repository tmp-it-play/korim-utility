using System;

namespace KoRimUtility.SlaveSuppression
{
    internal static class SuppressionRules
    {
        internal const int MinimumSocial = 3;
        internal const float MinimumPower = 0.20f;
        internal const float MinimumGainMultiplier = 0.25f;
        internal const float MaximumGainMultiplier = 3f;
        internal const float MinimumDifficulty = 0.25f;
        internal const float MaximumDifficulty = 2.5f;

        internal static float Clamp(float value, float min, float max, float fallback = 0f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                value = fallback;
            return Math.Max(min, Math.Min(max, value));
        }

        internal static bool MeetsMinimum(int social, float power)
        {
            return social >= MinimumSocial && !float.IsNaN(power) &&
                !float.IsInfinity(power) && power >= MinimumPower;
        }

        internal static float TitleBonus(int seniority)
        {
            // Vanilla imperial ranks use steps of 100; Count has seniority 600.
            return 0.06f * Clamp(seniority / 600f, 0f, 1f);
        }

        internal static float SuccessChance(int social, float power, float slaveCombat,
            float suppression, float difficulty)
        {
            if (!MeetsMinimum(social, power))
                return 0f;
            var resistance = 0.12f + Clamp(slaveCombat, 0f, 0.36f) +
                0.15f * (1f - Clamp(suppression, 0f, 1f));
            var ratio = resistance * Clamp(difficulty, MinimumDifficulty, MaximumDifficulty, 1f) /
                Clamp(power, MinimumPower, 1f);
            return Clamp(1f / (1f + ratio * ratio), 0.05f, 0.95f);
        }

        internal static float Gain(float power, float suppression, float multiplier)
        {
            var level = Clamp(suppression, 0f, 1f);
            // Preserve vanilla's recovery curve: 2 at 0%, 1 at 50%, 0.5 at 100%.
            var recovery = level <= 0.5f ? 2f - 2f * level : 1.5f - level;
            return Math.Min(1f - level, Clamp(power, 0f, 1f) * recovery *
                Clamp(multiplier, MinimumGainMultiplier, MaximumGainMultiplier, 1f));
        }

        internal static int MoodStage(float previousSuppression, bool masochist, bool approvesSlavery)
        {
            if (masochist)
                return 6;
            var severity = previousSuppression < 0.15f ? 2 : previousSuppression < 0.30f ? 1 : 0;
            return severity + (approvesSlavery ? 3 : 0);
        }
    }
}
