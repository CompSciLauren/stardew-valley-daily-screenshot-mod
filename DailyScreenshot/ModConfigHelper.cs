using System.IO;
using static DailyScreenshot.ModTrigger;

namespace DailyScreenshot
{

    /// <summary>
    /// Helper methods for checking and updating settings in the Config.
    /// </summary>
    public class ModConfigHelper
    {
        /// <summary>
        /// Checks if a specific weather condition is present in the WeatherFlags
        /// </summary>
        /// <param name="weather">WeatherFlags to check</param>
        /// <param name="targetWeather">The weather condition to check for</param>
        /// <returns>True if the weather condition is present, otherwise false</returns>
        public static bool IsWeatherConditionEnabled(ModTrigger.WeatherFlags weather, ModTrigger.WeatherFlags targetWeather)
        {
            // Check if the specific weather condition is present
            return (weather & targetWeather) != 0;
        }

        /// <summary>
        /// Updates weather with the new value for targetWeather.
        /// </summary>
        /// <param name="weather">WeatherFlags to check</param>
        /// <param name="targetWeather">The weather condition to check for</param>
        /// <param name="val">What the new value should be</param>
        /// <returns>Updated value for weather</returns>
        public static ModTrigger.WeatherFlags UpdateWeatherCondition(ModTrigger.WeatherFlags weather, ModTrigger.WeatherFlags targetWeather, bool val)
        {
            if (val)
            {
                // If targetWeather is true, add it to weather
                weather |= targetWeather;
            }
            else
            {
                // If targetWeather is false, remove it from weather
                weather &= ~targetWeather;
            }

            return weather;
        }

        /// <summary>
        /// Checks if a specific location condition is present in the LocationFlags
        /// </summary>
        /// <param name="location">LocationFlags to check</param>
        /// <param name="targetLocation">The location condition to check for</param>
        /// <returns>True if the location condition is present, otherwise false</returns>
        public static bool IsLocationConditionEnabled(ModTrigger.LocationFlags location, ModTrigger.LocationFlags targetLocation)
        {
            // Check if the specific location condition is present
            return (location & targetLocation) != 0;
        }

        /// <summary>
        /// Updates location with the new value for targetLocation.
        /// </summary>
        /// <param name="location">LocationFlags to check</param>
        /// <param name="targetLocation">The location condition to check for</param>
        /// <param name="val">What the new value should be</param>
        /// <returns>Updated value for location</returns>
        public static ModTrigger.LocationFlags UpdateLocationCondition(ModTrigger.LocationFlags location, ModTrigger.LocationFlags targetLocation, bool val)
        {
            if (val)
            {
                // If targetLocation is true, add it to location
                location |= targetLocation;
            }
            else
            {
                // If targetLocation is false, remove it from location
                location &= ~targetLocation;
            }

            return location;
        }

        /// <summary>
        /// Checks if a specific date condition is present in the DateFlags
        /// </summary>
        /// <param name="date">DateFlags to check</param>
        /// <param name="targetDate">The date condition to check for</param>
        /// <returns>True if the date condition is present, otherwise false</returns>
        public static bool IsDateConditionEnabled(ModTrigger.DateFlags date, ModTrigger.DateFlags targetDate)
        {
            // Check if the specific date condition is present
            return (date & targetDate) != 0;
        }

        /// <summary>
        /// Updates date with the new value for targetDate.
        /// </summary>
        /// <param name="date">DateFlags to check</param>
        /// <param name="targetDate">The date condition to check for</param>
        /// <param name="val">What the new value should be</param>
        /// <returns>Updated value for date</returns>
        public static ModTrigger.DateFlags UpdateDateCondition(ModTrigger.DateFlags date, ModTrigger.DateFlags targetDate, bool val)
        {
            // If updating a specific day or other flags, proceed as before
            if (val)
            {
                // If targetDate is true, add it to date
                date |= targetDate;
            }
            else
            {
                // If targetDate is false, remove it from date
                date &= ~targetDate;
            }

            return date;
        }

        /// <summary>
        /// Checks if a specific fileName condition is present in the FileNameFlags
        /// </summary>
        /// <param name="fileName">FileNameFlags to check</param>
        /// <param name="targetDate">The fileName condition to check for</param>
        /// <returns>True if the fileName condition is present, otherwise false</returns>
        public static bool IsFileNameConditionEnabled(ModRule.FileNameFlags fileName, ModRule.FileNameFlags targetFileName)
        {
            // Check if the specific fileName condition is present
            return (fileName & targetFileName) != 0;
        }

        /// <summary>
        /// Updates fileName with the new value for targetFileName.
        /// </summary>
        /// <param name="fileName">FileNameFlags to check</param>
        /// <param name="targetFileName">The fileName condition to check for</param>
        /// <param name="val">What the new value should be</param>
        /// <returns>Updated value for fileName</returns>
        public static ModRule.FileNameFlags UpdateFileNameCondition(ModRule.FileNameFlags fileName, ModRule.FileNameFlags targetFileName, bool val)
        {
            if (val)
            {
                // If targetFileName is true, add it to fileName
                fileName |= targetFileName;
            }
            else
            {
                // If targetFileName is false, remove it from fileName
                fileName &= ~targetFileName;
            }

            return fileName;
        }

