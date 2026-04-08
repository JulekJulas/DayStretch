using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DayStretched // for some reason when i change it to DayStretch it just breaks, its literally the tf2 coconut
{
    public class DayStretched : Mod
    { 
        public DayStretched(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("julekjulas.daystretch"); harmony.PatchAll();
        }
    }
}
