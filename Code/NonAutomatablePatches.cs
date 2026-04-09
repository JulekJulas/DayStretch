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

namespace DayStretch
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

    [HarmonyPatch(typeof(GenTemperature))]
    [HarmonyPatch("RotRateAtTemperature")]
    public static class RotRateAtTemperaturePatch
    {
        public static bool Prefix(ref float __result, ref float temperature)
        {
            if (temperature < 0f)
            {
                __result = 0f;
                return false;
            }
            if (temperature >= 10f)
            {
                __result = 1f / Settings.Instance.TimeMultiplier;
                return false;
            }
            __result = ((temperature - 0f) / 10f) / Settings.Instance.TimeMultiplier;
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
                    float num2 = (float)((count - 1) * historyAutoRecorder.def.recordTicksFrequency) / (60000f) / Settings.Instance.TimeMultiplier;
                    if (num2 > num)
                    {
                        num = num2;
                    }
                }
            }
            __result = num;
            return false;
        }

        [HarmonyPatch(typeof(Thing))]
        [HarmonyPatch("GetInspectStringLowPriority")]
        public static class GetInspectStringLowPriorityPatch
        {
            public static string Postfix(string __result, Thing __instance)
            {
                List<string> tmpDeteriorationReasons = GetTmpDeteriorationReasons(__instance);
                tmpDeteriorationReasons.Clear();
                float f = (SteadyEnvironmentEffects.FinalDeteriorationRate(__instance, tmpDeteriorationReasons)) * Settings.Instance.TimeMultiplier;
                if (tmpDeteriorationReasons.Count != 0)
                {
                    return string.Format("{0}: {1} ({2})", "DeterioratingBecauseOf".Translate(), tmpDeteriorationReasons.ToCommaList(false, false).CapitalizeFirst(), "PerDay".Translate(f.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
                }
                return null;
            }

            private static List<string> GetTmpDeteriorationReasons(Thing instance)
            {
                return (List<string>)AccessTools.Field(typeof(Thing), "tmpDeteriorationReasons")
                    .GetValue(instance);
            }


        }


        [HarmonyPatch(typeof(Hediff))]
        [HarmonyPatch("TickInterval")]
        static IEnumerable<CodeInstruction> IHateHediffs(IEnumerable<CodeInstruction> instructions)
        {
            float[] whyAreThereSoManyNumbers = { 60f, 60000f, 600f, 400f, 200f };
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float val)
                {
                    foreach (float target in whyAreThereSoManyNumbers)
                    {
                        if (Mathf.Approximately(val, target))
                        {
                            instr.operand = target * Settings.Instance.TimeMultiplier;
                            break;
                        }
                    }// my system can only handle 3 at most, why 5 ludeon?
                }
                yield return instr;
            }
        }
    }
}








