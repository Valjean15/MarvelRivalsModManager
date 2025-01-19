using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to unpack files
    /// </summary>
    public interface IUnpacker
    {
        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask<bool> StoreMetadata(Mod mods);

        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask Unpack(Mod[] mods, Action<string> informer);

        /// <summary>
        ///     Get the extraction folder
        /// </summary>
        string GetExtractionFolder(Action<string>? informer = null);
    }
}
