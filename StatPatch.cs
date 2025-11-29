using DayStretched;
using HarmonyLib;
using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using UnityEngine;
using Verse;
// we do a lil defing
public class StatPatchListDef : Def
{
    public List<string> statsName;
}

public class StatPatchListWorkDef : Def
{
    public List<string> statsName;
}



[StaticConstructorOnStartup]
public class StatPatch
{
    static StatPatch()
    {
        // temu ass code holy shit
        int pdef = 0;
        int wpdef = 0;
        foreach (StatPatchListDef def in DefDatabase<StatPatchListDef>.AllDefsListForReading)
        {
            foreach (string statName in def.statsName)
            {
                if (string.IsNullOrEmpty(statName))
                {
                    continue;
                }
                else
                {
                    StatDef statDef = DefDatabase<StatDef>.GetNamed(statName, true);
                    if (statDef != null)
                    {
                        statDef.defaultBaseValue /= Settings.Instance.TimeMultiplier;
                        pdef++;
                    }
                    else
                    {
                        Log.Warning($"[DayStretched] Could not find StatDef '{statName}' to patch.");
                    }
                }
            }
        }
        if (Settings.Instance.WorkRelated == true)
        {
            foreach (StatPatchListWorkDef def in DefDatabase<StatPatchListWorkDef>.AllDefsListForReading)
            {
                foreach (string statName in def.statsName)
                {
                    if (string.IsNullOrEmpty(statName))
                    {
                        continue;
                    }
                    else
                    {
                        StatDef statDef = DefDatabase<StatDef>.GetNamed(statName, true);
                        if (statDef != null)
                        {
                            statDef.defaultBaseValue /= Settings.Instance.TimeMultiplier;
                            wpdef++;
                        }
                        else
                        {
                            Log.Warning($"[DayStretched] Could not find work related StatDef '{statName}' to patch.");
                        }
                    }
                }
            }
        }
        if (pdef > 0)
        {
            Log.Message($"[DayStretched] Patched {pdef} defs.");
        }
        if (wpdef > 0)
        {
            Log.Message($"[DayStretched] Patched {wpdef} work defs.");
        }


    }
}

// super secret needs patch

