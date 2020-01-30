using Microsoft.Xna.Framework.Input;
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
        internal static ModEntry DailySS = null;
#region Constants 
        /// <summary>
        /// Maximum attempts to move the file
        /// </summary>
        private const int MAX_ATTEMPTS_TO_MOVE = 10000;

        /// <summary>
        /// Sharing violation code
        /// </summary>
        private const int SHARING_VIOLATION = 32;

        /// <summary>
        /// Tick countdown
        /// </summary>
        private const int MAX_COUNTDOWN_IN_TICKS = 60;

        /// <summary>
        /// Time to sleep between move attempts
        /// </summary>
        private const int MILLISECONDS_TIMEOUT = 10;

#endregion

        /// <summary>The mod configuration from the player.</summary>
        private ModConfig Config;

        int countdownInTicks = MAX_COUNTDOWN_IN_TICKS;

        public string defaultStardewValleyScreenshotsDirectory { get; private set; }

        /// <summary>
        /// Check that a directory contains no files or directories
        /// </summary>
        /// <param name="path">Directory to check</param>
        /// <returns>true if the directory is empty</returns>
        private bool DirectoryIsEmpty(string path) => Directory.GetDirectories(path).Length == 0 && Directory.GetFiles(path).Length == 0;

#region Logging
        /// <summary>
        /// Sends messages to the SMAPI console
        /// </summary>
        /// <param name="message">text to send</param>
        /// <param name="level">type of message</param>
        // Private copy so there's one place to alter all log messages if needed
#if DEBUG
        internal void LogMessageToConsole(string message, LogLevel level) => Monitor.Log(message, level);
#else
        internal void LogMessageToConsole(string message, LogLevel level) => Monitor.VerboseLog(message, level);
#endif


        /// <summary>
        /// Helper function for sending trace messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MTrace(string message) => LogMessageToConsole(message, LogLevel.Trace);


        /// <summary>
        /// Helper function for sending trace messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MDebug(string message) => LogMessageToConsole(message, LogLevel.Debug);

        /// <summary>
        /// Helper function for sending trace messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MInfo(string message) => LogMessageToConsole(message, LogLevel.Info);

        /// <summary>
        /// Helper function for sending trace messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MAlert(string message) => LogMessageToConsole(message, LogLevel.Alert);

        /// <summary>
        /// Helper function for sending warning messages
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MWarn(string message) => LogMessageToConsole(message, LogLevel.Warn);

        /// <summary>
        /// Helper function for sending error messages
        /// Always display even if verbose logging is off
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MError(string message) => Monitor.Log(message, LogLevel.Error);
#endregion

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            if(DailySS != null)
            {
                string message = "Entry called twice - breaking singelton";
                MError(message);
                throw new Exception(message);
            }
            DailySS = this;
            Config = Helper.ReadConfig<ModConfig>();
            //Config.ValidateUserInput();
            Config.NameRules();
            // Fixed something up, write new rules
            if(Config.RulesModified)
                Helper.WriteConfig<ModConfig>(Config);

            int num11 = Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28;
            var path = Environment.GetFolderPath((Environment.SpecialFolder)num11);

            // path is combined with StardewValley and then Screenshots
            defaultStardewValleyScreenshotsDirectory = Path.Combine(path, "StardewValley", "Screenshots");
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.TimeChanged += OnTimeChanged;
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
            Helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            RunTriggers();
        }

        /// <summary>Raised after a button is pressed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.TryGetKeyboard(out Keys k))
            {
                RunTriggers(k);
            }
        }

        /// <summary>Raised after day has started.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            countdownInTicks = MAX_COUNTDOWN_IN_TICKS;
            foreach (ModRule rule in Config.SnapshotRules)
            {
                rule.Trigger.ResetTrigger();
            }
            RunTriggers();
        }

        private void RunTriggers(Keys key = Keys.None)
        {
            foreach (ModRule rule in Config.SnapshotRules)
            {
                if(rule.Trigger.CheckTrigger(key))
                {
                    TakeScreenshot(rule);
                }
            }
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            RunTriggers();
        }

        /// <summary>Raised after game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            countdownInTicks--;

            if (countdownInTicks == 0)
            {
                while (_actions.Count > 0)
                    _actions.Dequeue().Invoke();
            }
        }

        private void TakeScreenshot(ModRule rule)
        {
            string ssPath = rule.GetFileName();
            string ssDirectory = Path.GetDirectoryName(ssPath);
            Directory.CreateDirectory(Path.Combine(defaultStardewValleyScreenshotsDirectory, ssDirectory));
            string mapScreenshot = Game1.game1.takeMapScreenshot(rule.ZoomLevel, ssPath);
            MTrace($"Snapshot saved to {mapScreenshot}");
            Game1.addHUDMessage(new HUDMessage(rule.Name, HUDMessage.screenshot_type));
            Game1.playSound("cameraNoise");
        }

        private Queue<Action> _actions = new Queue<Action>();

        /// <summary>Allows ability to enqueue actions to the queue.</summary>
        /// <param name="action">The action.</param>
        public void EnqueueAction(Action action)
        {
            if (null == action) return;
            _actions.Enqueue(action);
        }

