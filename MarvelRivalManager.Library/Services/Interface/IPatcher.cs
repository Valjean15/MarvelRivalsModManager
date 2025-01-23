namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///    Service related to patching files of the game
    /// </summary>
    public interface IPatcher
    {
        /// <summary>
        ///    Patch the game with the mod content provided
        /// </summary>
        ValueTask<bool> Patch(Action<string> informer);

        /// <summary>
        ///    Unpatch the game with the mod content provided
        /// </summary>
        ValueTask<bool> Unpatch(Action<string> informer);
    }
}