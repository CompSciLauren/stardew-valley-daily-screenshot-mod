﻿using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using StardewValley.Menus;
using System.Diagnostics;

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
        internal static ModEntry g_dailySS = null;

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
        /// Tracking time event registration
        /// </summary>
        /// <value>True if the time event is registered</value>
        private bool TimeEventRegistered { get; set; } = false;

        /// <summary>
        /// Tracking warp event registration
        /// </summary>
        /// <value>True if the warp event is registered</value>
        private bool WarpEventRegistered { get; set; } = false;

        /// <summary>
        /// Tracking key event registration
        /// </summary>
        /// <value>True if the key event is registered</value>
        private bool KeyEventRegistered { get; set; } = false;

        /// <summary>
        /// Rules waiting on the time event (must be in the correct location)
        /// </summary>
        /// <value>Rules waiting on time events</value>
        private List<ModRule> TimeRules { get; set; } = new List<ModRule>();

        /// <summary>
        /// Rules waiting on the warp event
        /// </summary>
        /// <value>Rules waiting on warp events</value>
        private List<ModRule> WarpRules { get; set; } = new List<ModRule>();

        /// <summary>
        /// Rules waiting on the key event, must be correct time and location
        /// </summary>
        /// <value>Rules waiting on key events</value>
        private List<ModRule> KeyRules { get; set; } = new List<ModRule>();

        /// <summary>
        /// Default screenshot directory set in the entry
        /// </summary>
        /// <value>Path to the screenshot directory for this platform</value>
        public DirectoryInfo DefaultSSdirectory { get; private set; }

        /// <summary>
        /// Are ticks being counted?
        /// </summary>
        /// <value>True if there's a tick event being monitored</value>
        public bool UpdateTickEventActive { get; private set; }

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
            I18n.Init(helper.Translation);

            if (null != g_dailySS)
            {
                string message = "Entry called twice - breaking singelton";
                MError(message);
                throw new Exception(message);
            }
            g_dailySS = this;
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

            int num11 = (Environment.OSVersion.Platform != PlatformID.Unix ? 26 : 28);
            var path = Environment.GetFolderPath((Environment.SpecialFolder)num11);

            // path is combined with StardewValley and then Screenshots
            DefaultSSdirectory = new DirectoryInfo(Path.Combine(path, "StardewValley", "Screenshots"));
            Helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            Helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu menu)
            {
                if (menu.pages[GameMenu.optionsTab] is OptionsPage oPage)
                {
                    oPage.options.Add(new OptionsElement("DailyScreenshot Mod:"));
                    oPage.options.Add(new OptionsButton("Show config.json", delegate
                         {
                             try
                             {
                                 Process.Start(new ProcessStartInfo
                                 {
                                     FileName = Path.Combine("Mods", "DailyScreenshot"),
                                     UseShellExecute = true,
                                     Verb = "open"
                                 });
                             }
                             catch (Exception)
                             {
                             }
                         }));
                    // Show a list of rules and allow the user to enable/disable them here
                    //oPage.options.Add(new OptionsElement("DailyScreenshot Mod Rules:"));
                }
            }
        }


        /// <summary>Enum for action taking with the events</summary>
        private enum EventAction
        {
            /// <summary>Don't change the event listeners</summary>
            None,

            /// <summary>Add a listener to this event</summary>
            Add,

            /// <summary>Remove a listener from this event</summary>
            Remove
        }

        /// <summary>
        /// Move the config rules into lists for warp, time and keypress
        /// and register events as needed
        /// 
        /// Use with caution, locks on this
        /// </summary>
        private void CheckRulesAndUpdateEventReg()
        {
            lock (this)
            {
                WarpRules.Clear();
                TimeRules.Clear();
                KeyRules.Clear();
                foreach (ModRule rule in m_config.SnapshotRules)
                {
                    if (rule.Trigger.IsWaitingOnWarp())
                    {
                        WarpRules.Add(rule);
                    }
                    else if (rule.Trigger.IsWaitingOnTime())
                    {
                        TimeRules.Add(rule);
                    }
                    else if (rule.Trigger.IsWaitingOnKeypress())
                    {
                        KeyRules.Add(rule);
                    }
                }
                EventAction warpAction = ShouldAlterEventReg(WarpEventRegistered, WarpRules.Count);
                EventAction timeAction = ShouldAlterEventReg(TimeEventRegistered, TimeRules.Count);
                EventAction keyAction = ShouldAlterEventReg(KeyEventRegistered, KeyRules.Count);
                MTrace($"Warp = {WarpRules.Count} {warpAction}, Time = {TimeRules.Count} {timeAction}, Key = {KeyRules.Count} {keyAction}");
                // Events cannot be passed, so this code must be duplicated
                if (EventAction.Add == warpAction)
                    Helper.Events.Player.Warped += OnWarped;
                else if (EventAction.Remove == warpAction)
                    Helper.Events.Player.Warped -= OnWarped;
                WarpEventRegistered = 0 < WarpRules.Count;

                if (EventAction.Add == timeAction)
                    Helper.Events.GameLoop.TimeChanged += OnTimeChanged;
                else if (EventAction.Remove == timeAction)
                    Helper.Events.GameLoop.TimeChanged -= OnTimeChanged;
                TimeEventRegistered = 0 < TimeRules.Count;

                if (EventAction.Add == keyAction)
                    Helper.Events.Input.ButtonPressed += OnButtonPressed;
                else if (EventAction.Remove == keyAction)
                    Helper.Events.Input.ButtonPressed -= OnButtonPressed;
                KeyEventRegistered = 0 < KeyRules.Count;
            }
        }

        /// <summary>
        /// Helper function to figure out if the event should be registered unregistered
        /// </summary>
        /// <param name="eventRegistered">Is the event currently registered</param>
        /// <param name="ruleCount">Number of rules are active for this event</param>
        /// <returns>Add if the event should be added, remove if it should be removed</returns>
        private EventAction ShouldAlterEventReg(bool eventRegistered, int ruleCount)
        {
            if (eventRegistered && 0 == ruleCount)
                return EventAction.Remove;
            if (!eventRegistered && 0 < ruleCount)
                return EventAction.Add;
            return EventAction.None;
        }

        /// <summary>
        /// Removes all of the events registed via CheckRulesAndUpdateEventReg
        /// 
        ///  Use with caution, locks on this
        /// </summary>
        private void ClearPictureEventRegistration()
        {
            lock (this)
            {
                if (WarpEventRegistered)
                    Helper.Events.Player.Warped -= OnWarped;
                if (TimeEventRegistered)
                    Helper.Events.GameLoop.TimeChanged -= OnTimeChanged;
                if (KeyEventRegistered)
                    Helper.Events.Input.ButtonPressed -= OnButtonPressed;
                WarpEventRegistered = false;
                TimeEventRegistered = false;
                KeyEventRegistered = false;
            }
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
            List<string> text = new() { FailedToLoadMessage };
            DialogueBox box = new(text);
            Game1.activeClickableMenu = box;
            Game1.dialogueUp = true;
            box.finishTyping();
        }

        /// <summary>Raised after the save file is loaded.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Move this to OnDayStart and only register what is needed
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            // add Generic Mod Config Menu integration
            IModInfo gmcm = this.Helper.ModRegistry.Get("spacechase0.GenericModConfigMenu");
            if (gmcm is null)
            {
                this.Monitor.Log(I18n.GmcmNotFound(), LogLevel.Debug);
                return;
            }
            if (gmcm.Manifest.Version.IsOlderThan("1.8.0"))
            {
                this.Monitor.Log(I18n.GmcmVersionMessage(version: "1.8.0", currentversion: gmcm.Manifest.Version), LogLevel.Info);
                return;
            }

            var gmcmApi = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            if (gmcmApi != null)
            {
                gmcmApi.Register(ModManifest, m_config.Reset, () => Helper.WriteConfig(m_config));

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Effects_Header_Title, I18n.Config_Effects_Header_Tooltip);

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.auditoryEffects,
                    setValue: (bool val) => m_config.auditoryEffects = val,
                    name: I18n.Config_Effects_Auditory_Title,
                    tooltip: I18n.Config_Effects_Auditory_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.visualEffects,
                    setValue: (bool val) => m_config.visualEffects = val,
                    name: I18n.Config_Effects_Visual_Title,
                    tooltip: I18n.Config_Effects_Visual_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.screenshotNotifications,
                    setValue: (bool val) => m_config.screenshotNotifications = val,
                    name: I18n.Config_Effects_Notification_Title,
                    tooltip: I18n.Config_Effects_Notification_Tooltip
                );

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_MainSettings_Header_Title, I18n.Config_MainSettings_Header_Tooltip);

                gmcmApi.AddTextOption(
                    mod: ModManifest,
                    getValue: () => m_config.snapshotRuleName,
                    setValue: (string val) => m_config.snapshotRuleName = val,
                    name: I18n.Config_MainSettings_SnapshotRuleName_Title,
                    tooltip: I18n.Config_MainSettings_SnapshotRuleName_Tooltip
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    getValue: () => m_config.zoomLevel,
                    setValue: (float val) => m_config.zoomLevel = val,
                    name: I18n.Config_MainSettings_ZoomLevel_Title,
                    tooltip: I18n.Config_MainSettings_ZoomLevel_Tooltip,
                    min: 0.01f,
                    max: 1,
                    interval: 0.01f
                );

                gmcmApi.AddTextOption(
                    mod: ModManifest,
                    getValue: () => m_config.snapshotDirectory,
                    setValue: (string val) => m_config.snapshotDirectory = val,
                    name: I18n.Config_MainSettings_SnapshotDirectory_Title,
                    tooltip: I18n.Config_MainSettings_SnapshotDirectory_Tooltip
                );

                gmcmApi.AddTextOption(
                    mod: ModManifest,
                    getValue: () => m_config.snapshotFileName,
                    setValue: (string val) => m_config.snapshotFileName = val,
                    name: I18n.Config_MainSettings_SnapshotFileName_Title,
                    tooltip: I18n.Config_MainSettings_SnapshotFileName_Tooltip
                );

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Disclaimer);

                gmcmApi.AddParagraph(ModManifest, I18n.Config_Disclaimer_Paragraph);

                gmcmApi.AddPageLink(ModManifest, "Weather", () => "Next Page");

                gmcmApi.AddPage(ModManifest, "Weather");

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Weather_Header_Title, I18n.Config_Weather_Header_Tooltip);
                
                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherAny,
                    setValue: (bool val) => m_config.weatherAny = val,
                    name: I18n.Config_Weather_Any_Title,
                    tooltip: I18n.Config_Weather_Any_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherSunny,
                    setValue: (bool val) => m_config.weatherSunny = val,
                    name: I18n.Config_Weather_Sunny_Title,
                    tooltip: I18n.Config_Weather_Sunny_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherRainy,
                    setValue: (bool val) => m_config.weatherRainy = val,
                    name: I18n.Config_Weather_Rainy_Title,
                    tooltip: I18n.Config_Weather_Rainy_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherWindy,
                    setValue: (bool val) => m_config.weatherWindy = val,
                    name: I18n.Config_Weather_Windy_Title,
                    tooltip: I18n.Config_Weather_Windy_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherStormy,
                    setValue: (bool val) => m_config.weatherStormy = val,
                    name: I18n.Config_Weather_Stormy_Title,
                    tooltip: I18n.Config_Weather_Stormy_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.weatherSnowy,
                    setValue: (bool val) => m_config.weatherSnowy = val,
                    name: I18n.Config_Weather_Snowy_Title,
                    tooltip: I18n.Config_Weather_Snowy_Tooltip
                );

                gmcmApi.AddPageLink(ModManifest, "", () => "Previous Page");

                gmcmApi.AddPageLink(ModManifest, "Location", () => "Next Page");

                gmcmApi.AddPage(ModManifest, "Location");

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Location_Header_Title, I18n.Config_Location_Header_Tooltip);

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationAny,
                    setValue: (bool val) => m_config.locationAny = val,
                    name: I18n.Config_Location_Any_Title,
                    tooltip: I18n.Config_Location_Any_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationFarm,
                    setValue: (bool val) => m_config.locationFarm = val,
                    name: I18n.Config_Location_Farm_Title,
                    tooltip: I18n.Config_Location_Farm_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationFarmhouse,
                    setValue: (bool val) => m_config.locationFarmhouse = val,
                    name: I18n.Config_Location_Farmhouse_Title,
                    tooltip: I18n.Config_Location_Farmhouse_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationGreenhouse,
                    setValue: (bool val) => m_config.locationGreenhouse = val,
                    name: I18n.Config_Location_Greenhouse_Title,
                    tooltip: I18n.Config_Location_Greenhouse_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationBeach,
                    setValue: (bool val) => m_config.locationBeach = val,
                    name: I18n.Config_Location_Beach_Title,
                    tooltip: I18n.Config_Location_Beach_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationFarmCave,
                    setValue: (bool val) => m_config.locationFarmCave = val,
                    name: I18n.Config_Location_FarmCave_Title,
                    tooltip: I18n.Config_Location_FarmCave_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationCellar,
                    setValue: (bool val) => m_config.locationCellar = val,
                    name: I18n.Config_Location_Cellar_Title,
                    tooltip: I18n.Config_Location_Cellar_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationDesert,
                    setValue: (bool val) => m_config.locationDesert = val,
                    name: I18n.Config_Location_Desert_Title,
                    tooltip: I18n.Config_Location_Desert_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationMuseum,
                    setValue: (bool val) => m_config.locationMuseum = val,
                    name: I18n.Config_Location_Museum_Title,
                    tooltip: I18n.Config_Location_Museum_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationCommunityCenter,
                    setValue: (bool val) => m_config.locationCommunityCenter = val,
                    name: I18n.Config_Location_CommunityCenter_Title,
                    tooltip: I18n.Config_Location_CommunityCenter_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationMountain,
                    setValue: (bool val) => m_config.locationMountain = val,
                    name: I18n.Config_Location_Mountain_Title,
                    tooltip: I18n.Config_Location_Mountain_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationIslandWest,
                    setValue: (bool val) => m_config.locationIslandWest = val,
                    name: I18n.Config_Location_IslandWest_Title,
                    tooltip: I18n.Config_Location_IslandWest_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationIslandFarmhouse,
                    setValue: (bool val) => m_config.locationIslandFarmhouse = val,
                    name: I18n.Config_Location_IslandFarmhouse_Title,
                    tooltip: I18n.Config_Location_IslandFarmhouse_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationIslandFieldOffice,
                    setValue: (bool val) => m_config.locationIslandFieldOffice = val,
                    name: I18n.Config_Location_IslandFieldOffice_Title,
                    tooltip: I18n.Config_Location_IslandFieldOffice_Tooltip
                );

                gmcmApi.AddBoolOption(
                    mod: ModManifest,
                    getValue: () => m_config.locationUnknown,
                    setValue: (bool val) => m_config.locationUnknown = val,
                    name: I18n.Config_Location_Unknown_Title,
                    tooltip: I18n.Config_Location_Unknown_Tooltip
                );

                gmcmApi.AddPageLink(ModManifest, "Weather", () => "Previous Page");

                gmcmApi.AddPageLink(ModManifest, "Time", () => "Next Page");

                gmcmApi.AddPage(ModManifest, "Time");

                gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Time_Header_Title, I18n.Config_Time_Header_Tooltip);

                gmcmApi.AddKeybind(
                    ModManifest,
                    getValue: () => m_config.shortcutKey,
                    setValue: (SButton val) => m_config.shortcutKey = val,
                    name: I18n.Config_Time_ShortcutKey_Title,
                    tooltip: I18n.Config_Time_ShortcutKey_Tooltip
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    getValue: () => m_config.startTime,
                    setValue: (int val) => m_config.startTime = val,
                    name: I18n.Config_Time_StartTime_Title,
                    tooltip: I18n.Config_Time_StartTime_Tooltip,
                    min: 600,
                    max: 2590,
                    interval: 10
                );

                gmcmApi.AddNumberOption(
                    mod: ModManifest,
                    getValue: () => m_config.endTime,
                    setValue: (int val) => m_config.endTime = val,
                    name: I18n.Config_Time_EndTime_Title,
                    tooltip: I18n.Config_Time_EndTime_Tooltip,
                    min: 610,
                    max: 2600,
                    interval: 10
                );
                
                gmcmApi.AddPageLink(ModManifest, "Location", () => "Previous Page");

                MInfo("Added \"DailyScreenshot\" config menu with \"Generic Mod Config Menu\".");
            }
        }

        /// <summary>
        /// Event to process on a time change
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event data</param>
        private void OnTimeChanged(object sender, TimeChangedEventArgs e)
        {
            RunTriggers(TimeRules);
        }

        /// <summary>Raised after a button is pressed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button.TryGetKeyboard(out Keys _))
            {
                RunTriggers(KeyRules, e.Button);
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
            RunTriggers(m_config.SnapshotRules);
        }

        /// <summary>
        /// Check the rule triggers and take a screenshot if appropriate
        /// </summary>
        /// <param name="key"></param>
        private void RunTriggers(List<ModRule> rules, SButton key = SButton.None)
        {
            if (!m_shouldProcessRules)
                return;
            foreach (ModRule rule in rules)
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
            CheckRulesAndUpdateEventReg();
        }

        /// <summary>
        /// Raised after the player enters a new location.
        /// 
        /// Use with caution, locks on this
        /// </summary>
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
            RunTriggers(WarpRules);
        }

        /// <summary>
        /// Raised after game state is updated.
        /// 
        /// Use with caution, locks on this
        /// </summary>
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
                    UpdateTickEventActive = false;
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

            if (m_config.visualEffects)
            {
                Game1.flashAlpha = 1f;
            }

            if (null != ssPath)
            {
                MTrace($"ssPath = \"{ssPath}\"");
                string ssDirectory = Path.GetDirectoryName(ssPath);

                Directory.CreateDirectory(Path.Combine(DefaultSSdirectory.FullName, ssDirectory));
            }
            string mapScreenshotPath = Game1.game1.takeMapScreenshot(rule.ZoomLevel, ssPath, () => {
                    //Nothing here. Just added Action as empty lambda to provide all now required parameters.
                }
            );
            FileInfo mapScreenshot = new FileInfo(Path.Combine(DefaultSSdirectory.FullName, mapScreenshotPath));
            MTrace($"Snapshot saved to {mapScreenshot.FullName}");

            if (m_config.auditoryEffects)
            {
                Game1.playSound("cameraNoise");
            }

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
        // Adding space based on user feedback
        private void DisplayRuleHUD(ModRule rule)
        {
            if (m_config.screenshotNotifications)
            {
                Game1.addHUDMessage(
                    new HUDMessage(" " + rule.Name, HUDMessage.screenshot_type)
                );
            }
        }

        /// <summary>
        /// Recursively cleanup empty directories
        /// </summary>
        /// <param name="directory">The directory to remove</param>
        private void CleanUpEmptyDirectories(DirectoryInfo directory)
        {
            if (DirectoryIsEmpty(directory) &&
                directory.FullName != DefaultSSdirectory.FullName)
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

        /// <summary>
        /// Allows ability to enqueue actions to the queue.
        /// 
        /// Use with caution, locks on this
        /// </summary>
        /// <param name="action">The action.</param>
        public void EnqueueAction(Action action, ref Queue<Action> actionQueue)
        {
            if (null == action) return;

            lock (this)
            {
                actionQueue.Enqueue(action);
                if (!UpdateTickEventActive)
                {
                    m_ssCntDwnTicks = MAX_COUNTDOWN_IN_TICKS;
                    Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                }
                UpdateTickEventActive = true;
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
#if DEBUG
                MTrace("Sleeping while takingMapScreenshot");
#endif
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
#if DEBUG
                        MWarn($"File may be in use, retrying in {MILLISECONDS_TIMEOUT} milliseconds, attempt {attemptCount} of {MAX_ATTEMPTS_TO_MOVE}");
#endif
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
            ClearPictureEventRegistration();

            // if there are pending screenshots, cancel them
            if (m_ssActions.Count > 0)
                m_ssActions.Clear();

            m_ssCntDwnTicks = 0;
        }
    }
}