# Todo List

## Documentation

- [ ] Functions
- [ ] User

## User Input Validation

- [x] Triggers
- [x] Rules
- [x] Config

## Warnings

- [ ] Colliding filenames
- [ ] Rules that won't trigger
- [x] Directory not rooted
  - [x] Check if it is possible to replace slashes for the user

## Known Defects

- [x] Wait till screen is rendered (warp)
- [x] Show notice first
- [ ] Config file is always written (may not be fixable)
- [x] Takes full daylight shot at night
- [x] Weekday enums don't work
- [x] Exception for key presses before save game is loaded
- [x] Day rules are not working

## Test Cases

- [x] Triggers
  - [x] Warp
  - [x] Time change
  - [x] Key press
- [ ] Others
  - [x] Is the date correct after midnight
  - [ ] Bad user input in config
  