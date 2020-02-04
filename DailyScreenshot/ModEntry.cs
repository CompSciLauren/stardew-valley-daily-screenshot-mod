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
    /// <summary>
    /// The mod entry point.
    /// </summary>
    public class ModEntry : Mod
    {
        /// <summary>
        /// Static global so ModConfig can log to the console
        /// </summary>
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
        private const int MAX_COUNTDOWN_IN_TICKS = 35;

        /// <summary>
        /// Time to sleep between move attempts
        /// </summary>
        private const int MILLISECONDS_TIMEOUT = 10;

        /// <summary>
        /// Message to show when the config file fails to load
        /// </summary>
        private const string FailedToLoadMessage = "Error: Failed to load the configuration file for DailyScreenshot. Pictures will not be taken. Check the console for more details.";

        #endregion

        /// <summary>
        /// The mod configuration from the player.
        /// </summary>
        private ModConfig m_config;

        /// <summary>
        /// Screenshot countdown ticks (make sure the world is rendered)
        /// </summary>
        int m_ssCntDwnTicks = 0;

        /// <summary>
        /// File move countdown ticks (let the screenshot finish and game process a little)
        /// </summary>
        int m_mvCntDwnTicks = 0;

        /// <summary>
        /// Way to disable rule processing
        /// </summary>
        bool m_shouldProcessRules = false;

        /// <summary>
        /// Default screenshot directory set in the entry
        /// </summary>
        /// <value>Path to the screenshot directory for this platform</value>
        public DirectoryInfo m_defaultSSdirectory { get; private set; }

        /// <summary>
        /// Are ticks being counted?
        /// </summary>
        /// <value>True if there's a tick event being monitored</value>
        public bool m_updateTickEventActive { get; private set; }

        /// <summary>
        /// Check that a directory contains no files or directories
        /// </summary>
        /// <param name="path">Directory to check</param>
        /// <returns>true if the directory is empty</returns>
        private bool DirectoryIsEmpty(DirectoryInfo directory) =>
            directory.GetDirectories().Length == 0 && directory.GetFiles().Length == 0;

        #region Logging
        // Private copies of these functions so there's one
        // place to alter all log messages if needed

        /// <summary>
        /// Sends messages to the SMAPI console
        /// </summary>
        /// <param name="message">text to send</param>
        /// <param name="level">type of message</param>
#if DEBUG
        internal void LogMessageToConsole(string message, LogLevel level) =>
            Monitor.Log(message, level);
#else
        internal void LogMessageToConsole(string message, LogLevel level) =>
            Monitor.VerboseLog(level.ToString() + ": " + message);
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
        internal void MWarn(string message) => Monitor.Log(message, LogLevel.Warn);

        /// <summary>
        /// Helper function for sending error messages
        /// Always display even if verbose logging is off
        /// </summary>
        /// <param name="message">text to send</param>
        internal void MError(string message) => Monitor.Log(message, LogLevel.Error);
        #endregion

        /// <summary>
        /// The mod entry point, called after the mod is first loaded.
        /// </summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            if (DailySS != null)
            {
                string message = "Entry called twice - breaking singelton";
                MError(message);
                throw new Exception(message);
            }
            DailySS = this;
            try
            {
                m_config = Helper.ReadConfig<ModConfig>();
                m_config.ValidateUserInput();
                // Fixed something up, write new rules
                if (m_config.RulesModified)
                    Helper.WriteConfig<ModConfig>(m_config);
                m_config.SortRules();
            }
            catch (Exception ex)
            {
                MError($"Failed to load config file.\nTechnical Details: {ex}");
                Helper.Events.GameLoop.OneSecondUpdateTicked += LoadingErrorOnTick;
            }

            int num11 = Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28;
            var path = Environment.GetFolderPath((Environment.SpecialFolder)num11);

            // path is combined with StardewValley and then Screenshots
            m_defaultSSdirectory = new DirectoryInfo(Path.Combine(path, "StardewValley", "Screenshots"));
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        /// <summary>
        /// Event for showing a loading error (based on StardewHack)
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event details</param>
        private void LoadingErrorOnTick(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (e.Ticks < 60) return;

            // And only fire once.
            Helper.Events.GameLoop.OneSecondUpdateTicked -= LoadingErrorOnTick;
            ReportLoadingError();
        }

        /// <summary>
        /// Shows dialog indicating a config file loading error
        /// </summary>
        private void ReportLoadingError()
        {
            List<string> text = new List<string>() { FailedToLoadMessage };
            StardewValley.Menus.DialogueBox box = new StardewValley.Menus.DialogueBox(text);
            StardewValley.Game1.activeClickableMenu = box;
            StardewValley.Game1.dialogueUp = true;
            box.finishTyping();
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

        /// <summary>
        /// Event to process on a time change
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event data</param>
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
                RunTriggers(e.Button);
            }
        }

        /// <summary>Raised after day has started.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            m_shouldProcessRules = true;
            foreach (ModRule rule in m_config.SnapshotRules)
            {
                rule.Trigger.ResetTrigger();
            }
            RunTriggers();
        }

        /// <summary>
        /// Check the rule triggers and take a screenshot if appropriate
        /// </summary>
        /// <param name="key"></param>
        private void RunTriggers(SButton key = SButton.None)
        {
            if (!m_shouldProcessRules)
                return;
            foreach (ModRule rule in m_config.SnapshotRules)
            {
                if (rule.Enabled && rule.Trigger.CheckTrigger(key))
                {
                    DisplayRuleHUD(rule);
                    EnqueueAction(() =>
                        {
                            TakeScreenshot(rule);
                        }, ref m_ssActions
                    );
                }
            }
        }

        /// <summary>Raised after the player enters a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // if we enqueued a screen shot and warped before
            // the timeout, reset the timeout
            lock (this)
            {
                if (m_ssActions.Count > 0)
                    m_ssCntDwnTicks = MAX_COUNTDOWN_IN_TICKS;
            }
            RunTriggers();
        }

        /// <summary>Raised after game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (m_ssCntDwnTicks > 0)
                m_ssCntDwnTicks--;

            if (m_ssCntDwnTicks == 0)
            {
                if (m_mvCntDwnTicks > 0)
                    m_mvCntDwnTicks--;
                while (m_ssActions.Count > 0)
                {
                    m_ssActions.Dequeue().Invoke();
                    // Ensure unique IDs
                    Thread.Sleep(1);
                    if (m_mvCntDwnTicks == 0 && m_mvActions.Count > 0)
                        m_mvCntDwnTicks = MAX_COUNTDOWN_IN_TICKS;
                }
                if (m_mvCntDwnTicks == 0)
                {
                    while (m_mvActions.Count > 0)
                        m_mvActions.Dequeue().Invoke();

                }
            }
            lock (this)
            {
                if (m_mvActions.Count == 0 &&
                    m_ssActions.Count == 0 &&
                    m_mvCntDwnTicks == 0 &&
                    m_ssCntDwnTicks == 0)
                {
                    m_updateTickEventActive = false;
                    Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                    return;
                }
            }
        }

        /// <summary>
        /// Rule said it was time to take a screenshot,
        /// HUD message has been added and we waited for
        /// our timeout in ticks, so take a screenshot
        /// </summary>
        /// <param name="rule">Rule to follow for this screenshot</param>
        private void TakeScreenshot(ModRule rule)
        {
            string ssPath = rule.GetFileName();
            if (null != ssPath)
            {
                MTrace($"ssPath = \"{ssPath}\"");
                string ssDirectory = Path.GetDirectoryName(ssPath);

                Directory.CreateDirectory(Path.Combine(m_defaultSSdirectory.FullName, ssDirectory));
            }
            string mapScreenshotPath = Game1.game1.takeMapScreenshot(rule.ZoomLevel, ssPath);
            FileInfo mapScreenshot = new FileInfo(Path.Combine(m_defaultSSdirectory.FullName, mapScreenshotPath));
            MTrace($"Snapshot saved to {mapScreenshot.FullName}");
            Game1.playSound("cameraNoise");
            if (ModConfig.DEFAULT_STRING != rule.Directory)
            {
                EnqueueAction(() =>
                    {
                        MoveScreenshotToCorrectFolder(mapScreenshot, new FileInfo(Path.Combine(rule.Directory, mapScreenshotPath)));
                        CleanUpEmptyDirectories(mapScreenshot.Directory);
                    }, ref m_mvActions
                    );
            }
        }

        /// <summary>
        /// Display the HUD message
        /// </summary>
        /// <param name="rule">Rule to use for HUD message</param>
        private void DisplayRuleHUD(ModRule rule) =>
            Game1.addHUDMessage(new HUDMessage(rule.Name, HUDMessage.screenshot_type));

        /// <summary>
        /// Recursively cleanup empty directories
        /// </summary>
        /// <param name="directory">The directory to remove</param>
        private void CleanUpEmptyDirectories(DirectoryInfo directory)
        {
            if (DirectoryIsEmpty(directory) &&
                directory.FullName != m_defaultSSdirectory.FullName)
            {
                directory.Delete();
                CleanUpEmptyDirectories(directory.Parent);
            }
        }

        /// <summary>
        /// Queue of screen shot actions to take when the timeout expires
        /// </summary>
        private Queue<Action> m_ssActions = new Queue<Action>();

        /// <summary>
        /// Queue of move actions to take when the timeout expires
        /// </summary>
        private Queue<Action> m_mvActions = new Queue<Action>();

        /// <summary>Allows ability to enqueue actions to the queue.</summary>
        /// <param name="action">The action.</param>
        public void EnqueueAction(Action action, ref Queue<Action> actionQueue)
        {
            if (null == action) return;

            lock (this)
            {
                actionQueue.Enqueue(action);
                if (!m_updateTickEventActive)
                {
                    m_ssCntDwnTicks = MAX_COUNTDOWN_IN_TICKS;
                    Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                }
                m_updateTickEventActive = true;
            }
        }

        /// <summary>Moves screenshot into StardewValley/Screenshots directory, in the save file folder.</summary>
        /// <param name="sourceFile">File to move</param>
        /// <param name="destinationFile">Where to move the file</param>
        private void MoveScreenshotToCorrectFolder(FileInfo sourceFile, FileInfo destinationFile)
        {
            // path for original screenshot location and new screenshot location
            string sourceFilePath = sourceFile.FullName;
            MTrace($"Snapshot moving from {sourceFile} to {destinationFile}");


            // create save directory if it doesn't already exist
            if (!Directory.Exists(destinationFile.DirectoryName))
                Directory.CreateDirectory(destinationFile.DirectoryName);

            // wait for screenshot to finish
            while (Game1.game1.takingMapScreenshot)
            {
                MTrace("Sleeping while takingMapScreenshot");
                Thread.Sleep(MILLISECONDS_TIMEOUT);
            }
            int attemptCount = 0;
            while (File.Exists(sourceFilePath) && attemptCount < MAX_ATTEMPTS_TO_MOVE)
            {
                try
                {
                    attemptCount++;
                    using (FileStream lockFile = new FileStream(
                        sourceFile.FullName,
                        FileMode.Open,
                        FileAccess.ReadWrite,
                        FileShare.Read | FileShare.Delete
                    ))
                    {
                        // delete old version of screenshot if one exists
                        if (destinationFile.Exists)
                            destinationFile.Delete();
                        sourceFile.MoveTo(destinationFile.FullName);
                    }
                }
                catch (IOException ex)
                {
                    int HResult = System.Runtime.InteropServices.Marshal.GetHRForException(ex);
                    if (SHARING_VIOLATION == (HResult & 0xFFFF))
                    {
                        // Hiding the warning as it isn't useful to other mod developers
                        MWarn($"File may be in use, retrying in {MILLISECONDS_TIMEOUT} milliseconds, attempt {attemptCount} of {MAX_ATTEMPTS_TO_MOVE}");
                        Thread.Sleep(MILLISECONDS_TIMEOUT);
                    }
                    else
                    {
                        MError($"Error moving file '{sourceFile.FullName}' to {destinationFile.FullName}. Technical details:\n{ex}");
                        attemptCount = MAX_ATTEMPTS_TO_MOVE;
                    }
                }
                catch (Exception ex)
                {
                    MError($"Error moving file '{sourceFile.FullName}' to {destinationFile.FullName} folder. Technical details:\n{ex}");
                    attemptCount = MAX_ATTEMPTS_TO_MOVE;
                }
            }
        }

        /// <summary>Raised after the player returns to the title screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            m_shouldProcessRules = false;

            // if there are pending screenshots, cancel them
            if (m_ssActions.Count > 0)
                m_ssActions.Clear();

            m_ssCntDwnTicks = 0;
        }
    }
}