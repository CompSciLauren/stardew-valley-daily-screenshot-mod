using System;
using System.Text;
using System.IO;
using StardewValley;
using System.Globalization;
using StardewModdingAPI;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified rule.  Use with caution, data is 
    /// not validated during construction.  Call 
    /// ValidateUserInput before using values
    /// </summary>
    public class ModRule : IComparable
    {
        void MError(string message) => ModEntry.DailySS.MError(message);

        /// <summary>
        /// Is this rule active?
        /// </summary>
        /// <value>True if the rule should be used to take a screenshot</value>
        [JsonIgnore]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Name of the rule to show
        /// Note: must validate
        /// </summary>
        /// <value>User specified name</value>
        public string Name { get; set; } = null;

        /// <summary>
        /// Zoom Level to use when taking a screenshot
        /// Note: must validate
        /// </summary>
        /// <value>User specified zoom factor</value>
        public float ZoomLevel { get; set; } = ModConfig.DEFAULT_ZOOM;

        /// <summary>
        /// Directory to save to
        /// Note: must validate
        /// </summary>
        /// <value>User specified path</value>
        public string Directory { get; set; } = ModConfig.DEFAULT_STRING;

        [Flags]
        public enum FileNameFlags
        {
            // Use standard game naming
            None = 0,
            Date = 1 << 0,
            FarmName = 1 << 1,
            GameID = 1 << 2,
            Location = 1 << 3,
            Weather = 1 << 4,
            PlayerName = 1 << 5,
            Time = 1 << 6,
            UniqueID = 1 << 7,
            Default = Date | FarmName | GameID | Location

        }

        /// <summary>
        /// Turns the current game date into a constant filename for the day
        /// Setup so OS naturally keeps the files in order
        /// </summary>
        /// <returns>01-02-03 for year 1, summer, day 3</returns>
        private string GameDateToName()
        {
            return string.Format("{0:D2}-{1:D2}-{2:D2}", Game1.Date.Year, Game1.Date.SeasonIndex + 1, Game1.Date.DayOfMonth);
        }

        /// <summary>
        /// What filename to use
        /// Note: Enum value, validation not needed
        /// </summary>
        /// <value>User specified filename</value>
        public FileNameFlags FileName { get; set; } = FileNameFlags.Default;

        /// <summary>
        /// Builds a filename based on the FileName flags
        /// {Farm Name}-{GameID}/{Location}/{Weather}/{Player Name}-{Date}-{Time}-{Unique ID}
        /// </summary>
        /// <returns></returns>
        public string GetFileName()
        {
            if (FileNameFlags.None == FileName)
                return null;
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            char sep = Path.DirectorySeparatorChar;
            StringBuilder sb = new StringBuilder(".");
            if (AddFilenamePart(FileNameFlags.FarmName,
                                sep,
                                ref sb,
                                Game1.player.farmName + "-Farm-Screenshots"))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.GameID,
                                sep,
                                ref sb,
                                Game1.uniqueIDForThisGame))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.Location,
                                sep,
                                ref sb,
                                Trigger.GetLocation()))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.Weather,
                                sep,
                                ref sb,
                                Trigger.GetWeather()))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.PlayerName,
                                sep,
                                ref sb,
                                Game1.player.name))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.Date,
                                sep,
                                ref sb,
                                GameDateToName()))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.Time,
                                sep,
                                ref sb,
                                Game1.timeOfDay))
                sep = '-';
            AddFilenamePart(FileNameFlags.UniqueID,
                            sep,
                            ref sb,
                            secondsSinceEpoch);

            return sb.ToString();
        }

        /// <summary>
        /// Function for adding elements to the string to build
        /// based on the flag values set
        /// </summary>
        /// <param name="flag">Flag to check</param>
        /// <param name="sep">Seperator to use, either . or Path.DirectorySeparatorChar</param>
        /// <param name="sb">Reference to the string builder object which will be modified</param>
        /// <param name="value">String to add to the string builder if the flag is set</param>
        /// <returns>true if sb was modified</returns>
        private bool AddFilenamePart(FileNameFlags flag,
                                     char sep,
                                     ref StringBuilder sb,
                                     object value)
        {
            if (FileNameFlags.None != (flag & FileName))
            {
                if ('-' == sep || Path.DirectorySeparatorChar == sep)
                {
                    sb.Append(sep);
                }
                sb.Append(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Trigger for this screenshot
        /// </summary>
        /// <value></value>
        public ModTrigger Trigger { get; set; } = new ModTrigger();

        /// <summary>
        /// Checks user input and sets default values if needed
        /// </summary>
        /// <returns>true if the user input was modified</returns>
        internal bool ValidateUserInput()
        {
            bool modified = Trigger.ValidateUserInput();
            if (ModConfig.DEFAULT_STRING != Directory)
            {
                // String.compare usually uses the current culture which can have
                // weird rules for uppercase/lowercase.  Do an insensitive match
                // using the InvariantCulture to prevent different behavior based
                // on the system language
                if (0 == String.Compare(ModConfig.DEFAULT_STRING,
                                       Directory.Trim(),
                                       true,
                                       CultureInfo.InvariantCulture) ||
                    String.IsNullOrEmpty(Directory.Trim()))
                {
                    modified = true;
                    Directory = ModConfig.DEFAULT_STRING;
                }
                else
                {
                    // rewrite the path as a full path
                    try
                    {
                        string path = Path.GetFullPath(Directory);
                        if (path != Directory)
                        {
                            modified = true;
                            Directory = path;
                        }
                    }
                    catch (Exception ex)
                    {
                        MError($"Invalid path {Directory} specified.\nTechnical Details:\n{ex}");
                        Enabled = false;
                    }
                }
            }
            if (ZoomLevel < 0.25f || ZoomLevel > 1.0f)
            {
                modified = true;
                ZoomLevel = Math.Max(ZoomLevel, 0.25f);
                ZoomLevel = Math.Min(ZoomLevel, 1.0f);
            }
            // What do we need to check for on the name?
            return modified;
        }

        /// <summary>
        /// Is each screenshot unique
        /// </summary>
        /// <returns>true if files cannot collide</returns>
        internal bool IsEachShotUnique()
        {
            if (FileNameFlags.UniqueID == (FileName & FileNameFlags.UniqueID))
                return true;
            return FileNameFlags.None == FileName;
        }

        // Add sort order so rules that move files go first
        // to prevent overlapping file names disappearing from
        // the screenshot directory
        /// <summary>
        /// Says if this rule comes before or after another rule
        /// </summary>
        /// <param name="obj">ModRule to compare</param>
        /// <returns>A value that indicates the relative order of the objects being compared.</returns>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            ModRule otherRule = obj as ModRule;
            if (otherRule != null)
            {
                if (ModConfig.DEFAULT_STRING != Directory)
                {
                    if (ModConfig.DEFAULT_STRING != otherRule.Directory)
                        return 0;
                    return -1;
                }
                if (ModConfig.DEFAULT_STRING != otherRule.Directory)
                    return 1;
                return 0;
            }
            else
                throw new ArgumentException("Object is not a ModRule");
        }
    }

}