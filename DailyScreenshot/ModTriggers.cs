using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified Triggers.  Use with caution, data is 
    /// not validated in this class
    /// </summary>
    public class ModTriggers
    {
        /// <summary>
        /// Differnet trigger types
        /// Right now only time based or keypress
        /// could add in the future: location
        /// </summary>
        public enum TriggerType
        {
            Time,
            Keypress
        }

        /// <summary>
        /// Location for screenshot
        /// Any means take anywhere
        /// </summary>
        [Flags]
        public enum LocationFlags
        {
            Farm,
            Any
        }

        [Flags]
        public enum WeatherFlags
        {
            Sunny = 0x01,
            Cloudy = 0x02,
            Rainy = 0x04,
            Stormy = 0x08,
            Breezy = 0x10,
            Any = 0x1f
        }

        [Flags]
        public enum DateFlags
        {
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
            Sundays = Day_01 | Day_07 | Day_14 | Day_21,
            Mondays = Sundays << 1,
            Tuesdays = Sundays << 2,
            Wednesdays = Sundays << 3,
            Thursdays = Sundays << 4,
            Fridays = Sundays << 5,
            Saturdays = Sundays << 6,
            FirstDayOfTheMonth = AnySeason | Day_01,
            LastDayOfTheMonth = AnySeason | Day_28,
            Daily = AnyDay | AnySeason
        }

        public WeatherFlags Weather { get; set; }= WeatherFlags.Sunny | WeatherFlags.Cloudy | WeatherFlags.Rainy | WeatherFlags.Stormy | WeatherFlags.Breezy;
        public TriggerType Type { get; set; } = TriggerType.Time;
        public DateFlags Days { get; set; } = DateFlags.Daily;
        public LocationFlags Location { get; set; } = LocationFlags.Farm;

    }
}