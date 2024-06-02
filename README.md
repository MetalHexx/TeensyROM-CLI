# TeensyROM Command Line Interface
CLI Tool for TeensyROM Emulator, Fast Loader, MIDI and Internet cartridge for Commodore64/128.

## Features
### Chipsynth C64 ASID Preset Generator
Chipsynth has terrific functionality to support ASID MIDI streaming to external hardware. However, the feature is a bit experimental and has a few pain points when switching between presets.
TeensyROM / Chipsynth ASID Demo: https://youtu.be/-Xs3h59-dOU 
Problem Demo: https://youtu.be/n4f4rqjOvIc

This tool aims to fix 3/4 of the issues by creating a clone of all your Chipsynth presets with the following defaults:
- Sets VOLUME to 0 (to silence the emulated SID)
- Sets POLY to 1 (Only mono is supported from Chipsynth)
- Sets the SID CLOCK to your preference
- New presets are generated in a new folder

### Generating Presets
  - The chipsynth preset directory and all child directories will be scanned for .fermatax files (these are preset files)
  - The process will not overwrite the existing factory patches, but create a new copy in a new folder (target)
  - The new presets will show up in your preset browser the next time you start Chipsynth
  - If you need help, seek out hExx on the [TeensyROM Discord Server](https://discord.com/invite/ubSAb74S5U)

#### Locate your Preset Directory
  - Backup your presets as a best practice
  - Locate your Chipsynth C64 preset directory.  Once located, copy the path for the preset folder.
    - Ex: C:\music\chipsynth C64\Presets\com.Plogue.Fermata.chipsynth C64
  - You will use this as the "source" directory in the tool.
    
#### Using the Wizard    
  - Run the command: `teensyrom.cli cs`
  - Follow the prompts
  <img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/ec96037e-eedd-4b3c-a9ab-8823d2a06cab" width="60%" height="60%"/>
  
#### Using Command Line Parameters
*You can specify everything by a command prompt as well if you wish*
  - Type `teensyrom.cli cs -h` for options
  - Ex:  `teensyrom.cli cs --source c:\your\patch\directory --target ASID --clock ntsc`
  <img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/de7206d8-92ea-4b21-b280-e2ab7530939a" width="60%" height="60%"/>    

### New Patches in Chipsynth
<img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/355ec5ec-8801-4339-ae63-4a389075872f" width="60%" height="60%"/>
