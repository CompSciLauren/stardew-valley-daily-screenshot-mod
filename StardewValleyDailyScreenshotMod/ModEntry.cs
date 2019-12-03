using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewValleyDailyScreenshotMod
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        bool screenshotTakenToday = false;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Player.Warped += this.OnNewLocationEntered;
            helper.Events.GameLoop.DayEnding += this.SetScreenshotTakenTodayToFalse;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnNewLocationEntered(object sender, WarpedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} exited {e.OldLocation} and entered {e.NewLocation}", LogLevel.Debug);

            if (e.NewLocation is StardewValley.Farm && !screenshotTakenToday)
            {
                TakeScreenshot();
            }
            else
            {
                this.Monitor.Log($"Either location is not Farm or screenshot was already taken today.", LogLevel.Debug);
            }
        }

        /// <summary>Takes a screenshot of the farm from a bird's eye view.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void TakeScreenshot()
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print screenshot taken to the console window
            this.Monitor.Log($"Setting screenshotTakenToday to true.", LogLevel.Debug);
            screenshotTakenToday = true;
        }

        /// <summary>Takes a screenshot of the farm from a bird's eye view.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void SetScreenshotTakenTodayToFalse(object sender, DayEndingEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print screenshot taken to the console window
            this.Monitor.Log($"Setting screenshotTakenToday to false.", LogLevel.Debug);
            screenshotTakenToday = false;
        }
    }
}