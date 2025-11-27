using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DayStretched
{
    public class DayStretch : ModSettings
    {

        public float TimeMultiplier = 1f;
        public bool WorkRelated = true;
        public float IndividualTimeMultiplier = 1f;
        public bool ShouldUseIndividual = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref TimeMultiplier, "TimeMultiplier", 1f);
            Scribe_Values.Look(ref IndividualTimeMultiplier, "IndividualTimeMultiplier", 1f);
            Scribe_Values.Look(ref WorkRelated, "WorkRelated", true);
            Scribe_Values.Look(ref ShouldUseIndividual, "ShouldUseIndividual", true);
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
            listingStandard.Label("Time Multiplier: " + settings.TimeMultiplier.ToString("0.00"));
            settings.TimeMultiplier = listingStandard.Slider(settings.TimeMultiplier, 0.1f, 20f);
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Should Needs be scaled individually ", ref settings.ShouldUseIndividual, "If ticked on, needs and work will be x times slower or faster ignoring main time modifier");
            listingStandard.Label("Multiplier: " + settings.IndividualTimeMultiplier.ToString("0.00"));
            settings.IndividualTimeMultiplier = listingStandard.Slider(settings.IndividualTimeMultiplier, 0.1f, 20f);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return "DayStretch".Translate();
        }
    }
}
