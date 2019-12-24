using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;

namespace DailyScreenshot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        IReflectedMethod takeScreenshot = null;
        private string stardewValleyYear, stardewValleySeason, stardewValleyDayOfMonth;
        private bool screenshotTakenToday = false;
        int countdownInSeconds = 60;
        ulong saveFileCode;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            saveFileCode = Game1.uniqueIDForThisGame;
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            takeScreenshot = Helper.Reflection.GetMethod(Game1.game1, "takeMapScreenshot");
        }

        /// <summary>Raised after day has started.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            screenshotTakenToday = false;
            countdownInSeconds = 60;

            EnqueueAction(() => {
                TakeScreenshot();
            });
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Farm && !screenshotTakenToday && Game1.timeOfDay >= Config.TimeScreenshotGetsTaken)
            {
                Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            }
        }

        /// <summary>Raised after game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            countdownInSeconds--;

            if (countdownInSeconds == 0)
            {
                while (_actions.Count > 0)
                    _actions.Dequeue().Invoke();
            }
        }

        /// <summary>Takes a screenshot of the entire farm.</summary>
        private void TakeScreenshot()
        {
            ConvertInGameDateToNumericFormat();

            string screenshotName = $"{stardewValleyYear}-{stardewValleySeason}-{stardewValleyDayOfMonth}";
            takeScreenshot.Invoke<string>(0.25f, screenshotName);
            screenshotTakenToday = true;

            MoveScreenshotToCorrectFolder(screenshotName);
        }

        private Queue<Action> _actions = new Queue<Action>();

        /// <summary>Allows ability to enqueue actions to the queue.</summary>
        /// <param name="action">The action.</param>
        public void EnqueueAction(Action action)
        {
            if (action == null) return;
            _actions.Enqueue(action);
        }

        /// <summary>Fixes the screenshot name to be in the proper format.</summary>
        private void ConvertInGameDateToNumericFormat()
        {
            stardewValleyYear = Game1.Date.Year.ToString();
            stardewValleySeason = Game1.Date.Season.ToString();
            stardewValleyDayOfMonth = Game1.Date.DayOfMonth.ToString();

            // fix year and month to be in numeric format
            if (int.Parse(stardewValleyYear) < 10)
            {
                stardewValleyYear = "0" + stardewValleyYear;
            }
            if (int.Parse(stardewValleyDayOfMonth) < 10)
            {
                stardewValleyDayOfMonth = "0" + stardewValleyDayOfMonth;
            }

            // fix season to be in numeric format
            switch (Game1.Date.Season)
            {
                case "spring":
                    stardewValleySeason = "01";
                    break;
                case "summer":
                    stardewValleySeason = "02";
                    break;
                case "fall":
                    stardewValleySeason = "03";
                    break;
                case "winter":
                    stardewValleySeason = "04";
                    break;
            }
        }

        /// <summary>Moves screenshot into StardewValley/Screenshots directory, in the save file folder.</summary>
        /// <param name="screenshotName">The name of the screenshot file.</param>
        private void MoveScreenshotToCorrectFolder(string screenshotName)
        {
            // special folder path
            int num11 = Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28;
            var path = Environment.GetFolderPath((Environment.SpecialFolder)num11);

            // path is combined with StardewValley and then Screenshots
            string stardewValleyScreenshotsDirectory = Path.Combine(path, "StardewValley", "Screenshots");

            // path for farm folder and screenshots
            string saveFilePath = Game1.player.farmName + "-Farm-Screenshots-" + saveFileCode;
            string screenshotNameWithExtension = screenshotName + ".png";

            // path for original screenshot location and new screenshot location
            string sourceFile = Path.Combine(stardewValleyScreenshotsDirectory, screenshotNameWithExtension);
            string destinationFile = Path.Combine(stardewValleyScreenshotsDirectory, saveFilePath, screenshotNameWithExtension);

            // path for farm folder
            string saveDirectoryFullPath = Path.Combine(stardewValleyScreenshotsDirectory, saveFilePath);

            // create save directory if it doesn't already exist
            if (!File.Exists(saveDirectoryFullPath))
            {
                Directory.CreateDirectory(saveDirectoryFullPath);
            }

            // delete old version of screenshot if one exists
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            try
            {
                File.Move(sourceFile, destinationFile);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error moving file '{screenshotNameWithExtension}' into {saveFilePath} folder. Technical details:\n{ex}", LogLevel.Error);
            }
        }

        /// <summary>Raised after the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            screenshotTakenToday = false;
            countdownInSeconds = 60;
        }
    }
}