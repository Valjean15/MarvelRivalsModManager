using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Collections.Concurrent;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IModDataAccess"/>
    internal class ModDataAccess(IEnvironment configuration) : IModDataAccess
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        #endregion

        #region Fields
        private Mod[]? Cache = null;
        #endregion

        /// <see cref="IModDataAccess.All(bool)"/>
        public async ValueTask<Mod[]> All(bool reload = false)
        {
            ValidateFoldersStructure(Configuration.Folders.ModsEnabled);
            ValidateFoldersStructure(Configuration.Folders.ModsDisabled);

            if (Cache is not null && !reload)
                return Cache;

            var enabled = await ExtractMods(Configuration.Folders.ModsEnabled);
            var disabled = await ExtractMods(Configuration.Folders.ModsDisabled);

            Cache = [.. enabled.Concat(disabled)
                .OrderBy(mod => mod.Metadata.Order)
                .ThenBy(mod => mod.Metadata.Name)];

            return Cache;
        }

        /// <see cref="IModDataAccess.AllFilepaths(bool)"/>
        public async ValueTask<string[]> AllFilepaths(bool reload = false)
        {
            return (await All(reload))
                .Select(mod => mod.File.Filepath)
                .ToArray();
        }

        /// <see cref="IModDataAccess.SupportedExtentensions"/>
        public string[] SupportedExtentensions()
        {
            return [".pak", ".zip", ".7z", ".rar"];
        }

        #region Private methods

        /// <summary>
        ///     Load the mods from the current configuration
        /// </summary>
        private async ValueTask<Mod[]> ExtractMods(string path)
        {
            var patterns = SupportedExtentensions().Select(extension => $"*{extension}");
            var files = await Task.Run(() => patterns.SelectMany(pattern => Directory.GetFiles(path, pattern)).ToArray());

            var mods = new ConcurrentBag<Mod>();
            Parallel.ForEach(files, file => { mods.Add(new Mod(file)); });
            return [.. mods];
        }

        /// <summary>
        ///     Validate the folder structure for the mods folder
        /// </summary>
        private static void ValidateFoldersStructure(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return;

            folder.CreateDirectoryIfNotExist();
            Path.Combine(folder, "profiles").CreateDirectoryIfNotExist();
            Path.Combine(folder, "images").CreateDirectoryIfNotExist();
        }

        #endregion
    }
}