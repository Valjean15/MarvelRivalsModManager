using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Diagnostics;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IPatcher"/>
    internal class Patcher(IEnvironment configuration, IUnpacker unpacker, IModManager manager) : IPatcher
    {
        #region Constants
        private const string RESTORE = "RSTRE";
        private const string TOGGLE = "TGGLE";
        private const string PATCH = "PATCH";
        #endregion

        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IUnpacker Unpacker = unpacker;
        private readonly IModManager Manager = manager;
        #endregion

        #region Cache
        private readonly Dictionary<KindOfMod, List<string>> Resources = [];
        #endregion

        /// <see cref="IPatcher.Restore(KindOfMod)"/>
        public async ValueTask<bool> Restore(KindOfMod kind, Action<string> informer)
        {
            return await Restore(kind, false, informer);
        }

        /// <see cref="IPatcher.HardRestore(KindOfMod, Action{string})"/>
        public async ValueTask<bool> HardRestore(KindOfMod kind, Action<string> informer)
        {
            return await Restore(kind, true, informer);
        }

        /// <see cref="IPatcher.Patch(string, Action{string})"/>
        public async ValueTask<bool> Patch(Action<string> informer)
        {
            var configuration = Configuration.Load();
            if (configuration is null || string.IsNullOrWhiteSpace(configuration?.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(PATCH));
                return false;
            }

            var content = Unpacker.GetExtractionFolder();
            if (string.IsNullOrEmpty(content) || !Directory.Exists(content))
            {
                informer("Content folder is not set. Skipping patch.".AsLog(PATCH));
                return false;
            }

            var all = Manager.All();
            await RemoveUnusedContent(all.Where(mod => !mod.Metadata.Enabled).ToArray(), informer);

            informer("Starting patching folder".AsLog(PATCH));
            var time = Stopwatch.StartNew();

            await content.MergeDirectoryAsync(configuration!.Folders.GameContent);
            foreach (var mod in all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled))
            {
                mod.Metadata.Active = true;
                mod.Update();
            }

            time.Stop();
            informer($"Content folder patched successfully - {Math.Round((decimal)(time.ElapsedMilliseconds / 6000L), 2)} min".AsLog(PATCH));

            return true;
        }

        /// <see cref="IPatcher.Toggle(KindOfMod, bool, Action{string})"/>
        public bool Toggle(KindOfMod kind, bool enable, Action<string> informer)
        {
            var configuration = Configuration.Load();
            if (configuration is null || string.IsNullOrWhiteSpace(configuration?.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(TOGGLE));
                return false;
            }

            var source = enable ? ".pak" : ".backup";
            var destination = enable ? ".backup" : ".pak";
            var folder = Path.Combine(configuration.Folders.GameContent, "Paks");

            string[] files = kind switch
            {
                KindOfMod.All => [
                    Path.Combine(folder, $"pakchunkCharacter-Windows{source}"),
                    Path.Combine(folder, $"pakchunkHQ-Windows{source}"),
                    Path.Combine(folder, $"pakchunkLQ-Windows{source}"),
                    Path.Combine(folder, $"pakchunkMovies-Windows{source}")
                    ],
                KindOfMod.Characters => [
                    Path.Combine(folder, $"pakchunkCharacter-Windows{source}")
                    ],
                KindOfMod.UI => [
                    Path.Combine(folder, $"pakchunkHQ-Windows{source}"),
                    Path.Combine(folder, $"pakchunkLQ-Windows{source}")
                    ],
                KindOfMod.Movies => [
                    Path.Combine(folder, $"pakchunkMovies-Windows{source}")
                    ],
                _ => throw new NotImplementedException("Invalid kind of mod."),
            };

            informer($"{(enable ? "Enabling" : "Disabling")} {kind} mods".AsLog(TOGGLE));
            foreach (var file in files)
            {
                informer(file.ChangeExtensionIfExists(destination) 
                    ? $"File {file} found".AsLog(TOGGLE) 
                    : $"File {file} not found. Skipping toggle.".AsLog(TOGGLE)
                );
            }

            return true;
        }

        #region Private Methods

        /// <summary>
        ///    Restore the original files of the game
        /// </summary>
        private async ValueTask<bool> Restore(KindOfMod kind, bool dooverride, Action<string> informer)
        {
            var configuration = Configuration.Load();
            if (configuration is null || string.IsNullOrWhiteSpace(configuration?.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(RESTORE));
                return false;
            }

            string[] backup = kind switch
            {
                KindOfMod.All => [
                    configuration?.Folders?.BackupResources?.Characters ?? string.Empty,
                    configuration?.Folders?.BackupResources?.Ui ?? string.Empty,
                    configuration?.Folders?.BackupResources?.Movies ?? string.Empty
                    ],
                KindOfMod.Characters => [configuration?.Folders?.BackupResources?.Characters ?? string.Empty],
                KindOfMod.UI => [configuration?.Folders?.BackupResources?.Ui ?? string.Empty],
                KindOfMod.Movies => [configuration?.Folders?.BackupResources?.Movies ?? string.Empty],
                _ => throw new NotImplementedException("Invalid kind of mod."),
            };

            informer("Restoring files...".AsLog(RESTORE));
            var time = Stopwatch.StartNew();

            var task = Task.Run(() =>
            {
                foreach (var folder in backup.Where(x => !string.IsNullOrWhiteSpace(x)))
                    folder.MergeDirectory(configuration!.Folders.GameContent, dooverride);
            });

            await task;

            time.Stop();
            informer($"Files restored - {Math.Round((decimal)(time.ElapsedMilliseconds / 6000L), 2)} min".AsLog(RESTORE));

            return true;
        }

        /// <summary>
        ///     Remove content
        /// </summary>
        private async ValueTask RemoveUnusedContent(Mod[] disabled, Action<string> informer)
        {
            if (disabled is null || disabled.Length == 0)
                return;

            var configuration = Configuration.Load();
            if (configuration?.Folders?.BackupResources is null)
            {
                informer("Backup content folder is not set. Skipping restore.".AsLog(PATCH));
                return;
            }

            var unpacked = disabled.Where(mod => mod.Metadata.Unpacked && !mod.Metadata.Active).ToArray();
            if (unpacked.Length > 0)
            {
                informer("Cleaning disabled mods unpacked".AsLog(PATCH));

                var content = Unpacker.GetExtractionFolder();

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
            }
            
            var patched = disabled.Where(mod => mod.Metadata.Active).ToArray();
            if (patched.Length > 0)
            {
                informer("Cleaning disabled mods already patched".AsLog(PATCH));
                await Task.Run(() =>
                {
                    foreach (var mod in patched)
                    {
                        foreach (var relativePath in mod.Metadata.FilePaths ?? [])
                        {
                            Path.Combine(configuration.Folders.GameContent, relativePath.TrimStart('\\'))
                                .OverrideFileWith(
                                    LookupOnBackup(configuration.Folders.BackupResources, relativePath));
                        }

                        // Set correct status
                        mod.Metadata.Unpacked = false;
                        mod.Metadata.Active = false;
                        mod.Update();
                    }
                });
            }
        }

        /// <summary>
        ///     Lookup on backup folder for the file
        /// </summary>
        private string LookupOnBackup(BackupFolders backups, string file)
        {
            var target = LookupOnFolder(KindOfMod.Characters);
            if (!string.IsNullOrWhiteSpace(target))
                return target;

            target = LookupOnFolder(KindOfMod.UI);
            if (!string.IsNullOrWhiteSpace(target))
                return target;

            target = LookupOnFolder(KindOfMod.Movies);
            if (!string.IsNullOrWhiteSpace(target))
                return target;

            return string.Empty;

            string LookupOnFolder(KindOfMod kind)
            {
                var files = GetFiles(kind);
                if (files is null || files.Count == 0)
                    return string.Empty;

                return files.FirstOrDefault(x => x.EndsWith(file)) ?? string.Empty;
            }

            List<string> GetFiles(KindOfMod kind) 
            {
                if (Resources.TryGetValue(kind, out var files))
                    return files;

                var folder = kind switch
                {
                    KindOfMod.Characters => backups.Characters,
                    KindOfMod.UI => backups.Ui,
                    KindOfMod.Movies => backups.Movies,
                    _ => string.Empty,
                };

                Resources.Add(kind, string.IsNullOrEmpty(folder) ? [] : folder.GetAllFilesFromDirectory());
                return Resources[kind];
            }
        }

        #endregion
    }
}