        /// <summary>
        /// Says whether a rule with the given directory should sort before,
        /// after, or the same as another rule based on the "moves files"
        /// heuristic: rules with a non-default directory go first so their
        /// files exist before a same-named default-directory rule runs.
        /// </summary>
        /// <param name="directory">Directory of the rule being placed</param>
        /// <param name="otherDirectory">Directory of the rule being compared against</param>
        /// <param name="defaultDirectory">Sentinel value meaning "use the default directory" (ModConfig.DEFAULT_STRING)</param>
        /// <returns>-1, 0 or 1, following the IComparable convention</returns>
        public static int CompareRuleDirectory(string directory, string otherDirectory, string defaultDirectory)
        {
            if (defaultDirectory != directory)
            {
                if (defaultDirectory != otherDirectory)
                    return 0;
                return -1;
            }
            if (defaultDirectory != otherDirectory)
                return 1;
            return 0;
        }

        /// <summary>
        /// Checks if every screenshot taken under this FileName configuration
        /// is guaranteed to have a unique name.
        /// </summary>
        /// <param name="fileName">FileNameFlags to check</param>
        /// <returns>true if files cannot collide</returns>
        public static bool IsEachShotUnique(ModRule.FileNameFlags fileName)
        {
            if (ModRule.FileNameFlags.UniqueID == (fileName & ModRule.FileNameFlags.UniqueID))
                return true;
            return ModRule.FileNameFlags.None == fileName;
        }

        /// <summary>
        /// Checks whether a time of day falls within the inclusive range
        /// [startTime, endTime].
        /// </summary>
        /// <param name="startTime">Start of the range</param>
        /// <param name="endTime">End of the range</param>
        /// <param name="time">Time to check</param>
        /// <returns>true if time is contained by the range</returns>
        public static bool CheckTime(int startTime, int endTime, int time)
        {
            return time >= startTime && time <= endTime;
        }

        /// <summary>
        /// Info about a rule's filename-affecting settings, used to check for
        /// overlapping filenames between two rules without needing a live
        /// ModRule/ModTrigger instance.
        /// </summary>
        public readonly struct FileNameOverlapInfo
        {
            public ModRule.FileNameFlags FileName { get; }
            public string Directory { get; }
            public WeatherFlags Weather { get; }
            public LocationFlags Location { get; }
            public int StartTime { get; }
            public int EndTime { get; }
            public DateFlags Days { get; }

            public FileNameOverlapInfo(
                ModRule.FileNameFlags fileName,
                string directory,
                WeatherFlags weather,
                LocationFlags location,
                int startTime,
                int endTime,
                DateFlags days)
            {
                FileName = fileName;
                Directory = directory;
                Weather = weather;
                Location = location;
                StartTime = startTime;
                EndTime = endTime;
                Days = days;
            }
        }

        /// <summary>
        /// Returns true if two rules can have overlapping filenames.
        /// </summary>
        /// <param name="rule">Info for the first rule</param>
        /// <param name="other">Info for the rule to compare against</param>
        /// <returns>True if the filenames can overlap</returns>
        public static bool FileNamesCanOverlap(FileNameOverlapInfo rule, FileNameOverlapInfo other)
        {
            if (ModRule.FileNameFlags.None != (rule.FileName & ModRule.FileNameFlags.UniqueID))
                return false;
            if (ModRule.FileNameFlags.None != (other.FileName & ModRule.FileNameFlags.UniqueID))
                return false;
            if (rule.FileName != other.FileName)
                return false;
            if (Path.GetFullPath(rule.Directory) != Path.GetFullPath(other.Directory))
                return false;
            if (ModRule.FileNameFlags.None != (ModRule.FileNameFlags.Weather & rule.FileName))
            {
                if (WeatherFlags.Weather_None == (rule.Weather & other.Weather))
                    return false;
            }
            if (ModRule.FileNameFlags.None != (ModRule.FileNameFlags.Location & rule.FileName))
            {
                if (LocationFlags.Location_None == (rule.Location & other.Location))
                    return false;
            }
            if (ModRule.FileNameFlags.None != (ModRule.FileNameFlags.Time & rule.FileName))
            {
                if (!CheckTime(rule.StartTime, rule.EndTime, other.StartTime) &&
                    !CheckTime(rule.StartTime, rule.EndTime, other.EndTime) &&
                    !CheckTime(other.StartTime, other.EndTime, rule.StartTime) &&
                    !CheckTime(other.StartTime, other.EndTime, rule.EndTime))
                    return false;
            }
            if (ModRule.FileNameFlags.None != (ModRule.FileNameFlags.Date & rule.FileName))
            {
                if (DateFlags.Day_None == (DateFlags.AnyDay & (rule.Days & other.Days)))
                    return false;
                if (DateFlags.Day_None == (DateFlags.AnySeason & (rule.Days & other.Days)))
                    return false;
            }
            return true;
        }
    }

}