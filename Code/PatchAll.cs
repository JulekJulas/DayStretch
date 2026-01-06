using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DayStretched
{
    public class DayStretched : Mod
    {
        public DayStretched(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("julekjulas.daystretch"); harmony.PatchAll();
        }
    }
}
