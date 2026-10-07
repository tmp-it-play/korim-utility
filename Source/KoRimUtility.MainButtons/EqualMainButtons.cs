using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace KoRimUtility.MainButtons
{
    [StaticConstructorOnStartup]
    internal static class EqualMainButtons
    {
        static EqualMainButtons()
        {
            var draw = AccessTools.Method(typeof(MainButtonsRoot), "DoButtons", Type.EmptyTypes);
            var buttons = AccessTools.Field(typeof(MainButtonsRoot), "allButtonsInOrder");
            if (draw == null || buttons?.FieldType != typeof(List<MainButtonDef>))
            {
                Log.Warning("[KoRim Utility] Main button layout API changed; equal widths were not installed.");
                return;
            }
            var harmony = new Harmony("snowykte0426.korimutility.mainbuttons");
            try
            {
                harmony.Patch(draw, prefix: new HarmonyMethod(typeof(EqualMainButtons), nameof(DrawPrefix)));
            }
            catch (Exception exception)
            {
                harmony.UnpatchAll(harmony.Id);
                Log.Warning("[KoRim Utility] Could not install equal main button widths: " + exception.Message);
            }
        }

        internal static Rect ButtonRect(int index, int count, int width, int height)
        {
            // Share the rounding remainder across the row instead of placing
            // it all on the last button. Adjacent edges are exactly identical.
            var left = (int)((long)width * index / count);
            var right = (int)((long)width * (index + 1) / count);
            return new Rect(left, height - 35, right - left, 36f);
        }

        private static bool DrawPrefix(List<MainButtonDef> ___allButtonsInOrder)
        {
            if (___allButtonsInOrder == null)
                return true;
            var width = UI.screenWidth;
            if (width <= 0)
                return false;
            // Evaluate each worker's visibility once: mods can hide buttons
            // dynamically based on the selected map, world view or game state.
            var visible = new List<MainButtonWorker>(___allButtonsInOrder.Count);
            foreach (var button in ___allButtonsInOrder)
            {
                var worker = button.Worker;
                if (worker.Visible)
                    visible.Add(worker);
            }
            GUI.color = Color.white;
            for (var index = 0; index < visible.Count; index++)
            {
                var rect = ButtonRect(index, visible.Count, width, UI.screenHeight);
                if (rect.width > 0)
                    visible[index].DoButton(rect);
            }
            return false;
        }
    }
}
