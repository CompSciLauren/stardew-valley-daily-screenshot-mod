# Testing

Here are the testing strategies available/recommended for this mod.

## warning_test_files

See the [how_to_test.md](./warning_test_files/how_to_test.md) guide for how to use these tests.

## Unit Tests

It would be nice to have unit tests and these will hopefully be added in the future, but this is not available yet.

## Manual In-Game Testing

This is the primary method of testing.

### Happy-Path Config Testing

The Happy-Path Config. Add this to the config.json file for testing:
``` json
{
  "AuditoryEffects": true,
  "VisualEffects": true,
  "ScreenshotNotifications": true,
  "SnapshotRules": [
    {
      "Name": "Daily Farm Picture",
      "ZoomLevel": 0.25,
      "Directory": "Default",
      "FileName": "Default",
      "Trigger": {
        "Days": "Daily",
        "Weather": "Any",
        "Location": "Farm",
        "Key": "None",
        "StartTime": 600,
        "EndTime": 2600
      }
    },
    {
      "Name": "Keypress Picture",
      "ZoomLevel": 1.0,
      "Directory": "/home/bob/SDV",
      "FileName": "None",
      "Trigger": {
        "Days": "Daily",
        "Weather": "Any",
        "Location": "Any",
        "Key": "Multiply",
        "StartTime": 600,
        "EndTime": 2600
      }
    }
  ]
}
```

1. Able to launch game and capture a screenshot using Happy-Path config.json file.
    * Game generally looks/behaves as expected.
    * No console errors or warnings.
    * Screenshot is taken and can see the notification with auditory (camera sound) and visual effects (flash).
        * Can be found in the designated folder.
        * Has the expected file name.
        * Was triggered under the correct conditions.
            * Days
            * Weather
            * Location
            * Key (if any)
            * StartTime
            * EndTime
    * Repeat this a few times for a few different configurations for the triggers.
1. Able to change the settings from the UI Config
    * The screenshot behavior updates as expected.
        * AuditoryEffects
        * VisualEffects
        * ScreenshotNotifications
        * SnapshotRules
            * Name
            * ZoomLevel
            * Directory
            * FileName
            * Triggers
                * Days
                * Weather
                * Location
                * Key
                * StartTime
                * EndTime
    * Other existing additional snapshot rules are NOT updated/reset/overridden. Only the global settings and first set of snapshot rules are updated.
1. Able to reset to default settings with the Default button from the UI Config.
    * The screenshot behavior updates as expected.
        * UI Config shows correct default options.
        * Screenshot behavior in-game and in designated folder are correct.
            * AuditoryEffects
            * VisualEffects
            * ScreenshotNotifications
            * SnapshotRules
                * Name
                * ZoomLevel
                * Directory
                * FileName
                * Triggers
                    * Days
                    * Weather
                    * Location
                    * Key
                    * StartTime
                    * EndTime
    * Other existing additional snapshot rules are NOT updated/reset/overridden. Only the global settings and first set of snapshot rules are updated.

### Unhappy-Path Config Testing - 1

The Happy-Path Config. Add this to the config.json file for testing:
``` json
{
  "SnapshotRules": []
}
```

1. Able to launch game and capture a screenshot using Unhappy-Path config.json file.
    * Game generally looks/behaves as expected.
    * No console errors or warnings.
    * For screenshots, it should just use the Default settings automatically when the config contents are found to be mostly empty.
    * Screenshot is taken and can see the notification with auditory (camera sound) and visual effects (flash).
        * Can be found in the designated folder.
        * Has the expected file name.
        * Was triggered under the correct conditions.
            * Days
            * Weather
            * Location
            * Key (if any)
            * StartTime
            * EndTime
1. Able to change the settings from the UI Config
    * The screenshot behavior updates as expected.
        * AuditoryEffects
        * VisualEffects
        * ScreenshotNotifications
        * SnapshotRules
            * Name
            * ZoomLevel
            * Directory
            * FileName
            * Triggers
                * Days
                * Weather
                * Location
                * Key
                * StartTime
                * EndTime
1. Able to reset to default settings with the Default button from the UI Config.
    * The screenshot behavior updates as expected.
        * UI Config shows correct default options.
        * Screenshot behavior in-game and in designated folder are correct.
            * AuditoryEffects
            * VisualEffects
            * ScreenshotNotifications
            * SnapshotRules
                * Name
                * ZoomLevel
                * Directory
                * FileName
                * Triggers
                    * Days
                    * Weather
                    * Location
                    * Key
                    * StartTime
                    * EndTime

### Unhappy-Path Config Testing - 2

The Happy-Path Config. Add this to the config.json file for testing:
``` json
{
}
```

1. Warnings show up in the Console as expected (todo: document what errors are expected here).
