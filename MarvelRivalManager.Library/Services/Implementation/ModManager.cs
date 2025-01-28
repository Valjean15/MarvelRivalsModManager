using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IModManager"/>
    internal class ModManager(IEnvironment configuration, IRepack unpacker) : IModManager
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Unpacker = unpacker;
        #endregion

        /// <see cref="IModManager.Add(string)"/>
        public async ValueTask<Mod> Add(string filepath)
        {
            var mod = await Evaluate(new Mod(filepath));
            mod.Metadata.Enabled = mod.Metadata.Valid;

            Move(mod, mod.Metadata.Enabled
                ? Configuration.Folders.ModsEnabled
                : Configuration.Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.Update(Mod)"/>
        public async ValueTask<Mod> Update(Mod mod)
        {
            if (Configuration.Options.EvaluateOnUpdate)
                mod = await Evaluate(mod);

            Move(mod, mod.Metadata.Enabled
                ? Configuration.Folders.ModsEnabled
                : Configuration.Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.Evaluate(Mod)"/>
        public async ValueTask<Mod> Evaluate(Mod mod)
        {
            // Evaluate mod
            mod.Metadata.Valid = Configuration.Options.IgnorePackerTool || await Unpacker.CanBeUnPacked(mod);

            // If the mod is valid we can set system information
            if (!Configuration.Options.IgnorePackerTool && mod.Metadata.Valid)
            {
                await mod.SetSystemInformation();
                mod.Update();
            }

            mod.File.Extraction.DeleteDirectoryIfExists();

            return mod;
        }

        /// <see cref="IModManager.Delete(Mod)"/>
        public void Delete(Mod mod)
        {
            mod.Delete();
        }

        #region Private Methods

        /// <summary>
        ///     Move mod to a specific folder
        /// </summary>
        private static Mod Move(Mod mod, string folder)
        {
            var destination = Path.Combine(folder, $"{mod.File.Filename}{mod.File.Extension}");
            var info = new FileInformation(destination);

            if (!mod.File.Filepath.Equals(info.Filepath))
                File.Move(mod.File.Filepath, info.Filepath, true);

            if (!mod.File.ProfileFilepath.Equals(info.ProfileFilepath))
                File.Move(mod.File.ProfileFilepath, info.ProfileFilepath, true);

            if (!string.IsNullOrEmpty(mod.Metadata.Logo))
            {
                var newLogoLocation = Path.Combine(info.ImagesLocation, Path.GetFileName(mod.Metadata.Logo));
                if (!mod.Metadata.Logo.Equals(newLogoLocation))
                    mod.Metadata.Logo = mod.Metadata.Logo.MakeSafeMove(newLogoLocation);
            }               

            mod.File.Filepath = destination;
            mod.Update();
            return mod;
        }

        #endregion
    }
}