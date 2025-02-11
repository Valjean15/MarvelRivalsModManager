using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Diagnostics;
using System.Linq;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IPatcher"/>
    internal class Patcher(IEnvironment configuration, IRepack repack, IModDataAccess query, IGameSettings game) : IPatcher
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Repack = repack;
        private readonly IModDataAccess Query = query;
        private readonly IGameSettings Game = game;
        #endregion

        #region Fields

        private GameSetting GameSetting => Game.Get();
        private string PATCH_FOLDER => Path.Combine(Configuration.Folders.GameContent ?? string.Empty, GameSetting.PakFilesFolder);
        private string PATCH_MODS_FOLDER => Path.Combine(Configuration.Folders.GameContent ?? string.Empty, GameSetting.ModPakFilesFolder);
        private const string PATCH_FILE_PREFIX = "pakchunkModManager";
        private const string PATCH_FILENAME_FORMAT = "pakchunkModManager_{0}_P.pak";

        #endregion

        /// <see cref="IPatcher.Patch(Delegates.Log)"/>
        public async ValueTask<bool> Patch(Delegates.Log informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                await informer(["GAME_FOLDER_NOT_FOUND", "SKIPPING_PATCH"], new PrintParams(LogConstants.PATCH));
                return false;
            }

            await informer(["STARTING_PATCH"], new PrintParams(LogConstants.PATCH));

            // Toggle patch files
            await TogglePatchFiles(informer, false);

            // Get mods to patch
            var all = await Query.All(true);

            // Remove unused content
            await RemoveUnusedContent(all.Where(mod => !mod.Metadata.Enabled).ToArray(), informer);

            // Get content to patch
            var packed = await Repack.Pack(informer);
            if (packed.Length == 0)
            {
                await informer(["PACKED_FILES_NOT_AVAILABLE", "SKIPPING_PATCH"], new PrintParams(LogConstants.PATCH));
                return false;
            }

            // Remove unsused content
            await RemoveMods(informer);

            // Create the patch folder
            PATCH_FOLDER.CreateDirectoryIfNotExist();
            PATCH_MODS_FOLDER.CreateDirectoryIfNotExist();

            // Patch the content
            var time = Stopwatch.StartNew();
            if (Configuration.Options.UseSingleThread)
            {
                foreach (var file in packed)
                    await PatchLocal(informer, file);
            }
            else
            {
                await Parallel.ForEachAsync(packed, async (mod, token) => await PatchLocal(informer, mod));
            }

            // Mark mods as active
            if (Configuration.Options.UseSingleThread)
            {
                foreach (var mod in all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled))
                {
                    mod.Metadata.Active = true;
                    mod.Update();
                }
            }
            else
            {
                Parallel.ForEach(all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled), mod =>
                {
                    mod.Metadata.Active = true;
                    mod.Update();
                });
            }

            time.Stop();
            await informer(["SUCCESS_PATCH"], new PrintParams(LogConstants.PATCH, time.GetHumaneElapsedTime()));

            return true;

            async ValueTask PatchLocal(Delegates.Log informer, string file)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!File.Exists(file))
                {
                    await informer(["CONTENT_DO_NOT_EXIST", "SKIPPING_PATCH"], new PrintParams(LogConstants.PATCH, Name: name));
                    return;
                }

                // Move the packed file to the game content folder
                await Task.Run(() =>
                {
                    File.Move(file, Path.Combine(PATCH_MODS_FOLDER, string.Format(PATCH_FILENAME_FORMAT, name)), true);
                    file.DeleteFileIfExist();
                });
            }
        }

        /// <see cref="IPatcher.Unpatch(Delegates.Log)"/>
        public async ValueTask<bool> Unpatch(Delegates.Log informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                await informer(["GAME_FOLDER_NOT_FOUND", "SKIPPING_PATCH"], new PrintParams(LogConstants.PATCH));
                return false;
            }

            // Toggle patch files
            await TogglePatchFiles(informer, true);

            // Look for patch files
            await RemoveMods(informer);

            await informer(["UPDATING_MOD_FILES"], new PrintParams(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            // Update the active mods
            var active = (await Query.All(true)).Where(mod => mod.Metadata.Active);
            if (Configuration.Options.UseSingleThread)
            {
                foreach (var mod in active)
                {
                    mod.Metadata.Active = false;
                    mod.Update();
                }
            }
            else
            {
                Parallel.ForEach(active, mod =>
                {
                    mod.Metadata.Active = false;
                    mod.Update();
                });
            }

            time.Stop();
            await informer(["UPDATING_SUCCESS_MOD_FILES"], new PrintParams(LogConstants.PATCH, time.GetHumaneElapsedTime()));

            return true;
        }

        #region Private Methods

        /// <summary>
        ///     Remove content
        /// </summary>
        private async ValueTask RemoveUnusedContent(Mod[] disabled, Delegates.Log informer)
        {
            if (disabled is null || disabled.Length == 0)
                return;

            var content = await Repack.GetUnpackedFolder();
            if (string.IsNullOrEmpty(content))
            {
                await informer(["CONTENT_FOLDER_DO_NOT_EXIST", "SKIPPING_CLEAN"], new PrintParams(LogConstants.PATCH));
                return;
            }

            await informer(["CLEANING_DISABLED_MODS"], new PrintParams(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseSingleThread)
            {
                foreach (var mod in disabled)
                    await Task.Run(() => DeleteModFile(mod, content));
            }
            else
            {
                Parallel.ForEach(disabled, mod => DeleteModFile(mod, content));
            }

            time.Stop();
            await informer(["CLEANING_SUCCESS_DISABLED_MODS"], new PrintParams(LogConstants.PATCH, time.GetHumaneElapsedTime()));

            static void DeleteModFile(Mod mod, string content)
            {
                // If the mod is unpacked and disabled, remove the files
                if (mod.Metadata.Unpacked)
                {
                    foreach (var relativePath in mod.Metadata.FilePaths ?? [])
                    {
                        var file = Path.Combine(content, relativePath.TrimStart('\\'));
                        file.DeleteFileIfExist();
                    }

                    mod.Metadata.Unpacked = false;
                }

                // Set correct status
                mod.Metadata.Active = false;
                mod.Update();
            }
        }

        /// <summary>
        ///     Toggle patch files of the game
        /// </summary>
        /// <returns></returns>
        private async ValueTask TogglePatchFiles(Delegates.Log informer, bool asPatch)
        {
            var files = PATCH_FOLDER.GetAllFilesFromDirectory()
                .Where(file => Path.GetFileName(file).StartsWith(GameSetting.PatchPakFilesFormat))
                .Where(file => Path.GetExtension(file).Equals(".pak"))
                .Where(file =>
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    return asPatch ? !name.EndsWith("_P") : name.EndsWith("_P");
                })
                .ToArray()
                ;

            if (files.Length == 0)
                return;

            await informer(["PATCHING_GAME_PATCH_FILES"], new PrintParams(LogConstants.PATCH));

            if (Configuration.Options.UseSingleThread)
            {
                foreach (var file in files)
                    await TogglePatchFile(file);
            }
            else
            {
                await Parallel.ForEachAsync(files, async (file, token) => await TogglePatchFile(file));
            }

            await informer(["PATCHING_GAME_PATCH_FILES_COMPLETE"], new PrintParams(LogConstants.PATCH));

            async ValueTask TogglePatchFile(string filepath) 
            {
                var directory = Path.GetDirectoryName(filepath);
                var name = Path.GetFileNameWithoutExtension(filepath);
                var newName = asPatch ? string.Concat(name, "_P") : name.Replace("_P", string.Empty);
                await Task.Run(() => filepath.MakeSafeMove(Path.Combine(directory!, $"{newName}.pak")));
            }
        }

        /// <summary>
        ///     Delete all mods files generated from the manager
        /// </summary>
        private async ValueTask<bool> RemoveMods(Delegates.Log informer)
        {
            // Look for mods files
            var files = new string[] { PATCH_FOLDER, PATCH_MODS_FOLDER }
                .SelectMany(directory =>
                {
                    return directory.GetAllFilesFromDirectory()
                        .Where(file => Path.GetFileName(file).StartsWith(PATCH_FILE_PREFIX))
                        .ToArray();
                })
                .ToArray();

            if (files.Length == 0)
            {
                await informer(["MOD_FILES_DO_NOT_EXIST", "SKIPPING_PATCH"], new PrintParams(LogConstants.PATCH));
                return false;
            }

            // Delete the patch files
            await informer(["DELETING_MOD_FILES"], new PrintParams(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseSingleThread)
            {
                foreach (var file in files)
                    file.DeleteFileIfExist();
            }
            else
            {
                Parallel.ForEach(files, file => file.DeleteFileIfExist());
            }

            time.Stop();
            await informer(["DELETING_SUCCESS_MOD_FILES"], new PrintParams(LogConstants.PATCH, time.GetHumaneElapsedTime()));

            return true;
        }

        #endregion
    }
}
