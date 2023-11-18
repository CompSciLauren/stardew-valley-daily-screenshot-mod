using StardewModdingAPI;
using System;
using System.Collections.Generic;

namespace DailyScreenshot
{

    /// <summary>
    /// User specified rule.  Use with caution, data is 
    /// not validated during construction.  Call 
    /// ValidateUserInput before using values
    /// </summary>
    public class ModConfigTypeConversion
    {
        /// <summary>
        /// Checks if a specific weather condition is present in the WeatherFlags
        /// </summary>
        /// <param name="weather">WeatherFlags to check</param>
        /// <param name="targetWeather">The weather condition to check for</param>
        /// <returns>True if the weather condition is present, otherwise false</returns>
        public static bool IsWeatherConditionEnabled(ModTrigger.WeatherFlags weather, ModTrigger.WeatherFlags targetWeather)
        {
            // If the Any flag is set, it includes all weather conditions
            // if ((weather & ModTrigger.WeatherFlags.Any) != 0)
            //     return true;

            // Check if the specific weather condition is present
            return (weather & targetWeather) != 0;
        }

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

        public static ModTrigger.DateFlags UpdateDateCondition(ModTrigger.DateFlags date, ModTrigger.DateFlags targetDate, bool val)
        {
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
    }

}