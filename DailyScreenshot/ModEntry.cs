using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DailyScreenshot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        IReflectedMethod takeScreenshot = null;
        private string stardewValleyLocation = "Farm";
        private string stardewValleyYear, stardewValleySeason, stardewValleyDayOfMonth;
        private bool screenshotTakenToday = false;

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
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.DayEnding += SetScreenshotTakenTodayToFalse;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            takeScreenshot = Helper.Reflection.GetMethod(Game1.game1, "takeMapScreenshot");
        }

        /// <summary>Raised after the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            screenshotTakenToday = false;
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.NewLocation is Farm && !screenshotTakenToday)
            {
                TakeScreenshot();
            }
        }

        /// <summary>Takes a screenshot of the entire farm.</summary>
        private async void TakeScreenshot()
        {
            // wait 0.6 seconds so that buildings on map can completely render
            await Task.Delay(600);

            ConvertInGameDateToNumericFormat();

            // take screenshot
            string screenshotName = $"{stardewValleyYear}-{stardewValleySeason}-{stardewValleyDayOfMonth}";
            takeScreenshot.Invoke<string>(0.25f, screenshotName);
            screenshotTakenToday = true;

            MoveScreenshotToCorrectFolder(screenshotName);
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

        /// <summary>Moves the screenshot into the StardewValley/Screenshots directory, in a folder for the save file.</summary>
        /// <param name="screenshotName">The name of the screenshot file.</param>
        private void MoveScreenshotToCorrectFolder(string screenshotName)
        {
            // gather directory and file paths
            string screenshotNameWithExtension = screenshotName + ".png";
            string stardewValleyScreenshotsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley\\Screenshots");

            string sourceFile = Path.Combine(stardewValleyScreenshotsDirectory, screenshotNameWithExtension);
            string destinationFile = Path.Combine(stardewValleyScreenshotsDirectory, Game1.player.farmName + "-Farm-Screenshots", screenshotNameWithExtension);

            string saveDirectoryFullPath = Path.Combine(stardewValleyScreenshotsDirectory, Game1.player.farmName + "-Farm-Screenshots");
            // create save directory if it doesn't already exist
            if (!System.IO.File.Exists(saveDirectoryFullPath))
            {
                System.IO.Directory.CreateDirectory(saveDirectoryFullPath);
            }

            // move screenshot into correct folder, overwrite file if already exists in folder
            System.IO.File.Copy(sourceFile, destinationFile, true);

            // delete original screenshot that still exists in StardewValley/Screenshots
            System.IO.File.Delete(sourceFile);
        }

        /// <summary>Sets screenshotTakenToday variable to false.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void SetScreenshotTakenTodayToFalse(object sender, DayEndingEventArgs e)
        {
            screenshotTakenToday = false;
        }
    }
}