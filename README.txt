Monster Prom Accessibility Mod

This mod adds screen reader and text-to-speech support for blind and low-vision players. It works with all versions of the game, including Steam, GOG, itch.io, and DRM-free releases.

FEATURES
On controller, press Square on PlayStation or X on Xbox while on the main menu to activate menu interaction. This behavior is required by the game’s input system and is not caused by the mod.

The mod provides menu narration. Buttons and screens are announced as you move through them.
Event dialogue is fully read aloud, including story text and choices.
Stat changes are spoken, for example “Blue Charm plus 3.”
Shop interactions are narrated, including dialogue, items, prices, and descriptions.
Turn flow is announced, including player order, locations, and table selection.
Cafeteria tables say who is sitting there and how many seats are free, for example “Table 2: Vera, Liam, Shopkeeper. 1 seat free.”
When you host an online match, the online code is read digit by digit.
Pop quiz questions and answers are read together.
Prom NPC names are announced while navigating selection.
You can press R or Left Bumper to repeat the last spoken line.
Press E to hear your current stat values.
Press C in an online lobby to hear the online code again.
If using built-in Windows speech, you can adjust speech rate with F4 to decrease and F5 to increase.
SCOPE AND LIMITATIONS
The mod supports the base game, demo, and DLC.
In the gallery, Monstersonas and More Art use available caption text such as character and artist lines. These are not full visual descriptions.
Endings and Polaroids use a fallback message of “Gallery image selected” because no text exists for them.
Thumbnails may be announced as “Art (N),” which reflects the game’s internal labeling.
In the settings menu, slider values are not spoken because the game does not expose those values.
The demo may not be available on all store platforms.
REQUIREMENTS
You must have Monster Prom installed.
You must also install BepInEx version 5.4 using the Mono build. Download it from the official BepInEx GitHub releases page and use the file named BepInEx_x86_5.4.x.x.zip.
On Windows, NVDA or JAWS is recommended. If no screen reader is running, the mod will fall back to built-in Windows speech.
INSTALLING WITH THE INSTALLER (RECOMMENDED)
The installer sets up everything for you, including BepInEx. It needs 64-bit Windows 10 or later and supports the Steam version of the game. For other stores, use the manual installation below.
Download MonsterPromAccessibilityInstaller.exe from the latest release:
https://github.com/shaysters/MonsterProm-Accessibility-Mod/releases/latest
Close the game, then run the installer.
Windows SmartScreen may warn that the app is unrecognized, because the installer is not code-signed. Choose More info, then Run anyway.
The installer finds the game through Steam. If it doesn't, use Browse to pick the folder that contains MonsterProm.exe.
Press Install. The installer adds BepInEx if it is missing, then the mod, and removes files left by older versions of the mod.
Run the installer again at any time to update, repair, or uninstall the mod.
MANUAL INSTALLATION
Install BepInEx by extracting it into your Monster Prom game folder. Launch the game once, then close it.
Locate your game folder.
For Steam, the default location is:
C:\Program Files (x86)\Steam\steamapps\common\Monster Prom\
You can also right-click the game in Steam, select Manage, and then choose Browse local files.
For other stores, use the location where you installed the game.
Open the BepInEx folder, then open the plugins folder inside it.
Copy all files from MonsterPromAccessibility.zip, including the Prism folder, into the plugins folder.
Launch the game. You should hear:
“Monster Prom accessibility loaded”
If you do not hear this, check:
Monster Prom\BepInEx\LogOutput.log
SPEECH PROVIDERS
The mod uses Prism for speech.
If NVDA, JAWS, or another supported screen reader is running, speech goes through it, and to your braille display if the screen reader supports it.
If no screen reader is running, the mod falls back to built-in Windows speech.
Start your screen reader before the game. If you start it after, restart the game to switch over.
You can override this behavior by editing:
BepInEx/config/com.accessibility.monsterprom.cfg
Set PreferredProvider to a Prism backend name, such as NVDA, JAWS, SAPI, or OneCore.
Leave it blank for automatic selection.
When built-in Windows speech is in use, the speech rate can be set between negative ten and ten, with zero as the default.
You can also adjust the rate during gameplay using F4 to decrease speed and F5 to increase it.
Prism is licensed under the Mozilla Public License 2.0. Its license files are included with the mod.
STORE SUPPORT
The mod works with Steam, GOG, itch.io, and DRM-free versions of the game.
It does not require Steam to function.
Store pages for Monster Prom can be found on Steam, GOG, and itch.io.
BUILDING FROM SOURCE
This section is for developers.
You need the .NET SDK (version 6 or later) and Monster Prom installed with BepInEx, because the build uses the game's own files and BepInEx's files from the game folder.
To build, run this from the repository folder:
dotnet build MonsterPromAccessibility.csproj -c Release
If the game is not in the default Steam folder, add the game folder to the command:
dotnet build MonsterPromAccessibility.csproj -c Release -p:GameDir="D:\Games\Monster Prom"
The built mod is in bin\Release\net46.
To build the release zip, run:
powershell -ExecutionPolicy Bypass -File scripts\package-release.ps1
Add -GameDir followed by the game folder if it is not in the default Steam folder. The zip is written to the dist folder.
To publish a release: update changes-latest.md, build the zip, create a draft GitHub release with the zip attached, then run the Release workflow from the Actions tab with the same version tag. The workflow builds the installer, writes loadstone-release.json, and publishes the release.
The installer's package files are in loadstone-packages, and its settings are in installer\profile.json.
CREDITS
Shaysters is the creator of the mod.
Claude was used for AI-assisted development.
The installer is built with Loadstone by Bradley Renshaw: https://github.com/bradjrenshaw/loadstone
Speech uses Prism by Ethin Probst, licensed under the Mozilla Public License 2.0: https://github.com/ethindp/prism