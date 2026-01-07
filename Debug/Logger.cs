using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

[StaticConstructorOnStartup]
public static class Logger
{
    static bool logShown = false;
    static Logger()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (!logShown)
            {
                logShown = true;
                string fullList = "[DayStretch]-(Logger)Mod Loaded Successfully, full list of all patches made:\n\n";
                fullList += "\n\n\n";
                fullList += AdvancedPatcher.loggerList;
                fullList += "\n\n\n";
                fullList += ResultPatcher.loggerList;
                fullList += "\n\n\n";
                fullList += DeltaPatcher.loggerList;
                fullList += "\n\n\n";
                fullList += StringPatcher.loggerList;
                fullList += "\n\n\n";
                fullList += VariablePatcher.loggerList;
                Log.Message(fullList);
            }
        });
    }

}
