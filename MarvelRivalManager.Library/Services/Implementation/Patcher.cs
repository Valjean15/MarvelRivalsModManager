using MarvelRivalManager.Library.Entities;
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

        /// <see cref="IPatcher.Patch(Delegates.Print)"/>
        public async ValueTask<bool> Patch(Delegates.Print informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                await informer("Game content folder is not set. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            await informer("Starting patching folder".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            // Get mods to patch
            var all = await Query.All(true);

            // Remove unused content
            await RemoveUnusedContent(all.Where(mod => !mod.Metadata.Enabled).ToArray(), informer);

            // Get content to patch
            var packed = await Repack.Pack(informer);
            if (packed.Length == 0)
            {
                await informer("The packed content file do not exist. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            if (Configuration.Options.UseParallelLoops)
            {
                await Parallel.ForEachAsync(packed, async (mod, token) => await PatchLocal(informer, mod));
            }
            else
            {
                foreach (var file in packed)
                    await PatchLocal(informer, file);
            }

            // Mark mods as active
            foreach (var mod in all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled))
            {
                mod.Metadata.Active = true;
                mod.Update();
            }

            time.Stop();
            await informer($"Content folder patched successfully - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));

            return true;

            async Task PatchLocal(Delegates.Print informer, string file)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (!File.Exists(file))
                {
                    await informer($"The packed content file ({name}) do not exist. Skipping patch.".AsLog(LogConstants.PATCH));
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

        /// <see cref="IPatcher.Unpatch(Delegates.Print)"/>
        public async ValueTask<bool> Unpatch(Delegates.Print informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                await informer("Game content folder is not set. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            var files = PATCH_FOLDER.GetAllFilesFromDirectory()
                .Where(file => Path.GetFileName(file).StartsWith(PATCH_FILE_PREFIX))
                .ToArray()
                ;

            if (files.Length == 0)
            {
                await informer("The mods patch files do no exist. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            await informer("Deleting mods patch files...".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseParallelLoops)
            {
                await Parallel.ForEachAsync(files, async (file, token) => file.DeleteFileIfExist());
            }
            else
            {
                foreach (var file in files)
                    file.DeleteFileIfExist();
            }

            time.Stop();
            await informer($"Deleting mods patch files complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));
            
            time.Reset();

            await informer("Upating mod status...".AsLog(LogConstants.PATCH));
            time.Start();

            var active = (await Query.All(true)).Where(mod => mod.Metadata.Active);

            if (Configuration.Options.UseParallelLoops)
            {
                await Parallel.ForEachAsync(active, async (mod, token) =>
                {
                    mod.Metadata.Active = false;
                    mod.Update();
                });
            }
            else
            {
                foreach (var mod in active)
                {
                    mod.Metadata.Active = false;
                    mod.Update();
                }
            }

            time.Stop();
            await informer($"Upating mod status complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));

            return true;
        }

        #region Private Methods

        /// <summary>
        ///     Remove content
        /// </summary>
        private async ValueTask RemoveUnusedContent(Mod[] disabled, Delegates.Print informer)
        {
            if (disabled is null || disabled.Length == 0)
                return;

            var content = await Repack.GetUnpackedFolder();
            if (string.IsNullOrEmpty(content))
            {
                await informer("The unpacked content folder do not exist. Skipping clean.".AsLog(LogConstants.PATCH));
                return;
            }

            await informer("Cleaning disabled mods".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseParallelLoops)
            {
                Parallel.ForEach(disabled, mod => DeleteModFile(mod, content));
            }
            else
            {
                foreach (var mod in disabled)
                    await Task.Run(() => DeleteModFile(mod, content));
            }

            time.Stop();
            await informer($"Cleaning disabled mods successfully - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));

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
