# Todo List

## Documentation

- [ ] Functions
- [x] User

## User Input Validation

- [x] Triggers
- [x] Rules
- [x] Config

## Warnings

- [ ] Colliding filenames
  - [x] Location
  - [x] Weather
  - [x] Date
  - [x] Directory
  - [ ] Game ID
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

## Test Cases

- [x] Triggers
  - [x] Warp
  - [x] Time change
  - [x] Key press
- [ ] Warnings
  - [ ] Overlapping file names
    - [ ] Location
    - [ ] Weather
    - [ ] Date
    - [ ] Directory
    - [ ] Game ID
    - [ ] Time of day
- [ ] Others
  - [x] Is the date correct after midnight
  - [ ] Bad user input in config
  - [ ] Warnings for inactive rules
  - [x] Error message for invalid config