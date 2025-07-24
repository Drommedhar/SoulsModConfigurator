using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SoulsConfigurator.Models
{
    /// <summary>
    /// Represents an enemy entry from valid_new.txt
    /// </summary>
    public class EnemyEntry
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool IsIgnored { get; set; }
        public int Size { get; set; }
        public int Difficulty { get; set; }
        public string Locations { get; set; } = string.Empty;
        public string ValidAI { get; set; } = string.Empty;
        public string Param { get; set; } = string.Empty;
        public string ValidIdleAnimIDs { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets the type name for display purposes
        /// </summary>
        public string TypeName
        {
            get
            {
                return Type switch
                {
                    0 => "Enemy",
                    1 => "Boss",
                    2 => "NPC",
                    _ => "Unknown"
                };
            }
        }
        
        /// <summary>
        /// Gets whether this enemy should be enabled by default (inverse of IsIgnored)
        /// </summary>
        public bool IsEnabledByDefault => !IsIgnored;
        
        /// <summary>
        /// Gets the display name with additional info
        /// </summary>
        public string DisplayName
        {
            get
            {
                var display = $"{Name} ({TypeName})";
                if (!string.IsNullOrEmpty(Comments))
                {
                    display += $" - {Comments}";
                }
                display += $" [Difficulty: {Difficulty}]";
                return display;
            }
        }
    }
    
    /// <summary>
    /// Parser and manager for the valid_new.txt enemy configuration file
    /// </summary>
    public class EnemyConfigManager
    {
        private List<EnemyEntry> _enemies;
        private readonly string _originalFilePath;
        private readonly string _outputFilePath;
        
        public EnemyConfigManager(string originalFilePath, string? outputFilePath = null)
        {
            _originalFilePath = originalFilePath;
            _outputFilePath = outputFilePath ?? originalFilePath;
            _enemies = new List<EnemyEntry>();
        }
        
        /// <summary>
        /// Load enemies from the valid_new.txt file
        /// </summary>
        public void LoadEnemies()
        {
            if (!File.Exists(_originalFilePath))
                throw new FileNotFoundException($"Could not find valid_new.txt at {_originalFilePath}");
            
            var lines = File.ReadAllLines(_originalFilePath);
            _enemies.Clear();
            
            // Skip header line
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                
                var parts = line.Split('\t');
                if (parts.Length < 10)
                    continue;
                
                var enemy = new EnemyEntry
                {
                    ID = parts[0],
                    Name = parts[1],
                    Type = int.Parse(parts[2]),
                    IsIgnored = parts[3] == "1",
                    Size = int.Parse(parts[4]),
                    Difficulty = int.Parse(parts[5]),
                    Locations = parts[6],
                    ValidAI = parts[7],
                    Param = parts[8],
                    ValidIdleAnimIDs = parts[9],
                    Comments = parts.Length > 10 ? parts[10] : ""
                };
                
                _enemies.Add(enemy);
            }
        }
        
        /// <summary>
        /// Get all enemies grouped by type
        /// </summary>
        public Dictionary<string, List<EnemyEntry>> GetEnemiesByType()
        {
            return _enemies
                .GroupBy(e => e.TypeName)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Name).ToList());
        }
        
        /// <summary>
        /// Get all enemies as a flat list
        /// </summary>
        public List<EnemyEntry> GetAllEnemies()
        {
            return _enemies.OrderBy(e => e.Type).ThenBy(e => e.Name).ToList();
        }
        
        /// <summary>
        /// Update the IsIgnored status for an enemy
        /// </summary>
        public void SetEnemyEnabled(string enemyId, bool enabled)
        {
            var enemy = _enemies.FirstOrDefault(e => e.ID == enemyId);
            if (enemy != null)
            {
                enemy.IsIgnored = !enabled; // IsIgnored is the inverse of enabled
            }
        }
        
        /// <summary>
        /// Save the modified enemy configuration back to valid_new.txt
        /// </summary>
        public void SaveConfiguration()
        {
            var lines = new List<string>();
            
            // Add header
            lines.Add("ID\tName\tType\tIsIgnored\tSize\tDifficulty\tLocations\tValidAI\tParam\tValidIdleAnimIDs\tComments");
            
            // Add enemy entries
            foreach (var enemy in _enemies)
            {
                var parts = new[]
                {
                    enemy.ID,
                    enemy.Name,
                    enemy.Type.ToString(),
                    enemy.IsIgnored ? "1" : "0",
                    enemy.Size.ToString(),
                    enemy.Difficulty.ToString(),
                    enemy.Locations,
                    enemy.ValidAI,
                    enemy.Param,
                    enemy.ValidIdleAnimIDs,
                    enemy.Comments
                };
                
                lines.Add(string.Join("\t", parts));
            }
            
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(_outputFilePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            File.WriteAllLines(_outputFilePath, lines);
        }
        
        /// <summary>
        /// Reset all enemies to their default enabled/disabled state
        /// </summary>
        public void ResetToDefaults()
        {
            // Reload from original file to get default settings
            LoadEnemies();
        }
        
        /// <summary>
        /// Enable or disable all enemies of a specific type
        /// </summary>
        public void SetAllEnemiesOfTypeEnabled(int type, bool enabled)
        {
            foreach (var enemy in _enemies.Where(e => e.Type == type))
            {
                enemy.IsIgnored = !enabled;
            }
        }
        
        /// <summary>
        /// Get statistics about current configuration
        /// </summary>
        public (int total, int enabled, int disabled) GetStatistics()
        {
            var total = _enemies.Count;
            var disabled = _enemies.Count(e => e.IsIgnored);
            var enabled = total - disabled;
            
            return (total, enabled, disabled);
        }
        
        /// <summary>
        /// Get statistics by enemy type
        /// </summary>
        public Dictionary<string, (int total, int enabled, int disabled)> GetStatisticsByType()
        {
            return _enemies
                .GroupBy(e => e.TypeName)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        total: g.Count(),
                        enabled: g.Count(e => !e.IsIgnored),
                        disabled: g.Count(e => e.IsIgnored)
                    )
                );
        }
    }
}
