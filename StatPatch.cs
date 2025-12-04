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
using System.Security.Cryptography;
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



namespace DayStretched
{
    [StaticConstructorOnStartup]
    public class StatPatch
    {
        static StatPatch()
        {
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
                            Log.Warning($"[DayStretch] Could not find StatDef '{statName}' to patch.");
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
                                Log.Warning($"[DayStretch] Could not find work related StatDef '{statName}' to patch.");
                            }
                        }
                    }
                }
            }
            if (pdef > 0)
            {
                Log.Message($"[DayStretch] Patched {pdef} defs.");
            }
            if (wpdef > 0)
            {
                Log.Message($"[DayStretch] Patched {wpdef} work defs.");
            }
        }
    }
    namespace DayStretched
    {


        // moved these to NonAutomatablePatches but gonna leave them here just in case
      /*  [HarmonyPatch(typeof(Pawn_NeedsTracker))]
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
                  // note to self: we did
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
        }*/
        public class DayStretched : Mod
        {
            public DayStretched(ModContentPack content) : base(content)
            {
                var harmony = new Harmony("julekjulas.daystretch");
                harmony.PatchAll();
            }
        }
    }
}








