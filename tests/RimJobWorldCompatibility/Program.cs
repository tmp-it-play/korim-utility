using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using HarmonyLib;
using Verse;

internal static class Program
{
    private static int checks;
    private static void Equal(object expected, object actual)
    {
        if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}");
        checks++;
    }
    private static string Expected(string key, string original, params NamedArgument[] names) =>
        LanguageDatabase.activeLanguage?.folderName?.StartsWith("Korean", StringComparison.OrdinalIgnoreCase) == true
            && Translator.Values.ContainsKey(key) ? key.Translate(names) : original;

    private static void Main(string[] args)
    {
        foreach (var node in XDocument.Load(Path.Combine(args[0], "Translations/RimJobWorld/Languages/Korean/Keyed/JobNotifications.xml")).Root.Elements())
            Translator.Values[node.Name.LocalName] = node.Value;
        RuntimeHelpers.RunClassConstructor(typeof(KoRimUtility.RimJobWorld.NotificationTranslation).TypeHandle);
        foreach (var language in new[] { "Korean", "Korean (한국어)", "korean", "English", "Japanese", null })
        foreach (var actor in new[] { "Alex", "프레데릭", "<color=red>A is being attacked by B</color>" })
        {
            LanguageDatabase.activeLanguage = language == null ? null : new LoadedLanguage { folderName = language };
            string target = "<color=blue>Kim</color>";
            var corpse = new rjw.JobDriver_ViolateCorpse();
            var iterator = corpse.FormatNotification(actor, target).GetEnumerator();
            Equal(true, iterator.MoveNext());
            Equal("before", iterator.Current);
            Equal(true, iterator.MoveNext());
            var original = actor + " is trying to rape a corpse of " + target;
            Equal(Expected("KoRimUtility.RJW.CorpseAttempt", original, actor.Named("ACTOR"), target.Named("TARGET")), iterator.Current);
            Equal(false, iterator.MoveNext());
            Equal("after", corpse.State);
            Equal("unrelated: " + actor, corpse.OtherText);
            Equal(actor, MessageSink.Target);
            Equal("neutral", MessageSink.Severity);
            Equal(false, MessageSink.Historical);
            Equal(original, UnrelatedNotifications.SameText(actor, target));

            var animal = new rjw.JobDriver_BestialityForMale();
            var callback = animal.FormatNotification(actor, target);
            callback();
            original = actor + " is being attacked by " + target + ".";
            Equal(Expected("KoRimUtility.RJW.AnimalAttack", original, actor.Named("TARGET"), target.Named("ATTACKER")), MessageSink.Text);
            Equal(actor, MessageSink.Target);
            Equal("threat", MessageSink.Severity);
            Equal(true, MessageSink.Historical);
            Equal("after", animal.State);
        }
        LanguageDatabase.activeLanguage = new LoadedLanguage { folderName = "Korean" };
        var type = typeof(KoRimUtility.RimJobWorld.NotificationTranslation);
        var attack = AccessTools.Method(type, "AnimalAttack");
        Equal("A changed B!", attack.Invoke(null, new object[] { "A", " changed ", "B", "!" }));
        Equal("A is being attacked by B?", attack.Invoke(null, new object[] { "A", " is being attacked by ", "B", "?" }));
        Translator.Values.Clear();
        Equal("A is being attacked by B.", attack.Invoke(null, new object[] { "A", " is being attacked by ", "B", "." }));
        Equal("A is trying to rape a corpse of B", AccessTools.Method(type, "CorpseAttempt").Invoke(null, new object[] { "A", " is trying to rape a corpse of ", "B" }));
        Equal(null, Harmony.GetPatchInfo(AccessTools.Method(typeof(UnrelatedNotifications), "SameText")));

        // Changed/ambiguous upstream IL must fail before any instruction is modified.
        var transpiler = AccessTools.Method(type, "Transpiler");
        var concat = AccessTools.Method(typeof(string), nameof(string.Concat), new[] { typeof(string), typeof(string), typeof(string) });
        var snippets = new[] {
            new List<CodeInstruction>(),
            new List<CodeInstruction> { new(OpCodes.Ldstr, " is trying to rape a corpse of "), new(OpCodes.Ret) },
            new List<CodeInstruction> { new(OpCodes.Ldstr, " is trying to rape a corpse of "), new(OpCodes.Call, concat), new(OpCodes.Ldstr, " is trying to rape a corpse of "), new(OpCodes.Call, concat) },
        };
        foreach (var snippet in snippets)
        {
            var operands = snippet.Select(c => c.operand).ToArray();
            try { transpiler.Invoke(null, new object[] { snippet }); throw new Exception("Changed IL was accepted"); }
            catch (TargetInvocationException ex) { Equal(typeof(InvalidOperationException), ex.InnerException.GetType()); }
            Equal(true, operands.SequenceEqual(snippet.Select(c => c.operand)));
        }
        Console.WriteLine($"RJW notification Harmony integration: {checks} checks passed (generated iterator/callback; display only).");
    }
}

internal static class MessageSink
{
    public static string Text, Target, Severity;
    public static bool Historical;
    public static void Message(string text, string target, string severity, bool historical)
        => (Text, Target, Severity, Historical) = (text, target, severity, historical);
}
internal static class UnrelatedNotifications
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string SameText(string first, string second) => first + " is trying to rape a corpse of " + second;
}
namespace rjw
{
    public class JobDriver_ViolateCorpse
    {
        public string State, OtherText;
        public IEnumerable<string> FormatNotification(string actor, string target)
        {
            yield return "before";
            OtherText = "unrelated: " + actor;
            var text = actor + " is trying to rape a corpse of " + target;
            MessageSink.Message(text, actor, "neutral", false);
            yield return text;
            State = "after";
        }
    }
    public class JobDriver_BestialityForMale
    {
        public string State;
        public Action FormatNotification(string actor, string target) => () =>
        {
            MessageSink.Message(actor + " is being attacked by " + target + ".", actor, "threat", true);
            State = "after";
        };
    }
}
namespace Verse
{
    [AttributeUsage(AttributeTargets.Class)] public class StaticConstructorOnStartup : Attribute { }
    public class LoadedLanguage { public string folderName; }
    public static class LanguageDatabase { public static LoadedLanguage activeLanguage; }
    public readonly record struct NamedArgument(string Value, string Name);
    public static class Log { public static void Warning(string message) { throw new Exception(message); } }
    public static class Translator
    {
        public static readonly Dictionary<string, string> Values = new();
        public static NamedArgument Named(this string value, string name) => new(value, name);
        public static bool CanTranslate(this string key) => Values.ContainsKey(key);
        public static string Translate(this string key, params NamedArgument[] args)
        {
            string value = Values[key];
            foreach (var arg in args) value = value.Replace("{" + arg.Name + "}", arg.Value);
            return value;
        }
    }
}
