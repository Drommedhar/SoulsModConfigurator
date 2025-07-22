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

### 🎯 Game Management
- Automatic game detection and validation
- Support for custom installation paths
- Built-in game file backup and restoration
- Automatic prerequisite installation (Mod Engine, crash fixes)

### 📦 Automatic Mod Downloads
- Integrated Nexus Mods download manager
- Simple API key authentication
- Automatic file placement and naming
- Real-time download progress tracking
- Support for both Nexus Mods and direct downloads
- Missing file detection and bulk downloads

### ⚙️ Mod Configuration
- Intuitive graphical interface for mod settings
- Real-time configuration validation
- Organized option groups for easy navigation
- Support for various control types (checkboxes, radio buttons, text fields)

### 🔄 User Presets
- Save and load custom mod configurations
- Share configurations between sessions
- Delete unwanted presets
- Quick preset selection and application

### 🤖 Automation
- Automated mod installation and extraction
- Automatic mod executable launching with pre-configured settings
- Real-time status monitoring during randomization
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
1. Download the latest release of SoulsConfigurator
2. Extract to a folder of your choice
3. Run `SoulsConfigurator.exe`
4. Use the built-in download manager to automatically get mod files (recommended)
   - OR manually download and place the required mod files (see "Manual Mod Files Setup" section below)

## Automatic Mod Download

SoulsConfigurator now includes an integrated download manager that can automatically download all required mod files from Nexus Mods and other sources.

### How to Use

