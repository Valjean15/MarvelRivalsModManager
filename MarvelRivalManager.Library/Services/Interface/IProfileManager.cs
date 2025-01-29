using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Service dedicated to the management of the profiles
    /// </summary>
    public interface IProfileManager
    {
        /// <summary>
        ///     Get the current profile
        /// </summary>
        public ValueTask<Profile> GetCurrent();

        /// <summary>
        ///     Get all profiles
        /// </summary>
        public ValueTask<Profile[]> All(bool reload = false);

        /// <summary>
        ///     Load the profile
        /// </summary>
        public ValueTask Load(Profile profile);

        /// <summary>
        ///     Save the profile
        /// </summary>
        public ValueTask<Profile> Create(string name, Mod[] mods);

        /// <summary>
        ///     Update the profile
        /// </summary>
        public ValueTask<Profile> Update(Profile profile);

        /// <summary>
        ///    Delete the profile
        /// </summary>
        public ValueTask Delete(Profile profile);
    }
}
