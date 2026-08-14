using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using StardewValley.Menus;
using System.Diagnostics;
using static DailyScreenshot.ModTrigger;
using StardewModdingAPI.Utilities;

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
        /// The Generic Mod Config Menu API, kept so the config menu can be
        /// rebuilt after a snapshot rule is added or removed.
        /// </summary>
        private GenericModConfigMenuAPI m_gmcmApi;

        /// <summary>
        /// Changes to <see cref="ModConfig.SnapshotRules"/> requested via the
        /// Add/Remove Rule config buttons, queued up to run after the current
        /// tick instead of immediately. GMCM's Save button flushes every
        /// registered option's setValue callback in one pass, so mutating the
        /// rule list from inside one of those callbacks would shift the
        /// indices other, not-yet-run callbacks in that same pass still expect.
        /// </summary>
        private readonly List<Action> m_pendingConfigMenuChanges = new();

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

            m_gmcmApi = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            if (m_gmcmApi != null)
            {
                if (m_config.SnapshotRules.Count == 0)
                {
                    m_config.Reset();
                }

                BuildConfigMenu();

                MInfo("Added \"DailyScreenshot\" config menu with \"Generic Mod Config Menu\".");
            }
        }

        /// <summary>
        /// Builds the "DailyScreenshot" config menu using the Generic Mod Config
        /// Menu API stored in <see cref="m_gmcmApi"/>. Adds a page for every rule
        /// currently in <c>m_config.SnapshotRules</c>, plus a way to add new ones.
        ///
        /// Called once when the game launches, and again (via <see cref="RebuildConfigMenu"/>)
        /// whenever a rule is added or removed, since the set of pages needs to change.
        /// </summary>
        private void BuildConfigMenu()
        {
            GenericModConfigMenuAPI gmcmApi = m_gmcmApi;

            gmcmApi.Register(ModManifest, m_config.Reset, () => Helper.WriteConfig(m_config));

            gmcmApi.AddSectionTitle(ModManifest, I18n.Config_About_Header_Title);

            gmcmApi.AddParagraph(ModManifest, I18n.Config_About_Description1);

            gmcmApi.AddParagraph(ModManifest, I18n.Config_About_Description2);

            gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Effects_Header_Title, I18n.Config_Effects_Header_Tooltip);

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                getValue: () => m_config.ScreenshotsEnabled,
                setValue: (bool val) => m_config.ScreenshotsEnabled = val,
                name: I18n.Config_Effects_ScreenshotsEnabled_Title,
                tooltip: I18n.Config_Effects_ScreenshotsEnabled_Tooltip
            );

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                getValue: () => m_config.AuditoryEffects,
                setValue: (bool val) => m_config.AuditoryEffects = val,
                name: I18n.Config_Effects_Auditory_Title,
                tooltip: I18n.Config_Effects_Auditory_Tooltip
            );

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                getValue: () => m_config.VisualEffects,
                setValue: (bool val) => m_config.VisualEffects = val,
                name: I18n.Config_Effects_Visual_Title,
                tooltip: I18n.Config_Effects_Visual_Tooltip
            );

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                getValue: () => m_config.ScreenshotNotifications,
                setValue: (bool val) => m_config.ScreenshotNotifications = val,
                name: I18n.Config_Effects_Notification_Title,
                tooltip: I18n.Config_Effects_Notification_Tooltip
            );

            gmcmApi.AddSectionTitle(ModManifest, I18n.Config_Rules_Header_Title, I18n.Config_Rules_Header_Tooltip);

            gmcmApi.AddParagraph(ModManifest, I18n.Config_Rules_Header_Description);

            for (int i = 0; i < m_config.SnapshotRules.Count; i++)
            {
                ModRule rule = m_config.SnapshotRules[i];
                gmcmApi.AddPageLink(ModManifest, RulePageId(i), () => rule.Name);
            }

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                getValue: () => false,
                setValue: (bool val) =>
                {
                    if (val)
                    {
                        ScheduleConfigMenuChange(() =>
                        {
                            ModRule newRule = ModConfig.CreateDefaultSnapshotRule();
                            newRule.Name = $"Unnamed Rule {m_config.SnapshotRules.Count + 1}";
                            m_config.SnapshotRules.Add(newRule);
                        });
                    }
                },
                name: I18n.Config_Rules_AddRule_Title,
                tooltip: I18n.Config_Rules_AddRule_Tooltip
            );

            for (int i = 0; i < m_config.SnapshotRules.Count; i++)
            {
                AddRulePages(gmcmApi, i);
            }
        }

        /// <summary>
        /// Unregisters and rebuilds the config menu. Needed after a snapshot
        /// rule is added or removed, since GMCM's registered pages/options
        /// can't be changed in place, only replaced wholesale.
        /// </summary>
        private void RebuildConfigMenu()
        {
            m_gmcmApi.Unregister(ModManifest);
            BuildConfigMenu();
        }

        /// <summary>
        /// Queues a change to <see cref="ModConfig.SnapshotRules"/> (adding or
        /// removing a rule) to run on the next tick, then rebuilds the config
        /// menu. See <see cref="m_pendingConfigMenuChanges"/> for why this
        /// can't happen immediately.
        /// </summary>
        /// <param name="change">The change to make to <c>m_config.SnapshotRules</c>.</param>
        private void ScheduleConfigMenuChange(Action change)
        {
            m_pendingConfigMenuChanges.Add(change);
            if (m_pendingConfigMenuChanges.Count == 1)
                Helper.Events.GameLoop.UpdateTicked += ApplyPendingConfigMenuChanges;
        }

        /// <summary>Applies queued rule list changes and rebuilds the config menu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ApplyPendingConfigMenuChanges(object sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= ApplyPendingConfigMenuChanges;

            foreach (Action change in m_pendingConfigMenuChanges)
                change();
            m_pendingConfigMenuChanges.Clear();

            RebuildConfigMenu();

            // GMCM already wrote m_config to disk during Save, before the change above
            // was applied, so that write missed it. Save again so it isn't lost.
            Helper.WriteConfig(m_config);

            // If our config UI is still open (the player clicked Save, not Save & Close),
            // replace it with a fresh menu bound to the rebuilt rule list so the
            // added/removed rule shows up immediately instead of only after reopening.
            if (m_gmcmApi.TryGetCurrentMenu(out IManifest currentMenuMod, out _) && currentMenuMod?.UniqueID == ModManifest.UniqueID)
            {
                m_gmcmApi.OpenModMenu(ModManifest);
            }
        }

        /// <summary>GMCM page ID for a rule's main settings page.</summary>
        private static string RulePageId(int ruleIndex) => $"Rule_{ruleIndex}";

        /// <summary>GMCM page ID for a rule's FileName conditions page.</summary>
        private static string FileNamePageId(int ruleIndex) => $"FileName_{ruleIndex}";

        /// <summary>GMCM page ID for a rule's Days (Seasons and Weekdays) conditions page.</summary>
        private static string Days1PageId(int ruleIndex) => $"Days1_{ruleIndex}";

        /// <summary>GMCM page ID for a rule's Days (Days of the Month) conditions page.</summary>
        private static string Days2PageId(int ruleIndex) => $"Days2_{ruleIndex}";

        /// <summary>GMCM page ID for a rule's Weather conditions page.</summary>
        private static string WeatherPageId(int ruleIndex) => $"Weather_{ruleIndex}";

        /// <summary>GMCM page ID for a rule's Location conditions page.</summary>
        private static string LocationPageId(int ruleIndex) => $"Location_{ruleIndex}";

        /// <summary>
        /// Adds the settings page for a single snapshot rule, along with its
        /// FileName/Days/Weather/Location condition subpages.
        /// </summary>
        /// <param name="api">The GenericModConfigMenu API</param>
        /// <param name="ruleIndex">Index of the rule in m_config.SnapshotRules</param>
        private void AddRulePages(GenericModConfigMenuAPI api, int ruleIndex)
        {
            // Every closure below binds to this specific ModRule object rather than
            // re-deriving it from m_config.SnapshotRules[ruleIndex] on each call. An
            // already-open menu's widgets outlive RebuildConfigMenu(), so an index
            // lookup here would silently start reading/writing a different rule once
            // another rule is added or removed and the list shifts.
            ModRule rule = m_config.SnapshotRules[ruleIndex];

            api.AddPage(ModManifest, RulePageId(ruleIndex), () => rule.Name);

            api.AddSectionTitle(ModManifest, I18n.Config_MainSettings_Header_Title, I18n.Config_MainSettings_Header_Tooltip);

            api.AddTextOption(
                mod: ModManifest,
                getValue: () => rule.Name,
                setValue: (string val) => rule.Name = val,
                name: I18n.Config_MainSettings_SnapshotRuleName_Title,
                tooltip: I18n.Config_MainSettings_SnapshotRuleName_Tooltip
            );

            api.AddNumberOption(
                mod: ModManifest,
                getValue: () => rule.ZoomLevel,
                setValue: (float val) => rule.ZoomLevel = val,
                name: I18n.Config_MainSettings_ZoomLevel_Title,
                tooltip: I18n.Config_MainSettings_ZoomLevel_Tooltip,
                min: 0.01f,
                max: 1,
                interval: 0.01f
            );

            api.AddTextOption(
                mod: ModManifest,
                getValue: () => rule.Directory,
                setValue: (string val) => rule.Directory = val,
                name: I18n.Config_MainSettings_SnapshotDirectory_Title,
                tooltip: I18n.Config_MainSettings_SnapshotDirectory_Tooltip
            );

            api.AddKeybind(
                ModManifest,
                getValue: () => rule.Trigger.Key,
                setValue: (SButton val) => rule.Trigger.Key = val,
                name: I18n.Config_MainSettings_ShortcutKey_Title,
                tooltip: I18n.Config_MainSettings_ShortcutKey_Tooltip
            );

            api.AddNumberOption(
                mod: ModManifest,
                getValue: () => rule.Trigger.StartTime,
                setValue: (int val) => rule.Trigger.StartTime = val,
                name: I18n.Config_MainSettings_StartTime_Title,
                tooltip: I18n.Config_MainSettings_StartTime_Tooltip,
                min: 600,
                max: 2590,
                interval: 10
            );

            api.AddNumberOption(
                mod: ModManifest,
                getValue: () => rule.Trigger.EndTime,
                setValue: (int val) => rule.Trigger.EndTime = val,
                name: I18n.Config_MainSettings_EndTime_Title,
                tooltip: I18n.Config_MainSettings_EndTime_Tooltip,
                min: 610,
                max: 2600,
                interval: 10
            );

            api.AddPageLink(ModManifest, FileNamePageId(ruleIndex), I18n.Config_FileName_Header1_Title);

            api.AddPageLink(ModManifest, Days1PageId(ruleIndex), I18n.Config_Days_Header1_Title);

            api.AddPageLink(ModManifest, Days2PageId(ruleIndex), I18n.Config_Days_Header2_Title);

            api.AddPageLink(ModManifest, WeatherPageId(ruleIndex), I18n.Config_Weather_Header_Title);

            api.AddPageLink(ModManifest, LocationPageId(ruleIndex), I18n.Config_Location_Header_Title);

            api.AddBoolOption(
                mod: ModManifest,
                getValue: () => false,
                setValue: (bool val) =>
                {
                    if (val)
                    {
                        ScheduleConfigMenuChange(() =>
                        {
                            if (m_config.SnapshotRules.Count > 1)
                                m_config.SnapshotRules.Remove(rule);
                        });
                    }
                },
                name: I18n.Config_Rules_RemoveRule_Title,
                tooltip: () => m_config.SnapshotRules.Count > 1
                    ? I18n.Config_Rules_RemoveRule_Tooltip()
                    : I18n.Config_Rules_RemoveRuleDisabled_Tooltip()
            );

            api.AddPage(ModManifest, FileNamePageId(ruleIndex));

            api.AddSectionTitle(ModManifest, I18n.Config_FileNameParts_Header1_Title, I18n.Config_FileNameParts_Header1_Tooltip);

            AddNameConditionOption(api, rule, ModRule.FileNameFlags.Date);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.FarmName);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.GameID);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.Location);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.Weather);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.PlayerName);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.Time);
            AddNameConditionOption(api, rule, ModRule.FileNameFlags.UniqueID);

            api.AddPage(ModManifest, Days1PageId(ruleIndex));

            api.AddSectionTitle(ModManifest, I18n.Config_Days_Header1_Title, I18n.Config_Days_Header1_Tooltip);

            api.AddParagraph(ModManifest, I18n.Config_Days_Header1_Description);

            AddDateConditionOption(api, rule, DateFlags.Spring);
            AddDateConditionOption(api, rule, DateFlags.Summer);
            AddDateConditionOption(api, rule, DateFlags.Fall);
            AddDateConditionOption(api, rule, DateFlags.Winter);
            AddDateConditionOption(api, rule, DateFlags.Mondays);
            AddDateConditionOption(api, rule, DateFlags.Tuesdays);
            AddDateConditionOption(api, rule, DateFlags.Wednesdays);
            AddDateConditionOption(api, rule, DateFlags.Thursdays);
            AddDateConditionOption(api, rule, DateFlags.Fridays);
            AddDateConditionOption(api, rule, DateFlags.Saturdays);
            AddDateConditionOption(api, rule, DateFlags.Sundays);

            api.AddPage(ModManifest, Days2PageId(ruleIndex));

            api.AddSectionTitle(ModManifest, I18n.Config_Days_Header2_Title, I18n.Config_Days_Header2_Tooltip);

            AddDateConditionOption(api, rule, DateFlags.Day_01);
            AddDateConditionOption(api, rule, DateFlags.Day_02);
            AddDateConditionOption(api, rule, DateFlags.Day_03);
            AddDateConditionOption(api, rule, DateFlags.Day_04);
            AddDateConditionOption(api, rule, DateFlags.Day_05);
            AddDateConditionOption(api, rule, DateFlags.Day_06);
            AddDateConditionOption(api, rule, DateFlags.Day_07);
            AddDateConditionOption(api, rule, DateFlags.Day_08);
            AddDateConditionOption(api, rule, DateFlags.Day_09);
            AddDateConditionOption(api, rule, DateFlags.Day_10);
            AddDateConditionOption(api, rule, DateFlags.Day_11);
            AddDateConditionOption(api, rule, DateFlags.Day_12);
            AddDateConditionOption(api, rule, DateFlags.Day_13);
            AddDateConditionOption(api, rule, DateFlags.Day_14);
            AddDateConditionOption(api, rule, DateFlags.Day_15);
            AddDateConditionOption(api, rule, DateFlags.Day_16);
            AddDateConditionOption(api, rule, DateFlags.Day_17);
            AddDateConditionOption(api, rule, DateFlags.Day_18);
            AddDateConditionOption(api, rule, DateFlags.Day_19);
            AddDateConditionOption(api, rule, DateFlags.Day_20);
            AddDateConditionOption(api, rule, DateFlags.Day_21);
            AddDateConditionOption(api, rule, DateFlags.Day_22);
            AddDateConditionOption(api, rule, DateFlags.Day_23);
            AddDateConditionOption(api, rule, DateFlags.Day_24);
            AddDateConditionOption(api, rule, DateFlags.Day_25);
            AddDateConditionOption(api, rule, DateFlags.Day_26);
            AddDateConditionOption(api, rule, DateFlags.Day_27);
            AddDateConditionOption(api, rule, DateFlags.Day_28);

            api.AddPage(ModManifest, WeatherPageId(ruleIndex));

            api.AddSectionTitle(ModManifest, I18n.Config_Weather_Header_Title, I18n.Config_Weather_Header_Tooltip);

            AddWeatherConditionOption(api, rule, WeatherFlags.Sunny);
            AddWeatherConditionOption(api, rule, WeatherFlags.Rainy);
            AddWeatherConditionOption(api, rule, WeatherFlags.Windy);
            AddWeatherConditionOption(api, rule, WeatherFlags.Stormy);
            AddWeatherConditionOption(api, rule, WeatherFlags.Snowy);

            api.AddPage(ModManifest, LocationPageId(ruleIndex));

            api.AddSectionTitle(ModManifest, I18n.Config_Location_Header_Title, I18n.Config_Location_Header_Tooltip);
            api.AddParagraph(ModManifest, I18n.Config_Location_Description);
            api.AddParagraph(ModManifest, I18n.Config_Location_Description2);

            AddLocationConditionOption(api, rule, LocationFlags.Farm);
            AddLocationConditionOption(api, rule, LocationFlags.Farmhouse);
            AddLocationConditionOption(api, rule, LocationFlags.GreenHouse);
            AddLocationConditionOption(api, rule, LocationFlags.FarmCave);
            AddLocationConditionOption(api, rule, LocationFlags.Cellar);
            AddLocationConditionOption(api, rule, LocationFlags.Beach);
            AddLocationConditionOption(api, rule, LocationFlags.Desert);
            AddLocationConditionOption(api, rule, LocationFlags.Museum);
            AddLocationConditionOption(api, rule, LocationFlags.CommunityCenter);
            AddLocationConditionOption(api, rule, LocationFlags.Town);
            AddLocationConditionOption(api, rule, LocationFlags.Mountain);
            AddLocationConditionOption(api, rule, LocationFlags.Mine);
            AddLocationConditionOption(api, rule, LocationFlags.MineShaft);
            AddLocationConditionOption(api, rule, LocationFlags.IslandWest);
            AddLocationConditionOption(api, rule, LocationFlags.IslandFarmhouse);
            AddLocationConditionOption(api, rule, LocationFlags.IslandFieldOffice);
            AddLocationConditionOption(api, rule, LocationFlags.Unknown);
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
            if (!m_shouldProcessRules || !m_config.ScreenshotsEnabled) {
                return;
            }
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
            // if we enqueued a screenshot and warped before
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

            if (m_config.VisualEffects)
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

            if (m_config.AuditoryEffects)
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
            if (m_config.ScreenshotNotifications)
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
        /// Queue of screenshot actions to take when the timeout expires
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

        /// <summary>
        /// Resets the Main Snapshot rule
        /// </summary>
        public void ResetMainSnapshotRule()
        {
            ModRule newRule = ModConfig.CreateDefaultSnapshotRule();

            if (m_config.SnapshotRules.Count == 0)
            {
                m_config.SnapshotRules.Add(newRule);
                return;
            }

            m_config.SnapshotRules[0] = newRule;
        }

        /// <summary>
        /// Adds a Weather condition to the Config.
        /// </summary>
        /// <param name="api">The GenericModConfigMenu API</param>
        /// <param name="rule">The rule to add the condition to</param>
        /// <param name="weatherFlag">The Weather type to add to the Config.</param>
        void AddWeatherConditionOption(GenericModConfigMenuAPI api, ModRule rule, WeatherFlags weatherFlag)
        {
            api.AddBoolOption(
                mod: ModManifest,
                getValue: () => ModConfigHelper.IsWeatherConditionEnabled(rule.Trigger.Weather, weatherFlag),
                setValue: (bool val) => rule.Trigger.Weather = ModConfigHelper.UpdateWeatherCondition(rule.Trigger.Weather, weatherFlag, val),
                name: () => Helper.Translation.Get($"Config.Weather.{weatherFlag}.Title"),
                tooltip: () => Helper.Translation.Get($"Config.Weather.{weatherFlag}.Tooltip")
            );
        }

        /// <summary>
        /// Adds a Location condition to the Config.
        /// </summary>
        /// <param name="api">The GenericModConfigMenu API</param>
        /// <param name="rule">The rule to add the condition to</param>
        /// <param name="locationFlag">The Location type to add to the Config.</param>
        void AddLocationConditionOption(GenericModConfigMenuAPI api, ModRule rule, LocationFlags locationFlag)
        {
            api.AddBoolOption(
                mod: ModManifest,
                getValue: () => ModConfigHelper.IsLocationConditionEnabled(rule.Trigger.Location, locationFlag),
                setValue: (bool val) => rule.Trigger.Location = ModConfigHelper.UpdateLocationCondition(rule.Trigger.Location, locationFlag, val),
                name: () => Helper.Translation.Get($"Config.Location.{locationFlag}.Title"),
                tooltip: () => Helper.Translation.Get($"Config.Location.{locationFlag}.Tooltip")
            );
        }

        /// <summary>
        /// Adds a Date condition to the Config.
        /// </summary>
        /// <param name="api">The GenericModConfigMenu API</param>
        /// <param name="rule">The rule to add the condition to</param>
        /// <param name="dateFlag">The Date type to add to the Config.</param>
        void AddDateConditionOption(GenericModConfigMenuAPI api, ModRule rule, DateFlags dateFlag)
        {
            api.AddBoolOption(
                mod: ModManifest,
                getValue: () => ModConfigHelper.IsDateConditionEnabled(rule.Trigger.Days, dateFlag),
                setValue: (bool val) => rule.Trigger.Days = ModConfigHelper.UpdateDateCondition(rule.Trigger.Days, dateFlag, val),
                name: () => Helper.Translation.Get($"Config.Days.{dateFlag}.Title"),
                tooltip: () => Helper.Translation.Get($"Config.Days.{dateFlag}.Tooltip")
            );
        }

        /// <summary>
        /// Adds a Name condition to the Config.
        /// </summary>
        /// <param name="api">The GenericModConfigMenu API</param>
        /// <param name="rule">The rule to add the condition to</param>
        /// <param name="fileNameFlag">The Name type to add to the Config.</param>
        void AddNameConditionOption(GenericModConfigMenuAPI api, ModRule rule, ModRule.FileNameFlags fileNameFlag)
        {
            api.AddBoolOption(
                mod: ModManifest,
                getValue: () => ModConfigHelper.IsFileNameConditionEnabled(rule.FileName, fileNameFlag),
                setValue: (bool val) => rule.FileName = ModConfigHelper.UpdateFileNameCondition(rule.FileName, fileNameFlag, val),
                name: () => Helper.Translation.Get($"Config.FileNameParts.{fileNameFlag}.Title"),
                tooltip: () => Helper.Translation.Get($"Config.FileNameParts.{fileNameFlag}.Tooltip")
            );
        }
    }
}
