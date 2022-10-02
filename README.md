# OpenBVE Discord Rich Presence
This is an Input Plugin for OpenBVE that serves the purpose of displaying a Rich Presence in Discord.  

## Roadmap
- ~Make it actually work~
- ~Customizable content when in the menu/in-game/boarding~
- ~Settings Window~
- Allow pasting Application ID
- Auto generate a config file
- Ensure stability
- Presets that can be toggled in-game with a keybind

## Setup
- Go to https://discord.com/developers/applications/ and create an application. (The name you chose will be displayed as "Playing <your name>")
- Copy the numeric ID that's visible below "Application ID".
- Download the files provided in the Release section/Compile the project, then copy the dll to **<Your OpenBVE Installation/Data/InputDevicePlugins>**
- Launch OpenBVE, Go to Options -> Next Page -> Input Device Plugin, select **Discord RPC** and click "Enable this Input Device Plugin".
- Click Config, fill out the details.

## Building
This project is compiled with Visual Studio 2022  
Please change the reference of `OpenBveApi.dll` to the one that comes with OpenBVE, and change the path accordingly in the Post Build Script.

## License
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0.txt)