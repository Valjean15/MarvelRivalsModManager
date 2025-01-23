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

        /// <see cref="IModManager.All"/>
        public Mod[] All()
        {
            Configuration.Folders.ModsEnabled.CreateDirectoryIfNotExist();
            Configuration.Folders.ModsDisabled.CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsEnabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsDisabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsEnabled, "images").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsDisabled, "images").CreateDirectoryIfNotExist();

            return [.. ExtractMods(Configuration.Folders.ModsEnabled)
                .Concat(ExtractMods(Configuration.Folders.ModsDisabled))
                .OrderBy(mod => mod.Metadata.Order)
                .ThenBy(mod => mod.Metadata.Name)
                ];
        }

        /// <see cref="IModManager.AllAsFilepaths"/>
        public string[] AllAsFilepaths()
        {
            Configuration.Folders.ModsEnabled.CreateDirectoryIfNotExist();
            Configuration.Folders.ModsDisabled.CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsEnabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsDisabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsEnabled, "images").CreateDirectoryIfNotExist();
            Path.Combine(Configuration.Folders.ModsDisabled, "images").CreateDirectoryIfNotExist();

            var patterns = SupportedExtentensions().Select(extension => $"*{extension}");
            var enabled = patterns.SelectMany(pattern => Directory.GetFiles(Configuration.Folders.ModsEnabled, pattern)).ToArray();
            var disabled = patterns.SelectMany(pattern => Directory.GetFiles(Configuration.Folders.ModsDisabled, pattern)).ToArray();

            return [.. enabled, .. disabled];
        }

        #region CRUD

        /// <see cref="IModManager.Add(string)"/>
        public async ValueTask<Mod> Add(string filepath)
        {
            var mod = new Mod(filepath);

            // Evaluate mod
            mod.Metadata.Valid = await Unpacker.CanBeUnPacked(mod);
            mod.Metadata.Enabled = mod.Metadata.Valid;

            // If the mod is valid we can set system information
            if (mod.Metadata.Valid)
            {
                await mod.SetSystemInformation();
                mod.Update();
            }

            mod.File.Extraction.DeleteDirectoryIfExists();

            Move(mod, mod.Metadata.Enabled
                ? Configuration.Folders.ModsEnabled
                : Configuration.Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.Update(Mod)"/>
        public async ValueTask<Mod> Update(Mod mod)
        {
            // Evaluate mod
            mod.Metadata.Valid = await Unpacker.CanBeUnPacked(mod);

            // If the mod is valid we can set system information
            if (mod.Metadata.Valid)
            {
                await mod.SetSystemInformation();
                mod.Update();
            }

            mod.File.Extraction.DeleteDirectoryIfExists();

            Move(mod, mod.Metadata.Enabled
                ? Configuration.Folders.ModsEnabled
                : Configuration.Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.Delete(Mod)"/>
        public void Delete(Mod mod)
        {
            mod.Delete();
        }

        #endregion

        /// <see cref="IModManager.SupportedExtentensions"/>
        public string[] SupportedExtentensions()
        {
            return [".pak", ".zip", ".7z", ".rar"];
        }

        #region Private Methods

        /// <summary>
        ///     Load the mods from the current configuration
        /// </summary>
        private Mod[] ExtractMods(string path)
        {
            var patterns = SupportedExtentensions().Select(extension =>  $"*{extension}");
            var files = patterns.SelectMany(pattern => Directory.GetFiles(path, pattern)).ToArray();
            return files.Select(file => new Mod(file)).ToArray();
        }


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