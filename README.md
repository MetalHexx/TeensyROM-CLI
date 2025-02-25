# TeensyROM Command Line Interface
A cross-platform CLI Tool for TeensyROM Emulator, Fast Loader, MIDI and Internet cartridge for Commodore 64/128.

## Demo
General demonstration of the available features.

https://github.com/user-attachments/assets/6a31d4cc-4a22-4253-9710-897ae6cff277

## Features

- Cross platform support for Windows, MacOS, and Linux
- Guided / BBS style interface
- Remote File Launching
- Remote Directory Listing
- Single Line Command Support

![image](https://github.com/user-attachments/assets/9ea2de1c-0ce7-4d54-a8ef-bfecfc1f75d0)

## Chipsynth C64 ASID Preset Generator
Chipsynth has terrific functionality to support ASID MIDI streaming to external hardware. However, the feature is a bit experimental and has a few pain points when switching between presets.  See videos below.

[TeensyROM / Chipsynth ASID Demo](https://youtu.be/-Xs3h59-dOU)

[Chipsynth Pain Points Demo](https://youtu.be/n4f4rqjOvIc)

[Plogue Forum Discussion on the issues](https://www.plogue.com/plgfrms/viewtopic.php?p=51755) <-- Come support the thread and chime in if you'd like to see the integration improved! 

[TeensyROM Hardware ASID Player Documentation](https://github.com/SensoriumEmbedded/TeensyROM/blob/main/docs/ASID_Player.md) <-- Read more about the TeensyROM ASID functionality here!

#### Solution
This tool aims to fix 3/4 of the issues by creating a clone of all your Chipsynth presets with the following defaults:
- Sets VOLUME to 0 (to silence the emulated SID)
- Sets POLY to 1 (Only mono is supported from Chipsynth)
- Sets the SID CLOCK to your preference
- **Note: You must still re-select "SYNTH V1" when switching between ASID patches.**
  - This is a known issue that is still unresolved by Plogue.  
  - See the Plogue forum thread mentioned above for more details.
- New presets are generated in a new folder

#### Generating Presets
  - The chipsynth preset directory and all child directories will be scanned for .fermatax files (these are preset files)
  - The process will make a copy of your presets and put them in a new folder with ASID friendly settings.
  - The process will not overwrite the existing factory patches.
  - The new presets will show up in your preset browser the next time you start Chipsynth

##### Locate your Preset Directory
  - Locate your Chipsynth C64 preset directory.  
    - Ex: C:\music\chipsynth C64\Presets\com.Plogue.Fermata.chipsynth C64
  - Copy the path for the preset folder. 
  - You will use this as the "source" directory in the tool
  - Backup your factory presets as a best practice
    
##### Using the Wizard    
  - Run the command: `teensyrom.cli cs`
  - Follow the prompts
  <img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/ec96037e-eedd-4b3c-a9ab-8823d2a06cab" width="60%" height="60%"/>
  
##### Using Command Line Parameters
*You can specify everything by a command prompt as well if you wish*
  - Type `teensyrom.cli cs -h` for help
  - Example command w/ options:  `teensyrom.cli cs --source c:\your\patch\directory --target ASID --clock ntsc`
  <img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/de7206d8-92ea-4b21-b280-e2ab7530939a" width="60%" height="60%"/>    

#### New Patches in Chipsynth
*Your patches are generated in a new sub-directory as seen below.  Enjoy!*
<img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/355ec5ec-8801-4339-ae63-4a389075872f" width="60%" height="60%"/>

#### Troubleshooting 
Depending on where your patches are located, you may need to run the tool with elevated "Admin" rights.  You may get the error below if this is the case.

<img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/23d05f97-fe32-444c-ac60-44a0a320ffb9" width="60%" height="60%"/>

#### Additional Notes
- The Linux and Mac releases are untested.  So if you try them and they don't work, please let me know and we can make it happen.
- If you need help, seek out hExx on the [TeensyROM Discord Server](https://discord.com/invite/ubSAb74S5U)
- Target Chipsynth Version: v1.006
- Untested with other versions.
