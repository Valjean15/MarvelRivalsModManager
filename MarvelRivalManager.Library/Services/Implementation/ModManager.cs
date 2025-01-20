using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IModManager"/>
    internal class ModManager(IEnvironment configuration, IUnpacker unpacker) : IModManager
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IUnpacker Unpacker = unpacker;
        #endregion

        /// <see cref="IModManager.Add(string)"/>
        public async ValueTask<Mod> Add(string filepath)
        {
            var configuration = Configuration.Load();
            var destination = Path.Combine(configuration.Folders.ModsEnabled, Path.GetFileName(filepath));

            filepath.MakeSafeCopy(destination);

            new FileInformation(destination).ProfileLocation.CreateDirectoryIfNotExist();
            var mod = new Mod(destination);

            mod.Metadata.Valid = await Unpacker.StoreMetadata(mod);
            mod.Metadata.Enabled = mod.Metadata.Valid;

            await mod.Metadata.Update(mod.File);
            mod.File.Extraction.DeleteDirectoryIfExists();

            mod = Move(mod, mod.Metadata.Enabled 
                ? configuration.Folders.ModsEnabled
                : configuration.Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.Delete(Mod)"/>
        public void Delete(Mod mod)
        {
            mod.Delete();
        }

        /// <see cref="IModManager.All"/>
        public Mod[] All()
        {
            var configuration = Configuration.Load();
            if (configuration is null)
                return [];

            configuration.Folders.ModsEnabled.CreateDirectoryIfNotExist();
            configuration.Folders.ModsDisabled.CreateDirectoryIfNotExist();
            Path.Combine(configuration.Folders.ModsEnabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(configuration.Folders.ModsDisabled, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(configuration.Folders.ModsEnabled, "images").CreateDirectoryIfNotExist();
            Path.Combine(configuration.Folders.ModsDisabled, "images").CreateDirectoryIfNotExist();

            return [.. ExtractMods(configuration.Folders.ModsEnabled)
                .Concat(ExtractMods(configuration.Folders.ModsDisabled))
                .OrderBy(mod => mod.Metadata.Order)
                .ThenBy(mod => mod.Metadata.Name)
                ];
        }

        /// <see cref="IModManager.Enable(Mod)"/>
        public async ValueTask<Mod> Enable(Mod mod)
        {
            if (mod.Metadata.Enabled)
                return mod;

            mod.Metadata.Valid = await Unpacker.StoreMetadata(mod);
            mod.Metadata.Enabled = true;

            await mod.Metadata.Update(mod.File);
            mod.File.Extraction.DeleteDirectoryIfExists();

            mod = Move(mod, Configuration.Load().Folders.ModsEnabled);

            return mod;
        }

        /// <see cref="IModManager.Disable(Mod)"/>
        public async ValueTask<Mod> Disable(Mod mod)
        {
            if (!mod.Metadata.Enabled)
                return mod;

            mod.Metadata.Valid = await Unpacker.StoreMetadata(mod);
            mod.Metadata.Enabled = false;

            await mod.Metadata.Update(mod.File);
            mod.File.Extraction.DeleteDirectoryIfExists();

            mod = Move(mod, Configuration.Load().Folders.ModsDisabled);

            return mod;
        }

        /// <see cref="IModManager.SupportedExtentensions"/>
        public string[] SupportedExtentensions()
        {
            return [".pak", ".zip", ".7z", ".rar"];
        }

        #region Private Methods

        /// <summary>
        ///     Load the mods from the current configuration
        /// </summary>
        private Mod[] ExtractMods(string? path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return [];

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
                if (!info.ImagesLocation.Equals(newLogoLocation))
                {
                    mod.Metadata.Logo.MakeSafeMove(newLogoLocation);
                    mod.Metadata.Logo = newLogoLocation;
                }
            }               

            mod.File.Filepath = destination;
            return mod;
        }

        #endregion
    }
}