// if you change the namespace the entire program breaks, this is literally the tf2 coconut
namespace DayStretched
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker))]
    [HarmonyPatch("NeedsTrackerTickInterval")]
    static class NeedsTrackerTickIntervalPatch
    {
        static int TickIntervalInt = Mathf.RoundToInt(150 * Settings.Instance.TimeMultiplier);


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int intVal && intVal == 150)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, TickIntervalInt);
                    continue;
                }
                yield return instr;
            }
        }
    }
    [HarmonyPatch(typeof(Pawn_HealthTracker))]
    [HarmonyPatch("HealthTickInterval")]
    static class HealthTrackerTickIntervalPatch
    {
        static int HealthTickIntervalSixHundredInt = Mathf.RoundToInt(600 * Settings.Instance.TimeMultiplier);
        static int HealthTickIntervalFifteenInt = Mathf.RoundToInt(15 * Settings.Instance.TimeMultiplier);
        static int HealthTickIntervalSixtyInt = Mathf.RoundToInt(60 * Settings.Instance.TimeMultiplier);

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int sixhintVal && sixhintVal == 600)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, HealthTickIntervalSixHundredInt);
                    continue;
                }
                if (instr.opcode == OpCodes.Ldc_I4_S && instr.operand is int SsixhintVal && SsixhintVal == 600)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, HealthTickIntervalSixHundredInt);
                    continue;
                }
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int fiftintVal && fiftintVal == 15)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, HealthTickIntervalFifteenInt);
                    continue;
                }
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int sixtyintVal && sixtyintVal == 60)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, HealthTickIntervalSixtyInt);
                    continue;
                }
                yield return instr; // yeah gotta fix the uh, bleeding thing
            } // fully possible that we have to do this manually 
        }
        [StaticConstructorOnStartup]
        [HarmonyPatch(typeof(HealthUtility))]
        [HarmonyPatch(nameof(HealthUtility.TicksUntilDeathDueToBloodLoss))]
        public static class DeathInFixer
        {
            static void Postfix(ref int __result)
            {
                __result = Mathf.RoundToInt(__result * Settings.Instance.TimeMultiplier);
            }
        }
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
    } // heat patches below
    [HarmonyPatch(typeof(HediffGiver_Hypothermia))]
    [HarmonyPatch("OnIntervalPassed")]
    static class HypothermiaPatch
    {
        static float SpeedGain = 0.00075f * (1f / Settings.Instance.TimeMultiplier);
        static float SpeedLoss = 0.027f * (1f / Settings.Instance.TimeMultiplier);


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float intVal && intVal == 0.00075f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedGain);
                    continue;
                }
                yield return instr; 
            }
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float intVal && intVal == 0.027f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedLoss);
                    continue;
                }
                yield return instr;
            }// straight up floating
        }
    }
    [HarmonyPatch(typeof(HediffGiver_Heat))]
    [HarmonyPatch("OnIntervalPassed")]
    static class HyperthermiaPatch
    {
        static float SpeedGain = 0.000375f * (1f / Settings.Instance.TimeMultiplier);
        static float SpeedLoss = 0.027f * (1f / Settings.Instance.TimeMultiplier);


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float intVal && intVal == 0.000375f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedGain);
                    continue;
                }
                yield return instr;
            }
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float intVal && intVal == 0.027f)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, SpeedLoss);
                    continue;
                }
                yield return instr;
            }// straight up floating again
        }
    }
    /* [HarmonyPatch(typeof(HediffComp_HealPermanentWounds))]
     [HarmonyPatch("ResetTicksToHeal")]
     static class HealPWoundsPatch
     {

         static int Speed = Mathf.RoundToInt(60000 * Settings.Instance.TimeMultiplier);


         static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
         {
             foreach (var instr in instructions)
             {
                 if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int intVal && intVal == 60000)
                 {
                     yield return new CodeInstruction(OpCodes.Ldc_I4, Speed);
                     continue;
                 }
                 yield return instr;
             }
         }
     }
     [HarmonyPatch(typeof(HediffComp_KillAfterDays))]
     [HarmonyPatch("CompPostPostAdd")]
     static class KillAfterDaysPatch
     {

         static int Days = Mathf.RoundToInt(60000 * Settings.Instance.TimeMultiplier);


         static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
         {
             foreach (var instr in instructions)
             {
                 if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int intVal && intVal == 60000)
                 {
                     yield return new CodeInstruction(OpCodes.Ldc_I4, Days);
                     continue;
                 }
                 yield return instr;
             }
         }
     }*/

    // it seems to work which makes the top code redundant but i will leave it for now
    [StaticConstructorOnStartup]
    public static class Namespace60000Patcher
    {
        static Namespace60000Patcher()
        {
            PatchNamespace("Verse");
        }

        static void PatchNamespace(string ns)
        {
            int scaledTime = Mathf.RoundToInt(60000 * Settings.Instance.TimeMultiplier);
            float scaledFloatTime = 60000f * Settings.Instance.TimeMultiplier;
            int number1 = 0;
            int number2 = 0;

            var asm = typeof(Verse.TickManager).Assembly;
            var harmony = new Harmony("com.julekjulas.experimental60kpatch");

            foreach (var type in asm.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith(ns)))
            {
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.IsAbstract || method.IsGenericMethodDefinition) continue;

                    try
                    {
                        var transpiler = new HarmonyMethod(typeof(Namespace60000Patcher).GetMethod(nameof(Transpile60000), BindingFlags.Static | BindingFlags.NonPublic));
                        harmony.Patch(method, transpiler: transpiler);
                        number1++;
                    }
                    catch (Exception)
                    {
                        number2++;
                    }
                }
            }
            Log.Message($"[DayStretched] 60000 not found in {number2} methods in namespace {ns}.");
            Log.Message($"[DayStretched] Patched 60000 in {number1} methods in namespace {ns}.");
        }
        static IEnumerable<CodeInstruction> Transpile60000(IEnumerable<CodeInstruction> instructions)
        {
            int scaledTime = Mathf.RoundToInt(60000 * Settings.Instance.TimeMultiplier);
            float scaledFloatTime = 60000f * Settings.Instance.TimeMultiplier;
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int val && val == 60000)
                {
                    instr.operand = scaledTime;
                }
                else if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float f && Mathf.Approximately(f, 60000f))
                {
                    instr.operand = scaledFloatTime;
                }
                yield return instr;
            }
        }
    } //40k reference


    [HarmonyPatch(typeof(Pawn_InfectionVectorTracker))]
    [HarmonyPatch("InfectionTickInterval")]
    static class InfectionVectorTrackeTickIntervalPatch
    {
        static int TickIntervalInt = Mathf.RoundToInt(600 * Settings.Instance.TimeMultiplier);


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int intVal && intVal == 600)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, TickIntervalInt);
                    continue;
                }
                yield return instr; // seems to work
            }
        }
    }
    [HarmonyPatch(typeof(HediffComp_SeverityModifierBase))]
    [HarmonyPatch("CompPostTickInterval")]
    static class SeveritySecondPatch
    {
        static int Speed = Mathf.RoundToInt(200 * Settings.Instance.TimeMultiplier);


        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && instr.operand is int intVal && intVal == 200)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4, Speed);
                    continue;
                }
                yield return instr;
            }
        }
    }






    public class DayStretched : Mod
    {
        public DayStretched(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("julekjulas.daystretch");
            harmony.PatchAll();
        }
    }
}








