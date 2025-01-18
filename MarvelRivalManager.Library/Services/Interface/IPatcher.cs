using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///    Service related to patching files of the game
    /// </summary>
    public interface IPatcher
    {
        /// <summary>
        ///     Restore the original files of the game
        /// </summary>
        ValueTask<bool> Restore(KindOfMod kind, Action<string> informer);

        /// <summary>
        ///     Restore the original files of the game
        /// </summary>
        ValueTask<bool> HardRestore(KindOfMod kind, Action<string> informer);

        /// <summary>
        ///    Patch the game with the mod content provided
        /// </summary>
        ValueTask<bool> Patch(Action<string> informer);

        /// <summary>
        ///     Enable/Disable the mods
        /// </summary>
        bool Toggle(KindOfMod kind, bool enable, Action<string> informer);
    }
}