Monster Prom Accessibility Mod

This mod adds screen reader and text-to-speech support for blind and low-vision players. It works with all versions of the game, including Steam, GOG, itch.io, and DRM-free releases.

FEATURES
On controller, press Square on PlayStation or X on Xbox while on the main menu to activate menu interaction. This behavior is required by the game’s input system and is not caused by the mod.

The mod provides menu narration. Buttons and screens are announced as you move through them.
Event dialogue is fully read aloud, including story text and choices.
Stat changes are spoken, for example “Blue Charm plus 3.”
Shop interactions are narrated, including dialogue, items, prices, and descriptions.
Turn flow is announced, including player order, locations, and table selection.
Pop quiz questions and answers are read together.
Prom NPC names are announced while navigating selection.
You can press R or Left Bumper to repeat the last spoken line.
Press E to hear your current stat values.
If using SAPI, you can adjust speech rate with F4 to decrease and F5 to increase.
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
On Linux, speech-dispatcher is required.
On Debian or Ubuntu, install it using:
sudo apt install speech-dispatcher
On Arch Linux, install it using:
sudo pacman -S speech-dispatcher
INSTALLATION ON WINDOWS
Install BepInEx by extracting it into your Monster Prom game folder. Launch the game once, then close it.
Locate your game folder.
For Steam, the default location is:
C:\Program Files (x86)\Steam\steamapps\common\Monster Prom\
You can also right-click the game in Steam, select Manage, and then choose Browse local files.
For other stores, use the location where you installed the game.
Open the BepInEx folder, then open the plugins folder inside it.
Copy all mod files into the plugins folder.
Launch the game. You should hear:
“Monster Prom accessibility loaded”
If you do not hear this, check:
Monster Prom\BepInEx\LogOutput.log
INSTALLATION ON LINUX
Linux support is experimental.
Install speech-dispatcher as described in the requirements section.
Install BepInEx by extracting it into the game folder.
If using Steam, set the launch option to:
./run_bepinex.sh %command%
A full guide is available at:
https://docs.bepinex.dev/articles/user_guide/installation/index.html
Locate your game folder.
For Steam, the default location is:
~/.steam/steam/steamapps/common/Monster Prom/
Open the BepInEx/plugins folder.
Copy MonsterPromAccessibility.dll into this folder.
Ignore Tolk.dll and nvdaControllerClient on Linux.
Launch the game. You should hear:
“Monster Prom accessibility loaded”
If not, check:
Monster Prom/BepInEx/LogOutput.log
SPEECH PROVIDERS
The mod automatically selects the first available speech provider.
On Windows, it will use Tolk for NVDA or JAWS if available.
On Linux, it uses Orca through speech-dispatcher.
If neither is available on Windows, it falls back to SAPI, the built-in text-to-speech system.
You can override this behavior by editing:
BepInEx/config/com.accessibility.monsterprom.cfg
Set PreferredProvider to Tolk, Orca, or SAPI.
Leave it blank for automatic selection.
If using SAPI, the speech rate can be set between negative ten and ten, with zero as the default.
You can also adjust the rate during gameplay using F4 to decrease speed and F5 to increase it.
STORE SUPPORT
The mod works with Steam, GOG, itch.io, and DRM-free versions of the game.
It does not require Steam to function.
Store pages for Monster Prom can be found on Steam, GOG, and itch.io.
CREDITS
Shaysters is the creator of the mod.
Claude was used for AI-assisted development.