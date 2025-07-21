using SoulsConfigurator.Games;
using System.Security.Cryptography;
using System.Text;

namespace SoulsConfigurator_Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestInstallDS3()
        {
            var game = new Game_DS3();
            game.InstallPath = @"D:\SteamLibrary\steamapps\common\DARK SOULS III\Game";

            bool success = true;
            try
            {
                success = game.InstallMods(game.Mods);
            }
            catch (Exception)
            {             
            }

            try
            {
                success = game.ClearMods();
            }
            catch (Exception)
            {
            }
            
        }
    }
}