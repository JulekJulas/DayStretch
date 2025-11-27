using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TimeControlRevived;
using UnityEngine;
using Verse;

namespace TimeControlRevived
{


    [HarmonyPatch(typeof(Need_Food))]
    [HarmonyPatch("BaseHungerRate")]
    static class Patch_BaseHungerRate_Postfix
        {
             static void Postfix(ref float __result)
         {
            __result /= Settings.Instance.TimeMultiplier;
    }
}
    // TODO
    [HarmonyPatch(typeof(Need_Outdoors), "NeedInterval")]
    static class Patch_NeedOutdoors
    {
        static void Postfix(Need_Outdoors __instance, ref float ___lastEffectiveDelta)
        {
            ___lastEffectiveDelta /= Settings.Instance.TimeMultiplier;
            __instance.CurLevel = Mathf.Clamp01(__instance.CurLevel + ___lastEffectiveDelta);
        }
    }
    [HarmonyPatch(typeof(Need_Indoors), "NeedInterval")]
    static class Patch_NeedIndoors
    {
        static void Postfix(Need_Indoors __instance, ref float ___lastEffectiveDelta)
        {
            ___lastEffectiveDelta /= Settings.Instance.TimeMultiplier;
            __instance.CurLevel = Mathf.Clamp01(__instance.CurLevel + ___lastEffectiveDelta);
        }
    }
    [HarmonyPatch(typeof(Need_Rest), "NeedInterval")]
    static class Patch_NeedRest
    {
        static void Postfix(Need_Rest __instance)
        {
            // --- get protected members via reflection ---
            PropertyInfo isFrozenProp = typeof(Need).GetProperty("IsFrozen", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo pawnProp = typeof(Need).GetProperty("pawn", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            FieldInfo lastRestField = typeof(Need_Rest).GetField("lastRestEffectiveness", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isFrozenProp == null || pawnProp == null || lastRestField == null)
                return; // fail-safe

            bool isFrozen = (bool)isFrozenProp.GetValue(__instance);
            Pawn pawn = pawnProp.GetValue(__instance) as Pawn;

            if (isFrozen || pawn == null)
                return; // skip animals/world pawns without full stats

            if (__instance.Resting)
            {
                float lastRestEffectiveness = (float)lastRestField.GetValue(__instance);
                if (lastRestEffectiveness <= 0f) return;

                float restRateMultiplier = pawn.GetStatValue(StatDefOf.RestRateMultiplier, true, -1);
                float restGain = 0.005714286f * lastRestEffectiveness * restRateMultiplier;
                restGain /= Settings.Instance.TimeMultiplier;
                __instance.CurLevel += restGain;
            }
            else
            {
                float restFallPerTick = __instance.RestFallPerTick;
                float restFallRateFactor = pawn.GetStatValue(StatDefOf.RestFallRateFactor, true, -1);
                float restFall = restFallPerTick * 150f * restFallRateFactor;
                restFall /= Settings.Instance.TimeMultiplier;
                __instance.CurLevel -= restFall;
            }

            __instance.CurLevel = Mathf.Clamp01(__instance.CurLevel);
        }
    }





}












