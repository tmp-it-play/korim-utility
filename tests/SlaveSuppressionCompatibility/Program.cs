using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using KoRimUtility.SlaveSuppression;
using RimWorld;
using Verse;

internal static class Program
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks++;
    }

    private static void Equal(float actual, float expected, string message)
        => Check(Math.Abs(actual - expected) < 0.00001f, message + $": {actual} != {expected}");

    private static Pawn Warden(int social = 8, float power = 0.4f) => new Pawn
    {
        Social = social, Power = power, IsFreeColonist = true, Faction = new Faction { IsPlayer = true }
    };

    private static Pawn Slave(float suppression = 0.1f)
    {
        var pawn = new Pawn { IsSlave = true, IsSlaveOfColony = true, Combat = 0.1f };
        pawn.needs.suppression.CurLevelPercentage = suppression;
        pawn.guest.slaveInteractionMode = SlaveInteractionModeDefOf.Suppress;
        return pawn;
    }

    public static void Main(string[] args)
    {
        RuntimeHelpers.RunClassConstructor(typeof(SuppressionPatches).TypeHandle);
        Check(SuppressionPatches.Installed, "Real Harmony hooks must install");
        VerifyMath();
        VerifyJobsAndInteractions();
        VerifyMood();
        VerifyAlerts();
        var root = args.Length > 0 ? args[0] : Path.GetFullPath(".");
        var thought = XDocument.Load(Path.Combine(root,
            "Integrations/SlaveSuppression/Defs/ThoughtDefs/Suppression.xml")).Root.Element("ThoughtDef");
        Equal((float)thought.Element("durationDays"), 1f, "Fixed one-day expiry");
        Check((int)thought.Element("stackLimit") == 1 && !(bool)thought.Element("stagesStack"), "No stacking");
        Check(!(bool)thought.Element("lerpMoodToZero"), "Full effect until expiry");
        var effects = thought.Element("stages").Elements().Select(x => (float)x.Element("baseMoodEffect")).ToArray();
        Check(effects.SequenceEqual(new[] { -5f, -10f, -15f, -2.5f, -5f, -7.5f, 5f }), "Mood values and stage order");
        Console.WriteLine($"Slave suppression: {checks} checks passed (real Harmony; game-state test doubles).");
    }

    private static void VerifyMath()
    {
        Check(!SuppressionRules.MeetsMinimum(2, 1f), "High power cannot bypass Social gate");
        Check(!SuppressionRules.MeetsMinimum(20, 0.199f), "High social cannot bypass power gate");
        Check(SuppressionRules.MeetsMinimum(3, 0.2f), "Inclusive minimum");
        Check(!SuppressionRules.MeetsMinimum(3, float.NaN), "Malformed stats cannot pass");
        Equal(SuppressionRules.TitleBonus(0), 0f, "Freeholder has no title bonus");
        Equal(SuppressionRules.TitleBonus(100), 0.01f, "Yeoman has a small title bonus");
        Equal(SuppressionRules.TitleBonus(600), 0.06f, "Count reaches the title cap");
        Equal(SuppressionRules.TitleBonus(1000), 0.06f, "Higher/modded titles cannot exceed the cap");
        foreach (var difficulty in new[] { 0.25f, 1f, 2.5f })
        {
            Equal(SuppressionRules.SuccessChance(2, 1f, 0f, 0.6f, difficulty), 0f, "Difficulty preserves skill gate");
            Equal(SuppressionRules.SuccessChance(20, 0.19f, 0f, 0.6f, difficulty), 0f, "Difficulty preserves power gate");
        }
        var ordinary = SuppressionRules.SuccessChance(3, 0.3f, 0.1f, 0.3f, 1f);
        Check(SuppressionRules.SuccessChance(3, 0.3f, 0.1f, 0.3f, 0.25f) > ordinary, "Lower difficulty increases chance");
        Check(SuppressionRules.SuccessChance(3, 0.3f, 0.1f, 0.3f, 2.5f) < ordinary, "Higher difficulty reduces chance");
        Check(SuppressionRules.SuccessChance(3, 0.6f, 0.1f, 0.3f, 1f) > ordinary, "Stronger warden improves chance");
        Check(SuppressionRules.SuccessChance(3, 0.3f, 0.3f, 0.3f, 1f) < ordinary, "Stronger slave resists");
        Check(SuppressionRules.SuccessChance(3, 0.3f, 0.1f, 0f, 1f) < ordinary, "Defiant slave resists");
        Equal(SuppressionRules.Gain(0.2f, 0f, 1f), 0.4f, "Vanilla low-suppression recovery");
        Equal(SuppressionRules.Gain(0.2f, 0.5f, 1f), 0.2f, "Vanilla mid-suppression recovery");
        Equal(SuppressionRules.Gain(0.2f, 0.6f, 0.5f), 0.09f, "Gain slider scales amount");
        Equal(SuppressionRules.Gain(1f, 0.69f, 3f), 0.31f, "Gain cannot exceed remaining room");
        Equal(SuppressionRules.Gain(1f, 1f, 3f), 0f, "Full suppression has no gain");
        Check(float.IsFinite(SuppressionRules.Gain(0.2f, 0f, float.NaN)), "Invalid setting gets safe default");
        Check(SuppressionRules.MoodStage(0.30f, false, false) == 0, "30% is mild");
        Check(SuppressionRules.MoodStage(0.15f, false, false) == 1, "15% is medium");
        Check(SuppressionRules.MoodStage(0.149f, false, false) == 2, "Below 15% is severe");
        foreach (var level in new[] { 0f, 0.15f, 0.3f, 0.69f })
            Check(SuppressionRules.MoodStage(level, true, true) == 6 &&
                SuppressionRules.MoodStage(level, true, false) == 6, "Masochist always receives the fixed buff");
    }

    private static void VerifyJobsAndInteractions()
    {
        var warden = Warden();
        var slave = Slave();
        var giver = new WorkGiver_Warden_SuppressSlave();
        Check(giver.JobOnThing(warden, slave) != null, "Qualified automatic job accepted");
        warden.Social = 2;
        Check(giver.JobOnThing(warden, slave) == null && giver.JobOnThing(warden, slave, true) == null,
            "Neither auto nor forced jobs bypass fixed threshold");
        warden.Social = 8;
        giver.VanillaAccepts = false;
        Check(giver.JobOnThing(warden, slave) == null, "Never create a job rejected by vanilla (including cooldown)");
        giver.VanillaAccepts = true;
        warden.health.capacities.Talking = false;
        Check(giver.JobOnThing(warden, slave) == null, "Incapable of talking cannot suppress");
        warden.health.capacities.Talking = true;

        Rand.Next = 0.999f;
        slave.needs.mood.thoughts.memories.TryGainMemoryFast(SuppressionDefOf.KoRimUtility_Suppressed, 0);
        var memory = slave.needs.mood.thoughts.memories.Memory;
        memory.Age = 12345;
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Equal(slave.needs.suppression.CurLevelPercentage, 0.1f, "Failure leaves suppression unchanged");
        Check(memory.Age == 12345 && memory.Stage == 0, "Failure never changes or renews existing mood");
        Check(SlaveRebellionUtility.SuccessMotes == 0, "Failure has no success text");
        Rand.Next = 0f;
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Check(slave.needs.suppression.CurLevelPercentage > 0.3f, "Success raises suppression");
        Check(memory.Stage == 2, "Mood uses the pre-interaction suppression");
        Check(memory.Age == 0 && slave.needs.mood.thoughts.memories.Count == 1, "Success replaces and renews memory");
        Check(warden.needs.mood.thoughts.memories.Count == 0, "Warden never receives slave mood");
        Check(SlaveRebellionUtility.VanillaCalls == 0, "Colony interaction does not also apply vanilla gain");

        slave.needs.suppression.CurLevelPercentage = 0.2f;
        memory.Age = 25000;
        warden.Power = 0.1f;
        var rolls = Rand.Rolls;
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Equal(slave.needs.suppression.CurLevelPercentage, 0.2f, "Equipment changes rechecked at interaction");
        Check(Rand.Rolls == rolls && memory.Age == 25000, "Ineligible interaction consumes neither RNG nor memory");
        warden.Power = 0.4f;
        slave.guest.slaveInteractionMode = new SlaveInteractionModeDef();
        var statReads = Pawn.StatReads;
        rolls = Rand.Rolls;
        Check(giver.JobOnThing(warden, slave, true) == null, "Changed slave treatment rejects even forced jobs");
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Equal(slave.needs.suppression.CurLevelPercentage, 0.2f, "Changed treatment blocks a previously accepted interaction");
        Check(Pawn.StatReads == statReads && Rand.Rolls == rolls && memory.Age == 25000,
            "Non-suppression treatment skips power, success roll and mood renewal");
        slave.guest.slaveInteractionMode = SlaveInteractionModeDefOf.Suppress;
        slave.needs.suppression.CurLevelPercentage = 0.8f;
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Equal(slave.needs.suppression.CurLevelPercentage, 0.8f, "No unnecessary suppression above vanilla threshold");
        warden.Faction.IsPlayer = false;
        SlaveRebellionUtility.IncrementInteractionSuppression(warden, slave);
        Check(SlaveRebellionUtility.VanillaCalls == 1, "Non-player behavior stays vanilla");
    }

    private static void VerifyMood()
    {
        var slave = Slave();
        SuppressionMemory.Apply(slave, 0.1f);
        var memory = slave.needs.mood.thoughts.memories.Memory;
        Check(memory.Stage == 2, "Severe penalty");
        memory.Age = 50000;
        SuppressionMemory.Apply(slave, 0.5f);
        Check(memory.Stage == 0 && memory.Age == 0, "Milder latest success replaces severe penalty and restarts expiry");
        slave.Ideo = new Ideo { Honorable = true };
        SuppressionMemory.Apply(slave, 0.2f);
        Check(memory.Stage == 4, "Slave's own ideology halves the penalty");
        slave.story.traits.Masochist = true;
        SuppressionMemory.Apply(slave, 0.01f);
        Check(memory.Stage == 6 && slave.needs.mood.thoughts.memories.Count == 1, "Buff replaces penalty, no mixed stacking");
        slave.story.traits.Masochist = false;
        SuppressionMemory.Apply(slave, 0.01f);
        Check(memory.Stage == 5, "Trait change replaces old buff on next success");
        slave.needs.mood = null;
        SuppressionMemory.Apply(slave, 0.01f);
        Check(true, "Moodless pawns are supported");
    }

    private static void VerifyAlerts()
    {
        Find.Maps.Clear();
        var first = new Map();
        var second = new Map();
        Find.Maps.Add(first);
        Find.Maps.Add(second);
        var slave = Slave();
        first.mapPawns.SlavesOfColonySpawned.Add(slave);
        var alert = new Alert_SuppressionDifficulty();
        Check(alert.GetReport().active, "No colonists triggers warning");
        var warden = Warden();
        second.mapPawns.FreeColonistsSpawned.Add(warden);
        Check(alert.GetReport().active, "Another map's warden cannot cover this colony");
        first.mapPawns.FreeColonistsSpawned.Add(warden);
        warden.workSettings.Active = false;
        Check(alert.GetReport().active, "Unassigned capable colonist still requires action");
        warden.workSettings.Active = true;
        Check(!alert.GetReport().active, "One assigned capable warden clears alert");
        warden.Downed = true;
        Check(alert.GetReport().active, "Downed only warden triggers warning");
        warden.Downed = false;
        warden.Social = 2;
        Check(alert.GetReport().active, "Unqualified warden triggers warning");
        slave.needs.suppression.CurLevelPercentage = 0.9f;
        Check(!alert.GetReport().active, "No suppression demand produces no warning");
        slave.needs.suppression.CurLevelPercentage = 0.1f;
        slave.guest.slaveInteractionMode = new SlaveInteractionModeDef();
        warden.Social = 8;
        var statReads = Pawn.StatReads;
        Check(!alert.GetReport().active, "Do not warn about intentionally disabled suppression");
        Check(Pawn.StatReads == statReads, "No suppression targets means no warden power calculations");
        slave.guest.slaveInteractionMode = SlaveInteractionModeDefOf.Suppress;
        slave.needs.suppression.CurLevelPercentage = 0.9f;
        Check(!alert.GetReport().active && Pawn.StatReads == statReads, "High suppression skips warden calculations too");
        slave.needs.suppression.CurLevelPercentage = 0.1f;
        warden.workSettings.Active = false;
        Check(alert.GetReport().active && Pawn.StatReads == statReads, "Unassigned wardens skip power calculations");
        warden.workSettings.Active = true;
        Check(!alert.GetReport().active && Pawn.StatReads > statReads, "Restoring suppression mode resumes qualification checks");
    }
}
