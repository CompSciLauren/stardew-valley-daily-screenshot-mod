using StardewModdingAPI;
using System.Collections.Generic;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified rule.  Use with caution, data is 
    /// not validated in this class
    /// </summary>
    public class ModRule
    {


        /// <summary>
        /// Name of the rule to show
        /// </summary>
        /// <value>User specified name</value>
        public string Name { get; set; } = null;
        
        /// <summary>
        /// Zoom Level to use when taking a screenshot
        /// </summary>
        /// <value>User specified zoom factor</value>
        public float ZoomLevel { get; set; } = ModConfig.DEFAULT_ZOOM;

        /// <summary>
        /// Directory to save to
        /// </summary>
        /// <value>User specified path</value>
        public string Directory { get; set; } = ModConfig.DEFAULT_STRING;

        /// <summary>
        /// What filename to use
        /// </summary>
        /// <value>User specified filename</value>
        public string FileName { get; set; } = ModConfig.DEFAULT_STRING;


        /// <summary>
        /// Triggers for this screenshot
        /// </summary>
        /// <value></value>
        public ModTriggers Triggers { get; set; } = new ModTriggers();
    }

}