using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;


public class DayStretch : ModSettings
{

    public float TimeMultiplier = 1f;
    public bool WorkRelated = true;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref TimeMultiplier, "TimeMultiplier", 1f);
        Scribe_Values.Look(ref WorkRelated, "WorkRelated", true);
        base.ExposeData();
    }
}
public class Settings : Mod
{
    DayStretch settings;
    public static DayStretch Instance;
    public Settings(ModContentPack content) : base(content)
    {
        settings = GetSettings<DayStretch>();
        Instance = settings;
    }
    public override void DoSettingsWindowContents(Rect inRect)
    {

        Listing_Standard listingStandard = new Listing_Standard();
        listingStandard.Begin(inRect);
        listingStandard.CheckboxLabeled("Should work be scaled ", ref settings.WorkRelated, "If ticked on, work will be x times slower or faster");
        listingStandard.Label("Time Multiplier: " + settings.TimeMultiplier.ToString("0.0"));
        settings.TimeMultiplier = listingStandard.Slider(settings.TimeMultiplier, 0.1f, 20f);
        settings.TimeMultiplier = (float)Math.Round(settings.TimeMultiplier, 1);
        listingStandard.Label("The game MUST be restarted every time the time multiplier is changed or else the code cannot update.");
        listingStandard.Gap();
        listingStandard.End();
        base.DoSettingsWindowContents(inRect);
    }
    public override string SettingsCategory()
    {
        return "DayStretch";
    }
}

