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
            var destination = Path.Combine(Configuration.Load().Folders.ModsEnabled, Path.GetFileName(filepath));
            filepath.MakeSafeCopy(destination);

            return await Enable(new Mod(destination));
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
                .OrderBy(mod => mod.Metadata.Order)];
        }

        /// <see cref="IModManager.Enable(Mod)"/>
        public async ValueTask<Mod> Enable(Mod mod)
        {
            if (mod.Metadata.Enabled || !await Unpacker.StoreMetadata(mod))
                return mod;

            mod.Metadata.Valid = true;
            mod.Metadata.Enabled = true;
            mod.File.Extraction.DeleteDirectoryIfExists();
            await mod.Metadata.Update(mod.File);
            mod = Move(mod, Configuration.Load().Folders.ModsEnabled);

            return mod;
        }

        /// <see cref="IModManager.Disable(Mod)"/>
        public async ValueTask<Mod> Disable(Mod mod)
        {
            if (!mod.Metadata.Enabled || !await Unpacker.StoreMetadata(mod))
                return mod;

            mod.Metadata.Valid = true;
            mod.Metadata.Enabled = false;
            mod.File.Extraction.DeleteDirectoryIfExists();
            await mod.Metadata.Update(mod.File);
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

            File.Move(mod.File.Filepath, info.Filepath, true);
            File.Move(mod.File.ProfileFilepath, info.ProfileFilepath, true);

            if (!string.IsNullOrEmpty(mod.Metadata.Logo))
                mod.Metadata.Logo.MakeSafeMove(Path.Combine(info.ImagesLocation, Path.GetFileName(mod.Metadata.Logo)));                

            mod.File.Filepath = destination;
            return mod;
        }

        #endregion
    }
}