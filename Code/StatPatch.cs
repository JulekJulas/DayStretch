using RimWorld;
using System.Collections.Generic;
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

public class StatPatchReverseListDef : Def
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
                    if (string.IsNullOrEmpty(statName)) continue;
                    else
                    {
                        StatDef statDef = DefDatabase<StatDef>.GetNamed(statName, true);
                        if (statDef != null)
                        {
                            statDef.defaultBaseValue /= Settings.Instance.TimeMultiplier;
                            pdef++;
                        }
                        else Log.Warning($"[DayStretch]-(StatPatch) Could not find StatDef '{statName}' to patch.");
                    }
                }
            }
            foreach (StatPatchReverseListDef def in DefDatabase<StatPatchReverseListDef>.AllDefsListForReading)
            {
                foreach (string statName in def.statsName)
                {
                    if (string.IsNullOrEmpty(statName)) continue;
                    else
                    {
                        StatDef statDef = DefDatabase<StatDef>.GetNamed(statName, true);
                        if (statDef != null)
                        {
                            statDef.defaultBaseValue *= Settings.Instance.TimeMultiplier;
                            pdef++;
                        }
                        else Log.Warning($"[DayStretch]-(StatPatch) Could not find StatDef '{statName}' to patch.");
                    }
                }
            }
            if (Settings.Instance.WorkRelated == true)
            {
                foreach (StatPatchListWorkDef def in DefDatabase<StatPatchListWorkDef>.AllDefsListForReading)
                {
                    foreach (string statName in def.statsName)
                    {
                        if (string.IsNullOrEmpty(statName)) continue;
                        else
                        {
                            StatDef statDef = DefDatabase<StatDef>.GetNamed(statName, true);
                            if (statDef != null)
                            {
                                statDef.defaultBaseValue /= Settings.Instance.TimeMultiplier;
                                wpdef++;
                            }
                            else Log.Warning($"[DayStretch]-(StatPatch) Could not find work related StatDef '{statName}' to patch.");
                        }
                    }
                }
            } 
            if (pdef > 0) Log.Message($"[DayStretch] Patched {pdef} defs.");
            if (wpdef > 0) Log.Message($"[DayStretch] Patched {wpdef} work defs.");
        }
    }

}








