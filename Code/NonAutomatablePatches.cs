using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace DayStretched
{// would like to say
    // FOR NOW unautomatable, i want to change that in the future







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

    [HarmonyPatch(typeof(HistoryAutoRecorderGroup))]
    [HarmonyPatch("GetMaxDay")]
    public static class GetMaxDayPatch
    {
        public static bool Prefix(HistoryAutoRecorderGroup __instance, ref float __result)
        {
            float num = 0f;
            foreach (HistoryAutoRecorder historyAutoRecorder in __instance.recorders)
            {
                int count = historyAutoRecorder.records.Count;
                if (count != 0)
                {
                    float num2 = (float)((count - 1) * historyAutoRecorder.def.recordTicksFrequency) / (60000f) * Settings.Instance.TimeMultiplier;
                    if (num2 > num)
                    {
                        num = num2;
                    }
                }
            }
            __result = num;
            return false;
        }
    }
}








