# SoulsConfigurator Automatic Download System Implementation

## Overview

This implementation adds an automatic download system to SoulsConfigura### Developer Notes

### API Key Setup
Users can get their free API key from https://next.nexusmods.com/settings/api-keys. No special registration or approval required.

### API Rate Limitingat can download mod files directly from Nexus Mods and other sources, eliminating the need for users to manually download and place files.

## New Components

### 1. NexusModsService (`Services/NexusModsService.cs`)

**Purpose**: Handles authentication and downloading from Nexus Mods using their API key system.

**Key Features**:
- Simple API key authentication
- HTTP client for downloading files
- Support for both Nexus Mods API and direct URL downloads
- Real-time download progress tracking
- Automatic file placement

**Authentication**: Uses standard API key authentication - no special registration required.

### 2. ModDownloadService (`Services/ModDownloadService.cs`)

**Purpose**: Higher-level service that manages downloading all mod files for a specific game.

**Key Features**:
- Pre-configured mod download information for DS3 and Sekiro
- Bulk download functionality
- Missing file detection
- Progress aggregation and reporting

**Supported Downloads**:
- DS3: Fog Gate Randomizer, Item/Enemy Randomizer, Mod Engine, Crash Fix
- Sekiro: Randomizer, Combined SFX, Divine Dragon Textures, Mod Engine

### 3. DownloadProgressForm (`UI/DownloadProgressForm.cs`)

**Purpose**: User interface for the download manager with API key input and progress tracking.

**Features**:
- API key input and validation
- Real-time download progress display
- Detailed logging of download operations
- Error handling and user feedback

## Integration Points

### Form1 Updates

1. **Download Button**: Added "Download Files" button next to "Check Files"
2. **Download Service**: Integrated ModDownloadService into main form
3. **Missing Files Dialog**: Updated to offer automatic download option
4. **Game Selection**: Maps game names to download service folder structure

### Project Configuration

1. **Dependencies**: Added System.Text.Json and WebSocket support
2. **Disposal**: Proper cleanup of download service resources

## API Integration

### Nexus Mods API Key Flow

1. **API Key Input**: User enters API key from Nexus Mods settings
2. **Key Validation**: Test API key with validate endpoint
3. **Header Setup**: Add API key to all subsequent requests
4. **API Requests**: Use API key for authenticated downloads

### Download Process

1. **File Enumeration**: Gets mod files list from Nexus API
2. **Latest Version**: Automatically selects most recent main file
3. **Download Links**: Requests temporary download URLs
4. **Streaming Download**: Downloads with progress tracking
5. **File Placement**: Saves to appropriate Data subfolder

## Configuration

### Mod Download Mapping

Each mod is configured with:
- **GameDomain**: Nexus Mods game identifier (e.g., "darksouls3", "sekiro")
- **ModId**: Nexus Mods mod ID number
- **FileName**: Expected filename in SoulsConfigurator
- **OutputFolder**: Target directory (DS3 or Sekiro)

### Direct Downloads

Non-Nexus files (like crash fixes) are configured with:
- **Url**: Direct download URL
- **FileName**: Target filename
- **OutputFolder**: Target directory

## User Experience Improvements

### Before Implementation
1. User manually downloads 7+ files from different sources
2. User must rename files to exact specifications
3. User must place files in correct folder structure
4. High chance of errors and missing files

### After Implementation
1. User gets free API key from Nexus Mods
2. User clicks "Download Files" button
3. User enters API key once
4. User clicks "Start Download"
5. All files automatically downloaded and placed correctly

## Error Handling

### Authentication Errors
- Invalid API key
- Network connectivity issues
- Nexus Mods server problems
- Account suspension or restrictions

### Download Errors
- File not found on Nexus Mods
- Network interruptions
- Insufficient disk space
- File permission issues

## Security Considerations

### API Key Authentication
- User-provided API keys
- Simple copy-paste setup
- HTTPS connections for all requests
- API key validation before use

### File Downloads
- HTTPS connections for all downloads
- File integrity checking (size validation)
- Safe file placement (no path traversal)
- Progress monitoring for stalled downloads

## Future Enhancements

### Potential Improvements
1. **Checksum Verification**: Verify downloaded file integrity
2. **Parallel Downloads**: Download multiple files simultaneously
3. **Resume Support**: Resume interrupted downloads
4. **Cache Management**: Avoid re-downloading existing files
5. **Update Checking**: Check for newer mod versions

### Additional Games
1. **Dark Souls 1**: Add DS1 mod download support
2. **Dark Souls 2**: Add DS2 mod download support
3. **Elden Ring**: Potential future game support

## Developer Notes

### Nexus Mods Registration
The application slug "souls-configurator" needs to be registered with Nexus Mods staff. Contact their Community Team for registration.

### API Rate Limiting
Nexus Mods has rate limits. The implementation includes appropriate delays and retry logic.

### File Naming
The system automatically handles file naming, eliminating user errors with manual file placement.

### Testing
Test with both authenticated and unauthenticated states. Verify error handling for network issues and file permission problems.

## Technical Dependencies

### NuGet Packages
- `System.Text.Json` (8.0.5+): JSON serialization for API responses

### .NET Framework
- Target: .NET 8.0 Windows
- Required: HTTP client support

### External Services
- Nexus Mods API: `https://api.nexusmods.com/v1`
- Direct downloads: Various HTTPS URLs

This implementation significantly improves the user experience by automating the most error-prone part of the mod installation process while maintaining security and providing comprehensive error handling.