1. **Get API Key**: Get your free API key from [Nexus Mods API Settings](https://next.nexusmods.com/settings/api-keys)
2. **Select Your Game**: Choose your game from the dropdown
3. **Set Installation Path**: Click "Browse" and navigate to your game installation folder
4. **Download Files**: Click the "Download Files" button to open the download manager
5. **Enter API Key**: Paste your API key and click "Set API Key" to authenticate
6. **Start Download**: Click "Start Download" to automatically download all missing files

The download manager will:
- Check which files are already present
- Download only missing files
- Place files in the correct directory structure
- Show real-time download progress
- Handle both Nexus Mods files and direct downloads

### Supported Downloads

The automatic download system can download:

#### Dark Souls 3:
- DS3 Fog Gate Randomizer (from Nexus Mods)
- DS3 Static Item and Enemy Randomizer (from Nexus Mods)
- Mod Engine (from Nexus Mods)
- Crash Fix 1.15 (from Souls Speedruns)

#### Sekiro:
- Sekiro Randomizer with prerequisites (from Nexus Mods)
- Mod Engine (from Nexus Mods)

### Authentication

The download manager uses Nexus Mods' API key system:
- Free API key from your Nexus Mods account
- Simple copy-paste authentication
- Secure API-based authentication
- Persistent authentication across sessions

**Getting Your API Key**:
1. Visit [Nexus Mods API Settings](https://next.nexusmods.com/settings/api-keys)
2. Log in to your Nexus Mods account (create one if needed - it's free)
3. Click "Generate API Key" 
4. Copy the generated key
5. Paste it into SoulsConfigurator's download manager

## Manual Mod Files Setup

If you prefer to download mod files manually or the automatic download system is not available, you can still manually download and place the required mod files.

### Required Downloads

Before using SoulsConfigurator, you need to download the mod files and place them in the correct Data folder structure.

#### Dark Souls 3 Mods

- **DS3 Fog Gate Randomizer**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/551)
- **DS3 Static Item and Enemy Randomizer**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/361)
- **Mod Engine**: Download from [Nexus Mods](https://www.nexusmods.com/darksouls3/mods/332)

#### Sekiro Mods

- **Sekiro Randomizer** (includes 2 prerequisites): Download from [Nexus Mods](https://www.nexusmods.com/sekiro/mods/543)
- **Mod Engine**: Download from [Nexus Mods](https://www.nexusmods.com/sekiro/mods/6)

### Data Folder Structure

Create the following folder structure in your SoulsConfigurator directory:

```
SoulsConfigurator/
├── Data/
│   ├── DS3/
│   │   ├── DS3_Item_Enemy_Randomizer.zip
│   │   ├── DS3_FogGate_Randomizer.zip
│   │   ├── ModEngine.zip
│   │   ├── DarkSoulsIII.exe
│   └── Sekiro/
│       ├── Sekiro_Randomizer.zip
│       ├── Combined_SFX.zip
│       ├── Divine_Dragon_Textures.zip
│       └── ModEngine.zip
```

### File Naming Requirements

**Important**: The mod files must be named exactly as shown above. SoulsConfigurator looks for these specific filenames:

**Note**: When downloading from Nexus Mods, the files may have different names or additional text. You must rename them to match the exact filenames listed above for SoulsConfigurator to recognize them.

**Tip**: Use the automatic download manager instead of manual downloads to avoid naming issues and ensure all files are correctly placed.

## Usage

### First Time Setup

1. **Select Your Game**: Choose "Dark Souls 3" from the game dropdown
2. **Set Installation Path**: Click "Browse" and navigate to your Dark Souls 3 installation folder (usually contains `DarkSoulsIII.exe`)
3. **Verify Installation**: The status should show "✓ Dark Souls 3 - Ready to install mods"
4. **Download Mod Files**: Click "Download Files" to use the automatic download manager, or ensure all files are manually placed in the Data folder

### Downloading Mods (Recommended Method)

1. **Open Download Manager**: Click the "Download Files" button on the main interface
2. **Get API Key**: Visit [Nexus Mods API Settings](https://next.nexusmods.com/settings/api-keys) to get your free API key
3. **Enter API Key**: Paste your API key into the download manager and click "Set API Key"
4. **Start Download**: Click "Start Download" to automatically download all missing mod files
5. **Monitor Progress**: Watch the real-time download progress and logs
6. **Completion**: All files will be automatically placed in the correct directories

### Installing Mods

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
- Internet connection for automatic mod downloads

### Setup
1. Download the latest release of SoulsConfigurator
2. Extract to a folder of your choice
3. Run `SoulsConfigurator.exe`
4. Use the built-in download manager to automatically get mod files (recommended)
   - OR manually download and place the required mod files (see "Manual Mod Files Setup" section below)

## Automatic Mod Download

SoulsConfigurator now includes an integrated download manager that can automatically download all required mod files from Nexus Mods and other sources.

### How to Use

1. **Get API Key**: Get your free API key from [Nexus Mods API Settings](https://next.nexusmods.com/settings/api-keys)
2. **Select Your Game**: Choose your game from the dropdown
3. **Set Installation Path**: Click "Browse" and navigate to your game installation folder
4. **Download Files**: Click the "Download Files" button to open the download manager
5. **Enter API Key**: Paste your API key and click "Set API Key" to authenticate
6. **Start Download**: Click "Start Download" to automatically download all missing files

The download manager will:
- Check which files are already present
- Download only missing files
- Place files in the correct directory structure
- Show real-time download progress
- Handle both Nexus Mods files and direct downloads

### Supported Downloads

The automatic download system can download:

#### Dark Souls 3:
- DS3 Fog Gate Randomizer (from Nexus Mods)
- DS3 Static Item and Enemy Randomizer (from Nexus Mods)
- Mod Engine (from Nexus Mods)
- Crash Fix 1.15 (from Souls Speedruns)

#### Sekiro:
- Sekiro Randomizer with prerequisites (from Nexus Mods)
- Mod Engine (from Nexus Mods)

### Authentication

The download manager uses Nexus Mods' API key system:
- Free API key from your Nexus Mods account
- Simple copy-paste authentication
- Secure API-based authentication
- Persistent authentication across sessions

**Getting Your API Key**:
1. Visit [Nexus Mods API Settings](https://next.nexusmods.com/settings/api-keys)
2. Log in to your Nexus Mods account (create one if needed - it's free)
3. Click "Generate API Key" 
4. Copy the generated key
5. Paste it into SoulsConfigurator's download manager

## Manual Mod Files Setup

If you prefer to download mod files manually or the automatic download system is not available, you can still manually download and place the required mod files.

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

**Tip**: Use the automatic download manager instead of manual downloads to avoid naming issues and ensure all files are correctly placed.

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

**Download Authentication Fails:**
- Ensure you have a valid Nexus Mods account (free registration)
- Get your API key from https://next.nexusmods.com/settings/api-keys
- Make sure you copied the entire API key correctly
- Check that your Nexus Mods account is in good standing
- Verify your internet connection

**Download Fails or Hangs:**
- Check your internet connection
- Ensure Nexus Mods isn't experiencing server issues
- Try downloading individual files instead of bulk download
- Verify you have sufficient disk space

**Missing Files After Download:**
- Check that downloads completed successfully in the log
- Verify files are in the correct Data subfolder (DS3/Sekiro)
- Some downloads may require manual browser completion
- Use "Check Files" button to verify file availability

**Mod Installation Fails:**
- Check that your game installation is not corrupted
- Ensure you have sufficient disk space
- Verify that antivirus software isn't blocking the installation
- Make sure all required mod files were downloaded successfully

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
