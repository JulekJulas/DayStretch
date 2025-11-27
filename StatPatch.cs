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
                        if (Settings.Instance.ShouldUseIndividual == true)
                        {
                            statDef.defaultBaseValue /= Settings.Instance.IndividualTimeMultiplier;
                        }
                        else
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
                            if (Settings.Instance.ShouldUseIndividual == true)
                            {
                                statDef.defaultBaseValue /= Settings.Instance.IndividualTimeMultiplier;
                            }
                            else
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
    }
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







    public class DayStretched : Mod
    {
        public DayStretched(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("julekjulas.daystretch");
            harmony.PatchAll();
            Log.Message($"[DayStretched]PatchAll ran");
        }
    }
}








