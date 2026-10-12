using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace KoRimUtility.RimJobWorld
{
    [StaticConstructorOnStartup]
    internal static class ComfortTraitGain
    {
        private static readonly FieldInfo PawnField;
        private static readonly FieldInfo PartnerField;
        private static readonly Func<Pawn, bool> IsDesignatedComfort;

        static ComfortTraitGain()
        {
            var harmony = new Harmony("snowykte0426.korimutility.rjw.comforttraits");
            MethodInfo target = null;
            try
            {
                var props = AccessTools.TypeByName("rjw.SexProps");
                var utility = AccessTools.TypeByName("rjw.SexUtility");
                var designations = AccessTools.TypeByName("rjw.PawnDesignations_Comfort");
                if (props == null || utility == null || designations == null)
                    throw new InvalidOperationException("RJW trait or designation types not found");

                PawnField = AccessTools.Field(props, "pawn");
                PartnerField = AccessTools.Field(props, "partner");
                target = AccessTools.Method(utility, "GiveTraits", new[] { props, typeof(List<Trait>) });
                var comfort = AccessTools.Method(designations, "IsDesignatedComfort", new[] { typeof(Pawn) });
                if (PawnField?.FieldType != typeof(Pawn) || PartnerField?.FieldType != typeof(Pawn)
                    || PawnField.IsStatic || PartnerField.IsStatic
                    || target == null || !target.IsStatic || target.ReturnType != typeof(void)
                    || comfort == null || !comfort.IsStatic || comfort.ReturnType != typeof(bool))
                    throw new InvalidOperationException("RJW trait or designation signature changed");

                IsDesignatedComfort = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), comfort);
                harmony.Patch(target, prefix: new HarmonyMethod(typeof(ComfortTraitGain), nameof(Prefix)));
            }
            catch (Exception exception)
            {
                if (target != null)
                    harmony.Unpatch(target, HarmonyPatchType.Prefix, harmony.Id);
                Log.Warning("[KoRim Utility] RJW comfort trait protection skipped: " + exception.Message);
            }
        }

        private static bool Prefix(object __0)
        {
            if (__0 == null)
                return true;

            var pawn = (Pawn)PawnField.GetValue(__0);
            var partner = (Pawn)PartnerField.GetValue(__0);
            // Only skip the participant's automatic trait roll. Aftersex still handles
            // records, pregnancy, memories and the recipient's separate trauma effects.
            return pawn?.IsFreeNonSlaveColonist != true || partner == null || !IsDesignatedComfort(partner);
        }
    }
}
