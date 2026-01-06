using DayStretched;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace DayStretched
{
    public static class DayConstants
    {
        public const int VanillaTicksPerDay = 60000;
        public const int VanillaTicksPerHour = 2500;
        public const int VanillaTicksPerTwelfth = 300000;
        public const int VanillaTicksPerQuadrum = 900000;
        public const int VanillaTicksPerYear = 3600000;

        public static int TicksPerDayInt() => Mathf.RoundToInt(VanillaTicksPerDay * Settings.Instance.TimeMultiplier);
        public static long TicksPerDayLong() => (long)TicksPerDayInt();
        public static float TicksPerDayFloat() => (float)TicksPerDayInt();  

        public static int TicksPerHourInt() => Mathf.RoundToInt((float)TicksPerDayInt() / 24f);
        public static long TicksPerHourLong() => (long)TicksPerHourInt();
        public static float TicksPerHourFloat() => (float)TicksPerHourInt();

        public static int TicksPerTwelfthInt() => TicksPerDayInt() * 5;
        public static long TicksPerTwelfthLong() => (long)TicksPerTwelfthInt();
        public static float TicksPerTwelfthFloat() => (float)TicksPerTwelfthInt();

        public static int TicksPerQuadrumInt() => TicksPerDayInt() * 15;
        public static long TicksPerQuadrumLong() => (long)TicksPerQuadrumInt();
        public static float TicksPerQuadrumFloat() => (float)TicksPerQuadrumInt();

        public static int TicksPerYearInt() => TicksPerDayInt() * 60;
        public static long TicksPerYearLong() => (long)TicksPerYearInt();
        public static float TicksPerYearFloat() => (float)TicksPerYearInt();

        public static long TicksPerDecadeLong() => (long)TicksPerYearInt() * 10L;
        // reverse
        public static int ReverseTicksPerDayInt() => Mathf.RoundToInt(VanillaTicksPerDay * (1f / Settings.Instance.TimeMultiplier));
        public static long ReverseTicksPerDayLong() => (long)ReverseTicksPerDayInt();
        public static float ReverseTicksPerDayFloat() => (float)ReverseTicksPerDayInt();

        public static int ReverseTicksPerHourInt() => Mathf.RoundToInt((float)ReverseTicksPerDayInt() / 24f);
        public static long ReverseTicksPerHourLong() => (long)ReverseTicksPerHourInt();
        public static float ReverseTicksPerHourFloat() => (float)ReverseTicksPerHourInt();

        public static int ReverseTicksPerTwelfthInt() => ReverseTicksPerDayInt() * 5;
        public static long ReverseTicksPerTwelfthLong() => (long)ReverseTicksPerTwelfthInt();
        public static float ReverseTicksPerTwelfthFloat() => (float)ReverseTicksPerTwelfthInt();

        public static int ReverseTicksPerQuadrumInt() => ReverseTicksPerDayInt() * 15;
        public static long ReverseTicksPerQuadrumLong() => (long)ReverseTicksPerQuadrumInt();
        public static float ReverseTicksPerQuadrumFloat() => (float)ReverseTicksPerQuadrumInt();

        public static int ReverseTicksPerYearInt() => ReverseTicksPerDayInt() * 60;
        public static long ReverseTicksPerYearLong() => (long)ReverseTicksPerYearInt();
        public static float ReverseTicksPerYearFloat() => (float)ReverseTicksPerYearInt();

        public static long ReverseTicksPerDecadeLong() => (long)ReverseTicksPerYearInt() * 10L;
    }

    [StaticConstructorOnStartup]
    public static class GenDatePatcher
    {
        static string[] reverse = { }; 
        static string[] skip = { "DaysPassedFloat", "DaysPassedSinceSettleFloat", "YearsPassedFloat", "TicksToPeriod", "DaysPassedInt" };

        static GenDatePatcher()
        {
            var harmony = new Harmony("com.julekjulas.daystretched.gendate");
            PatchGenDate(harmony);
        }

        static void PatchGenDate(Harmony harmony)
        {
            var genDateType = AccessTools.TypeByName("GenDate");
            var methods = genDateType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var m in methods)
            {
                
                if (skip.Contains(m.Name)) continue; //skip unneded methods
                if (m.IsAbstract || m.IsGenericMethod) continue;
                if (reverse.Contains(m.Name)) // some methods need to be divided
                {// nvm but ill leave it here just in case i need it later
                    try
                    {
                        var transpiler = new HarmonyMethod(typeof(GenDatePatcher).GetMethod(nameof(ReplaceReverseConstsTranspiler), BindingFlags.Static | BindingFlags.NonPublic));
                        harmony.Patch(m, transpiler: transpiler);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[DayStretched] Failed to patch GenDate.{ex}");
                    }
                }
                else
                {
                    try
                    {
                        var transpiler = new HarmonyMethod(typeof(GenDatePatcher).GetMethod(nameof(ReplaceConstsTranspiler), BindingFlags.Static | BindingFlags.NonPublic));
                        harmony.Patch(m, transpiler: transpiler);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[DayStretched] Failed to patch GenDate.{ex}");
                    }
                }
            }
        }

        static IEnumerable<CodeInstruction> ReplaceConstsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            var m_TicksPerDayInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerDayInt));
            var m_TicksPerDayLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerDayLong));
            var m_TicksPerDayFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerDayFloat));

            var m_TicksPerHourInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerHourInt));
            var m_TicksPerHourFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerHourFloat));
            var m_TicksPerHourLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerHourLong));

            var m_TicksPerTwelfthInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerTwelfthInt));
            var m_TicksPerTwelfthLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerTwelfthLong));
            var m_TicksPerTwelfthFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerTwelfthFloat));

            var m_TicksPerQuadrumInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerQuadrumInt));
            var m_TicksPerQuadrumLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerQuadrumLong));
            var m_TicksPerQuadrumFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerQuadrumFloat));

            var m_TicksPerYearInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerYearInt));
            var m_TicksPerYearLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerYearLong));
            var m_TicksPerYearFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerYearFloat));

            var m_TicksPerDecadeLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.TicksPerDecadeLong));

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                if (ci.opcode == OpCodes.Ldc_I4 && ci.operand is int ival)
                {
                    switch (ival)
                    {
                        case 60000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayInt); break;
                        case 2500: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourInt); break;
                        case 300000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthInt); break;
                        case 900000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumInt); break;
                        case 3600000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearInt); break;
                        default: break;
                    }
                    continue;
                }

                if (ci.opcode == OpCodes.Ldc_I8 && ci.operand is long lval)
                {
                    switch (lval)
                    {
                        case 60000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayLong); break;
                        case 2500L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourLong); break;
                        case 300000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthLong); break;
                        case 900000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumLong); break;
                        case 3600000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearLong); break;
                        case 36000000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDecadeLong); break;
                        default: break;
                    }
                    continue;
                }

                if ((ci.opcode == OpCodes.Ldc_R4) && ci.operand is float fval)
                {
                    if (Math.Abs(fval - 60000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayFloat);
                    else if (Math.Abs(fval - 2500f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourFloat);
                    else if (Math.Abs(fval - 300000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthFloat);
                    else if (Math.Abs(fval - 900000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumFloat);
                    else if (Math.Abs(fval - 3600000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearFloat);
                    continue;
                }
            }

            return codes.AsEnumerable();
        }
        static IEnumerable<CodeInstruction> ReplaceReverseConstsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            var m_TicksPerDayInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerDayInt));
            var m_TicksPerDayLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerDayLong));
            var m_TicksPerDayFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerDayFloat));

            var m_TicksPerHourInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerHourInt));
            var m_TicksPerHourFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerHourFloat));
            var m_TicksPerHourLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerHourLong));

            var m_TicksPerTwelfthInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerTwelfthInt));
            var m_TicksPerTwelfthLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerTwelfthLong));
            var m_TicksPerTwelfthFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerTwelfthFloat));

            var m_TicksPerQuadrumInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerQuadrumInt));
            var m_TicksPerQuadrumLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerQuadrumLong));
            var m_TicksPerQuadrumFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerQuadrumFloat));

            var m_TicksPerYearInt = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerYearInt));
            var m_TicksPerYearLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerYearLong));
            var m_TicksPerYearFloat = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerYearFloat));

            var m_TicksPerDecadeLong = AccessTools.Method(typeof(DayConstants), nameof(DayConstants.ReverseTicksPerDecadeLong));

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                if (ci.opcode == OpCodes.Ldc_I4 && ci.operand is int ival)
                {
                    switch (ival)
                    {
                        case 60000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayInt); break;
                        case 2500: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourInt); break;
                        case 300000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthInt); break;
                        case 900000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumInt); break;
                        case 3600000: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearInt); break;
                        default: break;
                    }
                    continue;
                }

                if (ci.opcode == OpCodes.Ldc_I8 && ci.operand is long lval)
                {
                    switch (lval)
                    {
                        case 60000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayLong); break;
                        case 2500L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourLong); break;
                        case 300000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthLong); break;
                        case 900000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumLong); break;
                        case 3600000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearLong); break;
                        case 36000000L: codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDecadeLong); break;
                        default: break;
                    }
                    continue;
                }

                if ((ci.opcode == OpCodes.Ldc_R4) && ci.operand is float fval)
                {
                    if (Math.Abs(fval - 60000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayFloat);
                    else if (Math.Abs(fval - 2500f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourFloat);
                    else if (Math.Abs(fval - 300000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthFloat);
                    else if (Math.Abs(fval - 900000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumFloat);
                    else if (Math.Abs(fval - 3600000f) < 0.001f) codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearFloat);
                    continue;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
