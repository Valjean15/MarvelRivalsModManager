using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to get settings of the game
    /// </summary>
    public interface IGameSettings
    {
        public GameSetting Get();
    }
}
