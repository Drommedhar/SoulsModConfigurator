# SSO Authentication Fixes

## Issue Identified
The SSO authentication was failing immediately before the user could authorize in the browser. The error logs showed "Authentication failed: Unknown error" messages.

## Root Cause Analysis
After analyzing the Vortex source code from the Nexus-Mods/Vortex GitHub repository, I identified several issues with our SSO implementation:

1. **Application ID Format**: We were using mixed casing and the wrong format
2. **Response Field Names**: Missing support for snake_case field names used by the API
3. **Two-Step Authentication Process**: The SSO flow requires handling a connection token first, then the API key
4. **Error Handling**: Too aggressive failure on non-success responses

## Fixes Applied

### 1. Corrected Application Identifiers
```csharp
// Before
private const string APPLICATION_SLUG = "vortex";
var authUrl = $"https://www.nexusmods.com/sso?id={uuid}&application={APPLICATION_SLUG}";

// After  
private const string APPLICATION_SLUG = "Vortex";
var authUrl = $"https://www.nexusmods.com/sso?id={uuid}&application=vortex";
```

### 2. Enhanced Response Models
Added support for snake_case JSON field names used by the Nexus Mods API:
```csharp
public class SSOData
{
    public string? ApiKey { get; set; }
    public string? Token { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("api_key")]
    public string? Api_Key { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("connection_token")]
    public string? Connection_Token { get; set; }
}
```

### 3. Implemented Two-Step Authentication Process
The SSO flow works as follows:
1. **Initial Request**: Send WebSocket message with UUID and no token
2. **Browser Opens**: User authenticates in browser
3. **Connection Token**: Server responds with `connection_token`
4. **Follow-up Request**: Send second WebSocket message with the connection token
5. **API Key**: Server responds with the final `api_key`

### 4. Improved Error Handling
- Added JSON parsing error handling that continues listening instead of failing
- Better debugging with raw response logging
- Proper handling of intermediate responses (connection tokens)
- Only fail on explicit error responses

### 5. Enhanced Debug Logging
Added comprehensive debug logging to help diagnose issues:
```csharp
System.Diagnostics.Debug.WriteLine($"SSO Response: {responseJson}");
System.Diagnostics.Debug.WriteLine($"Received connection token: {connectionToken}");
System.Diagnostics.Debug.WriteLine($"Sent follow-up request: {followUpJson}");
```

## Technical Details

### WebSocket Message Flow
1. **Initial Message**:
   ```json
   {
     "id": "uuid-here",
     "appid": "Vortex", 
     "token": null,
     "protocol": 2
   }
   ```

2. **Server Response (Step 1)**:
   ```json
   {
     "success": true,
     "data": {
       "connection_token": "token-here"
     }
   }
   ```

3. **Follow-up Message**:
   ```json
   {
     "id": "uuid-here",
     "appid": "Vortex",
     "token": "connection-token-from-step-1", 
     "protocol": 2
   }
   ```

4. **Server Response (Final)**:
   ```json
   {
     "success": true,
     "data": {
       "api_key": "final-api-key-here"
     }
   }
   ```

### Key Insights from Vortex Source
- Uses "Vortex" as appid in WebSocket messages
- Uses "vortex" in browser URL
- Handles connection tokens as intermediate step
- Continues listening until receiving api_key
- Uses proper JSON field name mapping

## Expected Outcome
The SSO authentication should now work correctly:
1. Browser opens to Nexus Mods SSO page
2. User can complete authentication
3. Application receives API key via WebSocket
4. Download functionality becomes available

## Testing Notes
- Check Debug output window for detailed SSO message flow
- Authentication should complete after user authorizes in browser
- Any remaining issues will be logged with detailed context
