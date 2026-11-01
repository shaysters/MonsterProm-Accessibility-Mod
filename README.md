# Monster Prom Accessibility

Monster Prom Accessibility is a mod that makes Monster Prom, the multiplayer dating sim by Beautiful Glitch, playable with a screen reader. It reads menus, event dialogue and choices, stat changes, shops, turn order and cafeteria tables, pop quizzes, and the online code when you host a match. It works with the base game, the demo and the DLC.

It works with NVDA, JAWS and other Windows screen readers, and falls back to Windows' built-in voices if no screen reader is running. If your screen reader has a braille display connected, everything the mod reads goes to braille too.

- Join the Discord to chat, get help or report problems: <https://discord.gg/KNDXhztGGK>
- Support shaysters on Ko-fi: <https://ko-fi.com/shaysters>

## Requirements

- Monster Prom for Windows, from Steam or GOG:
  - Steam: <https://store.steampowered.com/app/743450/Monster_Prom/>
  - GOG: <https://www.gog.com/game/monster_prom>
- A screen reader such as NVDA or JAWS (optional; without one, the mod uses Windows' built-in speech).

The mod has been tested with the Steam version. It doesn't need Steam to run, so the GOG and DRM-free versions should work too, but they haven't been tested yet.

## Installing

1. Close Monster Prom if it's running.
2. Download **MonsterPromAccessibilityInstaller.exe** from the [latest release](https://github.com/shaysters/MonsterProm-Accessibility-Mod/releases/latest). It needs 64-bit Windows 10 or later.
3. Run it. Windows SmartScreen may warn that the app is unrecognized, because the installer is not code-signed. Choose **More info**, then **Run anyway**.
4. The installer finds your Steam copy of Monster Prom by itself. For a GOG or DRM-free copy, or if it can't find the game, use the Browse button next to the game folder and choose the folder that contains `MonsterProm.exe`.
5. Choose **Install**. The installer adds BepInEx if it's missing, then the mod, and removes files left by older versions of the mod.
6. Start your screen reader, then start Monster Prom. You'll hear "Monster Prom accessibility loaded."

### Updating and uninstalling

Keep the installer. Run it again to update to the newest version, repair the mod, or uninstall it. Close the game first. The installer says on its first page when an update is available, and the mod tells you too, once the title menu has been read.

### Installing by hand

1. Download BepInEx 5.4 for 32-bit Windows (`BepInEx_win_x86_5.4.23.5.zip`) from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). The game is 32-bit, so the x64 package won't work.
2. Extract it into the game folder, the one that contains `MonsterProm.exe`. On a default Steam install that's:

   ```
   C:\Program Files (x86)\Steam\steamapps\common\Monster Prom
   ```

   In Steam you can also right-click the game, choose **Manage**, then **Browse local files**. For GOG or DRM-free copies, use the folder you installed the game to.
3. Start the game once so BepInEx can set itself up, then close it.
4. Download **MonsterPromAccessibility.zip** from the [latest release](https://github.com/shaysters/MonsterProm-Accessibility-Mod/releases/latest) and extract everything in it, including the `Prism` folder, into `BepInEx\plugins` in the game folder.
5. Start your screen reader, then start the game. You'll hear "Monster Prom accessibility loaded."

To uninstall a manual install, delete `MonsterPromAccessibility.dll`, `prism.dll` and the `Prism` folder from `BepInEx\plugins`. To remove BepInEx too, delete the `BepInEx` folder, `winhttp.dll`, `doorstop_config.ini`, `.doorstop_version` and `changelog.txt` from the game folder.

## Keys

| Keyboard | Controller | What it does |
|---|---|---|
| r | Left bumper | Repeat the last thing the mod said. |
| e | None | Read the current player's stats. |
| c | None | In an online lobby, read the online code again. |
| F4 and F5 | None | Make built-in Windows speech slower or faster. This doesn't change your screen reader's speed. |

Notes:

- On a controller, press Square on PlayStation or X on Xbox on the main menu to start using the menus. The game needs this; it isn't caused by the mod.
- Stat changes are read as they happen, for example "Blue Charm plus 3."
- Cafeteria tables say who is sitting there and how many seats are free, for example "Table 2: Vera, Liam, Shopkeeper. 1 seat free."
- Shops read the dialogue, each item, its price and its description.
- Pop quiz questions are read together with their answers.
- When you host an online match, the online code is read digit by digit.
- Prom NPC names are read as you move through the selection.

## Settings

The mod's settings are in this file in the game folder:

```
BepInEx\config\com.accessibility.monsterprom.cfg
```

- **PreferredProvider**: leave it blank to let the mod pick automatically, or set it to a speech output's name, such as NVDA, JAWS, SAPI or OneCore.
- **Speech rate** for built-in Windows speech: from -10 to 10, with 0 as the default. F4 and F5 change it during play too.

## Known limitations

- In the gallery, Monstersonas and More Art read the caption text the game has, such as character and artist lines. These aren't full descriptions of the pictures.
- Endings and Polaroids just say "Gallery image selected", because the game has no text for them.
- Thumbnails may be read as "Art" followed by a number, which is the game's own label.
- Slider values in the settings menu aren't read, because the game doesn't make them available.
- The demo may not be available on every store.

## Troubleshooting

BepInEx keeps a log in the game folder:

```
BepInEx\LogOutput.log
```

If something goes wrong, include the log when you report it.

- **No speech at all:** make sure BepInEx is installed and the game was started once after installing it, and that `MonsterPromAccessibility.dll` and `prism.dll` are in `BepInEx\plugins`.
- **Windows speech talks instead of your screen reader:** start your screen reader before the game. If you start it after, restart the game.

To report a problem or suggest something, open an issue on the [GitHub page](https://github.com/shaysters/MonsterProm-Accessibility-Mod/issues).

## Credits

This mod was developed by [Shaysters](https://github.com/shaysters), with code written with Claude, Anthropic's AI model.

The installer is built with [Loadstone](https://github.com/bradjrenshaw/loadstone) by [Bradley Renshaw](https://github.com/bradjrenshaw).

Speech is provided by [Prism](https://github.com/ethindp/prism) by Ethin Probst, under the MPL-2.0 license. Its license files are in the `Prism` folder.

Monster Prom is made by Beautiful Glitch. This mod isn't affiliated with or endorsed by Beautiful Glitch.

## For developers

Build and release instructions are in [DEVELOPING.md](DEVELOPING.md).
