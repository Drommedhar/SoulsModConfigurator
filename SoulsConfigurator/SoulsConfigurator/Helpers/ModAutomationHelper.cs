using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SoulsConfigurator.Helpers
{
    /// <summary>
    /// Helper class for mod automation and status detection functionality
    /// </summary>
    public static class ModAutomationHelper
    {
        #region Status Detection

        /// <summary>
        /// Waits for DS2 randomization to complete by looking for green success text at the bottom
        /// </summary>
        /// <param name="mainWindow">Main window handle</param>
        /// <param name="maxWaitTimeMs">Maximum wait time in milliseconds (default: 30 seconds)</param>
        /// <param name="checkIntervalMs">Check interval in milliseconds (default: 500ms)</param>
        /// <returns>True if randomization completed successfully, false if failed or timed out</returns>
        public static bool WaitForDS2RandomizationComplete(IntPtr mainWindow, int maxWaitTimeMs = 30000, int checkIntervalMs = 500)
        {
            int elapsedTime = 0;
            System.Diagnostics.Debug.WriteLine("Starting to monitor for DS2 randomization completion...");

            while (elapsedTime < maxWaitTimeMs)
            {
                // For DS2 Randomizer, we need to check for green text at the bottom
                // Since it uses a custom UI, we'll look for specific color patterns in the bottom area
                if (GetWindowRect(mainWindow, out RECT windowRect))
                {
                    int windowWidth = windowRect.right - windowRect.left;
                    int windowHeight = windowRect.bottom - windowRect.top;
                    
                    // Check bottom area for green pixels (indicating success text)
                    if (CheckForGreenTextInBottomArea(mainWindow, windowWidth, windowHeight))
                    {
                        System.Diagnostics.Debug.WriteLine("DS2 Randomization completed successfully (green text detected)");
                        return true;
                    }
                }

                Thread.Sleep(checkIntervalMs);
                elapsedTime += checkIntervalMs;
                
                if (elapsedTime % 5000 == 0) // Log every 5 seconds
                {
                    System.Diagnostics.Debug.WriteLine($"Still waiting for DS2 randomization... {elapsedTime/1000}s elapsed");
                }
            }

            System.Diagnostics.Debug.WriteLine("DS2 Randomization timed out");
            return false;
        }

        /// <summary>
        /// Checks for green text in the bottom area of the DS2 Randomizer window
        /// </summary>
        private static bool CheckForGreenTextInBottomArea(IntPtr windowHandle, int windowWidth, int windowHeight)
        {
            try
            {
                // Get the window rectangle in screen coordinates
                if (!GetWindowRect(windowHandle, out RECT windowRect))
                    return false;

                // Define much smaller capture area - only bottom 60 pixels where green text appears
                int bottomStartY = Math.Max(0, windowHeight - 60); // Only bottom 60 pixels
                int captureWidth = Math.Min(windowWidth, 400); // Max 400 pixels wide
                int captureHeight = windowHeight - bottomStartY;

                System.Diagnostics.Debug.WriteLine($"Capturing area: {captureWidth}x{captureHeight} at y={bottomStartY}");

                // Get screen DC instead of window DC
                IntPtr screenDC = GetDC(IntPtr.Zero);
                if (screenDC == IntPtr.Zero)
                    return false;

                try
                {
                    // Create bitmap to save the captured area
                    using (Bitmap bitmap = new Bitmap(captureWidth, captureHeight))
                    {
                        bool foundGreen = false;
                        int greenPixelCount = 0;

                        // Calculate screen coordinates for the capture area
                        int screenX = windowRect.left;
                        int screenY = windowRect.top + bottomStartY;

                        // Fast sampling - only check every 8th pixel for maximum speed
                        int sampleStep = 8; // Sample every 8th pixel for maximum speed
                        for (int x = 0; x < captureWidth; x += sampleStep)
                        {
                            for (int y = 0; y < captureHeight; y += sampleStep)
                            {
                                // Get pixel from screen coordinates
                                uint pixel = GetPixel(screenDC, screenX + x, screenY + y);
                                
                                // Extract RGB components
                                int red = (int)(pixel & 0xFF);
                                int green = (int)((pixel >> 8) & 0xFF);
                                int blue = (int)((pixel >> 16) & 0xFF);

                                // Check for green text FIRST before bitmap operations
                                // Look for pixels where green is dominant and reasonably bright
                                if (green > 120 && green > red && green > blue && (green - red) > 10 && (green - blue) > 10)
                                {
                                    foundGreen = true;
                                    greenPixelCount++;
                                    System.Diagnostics.Debug.WriteLine($"Found green pixel #{greenPixelCount} at screen({screenX + x},{screenY + y}) window({x},{y + bottomStartY}): R={red}, G={green}, B={blue}");
                                    
                                    // Exit IMMEDIATELY on first green pixel found
                                    System.Diagnostics.Debug.WriteLine($"Green pixel detected, exiting search immediately");
                                    goto ExitLoops;
                                }

                                // Only fill bitmap if we haven't found green yet (for debugging)
                                Color pixelColor = Color.FromArgb(red, green, blue);
                                for (int fx = 0; fx < sampleStep && x + fx < captureWidth; fx++)
                                {
                                    for (int fy = 0; fy < sampleStep && y + fy < captureHeight; fy++)
                                    {
                                        bitmap.SetPixel(x + fx, y + fy, pixelColor);
                                    }
                                }
                            }
                        }
                        
                        ExitLoops:

                        return foundGreen;
                    }
                }
                finally
                {
                    ReleaseDC(IntPtr.Zero, screenDC);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for green text: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Waits for randomization to complete by monitoring status bar color changes
        /// </summary>
        /// <param name="mainWindow">Main window handle</param>
        /// <param name="maxWaitTimeMs">Maximum wait time in milliseconds (default: 30 seconds)</param>
        /// <param name="checkIntervalMs">Check interval in milliseconds (default: 500ms)</param>
        /// <returns>True if randomization completed successfully, false if failed or timed out</returns>
        public static bool WaitForRandomizationComplete(IntPtr mainWindow, int maxWaitTimeMs = 30000, int checkIntervalMs = 500)
        {
            int elapsedTime = 0;

            while (elapsedTime < maxWaitTimeMs)
            {
                // Find the status bar (usually at the bottom of the window)
                IntPtr statusBar = FindStatusBar(mainWindow);
                if (statusBar != IntPtr.Zero)
                {
                    // Check background color instead of text
                    int backColor = GetControlBackgroundColor(statusBar);
                    System.Diagnostics.Debug.WriteLine($"Status bar background color: 0x{backColor:X8}");

                    // Check for completion indicators based on background color
                    if (IsPaleGreen(backColor))
                    {
                        System.Diagnostics.Debug.WriteLine("Randomization completed successfully (green background)");
                        return true;
                    }
                    
                    if (IsIndianRed(backColor))
                    {
                        System.Diagnostics.Debug.WriteLine("Randomization failed (red background)");
                        return false;
                    }
                    
                    // Continue checking if background color is still default/in-progress
                }

                Thread.Sleep(checkIntervalMs);
                elapsedTime += checkIntervalMs;
            }

            // Timeout - assume failure
            System.Diagnostics.Debug.WriteLine("Randomization timed out");
            return false;
        }

        /// <summary>
        /// Gets the background color of a control using pixel analysis
        /// </summary>
        /// <param name="handle">Handle to the control</param>
        /// <returns>Background color as integer</returns>
        public static int GetControlBackgroundColor(IntPtr handle)
        {
            try
            {
                return GetBackgroundColorFromPixelAnalysis(handle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting background color: {ex.Message}");
            }
            
            return 0; // Default/unknown color
        }

        public static int GetBackgroundColorFromPixelAnalysis(IntPtr handle)
        {
            try
            {
                // Get the window rectangle
                if (!GetWindowRect(handle, out RECT rect))
                    return 0;

                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                if (width <= 0 || height <= 0)
                    return 0;

                // Get device context for the window
                IntPtr hdc = GetWindowDC(handle);
                if (hdc == IntPtr.Zero)
                    return 0;

                try
                {
                    // Create a compatible DC and bitmap
                    IntPtr memDC = CreateCompatibleDC(hdc);
                    IntPtr bitmap = CreateCompatibleBitmap(hdc, width, height);
                    IntPtr oldBitmap = SelectObject(memDC, bitmap);

                    try
                    {
                        // Copy the window content to our bitmap
                        if (BitBlt(memDC, 0, 0, width, height, hdc, 0, 0, SRCCOPY))
                        {
                            // Sample multiple areas to get a better background color reading
                            Dictionary<uint, int> colorCounts = new Dictionary<uint, int>();
                            
                            // Sample from multiple regions to avoid text areas
                            SampleRegion(memDC, 5, height / 2, 20, 5, colorCounts);
                            SampleRegion(memDC, width - 25, height / 2, 20, 5, colorCounts);
                            SampleRegion(memDC, width / 2, 2, 20, 3, colorCounts);
                            SampleRegion(memDC, width / 2, height - 3, 20, 2, colorCounts);
                            SampleRegion(memDC, width / 4, height / 2, 10, 3, colorCounts);
                            SampleRegion(memDC, 3 * width / 4, height / 2, 10, 3, colorCounts);
                            
                            // Return the most common color (should be the background)
                            if (colorCounts.Count > 0)
                            {
                                var mostCommonColor = colorCounts.OrderByDescending(kv => kv.Value).First().Key;
                                System.Diagnostics.Debug.WriteLine($"Pixel analysis found background color: 0x{mostCommonColor:X8}");
                                return (int)mostCommonColor;
                            }
                        }
                    }
                    finally
                    {
                        SelectObject(memDC, oldBitmap);
                        DeleteObject(bitmap);
                        DeleteDC(memDC);
                    }
                }
                finally
                {
                    ReleaseDC(handle, hdc);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in pixel analysis: {ex.Message}");
            }

            return 0;
        }

        public static void SampleRegion(IntPtr hdc, int centerX, int centerY, int width, int height, Dictionary<uint, int> colorCounts)
        {
            int startX = Math.Max(0, centerX - width / 2);
            int startY = Math.Max(0, centerY - height / 2);
            int endX = startX + width;
            int endY = startY + height;
            
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    uint color = GetPixel(hdc, x, y);
                    // Filter out obvious text colors (black, very dark, pure white)
                    if (color != 0x000000 && color != 0xFFFFFF && !IsTextColor(color))
                    {
                        colorCounts[color] = colorCounts.GetValueOrDefault(color, 0) + 1;
                    }
                }
            }
        }

        public static bool IsTextColor(uint color)
        {
            // Extract RGB components
            int r = (int)(color & 0xFF);
            int g = (int)((color >> 8) & 0xFF);
            int b = (int)((color >> 16) & 0xFF);
            
            // Check if it's a very dark color (likely text) or very light (likely white text)
            int brightness = (r + g + b) / 3;
            return brightness < 50 || brightness > 240;
        }

        /// <summary>
        /// Checks if the color represents a successful completion (pale green variants)
        /// </summary>
        /// <param name="color">Color value to check</param>
        /// <returns>True if the color indicates success</returns>
        public static bool IsPaleGreen(int color)
        {
            // Extract BGR components (Windows API returns BGR format)
            int b = color & 0xFF;           // Blue
            int g = (color >> 8) & 0xFF;    // Green  
            int r = (color >> 16) & 0xFF;   // Red
            
            System.Diagnostics.Debug.WriteLine($"Testing color for PaleGreen: R={r}, G={g}, B={b} (0x{color:X8})");
            
            // Check for PaleGreen RGB(152, 251, 152) with generous tolerance
            bool isPaleGreen = Math.Abs(r - 152) <= 20 && Math.Abs(g - 251) <= 20 && Math.Abs(b - 152) <= 20;
            
            // Also check for other green variants that might indicate success
            bool isLightGreen = g > r && g > b && g > 150 && (g - r > 50 || g - b > 50);
            
            // Light green variant RGB(144, 238, 144)
            bool isLightGreenVariant = Math.Abs(r - 144) <= 25 && Math.Abs(g - 238) <= 25 && Math.Abs(b - 144) <= 25;
            
            if (isPaleGreen || isLightGreen || isLightGreenVariant)
            {
                System.Diagnostics.Debug.WriteLine($"Detected success green: R={r}, G={g}, B={b}");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if the color represents an error (red variants)
        /// </summary>
        /// <param name="color">Color value to check</param>
        /// <returns>True if the color indicates error</returns>
        public static bool IsIndianRed(int color)
        {
            // Extract BGR components  
            int b = color & 0xFF;           // Blue
            int g = (color >> 8) & 0xFF;    // Green
            int r = (color >> 16) & 0xFF;   // Red
            
            System.Diagnostics.Debug.WriteLine($"Testing color for IndianRed: R={r}, G={g}, B={b} (0x{color:X8})");
            
            // Check for IndianRed RGB(205, 92, 92) with generous tolerance
            bool isIndianRed = Math.Abs(r - 205) <= 20 && Math.Abs(g - 92) <= 20 && Math.Abs(b - 92) <= 20;
            
            // Also check for other red variants that might indicate error
            bool isGeneralRed = r > g && r > b && r > 150 && (r - g > 50 || r - b > 50);
            
            if (isIndianRed || isGeneralRed)
            {
                System.Diagnostics.Debug.WriteLine($"Detected error red: R={r}, G={g}, B={b}");
                return true;
            }
            
            return false;
        }

        #endregion

        #region Control Finding

        /// <summary>
        /// Finds a status bar control in the given window
        /// </summary>
        /// <param name="mainWindow">Main window handle</param>
        /// <returns>Handle to status bar control, or IntPtr.Zero if not found</returns>
        public static IntPtr FindStatusBar(IntPtr mainWindow)
        {
            System.Diagnostics.Debug.WriteLine($"FindStatusBar: Starting search for status bar in window 0x{mainWindow:X8}");

            // Try to find the status bar by class name (Win32 StatusBar)
            IntPtr statusBar = FindWindowEx(mainWindow, IntPtr.Zero, "msctls_statusbar32", null);
            if (statusBar != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Found Win32 status bar (msctls_statusbar32)");
                return statusBar;
            }

            // Alternative: look for StatusStrip (.NET control)
            string[] statusStripClasses = {
                "WindowsForms10.Window.8.app.0.141b42a_r6_ad1",
                "WindowsForms10.Window.8.app.0.*",
                "WindowsForms10.Window.*"
            };

            foreach (string className in statusStripClasses)
            {
                System.Diagnostics.Debug.WriteLine($"Searching for class: {className}");
                
                if (className.Contains("*"))
                {
                    statusBar = FindControlByClassPattern(mainWindow, className.Replace("*", ""));
                }
                else
                {
                    statusBar = FindWindowEx(mainWindow, IntPtr.Zero, className, null);
                }
                
                if (statusBar != IntPtr.Zero && IsStatusControl(statusBar, mainWindow))
                {
                    System.Diagnostics.Debug.WriteLine($"Found status control with class: {className}");
                    return statusBar;
                }
            }

            // Enhanced search: Look for any WindowsForms control that might be a StatusStrip
            statusBar = FindStatusStripEnhanced(mainWindow);
            if (statusBar != IntPtr.Zero)
                return statusBar;

            // Last resort: find any control that might contain status text
            System.Diagnostics.Debug.WriteLine("Falling back to recursive status search");
            return FindStatusBarRecursive(mainWindow);
        }

        public static IntPtr FindControlByClassPattern(IntPtr parent, string classPrefix)
        {
            IntPtr child = FindWindowEx(parent, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                string className = GetClassNameSafe(child);
                if (className.StartsWith(classPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    System.Diagnostics.Debug.WriteLine($"Found control with class pattern: {className}");
                    return child;
                }
                
                IntPtr found = FindControlByClassPattern(child, classPrefix);
                if (found != IntPtr.Zero)
                    return found;

                child = FindWindowEx(parent, child, null, null);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Enhanced search for StatusStrip controls using multiple strategies
        /// </summary>
        /// <param name="parent">Parent window handle</param>
        /// <returns>Handle to StatusStrip control, or IntPtr.Zero if not found</returns>
        public static IntPtr FindStatusStripEnhanced(IntPtr parent)
        {
            System.Diagnostics.Debug.WriteLine("FindStatusStripEnhanced: Starting enhanced StatusStrip search");
            
            // List all child controls and their class names for debugging
            ListAllChildControls(parent);
            
            // Search for controls with "StatusStrip" in their class name or window text
            IntPtr statusStrip = FindControlByNamePattern(parent, "StatusStrip");
            if (statusStrip != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Found StatusStrip by name pattern");
                return statusStrip;
            }

            // Search for controls that might be positioned like a status strip (at bottom)
            statusStrip = FindBottomMostControl(parent);
            if (statusStrip != IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("Found bottom-most control (potential StatusStrip)");
                return statusStrip;
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Lists all child controls for debugging purposes
        /// </summary>
        /// <param name="parent">Parent window handle</param>
        public static void ListAllChildControls(IntPtr parent)
        {
            System.Diagnostics.Debug.WriteLine($"Listing all child controls for window 0x{parent:X8}:");
            IntPtr child = FindWindowEx(parent, IntPtr.Zero, null, null);
            int count = 0;
            
            while (child != IntPtr.Zero && count < 50) // Limit to prevent infinite loops
            {
                string className = GetClassNameSafe(child);
                string windowText = GetWindowTextSafe(child);
                RECT rect;
                GetWindowRect(child, out rect);
                
                System.Diagnostics.Debug.WriteLine($"  [{count}] Handle: 0x{child:X8}, Class: '{className}', Text: '{windowText}', Rect: ({rect.left},{rect.top},{rect.right},{rect.bottom})");
                
                child = FindWindowEx(parent, child, null, null);
                count++;
            }
        }

        /// <summary>
        /// Finds control by searching for pattern in class name or window text
        /// </summary>
        /// <param name="parent">Parent window handle</param>
        /// <param name="pattern">Pattern to search for</param>
        /// <returns>Handle to matching control, or IntPtr.Zero if not found</returns>
        public static IntPtr FindControlByNamePattern(IntPtr parent, string pattern)
        {
            IntPtr child = FindWindowEx(parent, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                string className = GetClassNameSafe(child);
                string windowText = GetWindowTextSafe(child);
                
                if (className.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    windowText.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Found control by pattern '{pattern}': Class='{className}', Text='{windowText}'");
                    return child;
                }
                
                // Recursively search child controls
                IntPtr found = FindControlByNamePattern(child, pattern);
                if (found != IntPtr.Zero)
                    return found;

                child = FindWindowEx(parent, child, null, null);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Finds the bottom-most control that could be a status strip
        /// </summary>
        /// <param name="parent">Parent window handle</param>
        /// <returns>Handle to bottom-most control, or IntPtr.Zero if not found</returns>
        public static IntPtr FindBottomMostControl(IntPtr parent)
        {
            if (!GetWindowRect(parent, out RECT parentRect))
                return IntPtr.Zero;
                
            IntPtr bottomMostControl = IntPtr.Zero;
            int bottomMostY = parentRect.top;
            
            IntPtr child = FindWindowEx(parent, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                if (GetWindowRect(child, out RECT childRect))
                {
                    // Check if this control is positioned at the bottom of the parent
                    int parentHeight = parentRect.bottom - parentRect.top;
                    int controlBottom = childRect.bottom - parentRect.top;
                    
                    // Consider it a status control if it's in the bottom 25% and has reasonable height
                    int controlHeight = childRect.bottom - childRect.top;
                    if (controlBottom > (parentHeight * 0.75) && 
                        controlHeight > 10 && controlHeight < 50 && 
                        childRect.bottom > bottomMostY)
                    {
                        bottomMostY = childRect.bottom;
                        bottomMostControl = child;
                        
                        string className = GetClassNameSafe(child);
                        System.Diagnostics.Debug.WriteLine($"Found potential bottom control: Class='{className}', Height={controlHeight}, Bottom={controlBottom}");
                    }
                }
                child = FindWindowEx(parent, child, null, null);
            }
            
            return bottomMostControl;
        }

        public static bool IsStatusControl(IntPtr control, IntPtr mainWindow)
        {
            try
            {
                RECT mainRect, controlRect;
                if (GetWindowRect(mainWindow, out mainRect) && GetWindowRect(control, out controlRect))
                {
                    int mainHeight = mainRect.bottom - mainRect.top;
                    int controlBottom = controlRect.bottom - mainRect.top;
                    int controlHeight = controlRect.bottom - controlRect.top;
                    
                    // Check if the control is in the bottom portion of the window
                    bool isBottomPositioned = controlBottom > (mainHeight * 0.75);
                    
                    // Check if the control has a reasonable height for a status strip (typically 20-50 pixels)
                    bool hasReasonableHeight = controlHeight >= 15 && controlHeight <= 60;
                    
                    string className = GetClassNameSafe(control);
                    System.Diagnostics.Debug.WriteLine($"IsStatusControl: Class='{className}', Height={controlHeight}, BottomPos={isBottomPositioned}, ReasonableHeight={hasReasonableHeight}");
                    
                    return isBottomPositioned && hasReasonableHeight;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in IsStatusControl: {ex.Message}");
                // If we can't determine position, assume it's a status control
            }
            
            return true;
        }

        public static IntPtr FindStatusBarRecursive(IntPtr parent)
        {
            System.Diagnostics.Debug.WriteLine($"FindStatusBarRecursive: Searching in window 0x{parent:X8}");
            
            IntPtr child = FindWindowEx(parent, IntPtr.Zero, null, null);
            while (child != IntPtr.Zero)
            {
                string className = GetClassNameSafe(child);
                string windowText = GetWindowTextSafe(child);

                System.Diagnostics.Debug.WriteLine($"  Checking child: Class='{className}', Text='{windowText}'");

                // Check for status-related class names
                if (className.ToLower().Contains("status") || 
                    className.ToLower().Contains("toolstrip") ||
                    className.ToLower().Contains("windowsforms"))
                {
                    System.Diagnostics.Debug.WriteLine($"  Found potential status control by class: {className}");
                    return child;
                }

                // Check for status-related text content
                if (windowText.ToLower().Contains("done") ||
                    windowText.ToLower().Contains("error") ||
                    windowText.ToLower().Contains("complete") ||
                    windowText.ToLower().Contains("status") ||
                    windowText.ToLower().Contains("ready") ||
                    windowText.ToLower().Contains("processing"))
                {
                    System.Diagnostics.Debug.WriteLine($"  Found potential status control by text: {windowText}");
                    return child;
                }

                IntPtr found = FindStatusBarRecursive(child);
                if (found != IntPtr.Zero)
                    return found;

                child = FindWindowEx(parent, child, null, null);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Finds a control by its text content
        /// </summary>
        /// <param name="parent">Parent window handle</param>
        /// <param name="text">Text to search for</param>
        /// <returns>Handle to the control, or IntPtr.Zero if not found</returns>
        public static IntPtr FindControlByText(IntPtr parent, string text)
        {
            return FindWindowEx(parent, IntPtr.Zero, null, text);
        }

        /// <summary>
        /// Gets the class name of a window/control safely
        /// </summary>
        /// <param name="handle">Handle to the window/control</param>
        /// <returns>Class name as string</returns>
        public static string GetClassNameSafe(IntPtr handle)
        {
            StringBuilder className = new StringBuilder(256);
            GetClassName(handle, className, className.Capacity);
            return className.ToString();
        }

        /// <summary>
        /// Gets the window text safely
        /// </summary>
        /// <param name="handle">Handle to the window/control</param>
        /// <returns>Window text as string</returns>
        public static string GetWindowTextSafe(IntPtr handle)
        {
            int length = GetWindowTextLength(handle);
            if (length == 0) return string.Empty;
            
            StringBuilder text = new StringBuilder(length + 1);
            GetWindowText(handle, text, text.Capacity);
            return text.ToString();
        }

        /// <summary>
        /// Performs a mouse click at the specified coordinates relative to a window
        /// </summary>
        /// <param name="windowHandle">Handle to the target window</param>
        /// <param name="x">X coordinate relative to the window</param>
        /// <param name="y">Y coordinate relative to the window</param>
        /// <returns>True if the click was performed successfully</returns>
        public static bool ClickAtCoordinates(IntPtr windowHandle, int x, int y)
        {
            try
            {
                // Convert window-relative coordinates to screen coordinates
                POINT point = new POINT { x = x, y = y };
                if (!ClientToScreen(windowHandle, ref point))
                    return false;

                // Move cursor to the target position
                SetCursorPos(point.x, point.y);
                Thread.Sleep(50); // Small delay to ensure cursor movement

                // Perform left mouse button down and up
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ClickAtCoordinates error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Win32 API

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string? className, string? windowTitle);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public const uint SRCCOPY = 0x00CC0020;
        public const uint WM_CLOSE = 0x0010;
        public const uint BM_CLICK = 0x00F5;
        
        // Mouse event constants
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        #endregion

        public static bool ModifyModEngineIni(string gamePath, string modFolder)
        {
            var final = Path.Combine(gamePath, "modengine.ini");
            if(!File.Exists(final))
            {
                return false;
            }

            EditValues(final, "modOverrideDirectory", $"modOverrideDirectory=\"\\{modFolder}\"");

            return true;
        }

        static void EditValues(string path, string nameToFind, string newLine)
        {
            string[] lines = File.ReadAllLines(path);
            int index = Array.FindIndex(lines, line => line.StartsWith(nameToFind));
            lines[index] = newLine;
            File.WriteAllLines(path, lines);
        }
    }
}