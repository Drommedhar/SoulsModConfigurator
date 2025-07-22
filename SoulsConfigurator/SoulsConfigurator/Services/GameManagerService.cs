using SoulsConfigurator.Games;
using SoulsConfigurator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SoulsConfigurator.Services
{
    public class GameManagerService
    {
        private readonly List<IGame> _availableGames;
        private readonly SettingsService _settingsService;
        private IGame? _selectedGame;

        public GameManagerService()
        {
            _settingsService = new SettingsService();
            _availableGames = new List<IGame>
            {
                // Only DS3 and Sekiro are fully implemented for now
                new Game_DS2(),
                new Game_DS3(),
                new Game_Sekiro()
                
                // TODO: Enable these when fully implemented
                // new Game_DS1(),
                // new Game_DS2(),
            };

            LoadSavedPaths();
        }

        private void LoadSavedPaths()
        {
            foreach (var game in _availableGames)
            {
                var savedPath = _settingsService.GetGamePath(game.Name);
                if (!string.IsNullOrEmpty(savedPath) && game.ValidateInstallPath(savedPath))
                {
                    game.InstallPath = savedPath;
                }
            }
            
            // Auto-select the first game if there's only one available
            if (_availableGames.Count == 1)
            {
                _selectedGame = _availableGames[0];
            }
        }

        public List<IGame> GetAvailableGames()
        {
            return _availableGames;
        }

        public IGame? GetSelectedGame()
        {
            return _selectedGame;
        }

        public void SelectGame(IGame game)
        {
            _selectedGame = game;
        }

        public void SelectGame(string gameName)
        {
            _selectedGame = _availableGames.FirstOrDefault(g => g.Name == gameName);
        }

        public List<IMod> GetAvailableModsForSelectedGame()
        {
            return _selectedGame?.Mods ?? new List<IMod>();
        }

        public bool InstallSelectedMods(List<IMod> modsToInstall)
        {
            if (_selectedGame == null || string.IsNullOrEmpty(_selectedGame.InstallPath))
            {
                return false;
            }

            _selectedGame.ClearMods(); // Clear existing mods before installing new ones
            return _selectedGame.InstallMods(modsToInstall);
        }

        public bool ClearAllMods()
        {
            if (_selectedGame == null || string.IsNullOrEmpty(_selectedGame.InstallPath))
            {
                return false;
            }

            return _selectedGame.ClearMods();
        }

        public bool SetGameInstallPath(string installPath)
        {
            if (_selectedGame == null)
                return false;

            if (!_selectedGame.ValidateInstallPath(installPath))
                return false;

            _selectedGame.InstallPath = installPath;
            _settingsService.SaveGamePath(_selectedGame.Name, installPath);
            return true;
        }

        public bool ValidateInstallPath(string installPath)
        {
            return _selectedGame?.ValidateInstallPath(installPath) ?? false;
        }

        public string GetValidationMessage()
        {
            if (_selectedGame == null)
                return "No game selected.";

            return _selectedGame.Name switch
            {
                "Dark Souls 1" => $"Please select the 'DARK SOULS REMASTERED' folder containing '{_selectedGame.GetExpectedExecutableName()}'",
                "Dark Souls 2" => $"Please select the 'Game' folder containing '{_selectedGame.GetExpectedExecutableName()}'",
                "Dark Souls 3" => $"Please select the 'Game' folder containing '{_selectedGame.GetExpectedExecutableName()}'",
                "Sekiro: Shadows Die Twice" => $"Please select the main game folder containing '{_selectedGame.GetExpectedExecutableName()}'",
                _ => $"Please select the correct game folder containing '{_selectedGame.GetExpectedExecutableName()}'"
            };
        }
    }
}
