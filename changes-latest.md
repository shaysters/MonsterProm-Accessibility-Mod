# Monster Prom Accessibility 1.1.2

Run **MonsterPromAccessibilityInstaller.exe** to install, update, repair, or uninstall. Close the game first. It installs BepInEx if it is missing, then the mod, and removes files left by older versions. You can still install by hand from the zip; see the README.

## Changes

- New standalone installer.
- Speech now goes through Prism. If NVDA, JAWS, or another supported screen reader is running when the game starts, the mod speaks through it, and to your braille display if the screen reader supports it. Otherwise it uses built-in Windows speech. Tolk and the NVDA client files are no longer used.
- When you host an online match, the online code is read digit by digit a moment after the lobby opens. Press C in the lobby to hear it again.
- Cafeteria tables say who is sitting there and how many seats are free, for example "Table 2: Vera, Liam, Shopkeeper. 1 seat free."
- F4 and F5 change the speech rate whenever built-in Windows speech is in use.
- Linux support has been removed.
