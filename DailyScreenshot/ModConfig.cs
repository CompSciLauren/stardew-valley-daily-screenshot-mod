using StardewModdingAPI;

class ModConfig
{
    public int TimeScreenshotGetsTakenAfter { get; set; }
    public SButton TakeScreenshotKey { get; set; }
    public float TakeScreenshotKeyZoomLevel { get; set; }
    public string FolderDestinationForDailyScreenshots { get; set; }
    public string FolderDestinationForKeypressScreenshots { get; set; }

    public ModConfig()
    {
        TimeScreenshotGetsTakenAfter = 600; // 6:00 AM
        TakeScreenshotKey = SButton.OemPeriod; // period key on keyboard
        TakeScreenshotKeyZoomLevel = 0.25f; // zoomed out to view entire map
        FolderDestinationForDailyScreenshots = "default";
        FolderDestinationForKeypressScreenshots = "default";
    }
}