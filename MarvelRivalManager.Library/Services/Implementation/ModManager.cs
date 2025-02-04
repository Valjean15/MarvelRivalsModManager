using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IModManager"/>
    internal class ModManager(IEnvironment configuration, IRepack unpacker, IGameSettings game) : IModManager
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Unpacker = unpacker;
        private readonly IGameSettings Game = game;
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
            // Extract mod
            mod.Metadata.Valid = Configuration.Options.IgnorePackerTool || await Unpacker.CanBeUnPacked(mod);

            // If the mod is valid we can set system information
            if (!Configuration.Options.IgnorePackerTool && mod.Metadata.Valid)
                await Classify(mod, Game.Get());

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
        ///     Clasify mod for internal use
        /// </summary>
        private static async ValueTask Classify(Mod mod, GameSetting game)
        {
            var content = Path.Combine(mod.File.Extraction, game.GameContentFolder);
            if (!Directory.Exists(content))
                return;

            var tagTask = Task.Run(() => {

                var tags = new List<string>();

                if (game.Formats.TryGetValue(mod.File.Extension, out var format))
                    tags.Add(format);

                tags.AddRange(game.Categories
                    .Where(category => EvaluateDirectory(content, category.Key))
                    .Select(category => category.Value)
                    .Distinct()
                    .ToArray());

                tags.AddRange(game.SubCategories
                    .Where(category => EvaluateDirectory(content, category.Key))
                    .Select(category => category.Value)
                    .Distinct()
                    .ToArray());

                mod.Metadata.SystemTags = tags.Distinct().ToArray();
            });

            var filePathTask = Task.Run(() =>
            {
                mod.Metadata.FilePaths = [..
                        content
                            .GetAllFilesFromDirectory()
                            .Select(path => path.Replace(content, string.Empty)
                    )];
            });

            await Task.WhenAll(tagTask, filePathTask);
            mod.Update();
        }

        /// <summary>
        ///     Evaluate a folder to get a tag
        /// </summary>
        private static bool EvaluateDirectory(string folder, string subfolder)
        {
            var parts = subfolder.Split('/');
            if (parts.Length == 0)
                return false;

            if (parts.Length == 1 || !parts.Contains("*"))
                return folder.DirectoryContainsSubfolder(subfolder);

            var main = parts.Last();
            var candidates = Directory.GetDirectories(folder, main, SearchOption.AllDirectories);
            if (candidates.Length == 0)
                return false;

            var parent = parts.First().TrimStart('!');
            var negate = parts.First()[0] == '!';
            foreach (var candidate in candidates)
            {
                if (candidate.Contains($"\\{parent}\\") != negate)
                    return true;
            }

            return false;
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