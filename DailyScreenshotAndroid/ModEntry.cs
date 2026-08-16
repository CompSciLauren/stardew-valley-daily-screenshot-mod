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
        /// Default screenshot directory set in the entry
        /// </summary>
        /// <value>Path to the screenshot directory for this platform</value>
        public DirectoryInfo DefaultScreenshotDirectory { get; private set; }

        /// <summary>
        /// Per-save screenshot subdirectory, created once the save is loaded
        /// </summary>
        /// <value>Path to this save's screenshot subdirectory</value>
        public DirectoryInfo DefaultScreenshotSubdirectory { get; private set; }

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
            // matches the desktop mod's cross-platform "StardewValley/Screenshots" folder convention
            int specialFolderId = Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28;
            string path = Environment.GetFolderPath((Environment.SpecialFolder)specialFolderId);
            DefaultScreenshotDirectory = new DirectoryInfo(Path.Combine(path, "StardewValley", "Screenshots"));

            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveFileCode = Game1.uniqueIDForThisGame;
            string directoryName = $"{Game1.player.farmName.Value}-Farm-Screenshots-{saveFileCode}";
            DefaultScreenshotSubdirectory = DefaultScreenshotDirectory.CreateSubdirectory(directoryName);

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
                Directory.CreateDirectory(DefaultScreenshotSubdirectory.FullName);
                string relativePath = Path.Combine(DefaultScreenshotSubdirectory.Name, screenshotFileName);

                Game1.game1.takeMapScreenshot(ScreenshotZoomLevel, relativePath, () => {
                    // no post-screenshot action needed
                });

                Game1.addHUDMessage(new HUDMessage(screenshotFileName, HUDMessage.screenshot_type));
                Game1.playSound("cameraNoise");
                screenshotTakenToday = true;
            }
            catch (Exception ex)
            {
                MError($"Failed to take daily screenshot. Technical details:\n{ex}");
            }
        }

        /// <summary>Formats today's in-game date as a "year-season-day" filename, e.g. "01-02-03.png".</summary>
        private string FormatScreenshotFileName()
        {
            int seasonNumber = Game1.Date.SeasonIndex + 1;
            return $"{Game1.Date.Year:D2}-{seasonNumber:D2}-{Game1.Date.DayOfMonth:D2}.png";
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
