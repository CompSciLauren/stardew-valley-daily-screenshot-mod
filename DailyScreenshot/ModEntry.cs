using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DailyScreenshot
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private const int MAX_ATTEMPTS_TO_MOVE = 20;
        private const int SHARING_VIOLATION = 32;

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

            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        /// <summary>Raised after a button is pressed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.TryGetKeyboard(out Keys _))
            {
                if (e.Button == Config.TakeScreenshotKey)
                {
                    TakeScreenshotViaKeypress();
                }
            }
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
            if (e.NewLocation is Farm && !screenshotTakenToday && Game1.timeOfDay >= Config.TimeScreenshotGetsTakenAfter)
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

        /// <summary>Checks whether it is the appropriate day to take a screenshot of the entire farm.</summary>
        private void TakeScreenshot()
        {
            if (Config.HowOftenToTakeScreenshot["Daily"] == true)
            {
                ActuallyTakeScreenshot();
            }
            else if (Config.HowOftenToTakeScreenshot[Game1.Date.DayOfWeek + "s"] == true)
            {
                ActuallyTakeScreenshot();
            }
            else if (Config.HowOftenToTakeScreenshot["First Day of Month"] == true && Game1.Date.DayOfMonth.ToString() == "1")
            {
                ActuallyTakeScreenshot();
            }
            else if (Config.HowOftenToTakeScreenshot["Last Day of Month"] == true && Game1.Date.DayOfMonth.ToString() == "28")
            {
                ActuallyTakeScreenshot();
            }
        }

        /// <summary>Takes a screenshot of the entire farm.</summary>
        private void ActuallyTakeScreenshot()
        {
            ConvertInGameDateToNumericFormat();
            string screenshotName = $"{stardewValleyYear}-{stardewValleySeason}-{stardewValleyDayOfMonth}";

            string mapScreenshot = Game1.game1.takeMapScreenshot(0.25f, screenshotName);
            Game1.addHUDMessage(new HUDMessage(mapScreenshot, 6));
            Game1.playSound("cameraNoise");

            screenshotTakenToday = true;
            MoveScreenshotToCorrectFolder(screenshotName);
        }

        /// <summary>Takes a screenshot of the entire map, activated via keypress.</summary>
        private void TakeScreenshotViaKeypress()
        {
            string screenshotName = SaveGame.FilterFileName((string)(NetFieldBase<string, NetString>)Game1.player.name) + "_" + DateTime.UtcNow.Month + "-" + DateTime.UtcNow.Day.ToString() + "-" + DateTime.UtcNow.Year.ToString() + "_" + (int)DateTime.UtcNow.TimeOfDay.TotalMilliseconds;

            string mapScreenshot = Game1.game1.takeMapScreenshot(Config.TakeScreenshotKeyZoomLevel, screenshotName);
            Game1.addHUDMessage(new HUDMessage(mapScreenshot, 6));
            Game1.playSound("cameraNoise");

            if (Config.FolderDestinationForKeypressScreenshots != "default")
            {
                MoveScreenshotToCorrectFolder(screenshotName, true);
            }
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
        /// <param name="keypress">true if the user pressed the key</param>
        private void MoveScreenshotToCorrectFolder(string screenshotName, bool keypress=false)
        {
            // special folder path
            int num11 = Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28;
            var path = Environment.GetFolderPath((Environment.SpecialFolder)num11);

            // path is combined with StardewValley and then Screenshots
            string defaultStardewValleyScreenshotsDirectory = Path.Combine(path, "StardewValley", "Screenshots");
            string stardewValleyScreenshotsDirectory = defaultStardewValleyScreenshotsDirectory;

            // path for original screenshot location and new screenshot location
            string screenshotNameWithExtension = screenshotName + ".png";
            string sourceFile = Path.Combine(defaultStardewValleyScreenshotsDirectory, screenshotNameWithExtension);
            string destinationFile;

            if(keypress)
            {
                if(Config.FolderDestinationForKeypressScreenshots != "default")
                {
                    stardewValleyScreenshotsDirectory = Config.FolderDestinationForKeypressScreenshots;
                }
                else
                {
                    stardewValleyScreenshotsDirectory = defaultStardewValleyScreenshotsDirectory;
                }
            }   
            else
            {
                if (Config.FolderDestinationForDailyScreenshots != "default")
                {
                    stardewValleyScreenshotsDirectory = Path.Combine(Config.FolderDestinationForDailyScreenshots,
                        Game1.player.farmName + "-Farm-Screenshots-" + saveFileCode);
                }
                else
                {
                    stardewValleyScreenshotsDirectory = Path.Combine(defaultStardewValleyScreenshotsDirectory,
                        Game1.player.farmName + "-Farm-Screenshots-" + saveFileCode);
                }
            }
            destinationFile = Path.Combine(stardewValleyScreenshotsDirectory, screenshotNameWithExtension);

            // create save directory if it doesn't already exist
            if (!File.Exists(stardewValleyScreenshotsDirectory))
            {
                Directory.CreateDirectory(stardewValleyScreenshotsDirectory);
            }

            // delete old version of screenshot if one exists
            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            int attemptCount = 0;
            while(File.Exists(sourceFile) && attemptCount < MAX_ATTEMPTS_TO_MOVE)
            {
                try
                {
                    attemptCount++;
                    using (FileStream lockFile = new FileStream(
                        sourceFile,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.Read | FileShare.Delete
                    ))
                    {
                        File.Copy(sourceFile, destinationFile);
                        File.Delete(sourceFile);
                    }
                }
                catch (IOException ex)
                {
                    int HResult = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    if (SHARING_VIOLATION == (HResult & 0xFFFF))
                    {
                        Monitor.Log($"File may be in use, retrying in 10 milliseconds, attempt {attemptCount} of {MAX_ATTEMPTS_TO_MOVE}", LogLevel.Info);
                        Thread.Sleep(10);
                    }
                    else
                    {
                        Monitor.Log($"Error moving file '{screenshotNameWithExtension}' into {stardewValleyScreenshotsDirectory} folder. Technical details:\n{ex}", LogLevel.Error);
                        attemptCount = MAX_ATTEMPTS_TO_MOVE;
                    }
                }
                catch(Exception ex)
                {
                    Monitor.Log($"Error moving file '{screenshotNameWithExtension}' into {stardewValleyScreenshotsDirectory} folder. Technical details:\n{ex}", LogLevel.Error);
                    attemptCount = MAX_ATTEMPTS_TO_MOVE;
                }
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