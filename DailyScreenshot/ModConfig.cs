using StardewModdingAPI;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace DailyScreenshot
{
    class ModConfig
    {

        private string m_launchGuid;
        public static string DEFAULT_STRING = "default";
        public const float DEFAULT_ZOOM = 0.25f;
        public List<ModRule> SnapshotRules { get; set; } = new List<ModRule>();

        // Place to put json that doesn't match properties here
        // This can be used to upgrade the config file
        // See: https://www.newtonsoft.com/json/help/html/SerializationAttributes.htm
        [JsonExtensionData]
        [JsonIgnore]
        private IDictionary<string, JToken> _additionalData = null;

        [JsonIgnore]
        internal bool RulesModified { get; set; } = false;

#if false
        [JsonIgnore]
        public int TimeScreenshotGetsTakenAfter { get; set; }

        [JsonIgnore]
        public float TakeScreenshotZoomLevel { get; set; }

        [JsonIgnore]
        public SButton TakeScreenshotKey { get; set; }

        [JsonIgnore]
        public float TakeScreenshotKeyZoomLevel { get; set; }

        [JsonIgnore]
        public string FolderDestinationForDailyScreenshots { get; set; }

        [JsonIgnore]
        public string FolderDestinationForKeypressScreenshots { get; set; }

        [JsonIgnore]
        public Dictionary<string, bool> HowOftenToTakeScreenshot { get; set; }

        [JsonIgnore]
        public bool TakeScreenshotOnRainyDays { get; set; }
#endif

        public ModConfig()
        {
            m_launchGuid = Guid.NewGuid().ToString();
            SnapshotRules.Add(new ModRule());
            SnapshotRules[0].Name = m_launchGuid;
        }

        /// <summary>
        /// If the user has the old mod configuration format,
        /// migrate it to the new format
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        private void OnDeserializedFixup(StreamingContext context)
        {
            // If there's no extra Json attributes, there's nothing to fixup
            if(_additionalData == null)
                return;
            try
            {
                // Convert the automatic snapshot rules to the new format
                if (_additionalData.TryGetValue("HowOftenToTakeScreenshot", out JToken oldSSRules))
                {
                    ModRule autoRule;
                    if(SnapshotRules.Count == 1 && SnapshotRules[0].Name == m_launchGuid)
                        autoRule = SnapshotRules[0];
                    else
                    {
                        autoRule = new ModRule();
                        SnapshotRules.Add(autoRule);
                    }
                    ModTrigger autoTrigger = autoRule.Trigger;
                    autoTrigger.Location = ModTrigger.LocationFlags.Farm;
                    autoTrigger.EndTime = 2600;
                    if (_additionalData.TryGetValue("TakeScreenshotOnRainyDays", out JToken rainyDays))
                    {
                        if (!(bool)rainyDays)
                            autoTrigger.Weather = ModTrigger.WeatherFlags.Snowy |
                                ModTrigger.WeatherFlags.Sunny |
                                ModTrigger.WeatherFlags.Windy;
                        else
                            autoTrigger.Weather = ModTrigger.WeatherFlags.Any;
                    }
                    else
                        autoTrigger.Weather = ModTrigger.WeatherFlags.Any;
                    // Clear the default for a new value
                    autoTrigger.Days = ModTrigger.DateFlags.Day_None;
                    Dictionary<string, bool> ssDict = oldSSRules.ToObject<Dictionary<string, bool>>();
                    foreach (string key in ssDict.Keys)
                    {
                        if (ssDict[key])
                        {
                            // Replace "Last Day of Month" with "LastDayOfTheMonth"
                            string key_to_enum = key.Replace("of", "OfThe").Replace(" ", "");
                            if (Enum.TryParse<ModTrigger.DateFlags>(key_to_enum, out ModTrigger.DateFlags date))
                                autoTrigger.Days |= date;
                            else
                                ModEntry.DailySS.MWarn($"Unknown key: \"{key}\"");
                        }
                    }
                    // TODO: TryGet all these values and return default value if not found
                    autoRule.Directory = (string)_additionalData["FolderDestinationForDailyScreenshots"];
                    autoRule.FileName = ModRule.FileNameFlags.Default;
                    autoRule.ZoomLevel = (float)_additionalData["TakeScreenshotZoomLevel"];
                    autoTrigger.StartTime = (int)_additionalData["TimeScreenshotGetsTakenAfter"];
                    RulesModified = true;
                }
            }
            catch (Exception ex)
            {
                ModEntry.DailySS.MWarn($"Unable to read old config. Technical details:{ex}");
            }
            _additionalData=new Dictionary<string, JToken>();
        }

        public void ValidateUserInput()
        {
            foreach (ModRule rule in SnapshotRules)
            {
                if(rule.ValidateUserInput())
                    RulesModified = true;
            }
        }

        public void NameRules()
        {
            int cnt = 0;
            foreach (ModRule rule in SnapshotRules)
            {
                if (string.IsNullOrEmpty(rule.Name) || rule.Name == m_launchGuid)
                {
                    cnt++;
                    rule.Name = $"Unnamed Rule {cnt}";
                    RulesModified = true;
                }
            }
        }
    }
}