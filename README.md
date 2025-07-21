# SoulsConfigurator

A comprehensive mod manager and configuration tool for Dark Souls games, designed to simplify the installation and configuration of randomizer mods.

## Overview

SoulsConfigurator is a Windows Forms application built with .NET 8 that provides an intuitive interface for installing, configuring, and managing mods for Dark Souls games. The application automates the complex process of mod installation and configuration, making it accessible to users who want to enjoy randomized gameplay without dealing with technical details.

## Supported Games

Currently, SoulsConfigurator supports:

- **Dark Souls 3** with the following mods:
  - DS3 Static Item and Enemy Randomizer
  - DS3 Fog Gate Randomizer

## Features

### ?? Game Management
- Automatic game detection and validation
- Support for custom installation paths
- Built-in game file backup and restoration
- Automatic prerequisite installation (Mod Engine, crash fixes)

### ?? Mod Configuration
- Intuitive graphical interface for mod settings
- Real-time configuration validation
- Organized option groups for easy navigation
- Support for various control types (checkboxes, radio buttons, text fields)

### ?? User Presets
- Save and load custom mod configurations
- Share configurations between sessions
- Delete unwanted presets
- Quick preset selection and application

### ?? Automation
- Automated mod installation and extraction
- Automatic mod executable launching with pre-configured settings
- Real-time status monitoring during randomization
- Automatic cleanup of temporary files

### ??? Safety Features
- Automatic game file backup before mod installation
- Complete mod removal with file restoration
- Error handling and validation throughout the process
- Safe configuration management

## Installation

### Prerequisites
- Windows 10/11
- .NET 8 Runtime
- Valid installation of supported Dark Souls games

### Setup
1. Download the latest release of SoulsConfigurator
2. Extract to a folder of your choice
3. Download and place the required mod files (see "Mod Files Setup" section below)
4. Run `SoulsConfigurator.exe`

## Mod Files Setup

### Required Downloads

Before using SoulsConfigurator, you need to download the mod files and place them in the correct Data folder structure.

#### Dark Souls 3 Mods

