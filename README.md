# OpenBVE Discord Rich Presence
This is an Input Plugin for OpenBVE that serves the purpose of displaying a Rich Presence in Discord.  

![image](https://user-images.githubusercontent.com/28094366/193469301-118309fd-5bb7-47b8-9cb7-6250d8924fef.png)

## Roadmap
- ~Make it actually work~
- ~Customizable content when in the menu/in-game/boarding~
- ~Settings Window~
- ~Allow pasting Application ID~
- ~Ability to generate a config file~
- Ensure stability
- ~A tutorial for your average users~
- Presets that can be toggled in-game with a keybind

## Requirements
- OpenBVE Nightly Build (2022-09-06 and onwards)

## Setup
- Download the files provided in the **Release section** or compile the project.  
Then copy the dll to **<Your OpenBVE Installation/Data/InputDevicePlugins>**

- Launch OpenBVE, Go to Options -> Next Page -> Input Device Plugin, select **Discord RPC** and click "Enable this Input Device Plugin".
![image](https://user-images.githubusercontent.com/28094366/196678453-816c33c5-3ce9-4b9b-9216-ea2a2a393f11.png)

- Go to https://discord.com/developers/applications/ and create an application by clicking **New Application** button.  
(The name you chose will be displayed as "Playing [your name]")
- Copy the numeric ID that's visible below "Application ID".
![image](https://user-images.githubusercontent.com/28094366/196678999-80779eb8-d469-4318-afa9-045cf89b212b.png)

- Click Config, paste in the Application ID you copied earlier, then fill out the rest of the details.

## Building
This project has successfully been compiled with Visual Studio 2022 and MonoDevelop  
Please change the reference of `OpenBveApi.dll` to the one that comes with your OpenBVE Installation  
To automatically copy the merged dll to your OpenBVE Installation, set the build variable **OBVE_PATH** to your OpenBVE Installation

## License
[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0.txt)
