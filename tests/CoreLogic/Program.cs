using System;
using KoRimUtility.SlaveSuppression;

internal static class Program
{
    private static int cases;

    private static void Main()
    {
        var tests = new Action[] { Clamp, MinimumRequirements, TitleBonus, SuccessChance, Gain, MoodStage };
        foreach (var test in tests)
        {
            test();
            Console.WriteLine($"PASS {test.Method.Name}");
        }
        Console.WriteLine($"Core logic: {tests.Length} unit tests, {cases} cases passed.");
    }

    private static void Clamp()
    {
        var conditions = new[] { (-1f, 0f), (0.4f, 0.4f), (2f, 1f), (float.NaN, 0.5f), (float.PositiveInfinity, 0.5f) };
        foreach (var (value, expected) in conditions)
        {
            var actual = SuppressionRules.Clamp(value, 0f, 1f, 0.5f);

            Equal(expected, actual);
        }
    }

    private static void MinimumRequirements()
    {
        var conditions = new[]
        {
            (3, 0.2f, true), (2, 1f, false), (20, 0.199f, false),
            (3, float.NaN, false), (3, float.PositiveInfinity, false), (3, float.NegativeInfinity, false)
        };
        foreach (var (social, power, expected) in conditions)
        {
            var actual = SuppressionRules.MeetsMinimum(social, power);

            Equal(expected, actual);
        }
    }

    private static void TitleBonus()
    {
        var conditions = new[] { (0, 0f), (100, 0.01f), (600, 0.06f), (1000, 0.06f) };
        foreach (var (seniority, expected) in conditions)
        {
            var actual = SuppressionRules.TitleBonus(seniority);

            Equal(expected, actual);
        }
    }

    private static void SuccessChance()
    {
        var conditions = new[]
        {
            (2, 1f, 0f, 0.6f, 1f, 0f), (20, 0.19f, 0f, 0.6f, 1f, 0f),
            (3, 0.2f, 0f, 0.6f, 1f, 0.5524862f),
            (3, 0.2f, 0.36f, 0f, 2.5f, 0.05f), (3, 1f, 0f, 1f, 0.25f, 0.95f)
        };
        foreach (var (social, power, combat, suppression, difficulty, expected) in conditions)
        {
            var actual = SuppressionRules.SuccessChance(social, power, combat, suppression, difficulty);

            Equal(expected, actual);
        }
    }

    private static void Gain()
    {
        var conditions = new[]
        {
            (0.2f, 0f, 1f, 0.4f), (0.2f, 0.5f, 1f, 0.2f), (0.2f, 0.6f, 0.5f, 0.09f),
            (1f, 0.69f, 3f, 0.31f), (1f, 1f, 3f, 0f), (0.2f, 0f, float.NaN, 0.4f)
        };
        foreach (var (power, suppression, multiplier, expected) in conditions)
        {
            var actual = SuppressionRules.Gain(power, suppression, multiplier);

            Equal(expected, actual);
        }
    }

    private static void MoodStage()
    {
        var conditions = new[]
        {
            (0.3f, false, false, 0), (0.15f, false, false, 1), (0.1f, false, false, 2),
            (0.3f, false, true, 3), (0.2f, false, true, 4), (0.1f, false, true, 5),
            (0.1f, true, false, 6), (0.1f, true, true, 6)
        };
        foreach (var (suppression, masochist, approvesSlavery, expected) in conditions)
        {
            var actual = SuppressionRules.MoodStage(suppression, masochist, approvesSlavery);

            Equal(expected, actual);
        }
    }

    private static void Equal(object expected, object actual)
    {
        var equal = expected is float number && actual is float result
            ? Math.Abs(number - result) < 0.00001f : Equals(expected, actual);
        if (!equal)
            throw new Exception($"Expected {expected}, got {actual}");
        cases++;
    }
}