- **DS3 Fog Gate Randomizer**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/551)
- **DS3 Static Item and Enemy Randomizer**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/361)
- **Mod Engine**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/332)
- **Crash Fix 1.15**: Download the fixed executable from [Souls Speedruns](https://soulsspeedruns.com/darksouls3/crash-fix)

#### Sekiro Mods

- **Sekiro Randomizer** (includes 2 prerequisites): Download from [Nexus Mods](https://www.nexusmods.com/sekiro/mods/543)
- **Mod Engine**: Download from [Nexus Mods](https://www.nexusmods.com/sekiro/mods/6)

### Data Folder Structure

Create the following folder structure in your SoulsConfigurator directory:

```
SoulsConfigurator/
├── Data/
│   ├── DS3/
│   │   ├── DS3 Static Item and Enemy Randomizer-361-v0-3-1644921428.zip
│   │   ├── DS3 Fog Gate Randomizer-551-v0-2-1644925717.zip
│   │   ├── ModEngine.zip
│   │   └── DarkSoulsIII.exe
│   └── Sekiro/
│       ├── Sekiro Randomizer-543-v0-4-1-1622182119.zip
│       ├── Combined SFX-543-v2-1618274087.zip
│       ├── Divine Dragon Textures-543-v1-1588303766.zip
│       └── ModEngine.zip
```

### File Naming Requirements

**Important**: The mod files must be named exactly as shown above, including version numbers and IDs. SoulsConfigurator looks for these specific filenames:

#### Dark Souls 3:
- `DS3 Static Item and Enemy Randomizer-361-v0-3-1644921428.zip`
- `DS3 Fog Gate Randomizer-551-v0-2-1644925717.zip`
- `ModEngine.zip`
- `DarkSoulsIII.exe` (crash-fixed executable)

#### Sekiro:
- `Sekiro Randomizer-543-v0-4-1-1622182119.zip`
- `Combined SFX-543-v2-1618274087.zip`
- `Divine Dragon Textures-543-v1-1588303766.zip`
- `ModEngine.zip`

**Note**: When downloading from Nexus Mods, the files may have different names or additional text. You must rename them to match the exact filenames listed above for SoulsConfigurator to recognize them.

## Usage

### First Time Setup

1. **Select Your Game**: Choose "Dark Souls 3" from the game dropdown
2. **Set Installation Path**: Click "Browse" and navigate to your Dark Souls 3 installation folder (usually contains `DarkSoulsIII.exe`)
3. **Verify Installation**: The status should show "? Dark Souls 3 - Ready to install mods"

### Installing Mods

1. **Select Mods**: Check the boxes next to the mods you want to install
2. **Configure Mods**: Click the "Configure" button next to each selected mod to customize settings
3. **Install**: Click "Install Selected Mods" to begin the installation process

The application will:
- Backup your original game files
- Install required prerequisites (Mod Engine, crash fixes)
- Extract and configure the selected mods
- Launch mod tools with your configured settings
- Monitor the randomization process automatically

### Mod Configuration

#### DS3 Static Item and Enemy Randomizer

**Randomization Options:**
- **Item randomization**: Randomize item placements throughout the game
- **Enemy randomization**: Replace enemies with different types
- **Oops all** modes: Fill areas with specific enemy types
- **Various difficulty options**: Increase challenge with scaled enemies

**Special Features:**
- **Carthus Worm banned preset**: Automatically excludes problematic Carthus Worm spawns
- **Seed control**: Use fixed seeds for reproducible randomization
- **Progressive difficulty**: Scale enemy strength based on game progression

#### DS3 Fog Gate Randomizer

**Entrance Randomization:**
- **Boss fog gates**: Randomize connections to and from boss areas
- **Area warps**: Randomize transitions between major game areas
- **PvP fog gates**: Include PvP area connections in randomization
- **DLC integration**: Include Ariandel and Ringed City in randomization

**Progression Options:**
- **Early warp**: Coiled Sword available early for balanced start
- **Late warp**: Coiled Sword placement randomized for added challenge
- **Instant warp**: Immediate warping available for easier start

**Advanced Features:**
- **Enemy scaling**: Adjust enemy difficulty based on logical progression
- **Pacifist mode**: Allow escaping boss fights without defeating bosses
- **Tree skip logic**: Account for advanced movement techniques
- **Disconnected fog gates**: One-way connections for complex routing

### Managing User Presets

1. **Save Preset**: After configuring a mod, click "Save As..." to create a named preset
2. **Load Preset**: Select a preset from the dropdown and it will automatically apply those settings
3. **Delete Preset**: Select a preset and click "Delete" to remove it permanently

### Removing Mods

1. Click "Clear All Mods" to remove all installed mods
2. Confirm the action in the dialog box
3. The application will restore your original game files

## Technical Details

### Architecture
- **Modular Design**: Each game and mod is implemented as a separate module
- **Interface-Based**: Uses interfaces for consistent mod and game handling
- **Automation**: Advanced Windows API integration for mod tool automation
- **Configuration Management**: JSON-based preset storage and XML configuration file generation

### File Structure
```
SoulsConfigurator/
??? Data/                    # Mod archives and prerequisites
?   ??? DS3/                # Dark Souls 3 specific files
??? Helpers/                # Utility classes and automation helpers
??? Interfaces/             # Core interfaces for extensibility
??? Mods/                   # Mod-specific implementations
??? Services/               # Core services (presets, game management)
??? UI/                     # User interface components
??? Games/                  # Game-specific implementations
```

### Supported Mod Types
- **Configurable Mods**: Mods with user-customizable settings
- **Static Mods**: Mods with no configuration options
- **Prerequisite Mods**: Essential mods required for others to function

## Troubleshooting

### Common Issues

**"Invalid installation path" Error:**
- Ensure you've selected the correct Dark Souls 3 folder
- Verify the folder contains `DarkSoulsIII.exe`
- Try running SoulsConfigurator as administrator

**Mod Installation Fails:**
- Check that your game installation is not corrupted
- Ensure you have sufficient disk space
- Verify that antivirus software isn't blocking the installation

**Randomization Process Hangs:**
- The process may take several minutes for complex randomizations
- Check if mod windows are waiting for user input
- Try restarting the application if the process appears stuck

**Game Won't Launch After Mod Installation:**
- Use "Clear All Mods" to restore original files
- Verify game integrity through Steam/platform
- Ensure all prerequisites were installed correctly

### Getting Support

If you encounter issues:
1. Check the troubleshooting section above
2. Ensure you're using the latest version
3. Document the exact error message and steps to reproduce
4. Check that all required mod files are present in the Data folder

## Development

### Building from Source

1. Clone the repository
2. Open in Visual Studio 2022 or later
3. Ensure .NET 8 SDK is installed
4. Build the solution

### Adding New Mods

To add support for new mods:
1. Implement the `IMod` interface (or `IConfigurableMod` for configurable mods)
2. Create mod-specific configuration options
3. Implement installation, removal, and configuration logic
4. Add the mod to the appropriate game class

### Adding New Games

To add support for new games:
1. Implement the `IGame` interface
2. Define game-specific mod loading and management
3. Add game validation and path detection logic
4. Update the UI to include the new game option

## License

This project is provided as-is for educational and personal use. Users are responsible for ensuring they have the right to modify their games and use the included mod tools.

## Acknowledgments

- Thanks to the mod creators for their excellent randomizer tools
- Thanks to the Dark Souls modding community for their continued innovation
- Special thanks to contributors who help improve and maintain this tool
