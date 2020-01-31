using System;
using System.Text;
using System.IO;
using StardewValley;
using StardewModdingAPI;
using System.Collections.Generic;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified rule.  Use with caution, data is 
    /// not validated during construction.  Call 
    /// ValidateUserInput before using values
    /// </summary>
    public class ModRule : IComparable
    {
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
            if(FileNameFlags.None == FileName)
                return null;
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            int secondsSinceEpoch = (int)t.TotalSeconds;
            char sep = Path.DirectorySeparatorChar;
            StringBuilder sb = new StringBuilder(".");
            if (AddFilenamePart(FileNameFlags.FarmName, sep, ref sb, Game1.player.farmName, "-Farm-Screenshots"))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.GameID, sep, ref sb, Game1.uniqueIDForThisGame))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.Location, sep, ref sb, Trigger.GetLocation()))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.Weather, sep, ref sb, Trigger.GetWeather()))
                sep = Path.DirectorySeparatorChar;
            if ('-' == sep)
                sep = Path.DirectorySeparatorChar;
            if (AddFilenamePart(FileNameFlags.PlayerName, sep, ref sb, Game1.player.name))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.Date, sep, ref sb, GameDateToName()))
                sep = '-';
            if (AddFilenamePart(FileNameFlags.Time, sep, ref sb, Game1.timeOfDay))
                sep = '-';
            AddFilenamePart(FileNameFlags.UniqueID, sep, ref sb, secondsSinceEpoch);

            return sb.ToString();
        }

        private bool AddFilenamePart(FileNameFlags flag, char sep, ref StringBuilder sb, object value, string suffix = "")
        {
            if (FileNameFlags.None != (flag & FileName))
            {
                if ('\0' != sep)
                {
                    sb.Append(sep);
                }
                sb.Append(value);
                sb.Append(suffix);
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
            throw new NotImplementedException();
            //eturn modified;
        }

        /// <summary>
        /// Is each screenshot unique
        /// </summary>
        /// <returns>true if files cannot collide</returns>
        internal bool IsEachShotUnique()
        {
            if(FileNameFlags.UniqueID == (FileName & FileNameFlags.UniqueID))
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
                if(ModConfig.DEFAULT_STRING != Directory)
                {
                    if(ModConfig.DEFAULT_STRING != otherRule.Directory)
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