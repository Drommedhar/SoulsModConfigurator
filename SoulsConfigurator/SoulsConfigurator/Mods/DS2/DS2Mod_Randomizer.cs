using SoulsConfigurator.Helpers;
using SoulsConfigurator.Interfaces;
using SoulsConfigurator.Models;
using SoulsConfigurator.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace SoulsConfigurator.Mods.DS2
{
    public class DS2Mod_Randomizer : IMod, IConfigurableMod
    {
        public string Name => "DS2 Item & Enemy Randomizer";
        public string ModFile => "DS2SRandomizer.zip";

        private ModConfiguration? _configuration;
        private Dictionary<string, object>? _savedConfiguration;
        private string? _selectedPreset;
        private string? _executablePath;
        private readonly UserPresetService _presetService;

        public DS2Mod_Randomizer()
        {
            _presetService = new UserPresetService();
            InitializeConfiguration();
        }

        private void InitializeConfiguration()
        {
            _configuration = new ModConfiguration
            {
                ModName = Name,
                ExecutablePath = "DS2SRandomizer.exe",
                WindowTitle = "DS2 Item & Enemy Randomizer",
                Options = new List<ModConfigurationOption>
                {
                    // General Options
                    new ModConfigurationOption
                    {
                        Name = "seed",
                        DisplayName = "Seed",
                        Description = "Fixed seed for reproducible results",
                        ControlType = ModControlType.TextBox,
                        ControlName = "seed",
                        DefaultValue = "",
                        GroupName = "General"
                    },

                    // === ENEMY RANDOMIZER TAB ===

                    // Enemies Section
                    new ModConfigurationOption
                    {
                        Name = "randomize_enemies",
                        DisplayName = "Randomize enemies",
                        Description = "Enable enemy randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_enemies",
                        DefaultValue = true,
                        GroupName = "Enemies",
                        TabName = "Enemy",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "remove_invisible",
                        DisplayName = "Remove invisible enemies",
                        Description = "Remove enemies that are invisible or hard to see",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "remove_invisible",
                        DefaultValue = false,
                        GroupName = "Enemies",
                        TabName = "Enemy",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "randomize_boss_ads",
                        DisplayName = "Randomize boss ads",
                        Description = "Randomize additional enemies that spawn during boss fights",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_boss_ads",
                        DefaultValue = false,
                        GroupName = "Enemies",
                        TabName = "Enemy",
                        Order = 3
                    },

                    // Invaders Section
                    new ModConfigurationOption
                    {
                        Name = "randomize_invaders",
                        DisplayName = "Randomize invaders",
                        Description = "Enable invader randomization",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "randomize_invaders",
                        DefaultValue = true,
                        GroupName = "Invaders",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "randomize_invaders", "replace_invaders", "remove_invaders" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "replace_invaders",
                        DisplayName = "Replace with enemies",
                        Description = "Replace invaders with regular enemies",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "replace_invaders",
                        DefaultValue = false,
                        GroupName = "Invaders",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "randomize_invaders", "replace_invaders", "remove_invaders" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "remove_invaders",
                        DisplayName = "Remove invaders",
                        Description = "Remove all invaders from the game",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "remove_invaders",
                        DefaultValue = false,
                        GroupName = "Invaders",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "randomize_invaders", "replace_invaders", "remove_invaders" },
                        Order = 3
                    },

                    // Summons Section
                    new ModConfigurationOption
                    {
                        Name = "dont_replace_summons",
                        DisplayName = "Don't replace summons",
                        Description = "Keep friendly summons unchanged",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "dont_replace_summons",
                        DefaultValue = false,
                        GroupName = "Summons",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "dont_replace_summons", "replace_summons_fun", "remove_summons" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "replace_summons_fun",
                        DisplayName = "Replace with enemies (Fun)",
                        Description = "Replace friendly summons with enemies for added challenge",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "replace_summons_fun",
                        DefaultValue = false,
                        GroupName = "Summons",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "dont_replace_summons", "replace_summons_fun", "remove_summons" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "remove_summons",
                        DisplayName = "Remove summons",
                        Description = "Remove all friendly summons from the game",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "remove_summons",
                        DefaultValue = true,
                        GroupName = "Summons",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "dont_replace_summons", "replace_summons_fun", "remove_summons" },
                        Order = 3
                    },

                    // Enemy scaling
                    new ModConfigurationOption
                    {
                        Name = "enemy_scaling",
                        DisplayName = "Enemy scaling",
                        Description = "Scale enemy health and damage based on location",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "enemy_scaling",
                        DefaultValue = true,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 1
                    },

                    // Wandering bosses
                    new ModConfigurationOption
                    {
                        Name = "wandering_bosses",
                        DisplayName = "Wandering bosses",
                        Description = "Enable wandering bosses in regular areas",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "wandering_bosses",
                        DefaultValue = false,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "boss_chance",
                        DisplayName = "Boss chance%",
                        Description = "Percentage chance for wandering bosses to appear (0-100)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "boss_chance",
                        DefaultValue = 1,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } },
                        EnabledWhen = "wandering_bosses",
                        EnabledWhenValue = true,
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "respawn_wandering_bosses",
                        DisplayName = "Respawn wandering bosses",
                        Description = "Allow wandering bosses to respawn",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "respawn_wandering_bosses",
                        DefaultValue = true,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        EnabledWhen = "wandering_bosses",
                        EnabledWhenValue = true,
                        Order = 4
                    },

                    // Additional enemy options
                    new ModConfigurationOption
                    {
                        Name = "randomize_enemy_locations",
                        DisplayName = "Randomize enemy locations",
                        Description = "Randomize where enemies appear in the world",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_enemy_locations",
                        DefaultValue = false,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "npc_randomization",
                        DisplayName = "NPC randomization",
                        Description = "Include NPCs in enemy randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "npc_randomization",
                        DefaultValue = true,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "rainbow_enemies",
                        DisplayName = "Rainbow enemies",
                        Description = "Give enemies random colors",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "rainbow_enemies",
                        DefaultValue = false,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "write_cheatsheet_enemy",
                        DisplayName = "Write cheatsheet",
                        Description = "Generate a cheatsheet showing enemy placements",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "write_cheatsheet_enemy",
                        DefaultValue = true,
                        GroupName = "Enemy Options",
                        TabName = "Enemy",
                        Order = 8
                    },

                    // === ZONES & ENEMY LIMIT TAB ===

                    // Zone Limits
                    new ModConfigurationOption
                    {
                        Name = "things_betwixt_limit",
                        DisplayName = "Things Betwixt",
                        Description = "Enemy limit for Things Betwixt (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "things_betwixt_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "majula_limit",
                        DisplayName = "Majula",
                        Description = "Enemy limit for Majula (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "majula_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "forest_of_fallen_giants_limit",
                        DisplayName = "Forest Of Fallen Giants",
                        Description = "Enemy limit for Forest Of Fallen Giants (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "forest_of_fallen_giants_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "brightstone_cove_tseldora_limit",
                        DisplayName = "Brightstone Cove Tseldora",
                        Description = "Enemy limit for Brightstone Cove Tseldora (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "brightstone_cove_tseldora_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "aldias_keep_limit",
                        DisplayName = "Aldia's Keep",
                        Description = "Enemy limit for Aldia's Keep (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "aldias_keep_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "lost_bastille_belfry_luna_limit",
                        DisplayName = "The Lost Bastille & Belfry Luna",
                        Description = "Enemy limit for The Lost Bastille & Belfry Luna (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "lost_bastille_belfry_luna_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "harvest_valley_earthen_peak_limit",
                        DisplayName = "Harvest Valley & Earthen Peak",
                        Description = "Enemy limit for Harvest Valley & Earthen Peak (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "harvest_valley_earthen_peak_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "no_mans_wharf_limit",
                        DisplayName = "NO-man's Wharf",
                        Description = "Enemy limit for NO-man's Wharf (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "no_mans_wharf_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 8
                    },
                    new ModConfigurationOption
                    {
                        Name = "iron_keep_belfry_sol_limit",
                        DisplayName = "Iron Keep & Belfry Sol",
                        Description = "Enemy limit for Iron Keep & Belfry Sol (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "iron_keep_belfry_sol_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 9
                    },
                    new ModConfigurationOption
                    {
                        Name = "huntsmans_copse_undead_purgatory_limit",
                        DisplayName = "Huntsman's Copse & Undead Purgatory",
                        Description = "Enemy limit for Huntsman's Copse & Undead Purgatory (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "huntsmans_copse_undead_purgatory_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 10
                    },
                    new ModConfigurationOption
                    {
                        Name = "gutter_black_gulch_limit",
                        DisplayName = "The Gutter & Black Gulch",
                        Description = "Enemy limit for The Gutter & Black Gulch (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "gutter_black_gulch_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 11
                    },
                    new ModConfigurationOption
                    {
                        Name = "dragon_aerie_shrine_limit",
                        DisplayName = "Dragon Aerie & Shrine",
                        Description = "Enemy limit for Dragon Aerie & Shrine (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "dragon_aerie_shrine_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 12
                    },
                    new ModConfigurationOption
                    {
                        Name = "majula_shaded_woods_limit",
                        DisplayName = "Majula <=> Shaded Woods",
                        Description = "Enemy limit for Majula <=> Shaded Woods (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "majula_shaded_woods_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 13
                    },
                    new ModConfigurationOption
                    {
                        Name = "heides_tower_no_mans_wharf_limit",
                        DisplayName = "Heide's Tower <=> No-man's Wharf",
                        Description = "Enemy limit for Heide's Tower <=> No-man's Wharf (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "heides_tower_no_mans_wharf_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 14
                    },
                    new ModConfigurationOption
                    {
                        Name = "heides_tower_of_flame_limit",
                        DisplayName = "Heide's Tower of Flame",
                        Description = "Enemy limit for Heide's Tower of Flame (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "heides_tower_of_flame_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 15
                    },
                    new ModConfigurationOption
                    {
                        Name = "shaded_woods_shrine_of_winter_limit",
                        DisplayName = "Shaded Woods & Shrine of Winter",
                        Description = "Enemy limit for Shaded Woods & Shrine of Winter (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "shaded_woods_shrine_of_winter_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 16
                    },
                    new ModConfigurationOption
                    {
                        Name = "doors_of_pharros_limit",
                        DisplayName = "Doors of Pharros",
                        Description = "Enemy limit for Doors of Pharros (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "doors_of_pharros_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 17
                    },
                    new ModConfigurationOption
                    {
                        Name = "grave_of_saints_limit",
                        DisplayName = "Grave of Saints",
                        Description = "Enemy limit for Grave of Saints (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "grave_of_saints_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 18
                    },
                    new ModConfigurationOption
                    {
                        Name = "giant_memories_limit",
                        DisplayName = "Giant Memories",
                        Description = "Enemy limit for Giant Memories (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "giant_memories_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 19
                    },
                    new ModConfigurationOption
                    {
                        Name = "shrine_of_amana_limit",
                        DisplayName = "Shrine of Amana",
                        Description = "Enemy limit for Shrine of Amana (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "shrine_of_amana_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 20
                    },
                    new ModConfigurationOption
                    {
                        Name = "drangleic_castle_throne_limit",
                        DisplayName = "Drangleic Castle & Throne",
                        Description = "Enemy limit for Drangleic Castle & Throne (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "drangleic_castle_throne_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 21
                    },
                    new ModConfigurationOption
                    {
                        Name = "undead_crypt_limit",
                        DisplayName = "Undead Crypt",
                        Description = "Enemy limit for Undead Crypt (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "undead_crypt_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 22
                    },
                    new ModConfigurationOption
                    {
                        Name = "dragon_memories_limit",
                        DisplayName = "Dragon Memories",
                        Description = "Enemy limit for Dragon Memories (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "dragon_memories_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 23
                    },
                    new ModConfigurationOption
                    {
                        Name = "dark_chasm_of_old_limit",
                        DisplayName = "Dark Chasm of Old",
                        Description = "Enemy limit for Dark Chasm of Old (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "dark_chasm_of_old_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 24
                    },
                    new ModConfigurationOption
                    {
                        Name = "shulvar_sanctum_city_limit",
                        DisplayName = "Shulvar Sanctum City",
                        Description = "Enemy limit for Shulvar Sanctum City (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "shulvar_sanctum_city_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 25
                    },
                    new ModConfigurationOption
                    {
                        Name = "brume_tower_limit",
                        DisplayName = "Brume Tower",
                        Description = "Enemy limit for Brume Tower (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "brume_tower_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 26
                    },
                    new ModConfigurationOption
                    {
                        Name = "frozen_eleum_loyce_limit",
                        DisplayName = "Frozen Eleum Loyce",
                        Description = "Enemy limit for Frozen Eleum Loyce (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "frozen_eleum_loyce_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 27
                    },
                    new ModConfigurationOption
                    {
                        Name = "memory_of_the_king_limit",
                        DisplayName = "Memory of the King",
                        Description = "Enemy limit for Memory of the King (0-99)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "memory_of_the_king_limit",
                        DefaultValue = 99,
                        GroupName = "Zone Limits",
                        TabName = "Zones",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 99 } },
                        Order = 28
                    }, 

                    // === ENEMY SELECTION TAB ===

                    // Bosses
                    new ModConfigurationOption
                    {
                        Name = "select_skeleton_lord",
                        DisplayName = "Skeleton Lord (1540)",
                        Description = "Include Skeleton Lord in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_skeleton_lord",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_royal_rat_vanguard",
                        DisplayName = "Royal Rat Vanguard (2261)",
                        Description = "Include Royal Rat Vanguard in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_royal_rat_vanguard",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_guardian_dragon",
                        DisplayName = "Guardian Dragon (2120)",
                        Description = "Include Guardian Dragon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_guardian_dragon",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_flexile_sentry",
                        DisplayName = "Flexile Sentry (3033)",
                        Description = "Include Flexile Sentry in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_flexile_sentry",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_smelter_demon",
                        DisplayName = "Smelter Demon (3050)",
                        Description = "Include Smelter Demon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_smelter_demon",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_blue_smelter_demon",
                        DisplayName = "Blue Smelter Demon (3052)",
                        Description = "Include Blue Smelter Demon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_blue_smelter_demon",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_last_giant",
                        DisplayName = "Last Giant (3096)",
                        Description = "Include Last Giant in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_last_giant",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_giant_lord",
                        DisplayName = "Giant Lord (3097)",
                        Description = "Include Giant Lord in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_giant_lord",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 8
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_the_pursuer",
                        DisplayName = "The Pursuer (3180)",
                        Description = "Include The Pursuer in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_the_pursuer",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 9
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_belfry_gargoyle",
                        DisplayName = "Belfry Gargoyle (3240)",
                        Description = "Include Belfry Gargoyle in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_belfry_gargoyle",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 10
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_ruin_sentinel",
                        DisplayName = "Ruin Sentinel (3250)",
                        Description = "Include Ruin Sentinel in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_ruin_sentinel",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 11
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_the_rotten",
                        DisplayName = "The Rotten (3260)",
                        Description = "Include The Rotten in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_the_rotten",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 12
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_throne_defender",
                        DisplayName = "Throne Defender (3320)",
                        Description = "Include Throne Defender in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_throne_defender",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 13
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_throne_watcher",
                        DisplayName = "Throne Watcher (3340)",
                        Description = "Include Throne Watcher in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_throne_watcher",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 14
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_velstadt",
                        DisplayName = "Velstadt the Royal Aegis (3330)",
                        Description = "Include Velstadt the Royal Aegis in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_velstadt",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 15
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_covetous_demon",
                        DisplayName = "Covetous Demon (5000)",
                        Description = "Include Covetous Demon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_covetous_demon",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 16
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_mytha",
                        DisplayName = "Mytha the Baneful Queen (5010)",
                        Description = "Include Mytha the Baneful Queen in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_mytha",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 17
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_scorpioness_najka",
                        DisplayName = "Scorpioness Najka (5030)",
                        Description = "Include Scorpioness Najka in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_scorpioness_najka",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 18
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_looking_glass_knight",
                        DisplayName = "Looking Glass Knight (5040)",
                        Description = "Include Looking Glass Knight in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_looking_glass_knight",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 19
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_darklurker",
                        DisplayName = "Darklurker (5061)",
                        Description = "Include Darklurker in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_darklurker",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 20
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_vendrick",
                        DisplayName = "Vendrick (5146)",
                        Description = "Include Vendrick in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_vendrick",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 21
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_ancient_dragon",
                        DisplayName = "Ancient Dragon (6000)",
                        Description = "Include Ancient Dragon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_ancient_dragon",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 22
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_demon_of_song",
                        DisplayName = "Demon of Song (6020)",
                        Description = "Include Demon of Song in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_demon_of_song",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 23
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_dukes_dear_freja",
                        DisplayName = "The Duke's Dear Freja (6030)",
                        Description = "Include The Duke's Dear Freja in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_dukes_dear_freja",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 24
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_old_iron_king",
                        DisplayName = "Old Iron King (6070)",
                        Description = "Include Old Iron King in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_old_iron_king",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 25
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_dragonrider",
                        DisplayName = "Dragonrider (6115)",
                        Description = "Include Dragonrider in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_dragonrider",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 26
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_executioners_chariot",
                        DisplayName = "Executioner's Chariot (6191)",
                        Description = "Include Executioner's Chariot in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_executioners_chariot",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 27
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_old_dragonslayer",
                        DisplayName = "Old Dragonslayer (6250)",
                        Description = "Include Old Dragonslayer in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_old_dragonslayer",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 28
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_lost_sinner",
                        DisplayName = "The Lost Sinner (6260)",
                        Description = "Include The Lost Sinner in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_lost_sinner",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 29
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_nashandra",
                        DisplayName = "Nashandra (6270)",
                        Description = "Include Nashandra in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_nashandra",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 30
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_royal_rat_authority",
                        DisplayName = "Royal Rat Authority (6280)",
                        Description = "Include Royal Rat Authority in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_royal_rat_authority",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 31
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_fume_knight",
                        DisplayName = "Fume Knight (6750)",
                        Description = "Include Fume Knight in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_fume_knight",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 32
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_lud_zallen",
                        DisplayName = "Lud/Zallen the King's pets (6790)",
                        Description = "Include Lud/Zallen the King's pets in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_lud_zallen",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 33
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_aava",
                        DisplayName = "Aava the King's pet (6791)",
                        Description = "Include Aava the King's pet in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_aava",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 34
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_sir_alonne",
                        DisplayName = "Sir Alonne (6800)",
                        Description = "Include Sir Alonne in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_sir_alonne",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 35
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_sinh",
                        DisplayName = "Sinh the Slumbering Dragon (6810)",
                        Description = "Include Sinh the Slumbering Dragon in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_sinh",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 36
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_elana",
                        DisplayName = "Elana Squalid Queen (6820)",
                        Description = "Include Elana Squalid Queen in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_elana",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 37
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_burnt_ivory_king",
                        DisplayName = "Burnt Ivory King (6900)",
                        Description = "Include Burnt Ivory King in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_burnt_ivory_king",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 38
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_aldia",
                        DisplayName = "Aldia Scholar of the First Sin (6920)",
                        Description = "Include Aldia Scholar of the First Sin in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_aldia",
                        DefaultValue = true,
                        GroupName = "Bosses",
                        TabName = "Enemy Selection",
                        Order = 39
                    },

                    // Enemies I
                    new ModConfigurationOption
                    {
                        Name = "select_forest_grotesque",
                        DisplayName = "Forest Grotesque (1000)",
                        Description = "Include Forest Grotesque in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_forest_grotesque",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_kobold",
                        DisplayName = "Kobold (1010)",
                        Description = "Include Kobold in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_kobold",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_soldier",
                        DisplayName = "Hollow Soldier (1020)",
                        Description = "Include Hollow Soldier in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_soldier",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_royal_soldier",
                        DisplayName = "Royal Soldier (1021)",
                        Description = "Include Royal Soldier in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_royal_soldier",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_infantry",
                        DisplayName = "Hollow Infantry (1030)",
                        Description = "Include Hollow Infantry in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_infantry",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_drangleic_infantry",
                        DisplayName = "Drangleic Infantry (1031)",
                        Description = "Include Drangleic Infantry in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_drangleic_infantry",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_amana_shrine_maiden",
                        DisplayName = "Amana Shrine Maiden (1050)",
                        Description = "Include Amana Shrine Maiden in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_amana_shrine_maiden",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_priest",
                        DisplayName = "Hollow Priest (1060)",
                        Description = "Include Hollow Priest in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_priest",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 8
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_priestess",
                        DisplayName = "Hollow Priestess (1062)",
                        Description = "Include Hollow Priestess in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_priestess",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 9
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_parasitized_undead",
                        DisplayName = "Parasitized Undead (1070)",
                        Description = "Include Parasitized Undead in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_parasitized_undead",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 10
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_rogue",
                        DisplayName = "Hollow Rogue (1080)",
                        Description = "Include Hollow Rogue in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_rogue",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 11
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_varangian_sailor",
                        DisplayName = "Varangian Sailor (1130)",
                        Description = "Include Varangian Sailor in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_varangian_sailor",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 12
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_stone_soldier",
                        DisplayName = "Stone Soldier (1170)",
                        Description = "Include Stone Soldier in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_stone_soldier",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 13
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_black_hollow_mage",
                        DisplayName = "Black Hollow Mage (1180)",
                        Description = "Include Black Hollow Mage in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_black_hollow_mage",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 14
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_white_hollow_mage",
                        DisplayName = "White Hollow Mage (1182)",
                        Description = "Include White Hollow Mage in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_white_hollow_mage",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 15
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_giant",
                        DisplayName = "Giant (1210)",
                        Description = "Include Giant in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_giant",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 16
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_suspicious_shadow",
                        DisplayName = "Suspicious Shadow (1230)",
                        Description = "Include Suspicious Shadow in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_suspicious_shadow",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 17
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_manikin",
                        DisplayName = "Manikin (1240)",
                        Description = "Include Manikin in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_manikin",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 18
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_rupturing_hollow",
                        DisplayName = "Rupturing Hollow (1250)",
                        Description = "Include Rupturing Hollow in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_rupturing_hollow",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 19
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_hollow_prisoner",
                        DisplayName = "Hollow Prisoner (1270)",
                        Description = "Include Hollow Prisoner in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_hollow_prisoner",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 20
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_frozen_hollow_prisoner",
                        DisplayName = "Frozen Hollow Prisoner (1271)",
                        Description = "Include Frozen Hollow Prisoner in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_frozen_hollow_prisoner",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 21
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_forest_spirit",
                        DisplayName = "Forest Spirit (1290)",
                        Description = "Include Forest Spirit in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_forest_spirit",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 22
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_frozen_forest_spirit",
                        DisplayName = "Frozen Forest Spirit (1292)",
                        Description = "Include Frozen Forest Spirit in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_frozen_forest_spirit",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 23
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_lindelt_cleric",
                        DisplayName = "Lindelt Cleric (1310)",
                        Description = "Include Lindelt Cleric in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_lindelt_cleric",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 24
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_skeleton",
                        DisplayName = "Skeleton (1320)",
                        Description = "Include Skeleton in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_skeleton",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 25
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_bonewheel_skeleton",
                        DisplayName = "Bonewheel Skeleton (1330)",
                        Description = "Include Bonewheel Skeleton in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_bonewheel_skeleton",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 26
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_grym",
                        DisplayName = "Grym (1340)",
                        Description = "Include Grym in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_grym",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 27
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_grym_warrior",
                        DisplayName = "Grym Warrior (1350)",
                        Description = "Include Grym Warrior in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_grym_warrior",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 28
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_prowling_magus",
                        DisplayName = "Prowling Magus (1370)",
                        Description = "Include Prowling Magus in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_prowling_magus",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 29
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_torturer",
                        DisplayName = "Torturer (1380)",
                        Description = "Include Torturer in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_torturer",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 30
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_artificial_undead",
                        DisplayName = "Artificial Undead (1390)",
                        Description = "Include Artificial Undead in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_artificial_undead",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 31
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_undead_aberration",
                        DisplayName = "Undead Aberration (1410)",
                        Description = "Include Undead Aberration in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_undead_aberration",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 32
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_undead_supplicant",
                        DisplayName = "Undead Supplicant (1470)",
                        Description = "Include Undead Supplicant in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_undead_supplicant",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 33
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_undead_peasant",
                        DisplayName = "Undead Peasant (1480)",
                        Description = "Include Undead Peasant in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_undead_peasant",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 34
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_undead_steelworker",
                        DisplayName = "Undead Steelworker (1490)",
                        Description = "Include Undead Steelworker in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_undead_steelworker",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 35
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_stone_knight",
                        DisplayName = "Stone Knight (1500)",
                        Description = "Include Stone Knight in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_stone_knight",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 36
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_ironclad_soldier",
                        DisplayName = "Ironclad Soldier (1510)",
                        Description = "Include Ironclad Soldier in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_ironclad_soldier",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 37
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_old_ironclad_soldier",
                        DisplayName = "Old Ironclad Soldier (1512)",
                        Description = "Include Old Ironclad Soldier in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_old_ironclad_soldier",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 38
                    },
                    new ModConfigurationOption
                    {
                        Name = "select_royal_swordsman",
                        DisplayName = "Royal Swordsman (1520)",
                        Description = "Include Royal Swordsman in enemy selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "select_royal_swordsman",
                        DefaultValue = true,
                        GroupName = "Enemies I",
                        TabName = "Enemy Selection",
                        Order = 39
                    },

                    // Enemies II - Complete list
                    new ModConfigurationOption { Name = "select_syan_knight", DisplayName = "Syan Knight (1530)", Description = "Include Syan Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_syan_knight", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 1 },
                    new ModConfigurationOption { Name = "select_amana_aberration", DisplayName = "Amana Aberration (1550)", Description = "Include Amana Aberration in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_amana_aberration", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 2 },
                    new ModConfigurationOption { Name = "select_armored_skeleton", DisplayName = "Armored Skeleton (1570)", Description = "Include Armored Skeleton in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_armored_skeleton", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 3 },
                    new ModConfigurationOption { Name = "select_small_boar", DisplayName = "Small Boar (2011)", Description = "Include Small Boar in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_small_boar", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 4 },
                    new ModConfigurationOption { Name = "select_undead_boar", DisplayName = "Undead Boar (2021)", Description = "Include Undead Boar in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_undead_boar", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 5 },
                    new ModConfigurationOption { Name = "select_parasite_spider", DisplayName = "Parasite spider (2030)", Description = "Include Parasite spider in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_parasite_spider", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 6 },
                    new ModConfigurationOption { Name = "select_poison_moth", DisplayName = "Poison Moth (2040)", Description = "Include Poison Moth in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_poison_moth", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 7 },
                    new ModConfigurationOption { Name = "select_poison_horn_beetle", DisplayName = "Poison Horn Beetle (2050)", Description = "Include Poison Horn Beetle in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_poison_horn_beetle", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 8 },
                    new ModConfigurationOption { Name = "select_acid_horn_beetle", DisplayName = "Acid Horn Beetle (2051)", Description = "Include Acid Horn Beetle in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_acid_horn_beetle", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 9 },
                    new ModConfigurationOption { Name = "select_razorback_nightcrawler", DisplayName = "Razorback Nightcrawler (2060)", Description = "Include Razorback Nightcrawler in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_razorback_nightcrawler", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 10 },
                    new ModConfigurationOption { Name = "select_hunting_dog", DisplayName = "Hunting Dog (2090)", Description = "Include Hunting Dog in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_hunting_dog", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 11 },
                    new ModConfigurationOption { Name = "select_basilisk", DisplayName = "Basilisk (2100)", Description = "Include Basilisk in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_basilisk", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 12 },
                    new ModConfigurationOption { Name = "select_crystal_lizard", DisplayName = "Crystal Lizard (2130)", Description = "Include Crystal Lizard in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_crystal_lizard", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 13 },
                    new ModConfigurationOption { Name = "select_red_crystal_lizard", DisplayName = "Red Crystal Lizard (2131)", Description = "Include Red Crystal Lizard in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_red_crystal_lizard", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 14 },
                    new ModConfigurationOption { Name = "select_giant_undead_boar", DisplayName = "Giant Undead Boar (2140)", Description = "Include Giant Undead Boar in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_giant_undead_boar", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 15 },
                    new ModConfigurationOption { Name = "select_wall_ghost", DisplayName = "Wall Ghost (2160)", Description = "Include Wall Ghost in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_wall_ghost", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 16 },
                    new ModConfigurationOption { Name = "select_dark_stalker", DisplayName = "Dark Stalker (2170)", Description = "Include Dark Stalker in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_dark_stalker", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 17 },
                    new ModConfigurationOption { Name = "select_giant_acid_horn_beetle", DisplayName = "Giant Acid Horn Beetle (2200)", Description = "Include Giant Acid Horn Beetle in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_giant_acid_horn_beetle", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 18 },
                    new ModConfigurationOption { Name = "select_giant_basilisk", DisplayName = "Giant Basilisk (2220)", Description = "Include Giant Basilisk in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_giant_basilisk", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 19 },
                    new ModConfigurationOption { Name = "select_mongrel_rat", DisplayName = "Mongrel Rat (2230)", Description = "Include Mongrel Rat in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_mongrel_rat", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 20 },
                    new ModConfigurationOption { Name = "select_darksucker", DisplayName = "Darksucker (2240)", Description = "Include Darksucker in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_darksucker", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 21 },
                    new ModConfigurationOption { Name = "select_corpse_rat", DisplayName = "Corpse Rat (2260)", Description = "Include Corpse Rat in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_corpse_rat", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 22 },
                    new ModConfigurationOption { Name = "select_stray_dog", DisplayName = "Stray Dog (2270)", Description = "Include Stray Dog in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_stray_dog", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 23 },
                    new ModConfigurationOption { Name = "select_frozen_stray_dog", DisplayName = "Frozen Stray Dog (2271)", Description = "Include Frozen Stray Dog in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_frozen_stray_dog", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 24 },
                    new ModConfigurationOption { Name = "select_ogre", DisplayName = "Ogre (3000)", Description = "Include Ogre in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_ogre", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 25 },
                    new ModConfigurationOption { Name = "select_heide_knight", DisplayName = "Heide Knight (3010)", Description = "Include Heide Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_heide_knight", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 26 },
                    new ModConfigurationOption { Name = "select_undead_jailer", DisplayName = "Undead Jailer (3020)", Description = "Include Undead Jailer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_undead_jailer", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 27 },
                    new ModConfigurationOption { Name = "select_alonne_captain", DisplayName = "Alonne Captain (3060)", Description = "Include Alonne Captain in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_alonne_captain", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 28 },
                    new ModConfigurationOption { Name = "select_body_of_vengarl", DisplayName = "Body of Vengarl (3070)", Description = "Include Body of Vengarl in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_body_of_vengarl", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 29 },
                    new ModConfigurationOption { Name = "select_lion_clan_warrior", DisplayName = "Lion Clan Warrior (3080)", Description = "Include Lion Clan Warrior in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_lion_clan_warrior", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 30 },
                    new ModConfigurationOption { Name = "select_golden_lion_clan_warrior", DisplayName = "Golden Lion Clan Warrior (3081)", Description = "Include Golden Lion Clan Warrior in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_golden_lion_clan_warrior", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 31 },
                    new ModConfigurationOption { Name = "select_forgotten_giant", DisplayName = "Forgotten Giant (3090)", Description = "Include Forgotten Giant in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_forgotten_giant", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 32 },
                    new ModConfigurationOption { Name = "select_mounter_overseer", DisplayName = "Mounter Overseer (3110)", Description = "Include Mounter Overseer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_mounter_overseer", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 33 },
                    new ModConfigurationOption { Name = "select_grave_warden", DisplayName = "Grave Warden (3120)", Description = "Include Grave Warden in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_grave_warden", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 34 },
                    new ModConfigurationOption { Name = "select_hollow_falconer", DisplayName = "Hollow Falconer (3130)", Description = "Include Hollow Falconer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_hollow_falconer", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 35 },
                    new ModConfigurationOption { Name = "select_hollow_primal_knight", DisplayName = "Hollow Primal Knight (3140)", Description = "Include Hollow Primal Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_hollow_primal_knight", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 36 },
                    new ModConfigurationOption { Name = "select_primal_knight", DisplayName = "Primal Knight (3150)", Description = "Include Primal Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_primal_knight", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 37 },
                    new ModConfigurationOption { Name = "select_desert_sorceress", DisplayName = "Desert Sorceress (3160)", Description = "Include Desert Sorceress in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_desert_sorceress", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 38 },
                    new ModConfigurationOption { Name = "select_dragon_acolyte", DisplayName = "Dragon Acolyte (3170)", Description = "Include Dragon Acolyte in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_dragon_acolyte", DefaultValue = true, GroupName = "Enemies II", TabName = "Enemy Selection", Order = 39 },

                    // Enemies III - Complete list
                    new ModConfigurationOption { Name = "select_alonne_knight", DisplayName = "Alonne Knight (3190)", Description = "Include Alonne Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_alonne_knight", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 1 },
                    new ModConfigurationOption { Name = "select_mimic", DisplayName = "Mimic (3210)", Description = "Include Mimic in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_mimic", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 2 },
                    new ModConfigurationOption { Name = "select_old_knight", DisplayName = "Old Knight (3300)", Description = "Include Old Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_old_knight", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 3 },
                    new ModConfigurationOption { Name = "select_drakekeeper", DisplayName = "Drakekeeper (3310)", Description = "Include Drakekeeper in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_drakekeeper", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 4 },
                    new ModConfigurationOption { Name = "select_captive_undead", DisplayName = "Captive Undead (3370)", Description = "Include Captive Undead in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_captive_undead", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 5 },
                    new ModConfigurationOption { Name = "select_leydia_witch", DisplayName = "Leydia Witch (5090)", Description = "Include Leydia Witch in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_leydia_witch", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 6 },
                    new ModConfigurationOption { Name = "select_imperious_knight", DisplayName = "Imperious Knight (5110)", Description = "Include Imperious Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_imperious_knight", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 7 },
                    new ModConfigurationOption { Name = "select_leydia_pyromancer", DisplayName = "Leydia Pyromancer (5120)", Description = "Include Leydia Pyromancer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_leydia_pyromancer", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 8 },
                    new ModConfigurationOption { Name = "select_flame_lizard", DisplayName = "Flame Lizard (6010)", Description = "Include Flame Lizard in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_flame_lizard", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 9 },
                    new ModConfigurationOption { Name = "select_iron_warrior", DisplayName = "Iron Warrior (6500)", Description = "Include Iron Warrior in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_iron_warrior", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 10 },
                    new ModConfigurationOption { Name = "select_fume_sorcerer", DisplayName = "Fume Sorcerer (6510)", Description = "Include Fume Sorcerer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_fume_sorcerer", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 11 },
                    new ModConfigurationOption { Name = "select_ashen_warrior", DisplayName = "Ashen Warrior (6530)", Description = "Include Ashen Warrior in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_ashen_warrior", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 12 },
                    new ModConfigurationOption { Name = "select_ashen_crawler", DisplayName = "Ashen Crawler (6540)", Description = "Include Ashen Crawler in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_ashen_crawler", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 13 },
                    new ModConfigurationOption { Name = "select_possessed_armor", DisplayName = "Possessed Armor (6560)", Description = "Include Possessed Armor in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_possessed_armor", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 14 },
                    new ModConfigurationOption { Name = "select_barrel_carrier", DisplayName = "Barrel Carrier (6570)", Description = "Include Barrel Carrier in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_barrel_carrier", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 15 },
                    new ModConfigurationOption { Name = "select_rampart_golem", DisplayName = "Rampart Golem (6590)", Description = "Include Rampart Golem in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_rampart_golem", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 16 },
                    new ModConfigurationOption { Name = "select_crystal_golem", DisplayName = "Crystal Golem (6600)", Description = "Include Crystal Golem in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_crystal_golem", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 17 },
                    new ModConfigurationOption { Name = "select_frozen_reindeer", DisplayName = "Frozen Reindeer (6610)", Description = "Include Frozen Reindeer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_frozen_reindeer", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 18 },
                    new ModConfigurationOption { Name = "select_rampart_hedgehog", DisplayName = "Rampart Hedgehog (6620)", Description = "Include Rampart Hedgehog in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_rampart_hedgehog", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 19 },
                    new ModConfigurationOption { Name = "select_rampart_spearman", DisplayName = "Rampart Spearman (6630)", Description = "Include Rampart Spearman in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_rampart_spearman", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 20 },
                    new ModConfigurationOption { Name = "select_sanctum_knight", DisplayName = "Sanctum Knight (6650)", Description = "Include Sanctum Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_sanctum_knight", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 21 },
                    new ModConfigurationOption { Name = "select_sanctum_soldier", DisplayName = "Sanctum Soldier (6660)", Description = "Include Sanctum Soldier in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_sanctum_soldier", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 22 },
                    new ModConfigurationOption { Name = "select_sanctum_priestess", DisplayName = "Sanctum Priestess (6700)", Description = "Include Sanctum Priestess in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_sanctum_priestess", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 23 },
                    new ModConfigurationOption { Name = "select_poison_statue_cluster", DisplayName = "Poison Statue Cluster (6710)", Description = "Include Poison Statue Cluster in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_poison_statue_cluster", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 24 },
                    new ModConfigurationOption { Name = "select_petrifying_statue_cluster", DisplayName = "Petrifying Statue Cluster (6711)", Description = "Include Petrifying Statue Cluster in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_petrifying_statue_cluster", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 25 },
                    new ModConfigurationOption { Name = "select_corrosive_ant", DisplayName = "Corrosive Ant (6720)", Description = "Include Corrosive Ant in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_corrosive_ant", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 26 },
                    new ModConfigurationOption { Name = "select_retainer_sorcerer", DisplayName = "Retainer Sorcerer (6770)", Description = "Include Retainer Sorcerer in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_retainer_sorcerer", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 27 },
                    new ModConfigurationOption { Name = "select_ice_golem", DisplayName = "Ice Golem (6780)", Description = "Include Ice Golem in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_ice_golem", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 28 },
                    new ModConfigurationOption { Name = "select_imperfect", DisplayName = "Imperfect (Dick drake) (6830)", Description = "Include Imperfect in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_imperfect", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 29 },
                    new ModConfigurationOption { Name = "select_charred_loyce_knight", DisplayName = "Charred Loyce Knight (6890)", Description = "Include Charred Loyce Knight in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_charred_loyce_knight", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 30 },
                    new ModConfigurationOption { Name = "select_bell_keeper", DisplayName = "Bell Keeper (7530)", Description = "Include Bell Keeper in enemy selection", ControlType = ModControlType.CheckBox, ControlName = "select_bell_keeper", DefaultValue = true, GroupName = "Enemies III", TabName = "Enemy Selection", Order = 31 },

                    // === ENEMY TAB (continued) ===

                    // Randomization Algorithm - based on SHUFFLE_TYPE 3 = full_random
                    new ModConfigurationOption
                    {
                        Name = "single_card",
                        DisplayName = "Single card",
                        Description = "Each enemy type appears once",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "single_card",
                        DefaultValue = false,
                        GroupName = "Randomization Algorithm",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "single_card", "minimum_deck", "large_deck", "full_random" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "minimum_deck",
                        DisplayName = "Minimum deck",
                        Description = "Balanced enemy distribution",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "minimum_deck",
                        DefaultValue = false,
                        GroupName = "Randomization Algorithm",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "single_card", "minimum_deck", "large_deck", "full_random" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "large_deck",
                        DisplayName = "Large deck",
                        Description = "More varied enemy distribution",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "large_deck",
                        DefaultValue = false,
                        GroupName = "Randomization Algorithm",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "single_card", "minimum_deck", "large_deck", "full_random" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "full_random",
                        DisplayName = "Full random",
                        Description = "Completely random enemy placement",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "full_random",
                        DefaultValue = true,
                        GroupName = "Randomization Algorithm",
                        TabName = "Enemy",
                        RadioButtonGroup = new List<string> { "single_card", "minimum_deck", "large_deck", "full_random" },
                        Order = 4
                    },

                    // === BOSSES TAB ===

                    // Boss Options
                    new ModConfigurationOption
                    {
                        Name = "randomize_bosses",
                        DisplayName = "Randomize bosses",
                        Description = "Enable boss randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_bosses",
                        DefaultValue = true,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "belfry_boss_rush",
                        DisplayName = "Belfry boss rush",
                        Description = "Turn Belfry areas into boss rush challenges",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "belfry_boss_rush",
                        DefaultValue = false,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "reduced_congregation",
                        DisplayName = "Reduced congregation",
                        Description = "Reduce the number of enemies in Congregation boss fight",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "reduced_congregation",
                        DefaultValue = true,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "scaled_skelelords",
                        DisplayName = "Scaled skelelords",
                        Description = "Scale Skeleton Lords based on progression",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "scaled_skelelords",
                        DefaultValue = false,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "weak_twin",
                        DisplayName = "Weak twin",
                        Description = "Make Twin Dragonriders less challenging",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "weak_twin",
                        DefaultValue = true,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 5
                    },

                    // Rat Vanguard WhosWho
                    new ModConfigurationOption
                    {
                        Name = "rat_vanguard_whoswho",
                        DisplayName = "Rat Vanguard WhosWho",
                        Description = "Randomize which rat is the real Rat Vanguard",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "rat_vanguard_whoswho",
                        DefaultValue = true,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "rat_clones",
                        DisplayName = "#Clones",
                        Description = "Number of fake rat clones (1-12)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "rat_clones",
                        DefaultValue = 5,
                        GroupName = "Boss Options",
                        TabName = "Bosses",
                        Properties = new Dictionary<string, object> { { "min", 1 }, { "max", 12 } },
                        EnabledWhen = "rat_vanguard_whoswho",
                        EnabledWhenValue = true,
                        Order = 7
                    },

                    // Boss scaling
                    new ModConfigurationOption
                    {
                        Name = "boss_scaling",
                        DisplayName = "Boss scaling",
                        Description = "Enable boss health and damage scaling",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "boss_scaling",
                        DefaultValue = true,
                        GroupName = "Boss Scaling",
                        TabName = "Bosses",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "hp_factor",
                        DisplayName = "HP factor",
                        Description = "Boss health multiplier (0-100)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "hp_factor",
                        DefaultValue = 70,
                        GroupName = "Boss Scaling",
                        TabName = "Bosses",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } },
                        EnabledWhen = "boss_scaling",
                        EnabledWhenValue = true,
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "dmg_factor",
                        DisplayName = "DMG factor",
                        Description = "Boss damage multiplier (0-10)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "dmg_factor",
                        DefaultValue = 70,
                        GroupName = "Boss Scaling",
                        TabName = "Bosses",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 1000 } },
                        EnabledWhen = "boss_scaling",
                        EnabledWhenValue = true,
                        Order = 3
                    },

                    // Multiboss Options
                    new ModConfigurationOption
                    {
                        Name = "multiboss_skeleton_lords",
                        DisplayName = "Skeleton Lords",
                        Description = "Enable multiple Skeleton Lords",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_skeleton_lords",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_belfry_gargoyles",
                        DisplayName = "Belfry Gargoyles",
                        Description = "Enable multiple Belfry Gargoyles",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_belfry_gargoyles",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_ruin_sentinels",
                        DisplayName = "Ruin Sentinels",
                        Description = "Enable multiple Ruin Sentinels",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_ruin_sentinels",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_twin_dragonriders",
                        DisplayName = "Twin Dragonriders",
                        Description = "Enable multiple Twin Dragonriders",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_twin_dragonriders",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_throne_watchers",
                        DisplayName = "Throne Watchers",
                        Description = "Enable multiple Throne Watchers",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_throne_watchers",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_graverobbers",
                        DisplayName = "Graverobbers",
                        Description = "Enable multiple Graverobbers",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_graverobbers",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiboss_lud_zallen",
                        DisplayName = "Lud & Zallen",
                        Description = "Enable multiple Lud & Zallen",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiboss_lud_zallen",
                        DefaultValue = true,
                        GroupName = "Multiboss",
                        TabName = "Bosses",
                        Order = 7
                    },

                    // === ENEMY MULTIPLIER TAB ===

                    // Enemy Multiplier
                    new ModConfigurationOption
                    {
                        Name = "multiply_enemies",
                        DisplayName = "Multiply enemies",
                        Description = "Enable enemy multiplication",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_enemies",
                        DefaultValue = false,
                        GroupName = "Enemy Multiplier",
                        TabName = "Multiplier",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "enemy_multiplier",
                        DisplayName = "Enemy multiplier",
                        Description = "Multiplier for regular enemies (100-300%)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "enemy_multiplier",
                        DefaultValue = 150,
                        GroupName = "Enemy Multiplier",
                        TabName = "Multiplier",
                        Properties = new Dictionary<string, object> { { "min", 100 }, { "max", 300 } },
                        EnabledWhen = "multiply_enemies",
                        EnabledWhenValue = true,
                        Order = 2
                    },

                    // Boss Multiplier
                    new ModConfigurationOption
                    {
                        Name = "multiply_bosses",
                        DisplayName = "Multiply bosses",
                        Description = "Enable boss multiplication",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_bosses",
                        DefaultValue = false,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "boss_multiplier",
                        DisplayName = "Boss multiplier",
                        Description = "Multiplier for bosses (100-300%)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "boss_multiplier",
                        DefaultValue = 150,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        Properties = new Dictionary<string, object> { { "min", 100 }, { "max", 300 } },
                        EnabledWhen = "multiply_bosses",
                        EnabledWhenValue = true,
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "mb_skeletonlords",
                        DisplayName = "Skeleton Lords",
                        Description = "",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mb_skeletonlords",
                        DefaultValue = true,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_bosses",
                        EnabledWhenValue = true,
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "mb_ruinsentinels",
                        DisplayName = "Ruin Sentinels",
                        Description = "",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mb_ruinsentinels",
                        DefaultValue = true,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_bosses",
                        EnabledWhenValue = true,
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "mb_belfry",
                        DisplayName = "Belfry Gargoyles",
                        Description = "",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mb_belfry",
                        DefaultValue = true,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_bosses",
                        EnabledWhenValue = true,
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "mb_ganksquad",
                        DisplayName = "Ganksquad",
                        Description = "",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "mb_ganksquad",
                        DefaultValue = true,
                        GroupName = "Boss Multiplier",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_bosses",
                        EnabledWhenValue = true,
                        Order = 6
                    },

                    // Multiply Ads
                    new ModConfigurationOption
                    {
                        Name = "multiply_ads",
                        DisplayName = "Multiply ads",
                        Description = "Enable multiplication of boss adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_ads",
                        DefaultValue = false,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_executioner_chariot",
                        DisplayName = "Executioner's Chariot",
                        Description = "Multiply Executioner's Chariot adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_executioner_chariot",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_royal_rat_authority",
                        DisplayName = "Royal Rat Authority",
                        Description = "Multiply Royal Rat Authority adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_royal_rat_authority",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_covetous_demon",
                        DisplayName = "Covetous Demon",
                        Description = "Multiply Covetous Demon adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_covetous_demon",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_skeleton_lords",
                        DisplayName = "Skeleton Lords",
                        Description = "Multiply Skeleton Lords adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_skeleton_lords",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_dukes_dear_freja",
                        DisplayName = "Duke's Dear Freja",
                        Description = "Multiply Duke's Dear Freja adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_dukes_dear_freja",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "multiply_burnt_ivory_king",
                        DisplayName = "Burnt Ivory King",
                        Description = "Multiply Burnt Ivory King adds",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "multiply_burnt_ivory_king",
                        DefaultValue = true,
                        GroupName = "Multiply Ads",
                        TabName = "Multiplier",
                        EnabledWhen = "multiply_ads",
                        EnabledWhenValue = true,
                        Order = 7
                    },

                    // === ITEM RANDOMIZER TAB ===

                    // Items options
                    new ModConfigurationOption
                    {
                        Name = "randomize_key_items",
                        DisplayName = "Randomize key items",
                        Description = "Enable key item randomization",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_key_items",
                        DefaultValue = true,
                        GroupName = "Items options",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "early_blacksmith",
                        DisplayName = "Early blacksmith",
                        Description = "Make blacksmith available early",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "early_blacksmith",
                        DefaultValue = false,
                        GroupName = "Items options",
                        TabName = "Item",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "write_cheatsheet_item",
                        DisplayName = "Write cheatsheet",
                        Description = "Generate a cheatsheet showing item placements",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "write_cheatsheet_item",
                        DefaultValue = true,
                        GroupName = "Items options",
                        TabName = "Item",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "drop_consumable_keys_bulk",
                        DisplayName = "Drop consumable keys in bulk",
                        Description = "Drop multiple copies of consumable key items",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "drop_consumable_keys_bulk",
                        DefaultValue = false,
                        GroupName = "Items options",
                        TabName = "Item",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "random_upgraded_gear",
                        DisplayName = "Random upgraded gear",
                        Description = "Enable random weapon and armor upgrades",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "random_upgraded_gear",
                        DefaultValue = false,
                        GroupName = "Items options",
                        TabName = "Item",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "gear_upgrade",
                        DisplayName = "Gear upgrade",
                        Description = "Maximum upgrade level for random gear (0-100)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "gear_upgrade",
                        DefaultValue = 25,
                        GroupName = "Items options",
                        TabName = "Item",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } },
                        EnabledWhen = "random_upgraded_gear",
                        EnabledWhenValue = true,
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "weapon_infusion_percent",
                        DisplayName = "Weapon infusion %",
                        Description = "Percentage chance for weapon infusions (0-100%)",
                        ControlType = ModControlType.TrackBar,
                        ControlName = "weapon_infusion_percent",
                        DefaultValue = 10,
                        GroupName = "Items options",
                        TabName = "Item",
                        Properties = new Dictionary<string, object> { { "min", 0 }, { "max", 100 } },
                        EnabledWhen = "random_upgraded_gear",
                        EnabledWhenValue = true,
                        Order = 7
                    },

                    // Shop options
                    new ModConfigurationOption
                    {
                        Name = "unlock_shop_items",
                        DisplayName = "Unlock shop items",
                        Description = "Make all shop items available from the start",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unlock_shop_items",
                        DefaultValue = false,
                        GroupName = "Shop options",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "infinite_shops",
                        DisplayName = "Infinite shops",
                        Description = "Make shop items have infinite stock",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "infinite_shops",
                        DefaultValue = true,
                        GroupName = "Shop options",
                        TabName = "Item",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "unlock_straid_trades",
                        DisplayName = "Unlock Straid trades",
                        Description = "Make Straid's boss soul trades available immediately",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unlock_straid_trades",
                        DefaultValue = true,
                        GroupName = "Shop options",
                        TabName = "Item",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "unlock_ornifex_trades",
                        DisplayName = "Unlock Ornifex trades",
                        Description = "Make Ornifex's boss soul trades available immediately",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unlock_ornifex_trades",
                        DefaultValue = true,
                        GroupName = "Shop options",
                        TabName = "Item",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "melentials_lifegems",
                        DisplayName = "Melentia's lifegems",
                        Description = "Modify Melentia's lifegem inventory",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "melentials_lifegems",
                        DefaultValue = false,
                        GroupName = "Shop options",
                        TabName = "Item",
                        Order = 5
                    },

                    // Starting class options
                    new ModConfigurationOption
                    {
                        Name = "randomize_class_equipment",
                        DisplayName = "Randomize class equipment",
                        Description = "Randomize starting class equipment",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_class_equipment",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "randomize_class_stats",
                        DisplayName = "Randomize class stats",
                        Description = "Randomize starting class stats",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_class_stats",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "unusable_equipment",
                        DisplayName = "Unusable equipment",
                        Description = "Allow starting with equipment that cannot be used",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "unusable_equipment",
                        DefaultValue = false,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "two_handing",
                        DisplayName = "TWO handing",
                        Description = "Allow weapons that require two-handing",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "two_handing",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 4
                    },
                    new ModConfigurationOption
                    {
                        Name = "ranged_weapons",
                        DisplayName = "Ranged weapons",
                        Description = "Allow starting with ranged weapons",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "ranged_weapons",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 5
                    },
                    new ModConfigurationOption
                    {
                        Name = "catalysts",
                        DisplayName = "Catalysts",
                        Description = "Allow starting with spell catalysts",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "catalysts",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 6
                    },
                    new ModConfigurationOption
                    {
                        Name = "shield_weapon",
                        DisplayName = "Shield weapon",
                        Description = "Allow shields to be used as weapons",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "shield_weapon",
                        DefaultValue = false,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 7
                    },
                    new ModConfigurationOption
                    {
                        Name = "randomize_starting_gifts",
                        DisplayName = "Randomize starting gifts",
                        Description = "Randomize the starting gift selection",
                        ControlType = ModControlType.CheckBox,
                        ControlName = "randomize_starting_gifts",
                        DefaultValue = true,
                        GroupName = "Starting class options",
                        TabName = "Item",
                        Order = 8
                    },

                    // Equipment load limit - based on WEIGHT_LIMIT 70
                    new ModConfigurationOption
                    {
                        Name = "eq_load_70",
                        DisplayName = "70% limit",
                        Description = "Standard equipment load limit",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "eq_load_70",
                        DefaultValue = true,
                        GroupName = "Equipment load limit",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "eq_load_70", "eq_load_100", "eq_load_120", "eq_load_no_limit" },
                        Order = 1
                    },
                    new ModConfigurationOption
                    {
                        Name = "eq_load_100",
                        DisplayName = "100% limit",
                        Description = "Higher equipment load limit",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "eq_load_100",
                        DefaultValue = false,
                        GroupName = "Equipment load limit",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "eq_load_70", "eq_load_100", "eq_load_120", "eq_load_no_limit" },
                        Order = 2
                    },
                    new ModConfigurationOption
                    {
                        Name = "eq_load_120",
                        DisplayName = "120% limit",
                        Description = "Very high equipment load limit",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "eq_load_120",
                        DefaultValue = false,
                        GroupName = "Equipment load limit",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "eq_load_70", "eq_load_100", "eq_load_120", "eq_load_no_limit" },
                        Order = 3
                    },
                    new ModConfigurationOption
                    {
                        Name = "eq_load_no_limit",
                        DisplayName = "No limit",
                        Description = "Remove equipment load restrictions",
                        ControlType = ModControlType.RadioButton,
                        ControlName = "eq_load_no_limit",
                        DefaultValue = false,
                        GroupName = "Equipment load limit",
                        TabName = "Item",
                        RadioButtonGroup = new List<string> { "eq_load_70", "eq_load_100", "eq_load_120", "eq_load_no_limit" },
                        Order = 4
                    }
                }
            };
        }

        public bool IsAvailable()
        {
            string sourcePath = Path.Combine("Data", "DS2", ModFile);
            return File.Exists(sourcePath);
        }

        public bool TryInstallMod(string destPath)
        {
            try
            {
                ZipFile.ExtractToDirectory(Path.Combine("Data", "DS2", ModFile), destPath, true);

                // If we have saved configuration, run the mod with it
                if (_savedConfiguration != null)
                {
                    RunWithConfiguration(_savedConfiguration, destPath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryRemoveMod(string destPath)
        {
            try
            {
                // Remove randomizer files
                string[] filesToRemove = {
                    "dinput8.dll",
                    "ds2s_heap_x.dll",
                    "modengine.ini"
                };

                foreach (string file in filesToRemove)
                {
                    string fullPath = Path.Combine(destPath, file);
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }

                Directory.Delete(Path.Combine(destPath, "randomizer"), true);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public ModConfiguration GetUIConfiguration()
        {
            return _configuration ?? throw new InvalidOperationException("Configuration not initialized");
        }

        public List<UserPreset> GetUserPresets()
        {
            return _presetService.LoadPresets(Name);
        }

        public bool ApplyUserPreset(string presetName, string destPath)
        {
            var preset = _presetService.GetPreset(Name, presetName);
            if (preset == null)
                return false;

            return RunWithConfiguration(preset.OptionValues, destPath);
        }

        public bool RunWithConfiguration(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                if (_configuration == null)
                    return false;

                // Save configuration for potential use during installation
                _savedConfiguration = configuration;

                // Generate the configuration files
                if (!GenerateConfigurationFiles(configuration, destPath))
                {
                    return false;
                }

                // Find and launch the executable
                string exePath = Path.Combine(destPath, "randomizer", _configuration.ExecutablePath);
                if (!File.Exists(exePath))
                {
                    return false;
                }

                // Store the executable path for later use
                _executablePath = exePath;

                // Launch the mod executable
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath),
                    UseShellExecute = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;

                    // Wait a moment for the application to start
                    Thread.Sleep(2000);

                    // Try to automate the randomize button click
                    return AutomateRandomizeButton(process);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool GenerateConfigurationFiles(Dictionary<string, object> configuration, string destPath)
        {
            try
            {
                // Generate enemy randomizer config
                string erConfig = GenerateEnemyRandomizerConfig(configuration);
                string erConfigPath = Path.Combine(destPath, "randomizer", "er_config.txt");
                File.WriteAllText(erConfigPath, erConfig, Encoding.UTF8);

                // Generate item randomizer config
                string irConfig = GenerateItemRandomizerConfig(configuration);
                string irConfigPath = Path.Combine(destPath, "randomizer", "ir_config.txt");
                File.WriteAllText(irConfigPath, irConfig, Encoding.UTF8);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GenerateEnemyRandomizerConfig(Dictionary<string, object> configuration)
        {
            var config = new StringBuilder();
            
            // Version header
            config.AppendLine("#VERSION 0");

            // Invader settings
            if (GetBoolValue(configuration, "replace_invaders", false))
                config.AppendLine("#INV_REPLACE 1");
            else
                config.AppendLine("#INV_REPLACE 0");
                
            if (GetBoolValue(configuration, "remove_invaders", false))
                config.AppendLine("#INV_REMOVE 1");
            else
                config.AppendLine("#INV_REMOVE 0");

            // Summon settings  
            if (GetBoolValue(configuration, "replace_summons_fun", false))
                config.AppendLine("#SUM_REPLACE 1");
            else
                config.AppendLine("#SUM_REPLACE 0");
                
            if (GetBoolValue(configuration, "remove_summons", false))
                config.AppendLine("#SUM_REMOVE 1");
            else
                config.AppendLine("#SUM_REMOVE 0");

            // Basic enemy settings
            config.AppendLine($"#NPC_CLONING {(GetBoolValue(configuration, "npc_randomization", true) ? 1 : 0)}");

            // Shuffle type (algorithm)
            int shuffleType = 3; // Default to FullRandom
            if (GetBoolValue(configuration, "single_card", false))
                shuffleType = 0;
            else if (GetBoolValue(configuration, "minimum_deck", false))
                shuffleType = 1;
            else if (GetBoolValue(configuration, "large_deck", false))
                shuffleType = 2;
            else if (GetBoolValue(configuration, "full_random", true))
                shuffleType = 3;
            config.AppendLine($"#SHUFFLE_TYPE {shuffleType}");

            config.AppendLine($"#CHEATSHEET {(GetBoolValue(configuration, "write_cheatsheet_enemy", true) ? 1 : 0)}");
            config.AppendLine($"#RAINBOW {(GetBoolValue(configuration, "rainbow_enemies", false) ? 1 : 0)}");
            config.AppendLine($"#ENEMY_RANDO {(GetBoolValue(configuration, "randomize_enemies", true) ? 1 : 0)}");
            config.AppendLine("#ENEMY_MIMIC 1"); // Always enabled
            config.AppendLine("#ENEMY_LIZARD 1"); // Always enabled
            config.AppendLine($"#INVIS_ENEMY {(GetBoolValue(configuration, "remove_invisible", false) ? 1 : 0)}");
            config.AppendLine($"#ROAMING_BOSS {(GetBoolValue(configuration, "wandering_bosses", false) ? 1 : 0)}");
            
            if (GetBoolValue(configuration, "wandering_bosses", false))
            {
                int bossChance = GetIntValue(configuration, "boss_chance", 1);
                config.AppendLine($"#ROAMING_CHANCE {bossChance}");
                config.AppendLine($"#ROAMING_RESPAWN {(GetBoolValue(configuration, "respawn_wandering_bosses", false) ? 1 : 0)}");
            }
            else
            {
                config.AppendLine("#ROAMING_CHANCE 1");
                config.AppendLine("#ROAMING_RESPAWN 1");
            }

            config.AppendLine($"#ENEMY_LOCATION {(GetBoolValue(configuration, "randomize_enemy_locations", false) ? 1 : 0)}");
            config.AppendLine($"#RANDOMIZE_ADS {(GetBoolValue(configuration, "randomize_boss_ads", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_GARGOYLES {(GetBoolValue(configuration, "multiboss_belfry_gargoyles", true) ? 1 : 0)}");
            config.AppendLine($"#ENEMY_SCALING {(GetBoolValue(configuration, "enemy_scaling", true) ? 1 : 0)}");

            // HP and damage scaling
            int hpScale = GetIntValue(configuration, "hp_factor", 70);
            int dmgScale = GetIntValue(configuration, "dmg_factor", 70);
            config.AppendLine($"#HP_SCALE {hpScale}");
            config.AppendLine($"#DMG_SCALE {dmgScale}");
            config.AppendLine($"#BOSS_DMG_SCALE {dmgScale}");
            config.AppendLine($"#BOSS_SCALING {(GetBoolValue(configuration, "boss_scaling", true) ? 1 : 0)}");
            config.AppendLine($"#BOSS_HP_SCALE {hpScale}");

            // Boss settings
            config.AppendLine($"#BOSS_RANDO {(GetBoolValue(configuration, "randomize_bosses", true) ? 1 : 0)}");
            config.AppendLine($"#BELFRY_RUSH {(GetBoolValue(configuration, "belfry_boss_rush", false) ? 1 : 0)}");
            config.AppendLine($"#EASY_CONGRE {(GetBoolValue(configuration, "reduced_congregation", true) ? 1 : 0)}");
            config.AppendLine($"#EASY_TWINS {(GetBoolValue(configuration, "weak_twin", true) ? 1 : 0)}");
            config.AppendLine($"#EASY_SKELLYS {(GetBoolValue(configuration, "scaled_skelelords", false) ? 1 : 0)}");
            config.AppendLine($"#REMAKE_RAT {(GetBoolValue(configuration, "rat_vanguard_whoswho", true) ? 1 : 0)}");
            
            int ratClones = GetIntValue(configuration, "rat_clones", 5);
            config.AppendLine($"#RAT_CLONES {ratClones}");

            // Multiboss settings - only enable if any multiboss option is selected
            bool anyMultibossEnabled = GetBoolValue(configuration, "mb_skeletonlords", true) ||
                                     GetBoolValue(configuration, "mb_ruinsentinels", true) ||
                                     GetBoolValue(configuration, "mb_belfry", true) ||
                                     GetBoolValue(configuration, "mb_ganksquad", true);
            
            config.AppendLine($"#MULTIBOSS {(anyMultibossEnabled ? 1 : 0)}");
            config.AppendLine($"#DUPE_SKELELORDS {(GetBoolValue(configuration, "mb_skeletonlords", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_GARGOYLES {(GetBoolValue(configuration, "mb_belfry", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_SENTINELS {(GetBoolValue(configuration, "mb_ruinsentinels", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_GANKSQUAD {(GetBoolValue(configuration, "mb_skeletonlords", true) ? 1 : 0)}");

            // Enemy/Boss multipliers
            int enemyMult = GetIntValue(configuration, "enemy_multiplier", 150);
            int bossMult = GetIntValue(configuration, "boss_multiplier", 150);
            config.AppendLine($"#ENEMY_MULTIPLIER {enemyMult}");
            config.AppendLine($"#BOSS_MULTIPLIER {bossMult}");
            config.AppendLine($"#MULTIPLY_ENEMIES {(GetBoolValue(configuration, "multiply_enemies", false) ? 1 : 0)}");
            config.AppendLine($"#MULTIPLY_BOSSES {(GetBoolValue(configuration, "multiply_bosses", false) ? 1 : 0)}");

            // Duplicate settings
            config.AppendLine($"#DUPE_ADS {(GetBoolValue(configuration, "multiply_ads", false) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_CHARIOT {(GetBoolValue(configuration, "multiply_executioner_chariot", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_ROYAL_RAT {(GetBoolValue(configuration, "multiply_royal_rat_authority", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_COVETOUS {(GetBoolValue(configuration, "multiply_covetous_demon", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_SKELELORDS {(GetBoolValue(configuration, "multiply_skeleton_lords", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_FREJA {(GetBoolValue(configuration, "multiply_dukes_dear_freja", true) ? 1 : 0)}");
            config.AppendLine($"#DUPE_ADS_IVORY {(GetBoolValue(configuration, "multiply_burnt_ivory_king", true) ? 1 : 0)}");

            // Add seed if specified
            if (configuration.TryGetValue("seed", out object? seedValue) && seedValue != null)
            {
                string seed = seedValue.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(seed) && long.TryParse(seed, out long numericSeed))
                {
                    config.AppendLine($"#SEED {numericSeed}");
                }
            }

            // Generate dynamic banned enemies list based on user configuration
            // If an enemy is NOT selected (false), it gets added to the banned list
            var bannedEnemies = new List<int>();
            
            // Check each enemy selection option and add to banned list if not selected
            if (_configuration?.Options != null)
            {
                foreach (var option in _configuration.Options.Where(o => o.TabName == "Enemy Selection"))
                {
                    // Extract ID from DisplayName format: "Name (ID)"
                    var match = System.Text.RegularExpressions.Regex.Match(option.DisplayName, @"\((\d+)\)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int enemyId))
                    {
                        // If the enemy is NOT selected (false), add it to the banned list
                        if (!GetBoolValue(configuration, option.Name, true)) // Default to true (not banned)
                        {
                            bannedEnemies.Add(enemyId);
                        }
                    }
                }
            }
            
            // Output banned list
            config.AppendLine($"#BANNED [{string.Join(",", bannedEnemies)}]");

            // Generate dynamic area configurations based on zone limits
            var zoneMapping = new Dictionary<string, string>
            {
                {"things_betwixt_limit", "M10020000"},
                {"majula_limit", "M10040000"},
                {"forest_of_fallen_giants_limit", "M10100000"},
                {"brightstone_cove_tseldora_limit", "M10140000"},
                {"aldias_keep_limit", "M10150000"},
                {"lost_bastille_belfry_luna_limit", "M10160000"},
                {"harvest_valley_earthen_peak_limit", "M10170000"},
                {"no_mans_wharf_limit", "M10180000"},
                {"iron_keep_belfry_sol_limit", "M10190000"},
                {"huntsmans_copse_undead_purgatory_limit", "M10230000"},
                {"gutter_black_gulch_limit", "M10250000"},
                {"dragon_aerie_shrine_limit", "M10270000"},
                {"majula_shaded_woods_limit", "M10290000"},
                {"heides_tower_no_mans_wharf_limit", "M10300000"},
                {"heides_tower_of_flame_limit", "M10310000"},
                {"shaded_woods_shrine_of_winter_limit", "M10320000"},
                {"doors_of_pharros_limit", "M10330000"},
                {"grave_of_saints_limit", "M10340000"},
                {"giant_memories_limit", "M20100000"},
                {"shrine_of_amana_limit", "M20110000"},
                {"drangleic_castle_throne_limit", "M20210000"},
                {"undead_crypt_limit", "M20240000"},
                {"dragon_memories_limit", "M20260000"},
                {"dark_chasm_of_old_limit", "M40030000"},
                {"shulvar_sanctum_city_limit", "M50350000"},
                {"brume_tower_limit", "M50360000"},
                {"frozen_eleum_loyce_limit", "M50370000"},
                {"memory_of_the_king_limit", "M50380000"}
            };
            
            // Output zone configurations with user-specified limits
            foreach (var zone in zoneMapping)
            {
                int limit = GetIntValue(configuration, zone.Key, 99); // Default to 99
                config.AppendLine($"#{zone.Value} {limit} 1");
            }

            return config.ToString();
        }

        private string GenerateItemRandomizerConfig(Dictionary<string, object> configuration)
        {
            var config = new StringBuilder();
            
            // Version header
            config.AppendLine("#VERSION 0");

            // Add seed if specified
            if (configuration.TryGetValue("seed", out object? seedValue) && seedValue != null)
            {
                string seed = seedValue.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(seed) && long.TryParse(seed, out long numericSeed))
                {
                    config.AppendLine($"#SEED {numericSeed}");
                }
            }

            // Weight limit
            int weightLimit = 70; // Default
            if (GetBoolValue(configuration, "eq_load_100", false))
                weightLimit = 100;
            else if (GetBoolValue(configuration, "eq_load_120", false))
                weightLimit = 120;
            else if (GetBoolValue(configuration, "eq_load_no_limit", false))
                weightLimit = 999; // Effectively unlimited
                
            config.AppendLine($"#WEIGHT_LIMIT {weightLimit}");

            // Gear upgrade settings
            int gearUpgrade = GetIntValue(configuration, "gear_upgrade", 25);
            int weaponInfusion = GetIntValue(configuration, "weapon_infusion_percent", 10);
            config.AppendLine($"#GEAR_UPG_CHANCE {gearUpgrade}");
            config.AppendLine($"#WEAPON_INF_CHANCE {weaponInfusion}");

            // Shop settings
            config.AppendLine($"#UNLOCK_SHOP {(GetBoolValue(configuration, "unlock_shop_items", false) ? 1 : 0)}");
            config.AppendLine($"#UNLOCK_STRAID {(GetBoolValue(configuration, "unlock_straid_trades", true) ? 1 : 0)}");
            config.AppendLine($"#UNLOCK_ORNIFEX {(GetBoolValue(configuration, "unlock_ornifex_trades", true) ? 1 : 0)}");
            config.AppendLine($"#INFINITE_SHOP {(GetBoolValue(configuration, "infinite_shops", true) ? 1 : 0)}");
            config.AppendLine($"#MELENTIA_GEMS {(GetBoolValue(configuration, "melentials_lifegems", false) ? 1 : 0)}");

            // Weapon infusion
            config.AppendLine($"#INFUSE_WEAPONS {(GetBoolValue(configuration, "random_upgraded_gear", true) ? 1 : 0)}");

            // Class randomization
            config.AppendLine($"#RANDO_CLASSES {(GetBoolValue(configuration, "randomize_class_equipment", true) ? 1 : 0)}");
            config.AppendLine($"#RANDO_CLASS_STATS {(GetBoolValue(configuration, "randomize_class_stats", true) ? 1 : 0)}");
            config.AppendLine($"#RANDO_GIFTS {(GetBoolValue(configuration, "randomize_starting_gifts", true) ? 1 : 0)}");

            // Equipment handling
            config.AppendLine($"#CLASS_TWOHAND {(GetBoolValue(configuration, "two_handing", true) ? 1 : 0)}");
            config.AppendLine($"#CLASS_UNUSABLE {(GetBoolValue(configuration, "unusable_equipment", false) ? 1 : 0)}");
            config.AppendLine($"#CLASS_SHIELDWEAPON {(GetBoolValue(configuration, "shield_weapon", false) ? 1 : 0)}");
            config.AppendLine($"#CLASS_CATALYSTS {(GetBoolValue(configuration, "catalysts", true) ? 1 : 0)}");
            config.AppendLine($"#CLASS_BOWS {(GetBoolValue(configuration, "ranged_weapons", true) ? 1 : 0)}");

            // Other options
            config.AppendLine($"#EARLY_BLACKSMITH {(GetBoolValue(configuration, "early_blacksmith", false) ? 1 : 0)}");
            config.AppendLine($"#CHEATSHEET {(GetBoolValue(configuration, "write_cheatsheet_item", true) ? 1 : 0)}");
            config.AppendLine($"#RANDO_KEYS {(GetBoolValue(configuration, "randomize_key_items", true) ? 1 : 0)}");
            config.AppendLine($"#KEYS_BULK {(GetBoolValue(configuration, "drop_consumable_keys_bulk", false) ? 1 : 0)}");

            return config.ToString();
        }

        private bool GetBoolValue(Dictionary<string, object> configuration, string key, bool defaultValue)
        {
            if (configuration.TryGetValue(key, out object? value) && value != null)
            {
                if (value is JsonElement jsonElement)
                {
                    return jsonElement.ValueKind == JsonValueKind.True;
                }
                return Convert.ToBoolean(value);
            }
            return defaultValue;
        }

        private int GetIntValue(Dictionary<string, object> configuration, string key, int defaultValue)
        {
            if (configuration.TryGetValue(key, out object? value) && value != null)
            {
                if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                {
                    return jsonElement.GetInt32();
                }
                if (int.TryParse(value.ToString(), out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        public void SaveConfiguration(Dictionary<string, object> configuration)
        {
            _savedConfiguration = new Dictionary<string, object>(configuration);
        }

        public Dictionary<string, object>? GetSavedConfiguration()
        {
            return _savedConfiguration;
        }

        public string? GetSelectedPreset()
        {
            return _selectedPreset;
        }

        public void SetSelectedPreset(string? presetName)
        {
            _selectedPreset = presetName;
            
            // If a preset is selected, load its configuration
            if (!string.IsNullOrEmpty(presetName))
            {
                var preset = _presetService.GetPreset(Name, presetName);
                if (preset != null)
                {
                    SaveConfiguration(preset.OptionValues);
                }
            }
        }

        private bool AutomateRandomizeButton(Process process)
        {
            try
            {
                // DS2 Randomizer first runs command line loading, then opens GUI
                // Wait longer for the initial command-line loading phase
                Thread.Sleep(100); 
                
                // Try multiple times to find the window by title - more reliable than MainWindowHandle
                IntPtr mainWindow = IntPtr.Zero;
                for (int i = 0; i < 30; i++) // Increased from 10 to 30 attempts
                {
                    // Search for window with exact title "DS2S Randomizer"
                    mainWindow = ModAutomationHelper.FindWindow(null, "DS2S Randomizer");
                    if (mainWindow != IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found 'DS2S Randomizer' window on attempt {i + 1}");
                        break;
                    }
                    Thread.Sleep(100); // Wait 1 second between attempts
                    System.Diagnostics.Debug.WriteLine($"Attempt {i + 1}: Searching for 'DS2S Randomizer' window...");
                }
                
                if (mainWindow == IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("Could not find 'DS2S Randomizer' window after 30 attempts");
                    return false;
                }
                
                // Get window rectangle to calculate button positions for all steps
                if (!ModAutomationHelper.GetWindowRect(mainWindow, out ModAutomationHelper.RECT windowRect))
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get window rectangle");
                    return false;
                }
                
                int windowWidth = windowRect.right - windowRect.left;
                int windowHeight = windowRect.bottom - windowRect.top;
                System.Diagnostics.Debug.WriteLine($"Window size: {windowWidth}x{windowHeight}");
                
                // Step 1: Click "Randomize enemies" button using coordinates
                // The button appears to be in the bottom-right area of the window
                System.Diagnostics.Debug.WriteLine("Attempting coordinate-based click for 'Randomize enemies' button...");
                
                // Estimate button position - typically in bottom area, right side
                // Adjust these coordinates based on actual button position
                int buttonX = (int)(windowWidth * 0.85); // About 85% from left edge (right side)
                int buttonY = (int)(windowHeight * 0.92); // About 92% from top (lower in bottom area)
                
                System.Diagnostics.Debug.WriteLine($"Clicking Randomize enemies button at ({buttonX}, {buttonY})");
                
                bool clickSuccess = ModAutomationHelper.ClickAtCoordinates(mainWindow, buttonX, buttonY);
                if (!clickSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Coordinate click failed for 'Randomize enemies' button");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("Coordinate click performed for 'Randomize enemies' button");
                
                // Wait for enemy randomization to complete
                bool enemySuccess = ModAutomationHelper.WaitForDS2RandomizationComplete(mainWindow);
                if (!enemySuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Enemy randomization failed or timed out");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("Enemy randomization completed successfully");

                // Step 2: Click the "Item randomizer" tab using coordinates
                System.Diagnostics.Debug.WriteLine("Attempting coordinate-based click for 'Item randomizer' tab...");
                
                // Tab is typically at the top of the window, in the tab bar area
                // Moving higher up to click the actual tab, not sub-elements
                int tabX = (int)(windowWidth * 0.5); // Center of window
                int tabY = (int)(windowHeight * 0.04); // About 4% from top (main tab bar area)
                
                System.Diagnostics.Debug.WriteLine($"Clicking Item randomizer tab at ({tabX}, {tabY})");
                bool tabClickSuccess = ModAutomationHelper.ClickAtCoordinates(mainWindow, tabX, tabY);
                if (!tabClickSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Coordinate click failed for 'Item randomizer' tab");
                    return false;
                }
                
                Thread.Sleep(1000); // Wait for tab switch
                System.Diagnostics.Debug.WriteLine("Switched to Item randomizer tab");

                // Step 3: Click "Randomize items" button using coordinates
                System.Diagnostics.Debug.WriteLine("Attempting coordinate-based click for 'Randomize items' button...");
                
                // Item randomizer button is likely in similar position to enemy button (bottom right)
                int itemButtonX = (int)(windowWidth * 0.85); // About 85% from left edge (right side)
                int itemButtonY = (int)(windowHeight * 0.92); // About 92% from top (lower in bottom area)
                
                System.Diagnostics.Debug.WriteLine($"Clicking Randomize items button at ({itemButtonX}, {itemButtonY})");
                bool itemClickSuccess = ModAutomationHelper.ClickAtCoordinates(mainWindow, itemButtonX, itemButtonY);
                if (!itemClickSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Coordinate click failed for 'Randomize items' button");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("Coordinate click performed for 'Randomize items' button");
                
                // Wait for item randomization to complete
                bool itemSuccess = ModAutomationHelper.WaitForDS2RandomizationComplete(mainWindow);
                if (!itemSuccess)
                {
                    System.Diagnostics.Debug.WriteLine("Item randomization failed or timed out");
                    return false;
                }
                
                System.Diagnostics.Debug.WriteLine("Item randomization completed successfully");

                // Close the window after successful randomization
                ModAutomationHelper.PostMessage(mainWindow, ModAutomationHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                
                // Wait a bit before returning
                Thread.Sleep(2000);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomateRandomizeButton error: {ex.Message}");
                return false;
            }
        }
    }
}
