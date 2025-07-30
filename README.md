# SoulsModConfigurator

A comprehensive mod manager and configuration tool for Dark Souls games, designed to simplify the installation and configuration of randomizer mods.

## Overview

SoulsModConfigurator is a WPF application built with .NET 8 that provides an intuitive interface for installing, configuring, and managing mods for Dark Souls games. The application automates the complex process of mod installation and configuration, making it accessible to users who want to enjoy randomized gameplay without dealing with technical details.

## Supported Games

Currently, SoulsModConfigurator supports:

- **Dark Souls: Remastered** (DS1R)
- **Dark Souls II** (DS2)
- **Dark Souls III** (DS3)
- **Sekiro: Shadows Die Twice**

## Features

### 🎯 Game Management
- Automatic game detection and validation
- Automatic prerequisite installation (Mod Engine, crash fixes)

### 📦 Automatic Mod Downloads
- Integrated Nexus Mods download manager
- Simple SSO authentication
- Automatic file placement and naming
- Real-time download progress tracking
- Missing file detection and bulk downloads

### ⚙️ Mod Configuration
- Intuitive graphical interface for mod settings
- Real-time configuration validation
- Organized option groups for easy navigation

### 🔄 User Presets
- Save and load custom mod configurations
- Share configurations between sessions
- Delete unwanted presets
- Quick preset selection and application

### 🤖 Automation
- Automated mod installation and extraction
- Automatic mod executable launching with pre-configured settings
- Automatic cleanup of temporary files

### 🛡️ Safety Features
- Automatic game file backup before mod installation
- Complete mod removal with file restoration
- Error handling and validation throughout the process
- Safe configuration management

## Installation

### Prerequisites
- Windows 10/11
- .NET 8 Runtime
- Valid installation of supported Dark Souls games
- Internet connection for automatic mod downloads

### Setup
1. Download the latest release of SoulsModConfigurator
2. Extract to a folder of your choice
3. Run `SoulsModConfigurator.exe`
4. Use the built-in download manager to automatically get mod files (recommended)
   - OR manually download and place the required mod files (see "Manual Mod Files Setup" section below)

## Automatic Mod Download

SoulsModConfigurator now includes an integrated download manager that can automatically download all required mod files from Nexus Mods (Premium required) and other sources.

## Manual Mod Files Setup

If you prefer to download mod files manually or the automatic download system is not available, you can still manually download and place the required mod files.
For that open the Download manager and after that click on `Manual Download`. This will open the required mods in your browser and in the output give information on where to place the mods and how to name them.

**Tip**: Use the automatic download manager instead of manual downloads to avoid naming issues and ensure all files are correctly placed.

## Usage

### First Time Setup

1. **Select Your Game**: Choose a game from the game dropdown
2. **Set Installation Path**: Click "Browse" and navigate to your installation folder

### Installing Mods

1. **Select Mods**: Check the boxes next to the mods you want to install
2. **Configure Mods**: Click the "Configure" button next to each selected mod to customize settings and save presets
3. **Select Preset**: Select a mod preset from the dropdown.
4. **Install**: Click "Install Selected Mods" to begin the installation process

The application will:
- Backup your original game files
- Install required prerequisites (Mod Engine, crash fixes)
- Extract and configure the selected mods
- Launch mod tools with your configured settings
- Monitor the randomization process automatically

### Managing User Presets

1. **Save Preset**: After configuring a mod, click "Save As..." to create a named preset
2. **Load Preset**: Select a preset from the dropdown and it will automatically apply those settings
3. **Delete Preset**: Select a preset and click "Delete" to remove it permanently

### Removing Mods

1. Click "Clear All Mods" to remove all installed mods
2. Confirm the action in the dialog box
3. The application will restore your original game files

## License

This project is provided as-is for educational and personal use. Users are responsible for ensuring they have the right to modify their games and use the included mod tools.

## Acknowledgments

- Thanks to the mod creators for their excellent randomizer tools
- Thanks to the Dark Souls modding community for their continued innovation
- Special thanks to contributors who help improve and maintain this tool
- A very special thanks to [Divi](https://www.twitch.tv/divi) who let's me use her Twitch Emotes and her community for the idea and testing

- Also for DS1 I'm using my forks of [Dark-Souls-Enemy-Randomizer](https://github.com/Drommedhar/Dark-Souls-Enemy-Randomizer) and [DarkSoulsItemRandomizer](https://github.com/Drommedhar/DarkSoulsItemRandomizer) which are adjusted to allow CLI arguments and use INI files for configuration.

## Support
If you want to support me, feel free to [Buy me a Coffee](https://coff.ee/drommedhar) or donate over [PayPal](https://www.paypal.com/paypalme/drommedhar)
Also if any original Mod developer wants to directly integrate their mods, feel free to to it and create a PR. Maybe we can have all of them in without the need to download files in the future.

## Mods 'used'
- [Dark Souls Enemy Randomizer](https://www.nexusmods.com/darksouls/mods/1407)
- [Dark Souls Item Randomizer](https://www.nexusmods.com/darksoulsremastered/mods/86)
- [Fog Gate Randomizer](https://www.nexusmods.com/darksoulsremastered/mods/165)
- [Dark Souls II Randomizer (Enemy and Item)](https://www.nexusmods.com/darksouls2/mods/1317?tab=description)
- [DS3 Static Item and Enemy Randomizer](https://www.nexusmods.com/darksouls3/mods/361)
- [DS3 Fog Gate Randomizer](https://www.nexusmods.com/darksouls3/mods/551)
- [Sekiro Enemy and Item Randomizer](https://www.nexusmods.com/sekiro/mods/543)
