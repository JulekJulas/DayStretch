using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace DayStretched
{// would like to say
    // FOR NOW unautomatable, i want to change that in the future



        // prefix, not sure how to do that one
        [StaticConstructorOnStartup]
        [HarmonyPatch(typeof(HediffGiver_Bleeding))]
        [HarmonyPatch(nameof(HediffGiver_Bleeding.OnIntervalPassed))]
        public static class BloodStatusFixer
        {
            static bool Prefix(HediffGiver_Bleeding __instance, Pawn pawn, Hediff cause)
            {
                HediffSet hediffSet = pawn.health.hediffSet;
                if (hediffSet.BleedRateTotal >= 0.1f)
                {
                    float amount = hediffSet.BleedRateTotal * 0.001f * (1f / Settings.Instance.TimeMultiplier);
                    HealthUtility.AdjustSeverity(pawn, __instance.hediff, amount);
                    return false;
                }

                float negativeAmount = -0.00033333333f * (1f / Settings.Instance.TimeMultiplier);
                HealthUtility.AdjustSeverity(pawn, __instance.hediff, negativeAmount);
                return false;   
            }
        }



    [HarmonyPatch(typeof(GameCondition))]
    [HarmonyPatch("get_Expired")]
    public static class GameConditionPatch
    {
        public static bool Prefix(GameCondition __instance, ref bool __result)
        {
            __result = !__instance.Permanent && Find.TickManager.TicksGame > __instance.startTick + (__instance.Duration * Settings.Instance.TimeMultiplier);
            return false;
        }
    }

    [HarmonyPatch(typeof(WeatherEventMaker))]
    [HarmonyPatch("WeatherEventMakerTick")]
    public static class WeatherEventMakerTickPatch
    {
        public static bool Prefix(WeatherEventMaker __instance, Map map, float strength)
        {
            if (Rand.Value < 1f / __instance.averageInterval * strength * Settings.Instance.TimeMultiplier)
            {
                WeatherEvent newEvent = (WeatherEvent)Activator.CreateInstance(__instance.eventClass, new object[]
                {
                    map
                });
                map.weatherManager.eventHandler.AddEvent(newEvent);
            }
            return false;
        }
    }



}