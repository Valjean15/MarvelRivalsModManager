using MarvelRivalManager.Library.Entities;
using static MarvelRivalManager.Library.Entities.Delegates;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to unpack files
    /// </summary>
    public interface IRepack
    {
        /// <summary>
        ///     Validate if the repak tool is present in the application
        /// </summary>
        public ValueTask<bool> IsAvailable();

        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask<bool> CanBeUnPacked(Mod mod);

        /// <summary>
        ///     Unpack the mods into the extraction folder
        /// </summary>
        ValueTask<bool> Unpack(Print informer);

        /// <summary>
        ///     Get the folder of the unpacked mods
        /// </summary>
        ValueTask<string> GetUnpackedFolder(Print? informer = null);

        /// <summary>
        ///     Get the file (packed) of the unpacked mods
        /// </summary>
        ValueTask<string[]> Pack(Print informer);
    }
}
