using System;
using StardewModdingAPI;
using StardewValley;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified Triggers.  Use with caution, data is 
    /// not validated during construction.  Call 
    /// ValidateUserInput before using values
    /// </summary>
    public class ModTrigger
    {
        void MTrace(string message) => ModEntry.DailySS.MTrace(message);

        private bool m_triggered = false;

        #region Dates 
        /// <summary>
        /// Days of the season specifiers and season specifers
        /// </summary>
        [Flags]
        public enum DateFlags
        {
            Day_None = 0,
            Day_01 = 1 << 0,
            Day_02 = 1 << 1,
            Day_03 = 1 << 2,
            Day_04 = 1 << 3,
            Day_05 = 1 << 4,
            Day_06 = 1 << 5,
            Day_07 = 1 << 6,
            Day_08 = 1 << 7,
            Day_09 = 1 << 8,
            Day_10 = 1 << 9,
            Day_11 = 1 << 10,
            Day_12 = 1 << 10,
            Day_13 = 1 << 10,
            Day_14 = 1 << 13,
            Day_15 = 1 << 14,
            Day_16 = 1 << 15,
            Day_17 = 1 << 16,
            Day_18 = 1 << 17,
            Day_19 = 1 << 18,
            Day_20 = 1 << 19,
            Day_21 = 1 << 20,
            Day_22 = 1 << 21,
            Day_23 = 1 << 22,
            Day_24 = 1 << 23,
            Day_25 = 1 << 24,
            Day_26 = 1 << 25,
            Day_27 = 1 << 26,
            Day_28 = 1 << 27,
            Spring = 1 << 28,
            Summer = 1 << 29,
            Fall = 1 << 30,
            Winter = 1 << 31,
            AnyDay = Sundays | Mondays | Tuesdays | Wednesdays | Thursdays | Fridays | Saturdays,
            AnySeason = Spring | Summer | Fall | Winter,
            Sundays = Day_01 | Day_08 | Day_15 | Day_22,
            Mondays = Sundays << 1,
            Tuesdays = Sundays << 2,
            Wednesdays = Sundays << 3,
            Thursdays = Sundays << 4,
            Fridays = Sundays << 5,
            Saturdays = Sundays << 6,
            FirstDayOfTheMonth = Day_01,
            LastDayOfTheMonth = Day_28,
            Daily = AnyDay | AnySeason
        }

        /// <summary>
        /// Days to take screenshots
        /// Note: Enum value, no need to validate
        /// </summary>
        /// <value>User specified days to take screenshots</value>
        public DateFlags Days { get; set; } = DateFlags.Daily;
        #endregion

        #region Weather
        /// <summary>
        /// Weather conditions
        /// </summary>
        [Flags]
        public enum WeatherFlags
        {
            Weather_None = 0,
            Sunny = 1 << 0,
            Rainy = 1 << 1,
            Windy = 1 << 2,
            Stormy = 1 << 3,
            Snowy = 1 << 5,
            Any = Sunny | Rainy | Windy | Stormy | Snowy,
        }

        /// <summary>
        /// Weather for screenshot.
        /// Note: Enum value, no need to validate
        /// </summary>
        /// <value>User specified weather for screenshot</value>
        public WeatherFlags Weather { get; set; } = WeatherFlags.Any;

        /// <summary>
        /// Finds the current weather
        /// </summary>
        /// <returns>WeatherFlags of the current weather</returns>
        public WeatherFlags GetWeather()
        {
            if (Game1.isSnowing)
                return WeatherFlags.Snowy;
            if (Game1.isLightning)
                return WeatherFlags.Stormy;
            if (Game1.isRaining)
                return WeatherFlags.Rainy;
            if (Game1.isDebrisWeather)
                return WeatherFlags.Windy;
            return WeatherFlags.Sunny;
        }
        #endregion

        #region Locations
        /// <summary>
        /// Location for screenshot
        /// Any means take anywhere
        /// </summary>
        [Flags]
        public enum LocationFlags
        {
            Location_None = 0,
            Farm = 1 << 0,
            GreenHouse = 1 << 2,
            Farmhouse = 1 << 3,
            Beach = 1 << 4,
            FarmCave = 1 << 5,
            Cellar = 1 << 6,
            Desert = 1 << 7,
            Museum = 1 << 8,
            CommunityCenter = 1 << 10,
            Mountain = 1 << 11,
            Unknown = 1 << 12,
            Any = Farm | GreenHouse | Farmhouse | Beach | Unknown |
                Mountain | CommunityCenter | Museum | FarmCave | Cellar | Desert
        }

        internal bool ValidateUserInput()
        {
            bool modified = false;
            int startTime = SetLimits(StartTime);
            int endTime = SetLimits(EndTime);
            if (StartTime != startTime || EndTime != endTime || startTime > endTime)
            {
                modified = true;

                // if the times are impossible then swap them
                if (startTime > endTime)
                {
                    StartTime = endTime;
                    EndTime = startTime;
                }
                else
                {
                    StartTime = startTime;
                    EndTime = endTime;
                }
            }
            return modified;
        }

        private bool IsAtLimits(int time)
        {
            if (ModConfig.DEFAULT_START_TIME == time)
                return true;
            if (ModConfig.DEFAULT_END_TIME < time)
                return true;
            return false;
        }

        /// <summary>
        /// Makes sure the time is set to be a multiple of 10 and
        /// between ModConfig.DEFAULT_START_TIME and 
        /// ModConfig.DEFAULT_END_TIME
        /// </summary>
        /// <param name="time">User specified time</param>
        /// <returns>Fixed up time rounded to the nearest 10 minutes</returns>
        private int SetLimits(int time)
        {
            // round to the nearest 10 minutes
            int val = Math.Max(time + 5, ModConfig.DEFAULT_START_TIME);
            val = Math.Min(val, ModConfig.DEFAULT_END_TIME);

            // Round to the nearest 10 mintues
            // TODO: move these magic numbers to constants
            return val - (val % 10);
        }

        /// <summary>
        /// Location for screenshot.
        /// Note: Enum value, no need to validate
        /// </summary>
        /// <value>User specified location for screenshot</value>
        public LocationFlags Location { get; set; } = LocationFlags.Farm;

        /// <summary>
        /// Finds the user's current location
        /// </summary>
        /// <returns>LocationFlags enum of the location</returns>
        public LocationFlags GetLocation()
        {
            StardewValley.GameLocation location = Game1.currentLocation;
            if (location is Farm)
                return LocationFlags.Farm;
            if (location is StardewValley.Locations.Beach)
                return LocationFlags.Beach;
            if (location is StardewValley.Locations.FarmHouse)
                return LocationFlags.Farmhouse;
            if (location is StardewValley.Locations.FarmCave)
                return LocationFlags.FarmCave;
            if (location is StardewValley.Locations.Cellar)
                return LocationFlags.Cellar;
            if (location is StardewValley.Locations.Desert)
                return LocationFlags.Desert;
            if (location is StardewValley.Locations.LibraryMuseum)
                return LocationFlags.Museum;
            if (location is StardewValley.Locations.CommunityCenter)
                return LocationFlags.CommunityCenter;
            if (location is StardewValley.Locations.Mountain)
                return LocationFlags.Mountain;
            if (location.IsGreenhouse)
                return LocationFlags.GreenHouse;
            return LocationFlags.Unknown;
        }
        #endregion

        #region Keyboard
        /// <summary>
        /// Key for screenshot
        /// Note: Enum value, no need to validate
        /// </summary>
        /// <value>User specified key for a screenshot</value>
        public SButton Key { get; set; } = SButton.None;
        #endregion

        /// <summary>
        /// Start of the time frame to take screenshot
        /// Note: must validate
        /// </summary>
        /// <value>Value between ModConfig.DEFAULT_START_TIME and ModConfig.DEFAULT_END_TIME divisible by 10</value>
        public int StartTime { get; set; } = ModConfig.DEFAULT_START_TIME;

        /// <summary>
        /// End of the time frame to take screenshot
        /// Note: must validate
        /// </summary>
        /// <value>Value between ModConfig.DEFAULT_START_TIME and ModConfig.DEFAULT_END_TIME divisible by 10</value>
        public int EndTime { get; set; } = ModConfig.DEFAULT_END_TIME;

        /// <summary>
        /// Resets the triggered flag, should be called at the start of the day
        /// </summary>
        public void ResetTrigger()
        {
            MTrace("Triggers reset");
            m_triggered = false;
        }

        /// <summary>
        /// Function for cheking if the trigger should cause a screenshot
        /// </summary>
        /// <param name="key">Optional, used for keypress</param>
        /// <returns></returns>
        public bool CheckTrigger(SButton key = SButton.None)
        {
            MTrace($"m_triggered = {m_triggered}");
            if (m_triggered)
                return false;
            DateFlags current_date = (DateFlags)((1 << (Game1.Date.SeasonIndex + 28)) | (1 << (Game1.Date.DayOfMonth - 1)));
            WeatherFlags current_weather = GetWeather();
            LocationFlags current_location = GetLocation();
            if (current_weather != (current_weather & Weather))
                return false;
            if (current_location != (current_location & Location))
                return false;
            if (current_date != (current_date & Days))
            {
                // Trigger will never be valid for this day,
                // wait for the next to reset.
                // Some mods can mess with weather
                m_triggered = true;
                return false;
            }

            // Some mods can mess with time so don't set triggered flag
            if (Game1.timeOfDay < StartTime)
                return false;
            if (Game1.timeOfDay > EndTime)
                return false;

            // Keys is not a flags enum, only one can be set at a time
            if (Key != key)
                return false;


            // If it is button based, allow another screenshot after this one for this day
            if (SButton.None == key)
                m_triggered = true;

            return true;
        }

    }
}
