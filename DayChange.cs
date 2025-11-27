using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DayStretched;
using Verse;

namespace DayStretched
{
    [StaticConstructorOnStartup]
    public static class GenDateConstantsPatcher
    {
        static GenDateConstantsPatcher()
        {
            var harmony = new Harmony("com.julekjulas.rimworld.daystretch.ticksperday");
            PatchGenDateMethods(harmony);
        }

        static void PatchGenDateMethods(Harmony harmony)
        {
            var genDateType = AccessTools.TypeByName("GenDate");
            if (genDateType == null)
            {
                Log.Error("Could not find GenDate type to patch.");
                return;
            }

            var methods = genDateType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            foreach (var m in methods)
            {
                // Skip methods with no body
                if (m.IsAbstract || m.IsGenericMethod) continue;
                try
                {
                    var transpiler = new HarmonyMethod(typeof(GenDateConstantsPatcher).GetMethod(nameof(ReplaceConstsTranspiler), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(m, transpiler: transpiler);
                }
                catch (Exception ex)
                {
                    // Some methods may not be patchable (e.g. extern). Log and continue.
                    Log.Warning($"Failed to patch {m.Name}: {ex.Message}");
                }
            }
        }
        static IEnumerable<CodeInstruction> ReplaceConstsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Pre-resolve MethodInfos for calls
            var m_TicksPerDayInt = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerDayInt));
            var m_TicksPerDayLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerDayLong));
            var m_TicksPerDayFloat = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerDayFloat));

            var m_TicksPerHourInt = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerHourInt));
            var m_TicksPerHourFloat = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerHourFloat));
            var m_TicksPerHourLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerHourLong));

            var m_TicksPerTwelfthInt = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerTwelfthInt));
            var m_TicksPerTwelfthLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerTwelfthLong));
            var m_TicksPerTwelfthFloat = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerTwelfthFloat));

            var m_TicksPerQuadrumInt = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerQuadrumInt));
            var m_TicksPerQuadrumLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerQuadrumLong));
            var m_TicksPerQuadrumFloat = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerQuadrumFloat));

            var m_TicksPerYearInt = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerYearInt));
            var m_TicksPerYearLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerYearLong));
            var m_TicksPerYearFloat = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerYearFloat));

            var m_TicksPerDecadeLong = AccessTools.Method(typeof(DaySettings), nameof(DaySettings.TicksPerDecadeLong));

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                // Replace integer constants (ldc.i4)
                if (ci.opcode == OpCodes.Ldc_I4 && ci.operand is int ival)
                {
                    switch (ival)
                    {
                        case DaySettings.VanillaTicksPerDay:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayInt);
                            break;
                        case DaySettings.VanillaTicksPerHour:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourInt);
                            break;
                        case DaySettings.VanillaTicksPerTwelfth:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthInt);
                            break;
                        case DaySettings.VanillaTicksPerQuadrum:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumInt);
                            break;
                        case DaySettings.VanillaTicksPerYear:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearInt);
                            break;
                        // leave other ints intact
                        default:
                            break;
                    }
                    continue;
                }

                // Replace long constants (ldc.i8)
                if (ci.opcode == OpCodes.Ldc_I8 && ci.operand is long lval)
                {
                    switch (lval)
                    {
                        case (long)DaySettings.VanillaTicksPerDay:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayLong);
                            break;
                        case (long)DaySettings.VanillaTicksPerTwelfth:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthLong);
                            break;
                        case (long)DaySettings.VanillaTicksPerQuadrum:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerQuadrumLong);
                            break;
                        case (long)DaySettings.VanillaTicksPerYear:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearLong);
                            break;
                        // common vanilla "over a decade" check uses 36000000L:
                        case 36000000L:
                            codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDecadeLong);
                            break;
                        default:
                            break;
                    }
                    continue;
                }

                // Replace float constants (ldc.r4)
                if ((ci.opcode == OpCodes.Ldc_R4 || ci.opcode == OpCodes.Ldc_R8) && ci.operand is float fval)
                {
                    // Common float literals used in GenDate:
                    if (Math.Abs(fval - (float)DaySettings.VanillaTicksPerDay) < 0.001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerDayFloat);
                    }
                    else if (Math.Abs(fval - (float)DaySettings.VanillaTicksPerHour) < 0.001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerHourFloat);
                    }
                    else if (Math.Abs(fval - (float)DaySettings.VanillaTicksPerTwelfth) < 0.001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerTwelfthFloat);
                    }
                    else if (Math.Abs(fval - (float)DaySettings.VanillaTicksPerYear) < 0.001f)
                    {
                        codes[i] = new CodeInstruction(OpCodes.Call, m_TicksPerYearFloat);
                    }
                    // otherwise leave it
                    continue;
                }
            }

            return codes.AsEnumerable();
        }
    }
}
