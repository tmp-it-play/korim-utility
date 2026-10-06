// Narrow game-state doubles. Production also builds against RimWorld's real API.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse.AI;

namespace Verse
{
    public sealed class StaticConstructorOnStartupAttribute : Attribute { }
    public static class ModsConfig { public static bool IdeologyActive = true; }
    public static class Log { public static void Error(string text) => throw new Exception(text); }
    public struct TaggedString
    {
        private string value;
        public static implicit operator TaggedString(string text) => new TaggedString { value = text };
        public static implicit operator string(TaggedString text) => text.value;
        public override string ToString() => value;
    }
    public static class TextExtensions
    {
        public static TaggedString Translate(this string text, params object[] args) => text;
        public static string ToStringPercent(this float value) => value.ToString("P0");
    }
    public static class Find { public static readonly List<Map> Maps = new(); }
    public static class Rand
    {
        public static float Next;
        public static int Rolls;
        public static bool Chance(float chance) { Rolls++; return Next < chance; }
    }
    public class Thing { }
    public sealed class Pawn : Thing
    {
        public static int StatReads;
        public bool IsFreeColonist, IsSlave, IsPrisoner, IsSlaveOfColony, Dead, Downed, InMentalState, WardenDisabled;
        public int Social;
        public float Power, Combat;
        public RimWorld.Faction Faction;
        public RimWorld.Ideo Ideo;
        public Health health = new();
        public Needs needs = new();
        public Story story = new();
        public Guest guest = new();
        public WorkSettings workSettings = new();
        public string LabelShortCap => "Test pawn";
        public bool WorkTypeIsDisabled(RimWorld.WorkTypeDef type) => WardenDisabled;
        public float GetStatValue(RimWorld.StatDef stat) { StatReads++; return Power; }
    }
    public sealed class Health { public Capacities capacities = new(); }
    public sealed class Capacities
    {
        public bool Talking = true, Manipulation = true;
        public bool CapableOf(RimWorld.PawnCapacityDef type)
            => type == RimWorld.PawnCapacityDefOf.Talking ? Talking : Manipulation;
    }
    public sealed class Needs
    {
        public RimWorld.Need_Suppression suppression = new();
        public Mood mood = new();
        public T TryGetNeed<T>() where T : class => suppression as T;
    }
    public sealed class Mood { public Thoughts thoughts = new(); }
    public sealed class Thoughts { public Memories memories = new(); }
    public sealed class Memory { public int Age, Stage; }
    public sealed class Memories
    {
        public Memory Memory;
        public int Count => Memory == null ? 0 : 1;
        public void TryGainMemoryFast(RimWorld.ThoughtDef def, int stage)
        {
            Memory ??= new Memory();
            Memory.Age = 0;
            Memory.Stage = stage;
        }
    }
    public sealed class Story { public Traits traits = new(); }
    public sealed class Traits
    {
        public bool Masochist;
        public bool HasTrait(RimWorld.TraitDef def) => Masochist;
    }
    public sealed class Guest { public RimWorld.SlaveInteractionModeDef slaveInteractionMode; }
    public sealed class WorkSettings
    {
        public bool Active = true;
        public bool WorkIsActive(RimWorld.WorkTypeDef type) => Active;
    }
    public sealed class Map { public MapPawns mapPawns = new(); }
    public sealed class MapPawns
    {
        public List<Pawn> FreeColonistsSpawned = new(), SlavesOfColonySpawned = new();
    }
}
namespace Verse.AI
{
    public sealed class Job { }
    public static class JobFailReason { public static void Is(string text) { } }
}
namespace RimWorld
{
    public sealed class DefOfAttribute : Attribute { }
    public static class DefOfHelper { public static void EnsureInitializedInCtor(Type type) { } }
    public sealed class ThoughtDef { }
    public sealed class TraitDef { }
    public sealed class PreceptDef { }
    public sealed class StatDef { }
    public sealed class SkillDef { }
    public sealed class WorkTypeDef { }
    public sealed class PawnCapacityDef { }
    public sealed class SlaveInteractionModeDef { }
    public static class StatDefOf { public static readonly StatDef SuppressionPower = new(); }
    public static class SkillDefOf { public static readonly SkillDef Social = new(); }
    public static class WorkTypeDefOf { public static readonly WorkTypeDef Warden = new(); }
    public static class PawnCapacityDefOf { public static readonly PawnCapacityDef Talking = new(), Manipulation = new(); }
    public static class SlaveInteractionModeDefOf { public static readonly SlaveInteractionModeDef Suppress = new(); }
    public sealed class Faction { public bool IsPlayer; }
    public sealed class Ideo
    {
        public bool Honorable;
        public bool HasPrecept(PreceptDef def) => Honorable;
    }
    public sealed class Need_Suppression
    {
        private float level;
        public float CurLevelPercentage { get => level; set => level = Math.Clamp(value, 0f, 1f); }
        public bool CanBeSuppressedNow => level < 0.7f;
    }
    public sealed class WorkGiver_Warden_SuppressSlave
    {
        public bool VanillaAccepts = true;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Job JobOnThing(Verse.Pawn pawn, Verse.Thing t, bool forced = false)
            => VanillaAccepts ? new Job() : null;
    }
    public static class SlaveRebellionUtility
    {
        public static int VanillaCalls, SuccessMotes;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void IncrementInteractionSuppression(Verse.Pawn initiator, Verse.Pawn recipient) { VanillaCalls++; }
        public static void IncrementSuppression(Need_Suppression need, Verse.Pawn initiator, Verse.Pawn recipient, float gain)
        { need.CurLevelPercentage += gain; SuccessMotes++; }
    }
    public enum AlertPriority { High }
    public abstract class Alert
    {
        protected string defaultLabel;
        protected AlertPriority defaultPriority;
        protected bool requireIdeology;
        public abstract AlertReport GetReport();
        public virtual Verse.TaggedString GetExplanation() => "";
    }
    public struct AlertReport
    {
        public bool active;
        public static AlertReport CulpritsAre(List<Verse.Pawn> pawns) => new AlertReport { active = pawns.Count > 0 };
    }
}
namespace KoRimUtility.SlaveSuppression
{
    // The power adapter is tested separately in the installed engine; these fixtures isolate job policy.
    internal struct PowerBreakdown { internal float CombatTotal; }
    internal static class SuppressionPower
    {
        internal static int Skill(Verse.Pawn pawn, RimWorld.SkillDef skill) => pawn.Social;
        internal static PowerBreakdown Evaluate(Verse.Pawn pawn, bool includeAuthority = true)
            => new PowerBreakdown { CombatTotal = pawn.Combat };
    }
    internal sealed class SuppressionSettings { public float difficulty = 1f, gainMultiplier = 1f; }
    internal static class SuppressionMod { internal static SuppressionSettings Settings = new(); }
}
