# ToDo List

## Documentation

- [x] Functions
- [x] User

## User Input Validation

- [x] Triggers
- [x] Rules
- [x] Config

## Warnings

- [x] Colliding filenames
  - [x] Location
  - [x] Weather
  - [x] Date
  - [x] Directory
  - [x] Game ID
  - [x] Time of day
- [x] Changes to rules
- [x] Rules that won't trigger
- [x] Directory not rooted
  - [x] Check if it is possible to replace slashes for the user
- [x] Config file not loading

## Known Defects

- [x] Wait till screen is rendered (warp)
- [x] Show notice first
- [ ] Config file is always written (may not be fixable)
- [x] Takes full daylight shot at night
- [x] Weekday enums don't work
- [x] Exception for key presses before save game is loaded
- [x] Day rules are not working
- [x] Release builds not building (verbose logging)
- [ ] Warnings of file overlap are not showing up

## Improvements

- [x] Only follow events if a rule needs it
  - [x] Time Change
  - [x] Location Change
  - [x] Key press

## Test Cases

- [ ] Triggers
  - [x] Warp
  - [ ] Time change
  - [x] Key press
- [ ] Warnings
  - [ ] Overlapping file names
    - [ ] Location
    - [ ] Weather
    - [ ] Date
    - [ ] Directory
    - [ ] Game ID
    - [ ] Time of day
    - [x] Different saves
- [ ] Others
  - [x] Is the date correct after midnight
  - [ ] Bad user input in config
  - [ ] Warnings for inactive rules
  - [x] Error message for invalid config
