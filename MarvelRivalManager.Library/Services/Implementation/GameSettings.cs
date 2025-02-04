using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IGameSettings"/>
    internal class GameSettings : IGameSettings
    {
        private static readonly GameSetting StaticSettings = new();

        /// <see cref="IGameSettings.Get"/>
        public GameSetting Get()
        {
            return StaticSettings;
        }
    }
}