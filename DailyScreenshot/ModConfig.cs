using StardewModdingAPI;
using System.Collections.Generic;

namespace DailyScreenshot
{
    class ModConfig
    {
        public static string DEFAULT_STRING = "default";
        public const float DEFAULT_ZOOM = 0.25f;

        public List<ModRule> SnapshotRules {get; set;} = new List<ModRule>();
        public int TimeScreenshotGetsTakenAfter { get; set; }
        public float TakeScreenshotZoomLevel { get; set; }
        public SButton TakeScreenshotKey { get; set; }
        public float TakeScreenshotKeyZoomLevel { get; set; }
        public string FolderDestinationForDailyScreenshots { get; set; }
        public string FolderDestinationForKeypressScreenshots { get; set; }
        public Dictionary<string, bool> HowOftenToTakeScreenshot { get; set; }
        public bool TakeScreenshotOnRainyDays { get; set; }

        public ModConfig()
        {
            TimeScreenshotGetsTakenAfter = 600; // 6:00 AM
            TakeScreenshotZoomLevel = DEFAULT_ZOOM; // zoomed out to view entire map
            TakeScreenshotKey = SButton.None;
            TakeScreenshotKeyZoomLevel = DEFAULT_ZOOM; // zoomed out to view entire map
            FolderDestinationForDailyScreenshots = DEFAULT_STRING;
            FolderDestinationForKeypressScreenshots = DEFAULT_STRING;
            TakeScreenshotOnRainyDays = true;

            HowOftenToTakeScreenshot = new Dictionary<string, bool>
            {
                {"Daily", true},
                {"Mondays", true},
                {"Tuesdays", true},
                {"Wednesdays", true},
                {"Thursdays", true},
                {"Fridays", true},
                {"Saturdays", true},
                {"Sundays", true},
                {"First Day of Month", true},
                {"Last Day of Month", true}
            };
            SnapshotRules.Add(new ModRule());
            int cnt = 0;
            foreach (ModRule rule in SnapshotRules)
            {
                if (rule.Name == null)
                {
                    cnt++;
                    rule.Name = string.Format("Unnamed rule {}", cnt);
                }
            }
        }
    }
}