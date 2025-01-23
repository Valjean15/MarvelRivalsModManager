using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to unpack files
    /// </summary>
    public interface IRepack
    {
        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask<bool> CanBeUnPacked(Mod mods);

        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask Unpack(Mod[] mods, Action<string> informer);

        /// <summary>
        ///     Get the folder of the unpacked mods
        /// </summary>
        string GetUnpackedFolder(Action<string>? informer = null);

        /// <summary>
        ///     Get the file (packed) of the unpacked mods
        /// </summary>
        ValueTask<string> Pack(Action<string> informer);
    }
}
