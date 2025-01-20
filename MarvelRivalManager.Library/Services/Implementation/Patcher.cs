using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using System.Diagnostics;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IPatcher"/>
    internal class Patcher(IEnvironment configuration, IUnpacker unpacker, IModManager manager, IDirectoryCheker cheker) : IPatcher
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IUnpacker Unpacker = unpacker;
        private readonly IModManager Manager = manager;
        private readonly IDirectoryCheker Cheker = cheker;
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
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(LogConstants.PATCH));
                return false;
            }

            var content = Unpacker.GetExtractionFolder(informer);
            if (string.IsNullOrEmpty(content) || !Directory.Exists(content))
            {
                informer("The unpacked content folder do not exist. Skipping patch.".AsLog(LogConstants.PATCH));
                return false;
            }

            var all = Manager.All();
            await RemoveUnusedContent(all.Where(mod => !mod.Metadata.Enabled).ToArray(), informer);

            informer("Starting patching folder".AsLog(LogConstants.PATCH));
            var time = Stopwatch.StartNew();

            await content.MergeDirectoryAsync(Configuration.Folders.GameContent);
            foreach (var mod in all.Where(mod => mod.Metadata.Unpacked && mod.Metadata.Enabled))
            {
                mod.Metadata.Active = true;
                mod.Update();
            }

            time.Stop();
            informer($"Content folder patched successfully - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PATCH));

            return true;
        }

        /// <see cref="IPatcher.Toggle(KindOfMod, bool, Action{string})"/>
        public bool Toggle(KindOfMod kind, bool enable, Action<string> informer)
        {
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(LogConstants.TOGGLE));
                return false;
            }

            var source = enable ? ".pak" : ".backup";
            var destination = enable ? ".backup" : ".pak";
            var folder = Path.Combine(Configuration.Folders.GameContent, "Paks");

            string[] files = kind switch
            {
                KindOfMod.All => [
                    Path.Combine(folder, $"pakchunkCharacter-Windows{source}"),
                    Path.Combine(folder, $"pakchunkHQ-Windows{source}"),
                    Path.Combine(folder, $"pakchunkLQ-Windows{source}"),
                    Path.Combine(folder, $"pakchunkMovies-Windows{source}"),
                    Path.Combine(folder, $"pakchunkWwise-Windows{source}")
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
                KindOfMod.Audio => [
                    Path.Combine(folder, $"pakchunkWwise-Windows{source}")
                ],
                _ => throw new NotImplementedException("Invalid kind of mod."),
            };

            informer($"{(enable ? "Enabling" : "Disabling")} {kind} mods".AsLog(LogConstants.TOGGLE));
            foreach (var file in files)
            {
                informer(file.ChangeExtensionIfExists(destination) 
                    ? $"File {Path.GetFileName(file)} found".AsLog(LogConstants.TOGGLE) 
                    : $"File {Path.GetFileName(file)} not found. Skipping toggle.".AsLog(LogConstants.TOGGLE)
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
            if (string.IsNullOrWhiteSpace(Configuration.Folders?.GameContent))
            {
                informer("Game content folder is not set. Skipping restore.".AsLog(LogConstants.RESTORE));
                return false;
            }

            var backup = GetValidBackupResources(kind, informer);
            if (backup.Length == 0)
            {
                informer("No valid backup folder were found".AsLog(LogConstants.RESTORE));
                return false;
            }

            informer("Restoring files...".AsLog(LogConstants.RESTORE));
            var time = Stopwatch.StartNew();

            var task = Task.Run(() =>
            {
                foreach (var folder in backup.Where(x => !string.IsNullOrWhiteSpace(x)))
                    folder.MergeDirectory(Configuration!.Folders.GameContent, dooverride);
            });

            await task;

            time.Stop();
            informer($"Files restored - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.RESTORE));

            return true;
        }

        /// <summary>
        ///     Get valid backup resources
        /// </summary>
        private string[] GetValidBackupResources(KindOfMod kind, Action<string> informer)
        {
            if (kind.Equals(KindOfMod.All))
            {
                return new KindOfMod[] { KindOfMod.Characters, KindOfMod.UI, KindOfMod.Movies, KindOfMod.Audio }
                    .SelectMany(category => GetValidBackupResources(category, informer))
                    .ToArray();
            }

            (KindOfMod category, string folder)[] folders = kind switch
            {
                KindOfMod.Characters => [(KindOfMod.Characters, Configuration.Folders?.BackupResources?.Characters ?? string.Empty)],
                KindOfMod.UI => [(KindOfMod.UI, Configuration.Folders?.BackupResources?.Ui ?? string.Empty)],
                KindOfMod.Movies => [(KindOfMod.Movies, Configuration.Folders?.BackupResources?.Movies ?? string.Empty)],
                KindOfMod.Audio => [(KindOfMod.Audio, Configuration.Folders?.BackupResources?.Audio ?? string.Empty)],
                _ => throw new NotImplementedException("Invalid kind of mod."),
            };

            return folders
                .Where(tuple =>
                {
                    if (string.IsNullOrEmpty(tuple.folder) || !Directory.Exists(tuple.folder))
                    {
                        informer($"Backup folder for category {tuple.category} cannot be found".AsLog(LogConstants.RESTORE));
                        return false;
                    }

                    return Cheker.BackupResource(tuple.category);
                })
                .Select(tuple => tuple.folder)
                .ToArray();
        }

        /// <summary>
        ///     Remove content
        /// </summary>
        private async ValueTask RemoveUnusedContent(Mod[] disabled, Action<string> informer)
        {
            if (disabled is null || disabled.Length == 0)
                return;

            if (Configuration.Folders?.BackupResources is null)
            {
                informer("Backup content folder is not set. Skipping restore.".AsLog(LogConstants.PATCH));
                return;
            }

            var unpacked = disabled.Where(mod => mod.Metadata.Unpacked && !mod.Metadata.Active).ToArray();
            if (unpacked.Length > 0)
            {
                informer("Cleaning disabled mods unpacked".AsLog(LogConstants.PATCH));

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
                        mod.Update();
                    }
                });
            }
            
            var patched = disabled.Where(mod => mod.Metadata.Active).ToArray();
            if (patched.Length > 0)
            {
                informer("Cleaning disabled mods already patched".AsLog(LogConstants.PATCH));
                await Task.Run(() =>
                {
                    foreach (var mod in patched)
                    {
                        foreach (var relativePath in mod.Metadata.FilePaths ?? [])
                        {
                            var backup = LookupOnBackup(Configuration.Folders.BackupResources, relativePath);
                            if (string.IsNullOrEmpty(backup))
                            {
                                informer($"File for restore from backup cannot be found => {relativePath}".AsLog(LogConstants.PATCH));
                                continue;
                            }

                            Path.Combine(Configuration.Folders.GameContent, relativePath.TrimStart('\\'))
                                .OverrideFileWith(backup);
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
            foreach (var kind in new KindOfMod[] { KindOfMod.Characters, KindOfMod.UI, KindOfMod.Movies, KindOfMod.Audio })
            {
                var files = GetFiles(kind);
                if (files is null || files.Count == 0)
                    continue;

                var target = files.FirstOrDefault(x => x.EndsWith(file)) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(target))
                    return target;
            }   

            return string.Empty;

            List<string> GetFiles(KindOfMod kind) 
            {
                if (Resources.TryGetValue(kind, out var files))
                    return files;

                var folder = backups.Get(kind);
                Resources.Add(kind, string.IsNullOrEmpty(folder) ? [] : folder.GetAllFilesFromDirectory());
                return Resources[kind];
            }
        }

        #endregion
    }
}
