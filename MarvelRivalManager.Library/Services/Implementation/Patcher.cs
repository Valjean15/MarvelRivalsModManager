﻿using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Diagnostics;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IPatcher"/>
    internal class Patcher(IEnvironment configuration, IRepack repack, IModDataAccess query) : IPatcher
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Repack = repack;
        private readonly IModDataAccess Query = query;
        #endregion

        #region Fields

        private string PATCH_FOLDER => Path.Combine(Configuration?.Folders?.GameContent ?? string.Empty, "Paks");
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
                    File.Move(file, Path.Combine(PATCH_FOLDER, string.Format(PATCH_FILENAME_FORMAT, name)), true);
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

            // Look for patch files
            var files = PATCH_FOLDER.GetAllFilesFromDirectory()
                .Where(file => Path.GetFileName(file).StartsWith(PATCH_FILE_PREFIX))
                .ToArray()
                ;

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
            
            time.Reset();

            await informer(["UPDATING_MOD_FILES"], new PrintParams(LogConstants.PATCH));
            time.Start();

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

        #endregion
    }
}
