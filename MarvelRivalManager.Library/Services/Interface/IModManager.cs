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
        ///    Enable a mod
        /// </summary>
        public ValueTask<Mod> Enable(Mod mod);

        /// <summary>
        ///    Disable a mod
        /// </summary>
        public ValueTask<Mod> Disable(Mod mod);

        /// <summary>
        ///     Add a mod to the manager
        /// </summary>
        public ValueTask<Mod> Add(string filepath);

        /// <summary>
        ///     Remove a mod from manager
        /// </summary>
        public void Delete(Mod mod);

        /// <summary>
        ///     Get supported extension for the mods
        /// </summary>
        /// <returns></returns>
        public string[] SupportedExtentensions();
    }
}
