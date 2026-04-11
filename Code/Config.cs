using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Verse;
namespace DayStretch
{
    public class DayStretch : ModSettings
    {
        public float FakeTimeMultiplier = 1f;
        public float TimeMultiplier = 1f;
        public float FakeWorkMultiplier = 1f;
        public float WorkMultiplier = 1f;
        public bool ShouldWorkFollow = true;
        public bool firstPopup = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref TimeMultiplier, "TimeMultiplier", 1f);
            Scribe_Values.Look(ref FakeTimeMultiplier, "FakeTimeMultiplier", 1f); // for the apply button
            Scribe_Values.Look(ref WorkMultiplier, "WorkRelated", 1f);
            Scribe_Values.Look(ref FakeWorkMultiplier, "FakeWorkRelated", 1f); // for the apply button
            Scribe_Values.Look(ref ShouldWorkFollow, "ShouldWorkFollow", true);
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
            settings.firstPopup = false;
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label("Time Multiplier: " + settings.FakeTimeMultiplier.ToString("0.0"));
            settings.FakeTimeMultiplier = listingStandard.Slider(settings.FakeTimeMultiplier, 0.1f, 20f);
            settings.FakeTimeMultiplier = (float)Math.Round(settings.FakeTimeMultiplier, 1);
            listingStandard.Label($"Current Time Multiplier: {settings.TimeMultiplier}");
            listingStandard.CheckboxLabeled("Should work use its own slider ", ref settings.ShouldWorkFollow);
            if (settings.ShouldWorkFollow)
            {
                listingStandard.Label("Work Related Multiplier: " + settings.FakeWorkMultiplier.ToString("0.0"));
                settings.FakeWorkMultiplier = listingStandard.Slider(settings.FakeWorkMultiplier, 0.1f, 20f);
                settings.FakeWorkMultiplier = (float)Math.Round(settings.FakeWorkMultiplier, 1);
                listingStandard.Label($"Current Work Related Multiplier: {settings.WorkMultiplier}"); // hello everybody my name is Multiplier
            }
            else
            {
                settings.FakeWorkMultiplier = settings.TimeMultiplier;
                settings.WorkMultiplier = settings.TimeMultiplier;
            }
            if (listingStandard.ButtonText("Apply"))
            {
                settings.TimeMultiplier = settings.FakeTimeMultiplier;
                settings.WorkMultiplier = settings.FakeWorkMultiplier;
                if (!settings.ShouldWorkFollow)
                {
                    settings.WorkMultiplier = settings.TimeMultiplier;
                    Find.WindowStack.Add(new Dialog_MessageBox($"Game has to be restarted for the settings to load, new multiplier: {settings.FakeTimeMultiplier}"));
                }
                else
                {
                    Find.WindowStack.Add(new Dialog_MessageBox($"Game has to be restarted for the settings to load\nNew multipliers: (Global){settings.FakeTimeMultiplier}, (Work){settings.FakeWorkMultiplier}"));
                }


            }
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return "DayStretch";
        }
    }
}


