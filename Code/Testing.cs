using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DayStretch
{
    /*
     * this may be smart in the future if i find a way to make it work, but for now it just causes more problems than it solves
    [HarmonyPatch(typeof(TickManager), "TickRateMultiplier", MethodType.Getter)]
    public static class TickRateMultiplierPatch
    {
        public static void Postfix(ref float __result)
        {
            __result /= 10f;
        }
    }*/
}
