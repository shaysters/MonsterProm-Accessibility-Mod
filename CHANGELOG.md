# Changelog

## 1.1.4 - 2026-10-31

### Added
- When the title menu has been read, the mod tells you if a newer version is out. Run the installer to update.

### Changed
- The installer accepts GOG and DRM-free copies of the game. It doesn't find them by itself, so use Browse to pick the game folder. The mod has only been tested with the Steam version, and the installer says so.

## 1.1.3 - 2026-10-30

### Fixed
- The mod was silent in 1.1.2: Prism could not be loaded by the game's older runtime. Speech works again.

## 1.1.2 - 2026-10-30

### Added
- Standalone installer (MonsterPromAccessibilityInstaller.exe). It installs BepInEx if needed, then the mod, and can update, repair, or uninstall.
- When you host an online match, the online code is read digit by digit. Press C in the lobby to hear it again.
- Cafeteria tables say who is sitting there and how many seats are free.

### Changed
- Speech goes through Prism instead of Tolk: screen readers (NVDA, JAWS and others), braille displays, or built-in Windows speech when no screen reader is running.
- F4 and F5 change the speech rate whenever built-in Windows speech is in use.
- The PreferredProvider setting takes Prism backend names (NVDA, JAWS, SAPI, OneCore).

### Removed
- Linux support.

## 1.0.0 - 2026-03-31

- First release.
