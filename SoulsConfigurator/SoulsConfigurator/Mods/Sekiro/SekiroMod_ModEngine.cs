using SoulsConfigurator.Interfaces;
using System;
using System.IO;
using System.IO.Compression;

namespace SoulsConfigurator.Mods.Sekiro
{
    public class SekiroMod_ModEngine : IMod
    {
        public string Name => "Sekiro Mod Engine";
        public string ModFile => "ModEngine.zip";

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                string sourcePath = Path.Combine("Data", "Sekiro", ModFile);
                if (!File.Exists(sourcePath))
                {
                    return false;
                }

                // Use ZipArchive for selective extraction from subdirectory
                using (var archive = ZipFile.OpenRead(sourcePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        // Skip empty entries and directories
                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        // Check if this is a ModEngine file we want to extract
                        // ModEngine files are typically in a subdirectory and we want the actual files
                        string fileName = Path.GetFileName(entry.FullName);
                        
                        // Extract ModEngine essential files: dinput8.dll, modengine.ini, and mod folder contents
                        if (IsModEngineFile(fileName, entry.FullName))
                        {
                            // Extract to the destination, flattening the directory structure for main files
                            string destinationPath = GetDestinationPath(entry.FullName, destPath);
                            
                            // Create directory if it doesn't exist
                            string? destinationDir = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(destinationDir))
                            {
                                Directory.CreateDirectory(destinationDir);
                            }

                            // Extract the file
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsModEngineFile(string fileName, string fullPath)
        {
            // Check for essential ModEngine files
            if (fileName.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("modengine.ini", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check for mod folder contents
            if (fullPath.Contains("/mod/", StringComparison.OrdinalIgnoreCase) ||
                fullPath.Contains("\\mod\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private string GetDestinationPath(string entryFullName, string destPath)
        {
            string fileName = Path.GetFileName(entryFullName);
            
            // For main ModEngine files (dinput8.dll, modengine.ini), place them directly in game root
            if (fileName.Equals("dinput8.dll", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("modengine.ini", StringComparison.OrdinalIgnoreCase))
            {
                return Path.Combine(destPath, fileName);
            }

            // For mod folder contents, preserve the mod folder structure
            if (entryFullName.Contains("/mod/", StringComparison.OrdinalIgnoreCase) ||
                entryFullName.Contains("\\mod\\", StringComparison.OrdinalIgnoreCase))
            {
                // Find the mod folder part and preserve everything after it
                string[] pathParts = entryFullName.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
                int modIndex = -1;
                
                for (int i = 0; i < pathParts.Length; i++)
                {
                    if (pathParts[i].Equals("mod", StringComparison.OrdinalIgnoreCase))
                    {
                        modIndex = i;
                        break;
                    }
                }

                if (modIndex >= 0 && modIndex < pathParts.Length - 1)
                {
                    // Reconstruct path starting from "mod"
                    string[] modPathParts = new string[pathParts.Length - modIndex];
                    Array.Copy(pathParts, modIndex, modPathParts, 0, modPathParts.Length);
                    return Path.Combine(destPath, Path.Combine(modPathParts));
                }
            }

            // Fallback: place in destination root
            return Path.Combine(destPath, fileName);
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                // ModEngine files to remove
                string[] filesToRemove = {
                    "dinput8.dll",
                    "modengine.ini"
                };

                string[] dirsToRemove = {
                    "mod"
                };

                // Remove files
                foreach (string file in filesToRemove)
                {
                    string fullPath = Path.Combine(destPath, file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                // Remove directories
                foreach (string dir in dirsToRemove)
                {
                    string fullPath = Path.Combine(destPath, dir);
                    if (Directory.Exists(fullPath))
                    {
                        Directory.Delete(fullPath, true);
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}