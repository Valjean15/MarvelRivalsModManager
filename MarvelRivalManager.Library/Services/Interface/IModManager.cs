using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Manager of the pak mods
    /// </summary>
    public interface IModManager
    {
        /// <summary>
        ///     Get all mods
        /// </summary>
        public Mod[] All();

        /// <summary>
        ///     Get all mods
        /// </summary>
        public string[] AllAsFilepaths();

        #region CRUD

        /// <summary>
        ///     Add a mod to the manager
        /// </summary>
        public ValueTask<Mod> Add(string filepath);

        /// <summary>
        ///     Update a mod from the manager
        /// </summary>
        public ValueTask<Mod> Update(Mod mod);

        /// <summary>
        ///     Remove a mod from manager
        /// </summary>
        public void Delete(Mod mod);

        #endregion

        /// <summary>
        ///     Get supported extension for the mods
        /// </summary>
        /// <returns></returns>
        public string[] SupportedExtentensions();
    }
}
