using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DailyScreenshot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private DirectoryInfo exportDirectory, screenshotsDirectory;
        private string stardewValleyLocation = "Farm";
        private string stardewValleyYear, stardewValleySeason, stardewValleyDayOfMonth;
        private bool screenshotTakenToday = false;
        IReflectedMethod takeScreenshot = null;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            var stardewValleyRootDirectory = new DirectoryInfo(Constants.ExecutionPath);
            exportDirectory = stardewValleyRootDirectory.EnumerateDirectories("MapExport").FirstOrDefault();
            if (exportDirectory == null)
            {
                exportDirectory = stardewValleyRootDirectory.CreateSubdirectory("MapExport");
            }
            CreateFileSystemWatcher();
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            var farmName = Game1.player.farmName;
            var directoryName = $"{farmName}-Farm-Screenshots";
            screenshotsDirectory = exportDirectory.CreateSubdirectory(directoryName);
            Helper.Events.Player.Warped += OnNewLocationEntered;
            Helper.Events.GameLoop.DayEnding += SetScreenshotTakenTodayToFalse;
            takeScreenshot = Helper.Reflection.GetMethod(Game1.game1, "takeMapScreenshot");
        }



        /// <summary>Creates a new FileSystemWatcher and set its properties.</summary>
        private void CreateFileSystemWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher
            {

                // Set watcher path.
                Path = exportDirectory.FullName,

                // Watch for changes in LastAccess and LastWrite times, and the renaming of files or directories.
                NotifyFilter = NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.FileName
                                 | NotifyFilters.DirectoryName,

                // Only watch Portable Network Graphics files.
                Filter = $"*.png"
            };

            // Add event handlers.
            watcher.Changed += OnScreenshotChanged;
            watcher.Created += OnScreenshotChanged;

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnNewLocationEntered(object sender, WarpedEventArgs e)
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
            await Task.Delay(10000);

            // prepare screenshot name
            stardewValleyYear = Game1.Date.Year.ToString();
            stardewValleySeason = Game1.Date.Season.ToString();
            stardewValleyDayOfMonth = Game1.Date.DayOfMonth.ToString();

            if (int.Parse(stardewValleyYear) < 10)
            {
                stardewValleyYear = "0" + stardewValleyYear;
            }
            if (int.Parse(stardewValleyDayOfMonth) < 10)
            {
                stardewValleyDayOfMonth = "0" + stardewValleyDayOfMonth;
            }

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

            // take screenshot
            string screenshotName = $"{stardewValleyYear}-{stardewValleySeason}-{stardewValleyDayOfMonth}";
            takeScreenshot.Invoke<string>(0.25f, screenshotName);
            screenshotTakenToday = true;
        }

        /// <summary>Raised if the screenshot is changed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnScreenshotChanged(object sender, FileSystemEventArgs e)
        {
            var fileName = Path.GetFileNameWithoutExtension(e.FullPath);
            if (fileName == stardewValleyLocation &&
                (e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed))
            {
                try
                {
                    stardewValleyYear = Game1.Date.Year.ToString();
                    stardewValleySeason = Game1.Date.Season.ToString();
                    stardewValleyDayOfMonth = Game1.Date.DayOfMonth.ToString();

                    if (int.Parse(stardewValleyYear) < 10)
                    {
                        stardewValleyYear = "0" + stardewValleyYear;
                    }
                    if (int.Parse(stardewValleyDayOfMonth) < 10)
                    {
                        stardewValleyDayOfMonth = "0" + stardewValleyDayOfMonth;
                    }

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

                    FileInfo screenshot = new FileInfo(e.FullPath);
                    var correctPath = Path.Combine(screenshotsDirectory.FullName, $"{stardewValleyYear}-{stardewValleySeason}-{stardewValleyDayOfMonth}.png");
                    var correctFile = new FileInfo(correctPath);
                    if (!correctFile.Exists)
                    {
                        screenshot.CopyTo(correctFile.FullName);
                    }
                }
                catch (IOException)
                {
                }
            }
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