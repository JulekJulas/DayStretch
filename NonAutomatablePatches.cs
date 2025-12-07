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


