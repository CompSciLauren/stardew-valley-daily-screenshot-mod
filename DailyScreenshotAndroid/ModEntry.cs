using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.IO;

namespace DailyScreenshot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /// <summary>Number of update ticks to wait after warping onto the farm before taking the screenshot, so the world has time to finish rendering.</summary>
        private const int ScreenshotDelayTicks = 60;

        /// <summary>The zoom level used for the daily screenshot.</summary>
        private const float ScreenshotZoomLevel = 0.25f;

        private string screenshotFileName;
        private bool screenshotTakenToday;
        private int countdownTicks;
        private ulong saveFileCode;

        /// <summary>
        /// Per-save screenshot subdirectory name, created once the save is loaded.
        /// Relative to <see cref="Game1.GetScreenshotFolder"/> — the game's own
        /// screenshot folder, not a path this mod guesses at, since
        /// <see cref="Game1.takeMapScreenshot(float, string, Action)"/> always
        /// resolves the given name against that folder.
        /// </summary>
        /// <value>Name of this save's screenshot subdirectory</value>
        public string ScreenshotSubdirectoryName { get; private set; }

        /// <summary>
        /// Helper function for sending trace messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MTrace(string message) => Monitor.Log(message, LogLevel.Trace);

        /// <summary>
        /// Helper function for sending error messages
        /// Always display even if verbose logging is off
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MError(string message) => Monitor.Log(message, LogLevel.Error);

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveFileCode = Game1.uniqueIDForThisGame;
            ScreenshotSubdirectoryName = $"{Game1.player.farmName.Value}-Farm-Screenshots-{saveFileCode}";

            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        }

        /// <summary>Raised after day has started.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            screenshotFileName = FormatScreenshotFileName();
            screenshotTakenToday = false;
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Farm && !screenshotTakenToday)
            {
                countdownTicks = ScreenshotDelayTicks;
                Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            countdownTicks--;
            if (countdownTicks > 0)
                return;

            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            TakeScreenshot();
        }

        /// <summary>Takes a screenshot of the entire farm using the game's built-in screenshot support.</summary>
        private void TakeScreenshot()
        {
            try
            {
                // takeMapScreenshot() resolves its "screenshot_name" argument against this
                // folder itself (and appends ".png" to it), so the subdirectory we create
                // has to live under here rather than under a path this mod guesses at.
                string screenshotsFolder = Game1.game1.GetScreenshotFolder();
                Directory.CreateDirectory(Path.Combine(screenshotsFolder, ScreenshotSubdirectoryName));
                string relativePath = Path.Combine(ScreenshotSubdirectoryName, screenshotFileName);

                Game1.game1.takeMapScreenshot(ScreenshotZoomLevel, relativePath, () => {
                    // no post-screenshot action needed
                });

                Game1.addHUDMessage(new HUDMessage($"{screenshotFileName}.png", HUDMessage.screenshot_type));
                Game1.playSound("cameraNoise");
                screenshotTakenToday = true;
            }
            catch (Exception ex)
            {
                MError($"Failed to take daily screenshot. Technical details:\n{ex}");
            }
        }

        /// <summary>Formats today's in-game date as a "year-season-day" filename, e.g. "01-02-03", with no extension since takeMapScreenshot() appends ".png" itself.</summary>
        private string FormatScreenshotFileName()
        {
            int seasonNumber = Game1.Date.SeasonIndex + 1;
            return $"{Game1.Date.Year:D2}-{seasonNumber:D2}-{Game1.Date.DayOfMonth:D2}";
        }

        /// <summary>Raised after the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            Helper.Events.Player.Warped -= OnWarped;
            Helper.Events.GameLoop.DayStarted -= OnDayStarted;
            Helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            screenshotTakenToday = false;
        }
    }
}
