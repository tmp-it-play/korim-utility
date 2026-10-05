using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using KoRimUtility.MainButtons;
using RimWorld;
using UnityEngine;
using Verse;

internal static class Program
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    private static void CheckRow(List<MainButtonDef> buttons, int width)
    {
        UI.screenWidth = width; UI.screenHeight = 900;
        foreach (var button in buttons) { button.Worker.Drawn.Clear(); button.Worker.VisibilityReads = 0; }
        var root = new MainButtonsRoot(buttons);
        root.MainButtonsOnGUI();
        var visible = buttons.Where(b => b.buttonVisible && b.Worker.StateVisible).ToList();
        Check(root.OriginalDrawCalls == 0, "Original unequal allocation must be replaced");
        Check(buttons.All(b => b.Worker.VisibilityReads == 1), "Visibility must be evaluated only once");
        Check(buttons.Where(b => !visible.Contains(b)).All(b => b.Worker.Drawn.Count == 0), "Hidden buttons must consume no space");
        if (visible.Count == 0) return;
        var allRects = visible.SelectMany(b => b.Worker.Drawn).ToList();
        Check(allRects.Count == Math.Min(width, visible.Count), "Only positive-width buttons should draw");
        Check(allRects.Sum(r => r.width) == width, "Buttons must fill the row");
        Check(allRects.First().x == 0 && allRects.Last().xMax == width, "No edge gaps or overflow");
        Check(allRects.Max(r => r.width) - allRects.Min(r => r.width) <= 1, "At most one pixel of rounding difference");
        for (var i = 0; i < allRects.Count; i++)
        {
            var rect = allRects[i];
            Check(rect.y == 865 && rect.height == 36, "Preserve native bar height and position");
            if (i > 0) Check(allRects[i-1].xMax == rect.x, "No gaps or overlapping hitboxes");
        }
        Check(buttons.Select(b => b.minimized).SequenceEqual(buttons.Select((_, i) => i % 3 == 0)), "Do not modify Def flags");
    }

    public static void Main()
    {
        RuntimeHelpers.RunClassConstructor(typeof(EqualMainButtons).TypeHandle);
        foreach (var count in new[] { 0, 1, 3, 13, 16, 27, 64 })
        {
            var buttons = Enumerable.Range(0, count).Select(i => new MainButtonDef { minimized = i % 3 == 0 }).ToList();
            foreach (var width in new[] { 320, 640, 800, 1024, 1280, 1536, 1908, 1920, 2560, 3440, 3840 })
                CheckRow(buttons, width);
        }
        var dynamic = Enumerable.Range(0, 16).Select(i => new MainButtonDef { minimized = i % 3 == 0 }).ToList();
        dynamic[2].buttonVisible = false;
        dynamic[5].Worker.StateVisible = false;
        CheckRow(dynamic, 1908);
        dynamic[2].buttonVisible = true;
        dynamic[5].Worker.StateVisible = true;
        CheckRow(dynamic, 1280);
        CheckRow(dynamic, 3);
        var shortcut = new MainButtonsRoot(dynamic);
        var before = dynamic[4].Worker.Activations;
        shortcut.HotkeyIndex = 4;
        shortcut.MainButtonsOnGUI();
        Check(dynamic[4].Worker.Activations == before + 1, "Keep the native shortcut loop");
        Console.WriteLine($"Main button layout: {checks} checks passed (real Harmony; Unity test doubles).");
    }
}

namespace UnityEngine
{
    public struct Color { public static Color white => new Color(); }
    public static class GUI { public static Color color; }
    public struct Rect
    {
        public float x, y, width, height;
        public float xMax => x + width;
        public Rect(float x, float y, float width, float height)
        { this.x=x; this.y=y; this.width=width; this.height=height; }
    }
}
namespace Verse
{
    public sealed class StaticConstructorOnStartupAttribute : Attribute { }
    public static class UI { public static int screenWidth, screenHeight; }
    public static class Log { public static void Warning(string message) => throw new Exception(message); }
}
namespace RimWorld
{
    public sealed class MainButtonDef
    {
        public bool minimized, buttonVisible = true;
        public MainButtonWorker Worker;
        public MainButtonDef() { Worker = new MainButtonWorker { Def = this }; }
    }
    public sealed class MainButtonWorker
    {
        public MainButtonDef Def;
        public bool StateVisible = true;
        public int VisibilityReads, Activations;
        public readonly List<Rect> Drawn = new();
        public bool Visible { get { VisibilityReads++; return Def.buttonVisible && StateVisible; } }
        public void DoButton(Rect rect) { Drawn.Add(rect); }
        public void InterfaceTryActivate() { Activations++; }
    }
    public sealed class MainButtonsRoot
    {
        private readonly List<MainButtonDef> allButtonsInOrder;
        public int OriginalDrawCalls, HotkeyIndex = -1;
        public MainButtonsRoot(List<MainButtonDef> buttons) { allButtonsInOrder = buttons; }
        public void MainButtonsOnGUI()
        {
            DoButtons();
            if (HotkeyIndex >= 0) allButtonsInOrder[HotkeyIndex].Worker.InterfaceTryActivate();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void DoButtons() { OriginalDrawCalls++; }
    }
}
