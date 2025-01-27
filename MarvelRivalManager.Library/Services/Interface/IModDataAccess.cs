using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service to access mod data only
    /// </summary>
    public interface IModDataAccess
    {
        /// <summary>
        ///    Get all mods
        /// </summary>
        public ValueTask<Mod[]> All(bool reload = false);

        /// <summary>
        ///    Get all mods filepaths
        /// </summary>
        public ValueTask<string[]> AllFilepaths(bool reload = false);

        /// <summary>
        ///     Get supported extension for the mods
        /// </summary>
        public string[] SupportedExtentensions();
    }
}