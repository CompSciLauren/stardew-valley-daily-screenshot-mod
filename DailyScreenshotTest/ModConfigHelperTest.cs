using Xunit;

using DailyScreenshot;
using static DailyScreenshot.ModTrigger;


namespace DailyScreenshotTests
{
    public class ModConfigHelperTest
    {
        [Fact]
        public void IsWeatherConditionEnabled_AnySetInConfig_ReturnsTrueForAllWeather()
        {
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Any, WeatherFlags.Sunny));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Any, WeatherFlags.Rainy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Any, WeatherFlags.Windy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Any, WeatherFlags.Stormy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Any, WeatherFlags.Snowy));
        }

        [Fact]
        public void IsWeatherConditionEnabled_NoneSetInConfig_ReturnsFalseForAllWeather()
        {
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Weather_None, WeatherFlags.Sunny));
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Weather_None, WeatherFlags.Rainy));
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Weather_None, WeatherFlags.Windy));
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Weather_None, WeatherFlags.Stormy));
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Weather_None, WeatherFlags.Snowy));
        }

        [Fact]
        public void IsWeatherConditionEnabled_SpecificWeather_ReturnsTrueForSameWeather()
        {
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Sunny, WeatherFlags.Sunny));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Rainy, WeatherFlags.Rainy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Windy, WeatherFlags.Windy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Stormy, WeatherFlags.Stormy));
            Assert.True(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Snowy, WeatherFlags.Snowy));
        }

        [Fact]
        public void IsWeatherConditionEnabled_SpecificWeather_ReturnsFalseForDifferentWeather()
        {
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(WeatherFlags.Sunny, WeatherFlags.Rainy));
        }

        [Fact]
        public void UpdateWeatherCondition_ValTrue_AddsFlagWithoutRemovingOthers()
        {
            WeatherFlags result = ModConfigHelper.UpdateWeatherCondition(WeatherFlags.Sunny, WeatherFlags.Rainy, true);

            Assert.True(result == (WeatherFlags.Sunny | WeatherFlags.Rainy));
        }

        [Fact]
        public void UpdateWeatherCondition_ValTrue_FlagAlreadySet_IsUnchanged()
        {
            WeatherFlags result = ModConfigHelper.UpdateWeatherCondition(WeatherFlags.Any, WeatherFlags.Sunny, true);

            Assert.True(result == WeatherFlags.Any);
        }

        [Fact]
        public void UpdateWeatherCondition_ValFalse_RemovesFlagWithoutRemovingOthers()
        {
            WeatherFlags result = ModConfigHelper.UpdateWeatherCondition(WeatherFlags.Any, WeatherFlags.Sunny, false);

            Assert.True(result == (WeatherFlags.Any & ~WeatherFlags.Sunny));
            Assert.False(ModConfigHelper.IsWeatherConditionEnabled(result, WeatherFlags.Sunny));
        }

        [Fact]
        public void UpdateWeatherCondition_ValFalse_FlagAlreadyUnset_IsUnchanged()
        {
            WeatherFlags result = ModConfigHelper.UpdateWeatherCondition(WeatherFlags.Weather_None, WeatherFlags.Sunny, false);

            Assert.True(result == WeatherFlags.Weather_None);
        }

        [Fact]
        public void IsLocationConditionEnabled_AnySetInConfig_ReturnsTrueForAllLocations()
        {
            Assert.True(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Any, LocationFlags.Farm));
            Assert.True(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Any, LocationFlags.Town));
            Assert.True(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Any, LocationFlags.Mine));
        }

        [Fact]
        public void IsLocationConditionEnabled_NoneSetInConfig_ReturnsFalseForAllLocations()
        {
            Assert.False(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Location_None, LocationFlags.Farm));
        }

        [Fact]
        public void IsLocationConditionEnabled_SpecificLocation_ReturnsTrueForSameLocation()
        {
            Assert.True(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Beach, LocationFlags.Beach));
        }

        [Fact]
        public void IsLocationConditionEnabled_SpecificLocation_ReturnsFalseForDifferentLocation()
        {
            Assert.False(ModConfigHelper.IsLocationConditionEnabled(LocationFlags.Beach, LocationFlags.Farm));
        }

        [Fact]
        public void UpdateLocationCondition_ValTrue_AddsFlagWithoutRemovingOthers()
        {
            LocationFlags result = ModConfigHelper.UpdateLocationCondition(LocationFlags.Farm, LocationFlags.Beach, true);

            Assert.True(result == (LocationFlags.Farm | LocationFlags.Beach));
        }

        [Fact]
        public void UpdateLocationCondition_ValFalse_RemovesFlagWithoutRemovingOthers()
        {
            LocationFlags result = ModConfigHelper.UpdateLocationCondition(LocationFlags.Any, LocationFlags.Beach, false);

            Assert.False(ModConfigHelper.IsLocationConditionEnabled(result, LocationFlags.Beach));
            Assert.True(ModConfigHelper.IsLocationConditionEnabled(result, LocationFlags.Farm));
        }

        [Fact]
        public void IsDateConditionEnabled_SpecificDay_ReturnsTrueForSameDay()
        {
            Assert.True(ModConfigHelper.IsDateConditionEnabled(DateFlags.Day_01, DateFlags.Day_01));
        }

        [Fact]
        public void IsDateConditionEnabled_SpecificDay_ReturnsFalseForDifferentDay()
        {
            Assert.False(ModConfigHelper.IsDateConditionEnabled(DateFlags.Day_01, DateFlags.Day_02));
        }

        [Fact]
        public void IsDateConditionEnabled_DayNone_ReturnsFalse()
        {
            Assert.False(ModConfigHelper.IsDateConditionEnabled(DateFlags.Day_None, DateFlags.Day_01));
        }

        [Fact]
        public void IsDateConditionEnabled_Mondays_ReturnsTrueForEveryMonday()
        {
            Assert.True(ModConfigHelper.IsDateConditionEnabled(DateFlags.Mondays, DateFlags.Day_01));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(DateFlags.Mondays, DateFlags.Day_08));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(DateFlags.Mondays, DateFlags.Day_15));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(DateFlags.Mondays, DateFlags.Day_22));
            Assert.False(ModConfigHelper.IsDateConditionEnabled(DateFlags.Mondays, DateFlags.Day_02));
        }

        [Fact]
        public void UpdateDateCondition_ValTrue_AddsFlagWithoutRemovingOthers()
        {
            DateFlags result = ModConfigHelper.UpdateDateCondition(DateFlags.Spring, DateFlags.Summer, true);

            Assert.True(result == (DateFlags.Spring | DateFlags.Summer));
        }

        [Fact]
        public void UpdateDateCondition_ValFalse_RemovesFlagWithoutRemovingOthers()
        {
            DateFlags result = ModConfigHelper.UpdateDateCondition(DateFlags.AnySeason, DateFlags.Winter, false);

            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Winter));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Spring));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Summer));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Fall));
        }

        [Fact]
        public void UpdateDateCondition_AppliedTwice_IsIdempotent()
        {
            // Regression test: GMCM re-invokes setValue for every option on each Save click,
            // so applying the same update more than once (e.g. clicking Save twice) must not
            // change the outcome.
            DateFlags result = ModConfigHelper.UpdateDateCondition(DateFlags.Daily, DateFlags.Mondays, false);
            result = ModConfigHelper.UpdateDateCondition(result, DateFlags.Mondays, false);

            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_01));
            Assert.True(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_02));
        }

        [Fact]
        public void UpdateDateCondition_IndividualDayAndOverlappingWeekday_DoNotConflict()
        {
            // Enabling a single day of the month (Day_01) should not implicitly enable the
            // other days that share its weekday (Day_08, Day_15, Day_22 are also Mondays).
            DateFlags result = ModConfigHelper.UpdateDateCondition(DateFlags.Day_None, DateFlags.Day_01, true);

            Assert.True(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_01));
            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_08));
            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_15));
            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_22));

            // Disabling the composite "Mondays" flag afterwards should clear Day_01 along with
            // the rest of the days it represents.
            result = ModConfigHelper.UpdateDateCondition(result, DateFlags.Mondays, false);

            Assert.False(ModConfigHelper.IsDateConditionEnabled(result, DateFlags.Day_01));
        }

        [Fact]
        public void IsFileNameConditionEnabled_DefaultFlags_ReturnsTrueForIncludedParts()
        {
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.Date));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.FarmName));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.GameID));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.Location));
        }

        [Fact]
        public void IsFileNameConditionEnabled_DefaultFlags_ReturnsFalseForExcludedParts()
        {
            Assert.False(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.Weather));
            Assert.False(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.Time));
            Assert.False(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.Default, ModRule.FileNameFlags.UniqueID));
        }

        [Fact]
        public void IsFileNameConditionEnabled_None_ReturnsFalse()
        {
            Assert.False(ModConfigHelper.IsFileNameConditionEnabled(ModRule.FileNameFlags.None, ModRule.FileNameFlags.Date));
        }

        [Fact]
        public void UpdateFileNameCondition_ValTrue_AddsFlagWithoutRemovingOthers()
        {
            ModRule.FileNameFlags result = ModConfigHelper.UpdateFileNameCondition(
                ModRule.FileNameFlags.Date, ModRule.FileNameFlags.Time, true);

            Assert.Equal(ModRule.FileNameFlags.Date | ModRule.FileNameFlags.Time, result);
        }

        [Fact]
        public void UpdateFileNameCondition_ValFalse_RemovesFlagWithoutRemovingOthers()
        {
            ModRule.FileNameFlags result = ModConfigHelper.UpdateFileNameCondition(
                ModRule.FileNameFlags.Default, ModRule.FileNameFlags.Location, false);

            Assert.False(ModConfigHelper.IsFileNameConditionEnabled(result, ModRule.FileNameFlags.Location));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(result, ModRule.FileNameFlags.Date));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(result, ModRule.FileNameFlags.FarmName));
            Assert.True(ModConfigHelper.IsFileNameConditionEnabled(result, ModRule.FileNameFlags.GameID));
        }

        private const string DEFAULT_DIRECTORY = "Default";

        [Fact]
        public void CompareRuleDirectory_BothDefaultDirectory_ReturnsZero()
        {
            Assert.Equal(0, ModConfigHelper.CompareRuleDirectory(DEFAULT_DIRECTORY, DEFAULT_DIRECTORY, DEFAULT_DIRECTORY));
        }

        [Fact]
        public void CompareRuleDirectory_BothCustomDirectory_ReturnsZero()
        {
            Assert.Equal(0, ModConfigHelper.CompareRuleDirectory("custom-a", "custom-b", DEFAULT_DIRECTORY));
        }

        [Fact]
        public void CompareRuleDirectory_ThisCustomOtherDefault_ReturnsNegativeOne()
        {
            Assert.Equal(-1, ModConfigHelper.CompareRuleDirectory("custom-a", DEFAULT_DIRECTORY, DEFAULT_DIRECTORY));
        }

        [Fact]
        public void CompareRuleDirectory_ThisDefaultOtherCustom_ReturnsOne()
        {
            Assert.Equal(1, ModConfigHelper.CompareRuleDirectory(DEFAULT_DIRECTORY, "custom-b", DEFAULT_DIRECTORY));
        }

        [Fact]
        public void IsEachShotUnique_UniqueIDFlagSet_ReturnsTrue()
        {
            Assert.True(ModConfigHelper.IsEachShotUnique(ModRule.FileNameFlags.Default | ModRule.FileNameFlags.UniqueID));
        }

        [Fact]
        public void IsEachShotUnique_FileNameIsNone_ReturnsTrue()
        {
            Assert.True(ModConfigHelper.IsEachShotUnique(ModRule.FileNameFlags.None));
        }

        [Fact]
        public void IsEachShotUnique_FileNameSetWithoutUniqueID_ReturnsFalse()
        {
            Assert.False(ModConfigHelper.IsEachShotUnique(ModRule.FileNameFlags.Default));
        }

        [Fact]
        public void CheckTime_TimeWithinRange_ReturnsTrue()
        {
            Assert.True(ModConfigHelper.CheckTime(600, 1200, 900));
        }

        [Fact]
        public void CheckTime_TimeEqualsStartTime_ReturnsTrue()
        {
            Assert.True(ModConfigHelper.CheckTime(600, 1200, 600));
        }

        [Fact]
        public void CheckTime_TimeEqualsEndTime_ReturnsTrue()
        {
            Assert.True(ModConfigHelper.CheckTime(600, 1200, 1200));
        }

        [Fact]
        public void CheckTime_TimeBeforeStartTime_ReturnsFalse()
        {
            Assert.False(ModConfigHelper.CheckTime(600, 1200, 500));
        }

        [Fact]
        public void CheckTime_TimeAfterEndTime_ReturnsFalse()
        {
            Assert.False(ModConfigHelper.CheckTime(600, 1200, 1300));
        }

        private static ModConfigHelper.FileNameOverlapInfo CreateOverlapInfo(
            ModRule.FileNameFlags fileName,
            string directory = "shots",
            WeatherFlags weather = WeatherFlags.Any,
            LocationFlags location = LocationFlags.Farm,
            int startTime = 600,
            int endTime = 2600,
            DateFlags days = DateFlags.Daily)
        {
            return new ModConfigHelper.FileNameOverlapInfo(fileName, directory, weather, location, startTime, endTime, days);
        }

        [Fact]
        public void FileNamesCanOverlap_SameSimpleFileNameAndDirectory_ReturnsTrue()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.FarmName | ModRule.FileNameFlags.GameID);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.FarmName | ModRule.FileNameFlags.GameID);

            Assert.True(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_ThisHasUniqueID_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.FarmName | ModRule.FileNameFlags.UniqueID);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.FarmName | ModRule.FileNameFlags.UniqueID);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_OtherHasUniqueID_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.FarmName);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.FarmName | ModRule.FileNameFlags.UniqueID);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_DifferentFileNameFlags_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.FarmName);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.GameID);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_DifferentDirectories_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.FarmName, directory: "shots-a");
            var other = CreateOverlapInfo(ModRule.FileNameFlags.FarmName, directory: "shots-b");

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_WeatherFlagNonOverlappingWeather_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Weather, weather: WeatherFlags.Sunny);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Weather, weather: WeatherFlags.Rainy);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_WeatherFlagOverlappingWeather_ReturnsTrue()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Weather, weather: WeatherFlags.Sunny | WeatherFlags.Rainy);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Weather, weather: WeatherFlags.Rainy);

            Assert.True(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_LocationFlagNonOverlappingLocation_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Location, location: LocationFlags.Farm);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Location, location: LocationFlags.Beach);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_LocationFlagOverlappingLocation_ReturnsTrue()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Location, location: LocationFlags.Farm | LocationFlags.Beach);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Location, location: LocationFlags.Beach);

            Assert.True(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_TimeFlagNonOverlappingTime_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Time, startTime: 600, endTime: 800);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Time, startTime: 1000, endTime: 1200);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_TimeFlagOverlappingTime_ReturnsTrue()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Time, startTime: 600, endTime: 1000);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Time, startTime: 800, endTime: 1200);

            Assert.True(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_DateFlagNonOverlappingDayOfWeek_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Mondays | DateFlags.Spring);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Tuesdays | DateFlags.Spring);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_DateFlagOverlappingDayDifferentSeason_ReturnsFalse()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Mondays | DateFlags.Spring);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Mondays | DateFlags.Summer);

            Assert.False(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }

        [Fact]
        public void FileNamesCanOverlap_DateFlagOverlappingDayAndSeason_ReturnsTrue()
        {
            var rule = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Mondays | DateFlags.Spring);
            var other = CreateOverlapInfo(ModRule.FileNameFlags.Date, days: DateFlags.Mondays | DateFlags.Spring | DateFlags.Summer);

            Assert.True(ModConfigHelper.FileNamesCanOverlap(rule, other));
        }
    }
}
