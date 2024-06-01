# TeensyROM Command Line Interface
CLI Tool for TeensyROM Emulator, Fast Loader, MIDI and Internet cartridge for Commodore64/128.

## Features
### Chipsynth C64 ASID Patch Convertor
Chipsynth has terrific functionality to support ASID MIDI streaming to external hardware.  You can check out the ASID demo here: 
https://youtu.be/-Xs3h59-dOU 

However, the feature is a bit experimental and has a few pain points when switching between patches as described in this video:
https://youtu.be/n4f4rqjOvIc

This tool aims to fix 3/4 of the issues by creating a clone of all your Chipsynth patches with the following defaults:
- Sets VOLUME to 0 (to silence the emulated SID)
- Sets POLY to 1 (Only mono is supported from Chipsynth)
- Sets the SID CLOCK to your preference

#### To Run:
  - Backup your patches (just in case) -- You have been warned and I will not be held liable for data loss!
  - Copy the tool to your ChipSynth patch directory -- OR -- copy your patch directory to the location of this tool
  - From a command prompt 2 options: 
    - TeensyRom.Cli chipsynth transform
    - TeensyRom.Cli c t
  - Your new patches will be placed in a relative directory called /ASID
  - If you place this /ASID folder in your /Presets/com.Plogue.Fermata.chipsynth C64/ folder, you will see them appear in Chipsynth C64
  - If you need help, seek me out on the [TeensyROM Discord Server](https://discord.com/invite/ubSAb74S5U)

### ASID Patch Generator
<img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/e583a03d-e765-4c4d-b24f-09299f2ad0cf" width="60%" height="60%"/>

### Chipsynth Patch Defaults
<img src="https://github.com/MetalHexx/TeensyROM-CLI/assets/9291740/355ec5ec-8801-4339-ae63-4a389075872f" width="60%" height="60%"/>
