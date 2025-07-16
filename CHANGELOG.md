# Changelog
## 0.3.0
- Added NonLazy function for registration.

## 0.2.3
- Fixed incorrect package name
- Fixed license

## 0.2.2
- Updated dependency of Foundation package to 1.0.0

## 0.2.1
- Fixed incorrect install method name.

## 0.2.0
- Added installers for installing contracts by different concerns in different ways

## 0.1.4
- Prevent validation from running in play mode
- Fixed validation not working when it has thrown exceptions during validation or the waiting list has been null.

## 0.1.3
- Fixed the registration validator not working with parent relationship setup on lifetime scope
- Changed the hotkey of `Validate and Play` to `Alt + Shift + C`

## 0.1.2
- Fixed the package name.

## 0.1.1
- Fixed project cannot be built due to incorrect setting of the assembly definition

## 0.1.0
- Added Registration Validator
  - Validate the registration
  - Identify which gameObjects in the scene are not assigned to the auto-injected gameObjects List
  - Support hotkeys, validate and play the editor