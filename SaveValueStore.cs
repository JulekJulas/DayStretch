using System;
using Verse;

namespace DayStretched
{
    public class DayStretchedGameComp : GameComponent
    {
        public float savedTimeMultiplier = 1f;

        public DayStretchedGameComp(Game game) : base()
        {
        }
        public override void StartedNewGame()
        {
            base.StartedNewGame();
            // capture the current global setting once for this save
            savedTimeMultiplier = Settings.Instance.TimeMultiplier;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref savedTimeMultiplier, "DayStretched_SavedTimeMultiplier", 1f);
        }
        public static float ForCurrentSave()
        {
            var comp = Current.Game?.GetComponent<DayStretchedGameComp>();
            return comp != null ? comp.savedTimeMultiplier : Settings.Instance.TimeMultiplier;
        }
    


    public override void LoadedGame()
        {
            base.LoadedGame();
            if (savedTimeMultiplier != Settings.Instance.TimeMultiplier)
            {
                string text = $"The saved time multiplier for this save is {savedTimeMultiplier}.\n" +
                              $"Your current multiplier is {Settings.Instance.TimeMultiplier}.\n" +
                              $"Continuing with the current multiplier may cause save corruption and many bugs.\n\n" +
                              $"Do you want to switch to the saved multiplier?\n\n" +
                              $"Though also note that the mod cannot be added midgame.";
                string text2 = $"Are you absolutely sure?";



                Find.WindowStack.Add(new Dialog_MessageBox(
                    text,
                    "Yes".Translate(), () =>
                    {
                        Settings.Instance.TimeMultiplier = savedTimeMultiplier;
                        Find.WindowStack.Add(new Dialog_MessageBox("Done! Please restart the game for the changes to apply. DO NOT SAVE NOW"));
                    },
                    "No".Translate(), () =>
                    {
                        Find.WindowStack.Add(new Dialog_MessageBox(
                        text2,
                        "Yes".Translate(), () =>
                        {
                            Find.WindowStack.Add(new Dialog_MessageBox("You have been warned."));
                        },
                            "No".Translate(), () =>
                            {
                                Settings.Instance.TimeMultiplier = savedTimeMultiplier;
                                Find.WindowStack.Add(new Dialog_MessageBox("Value changed! Please restart the game for the changes to apply. DO NOT SAVE NOW"));
                            }
                    ));
                    }
                ));
            }
        }
    }
}