#if false
        /// <summary>Moves screenshot into StardewValley/Screenshots directory, in the save file folder.</summary>
        /// <param name="screenshotPath">The name of the screenshot file.</param>
        /// <param name="keypress">true if the user pressed the key</param>
        private void MoveScreenshotToCorrectFolder(string screenshotPath, bool keypress = false)
        {
            // special folder path

            // path for original screenshot location and new screenshot location
            string sourceFile = Path.Combine(defaultStardewValleyScreenshotsDirectory, screenshotPath);
            string destinationFile;
            if(keypress)
            {
                destinationFile = Path.Combine(Config.FolderDestinationForKeypressScreenshots, screenshotPath);
            }
            else
            {
                destinationFile = Path.Combine(Config.FolderDestinationForDailyScreenshots, screenshotPath);
            }
            MTrace($"Snapshot moving from {sourceFile} to {destinationFile}");


            // create save directory if it doesn't already exist
            if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }

            // wait for screenshot to finish
            while (Game1.game1.takingMapScreenshot)
            {
                MTrace("Sleeping while takingMapScreenshot");
                Thread.Sleep(MILLISECONDS_TIMEOUT);
            }
            int attemptCount = 0;
            while (File.Exists(sourceFile) && attemptCount < MAX_ATTEMPTS_TO_MOVE)
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
                        // delete old version of screenshot if one exists
                        if (File.Exists(destinationFile))
                        {
                            File.Delete(destinationFile);
                        }
                        File.Move(sourceFile, destinationFile);
                    }
                }
                catch (IOException ex)
                {
                    int HResult = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    if (SHARING_VIOLATION == (HResult & 0xFFFF))
                    {
                        // Hiding the warning as it isn't useful to other mod developers
                        //MWarn($"File may be in use, retrying in {MILLISECONDS_TIMEOUT} milliseconds, attempt {attemptCount} of {MAX_ATTEMPTS_TO_MOVE}");
                        Thread.Sleep(MILLISECONDS_TIMEOUT);
                    }
                    else
                    {
                        MError($"Error moving file '{screenshotPath}' to {destinationFile}. Technical details:\n{ex}");
                        attemptCount = MAX_ATTEMPTS_TO_MOVE;
                    }
                }
                catch (Exception ex)
                {
                    MError($"Error moving file '{screenshotPath}' to {destinationFile} folder. Technical details:\n{ex}");
                    attemptCount = MAX_ATTEMPTS_TO_MOVE;
                }
            }
        }
#endif
        /// <summary>Raised after the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            countdownInTicks = MAX_COUNTDOWN_IN_TICKS;
        }
    }
}