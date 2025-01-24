using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Diagnostics;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IPatcher"/>
    internal class Patcher(IEnvironment configuration, IRepack repack, IModManager manager) : IPatcher
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Repack = repack;
        private readonly IModManager Manager = manager;
        #endregion

        #region Fields
        private string PATCH_FILE => Path.Combine(Configuration?.Folders?.GameContent ?? string.Empty, "Paks", "pakchunkModManager_P.pak");
        #endregion

        /// <see cref="IPatcher.Patch(Action{string})"/>
        public async ValueTask<bool> Patch(Action<string> informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            var all = Manager.All();
            await RemoveUnusedContent(all.Where(mod => !mod.Metadata.Enabled).ToArray(), informer);

            informer("Starting patching folder".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            var packed = await Repack.Pack(informer);
            if (string.IsNullOrEmpty(packed) || !File.Exists(packed))
            {
                informer("The packed content file do not exist. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            await Task.Run(() => File.Move(packed, PATCH_FILE, true));
            packed.DeleteFileIfExist();

            foreach (var mod in all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled))
            {
                mod.Metadata.Active = true;
                mod.Update();
            }

            time.Stop();
            informer($"Content folder patched successfully - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));

            return true;
        }

        /// <see cref="IPatcher.Unpatch(Action{string})"/>
        public async ValueTask<bool> Unpatch(Action<string> informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            if (!File.Exists(PATCH_FILE))
            {
                informer("The mod patch file do no exist. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            PATCH_FILE.DeleteFileIfExist();
            informer("The mod patch file was deleted".AsLog(LogConstants.PATCH));

            return true;
        }

        #region Private Methods

        /// <summary>
        ///     Remove content
        /// </summary>
        private async ValueTask RemoveUnusedContent(Mod[] disabled, Action<string> informer)
        {
            if (disabled is null || disabled.Length == 0)
                return;

            var unpacked = disabled.Where(mod => mod.Metadata.Unpacked && !mod.Metadata.Active).ToArray();
            if (unpacked.Length == 0)
                return;

            var content = Repack.GetUnpackedFolder();
            if (string.IsNullOrEmpty(content))
            {
                informer("The unpacked content folder do not exist. Skipping clean.".AsLog(LogConstants.PATCH));
                return;
            }

            informer("Cleaning disabled mods".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                foreach (var mod in unpacked)
                {
                    foreach (var relativePath in mod.Metadata.FilePaths ?? [])
                    {
                        var file = Path.Combine(content, relativePath.TrimStart('\\'));
                        file.DeleteFileIfExist();
                    }

                    // Set correct status
                    mod.Metadata.Unpacked = false;
                    mod.Metadata.Active = false;
                    mod.Update();
                }
            });

            time.Stop();
            informer($"Cleaning disabled mods successfully - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));
        }

        #endregion
    }
}
