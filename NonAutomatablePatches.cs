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

namespace DayStretched
{// would like to say
    // FOR NOW unautomatable, i want to change that in the future



    // reason: three variables, dont want to change the whole code for this edge case
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
    }
}
