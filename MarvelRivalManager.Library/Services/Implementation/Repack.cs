﻿using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IRepack"/>
    internal class Repack(IEnvironment configuration, IModDataAccess query, IGameSettings game) : IRepack
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IModDataAccess Query = query;
        private readonly IGameSettings Game = game;
        #endregion

        #region Properties
        private string Executable => $"{Configuration?.Folders?.RepackFolder}/repak.exe";
        private string ExtractionFolder => $"{Configuration?.Folders?.RepackFolder}/extraction";
        private GameSetting GameSettings => Game.Get();
        #endregion

        /// <see cref="IRepack.IsAvailable"/>
        public async ValueTask<bool> IsAvailable()
        {
            return await ValidateConfiguration();
        }

        /// <see cref="IRepack.CanBeUnPacked(Mod)"/>
        public async ValueTask<bool> CanBeUnPacked(Mod mod)
        {
            return await ValidateConfiguration() && await UnpackInternal(mod);
        }

        /// <see cref="IRepack.Unpack(Delegates.Log)"/>
        public async ValueTask<bool> Unpack(Delegates.Log informer)
        {
            if (!await ValidateConfiguration(informer))
                return false;

            // Remove old content
            if (!Directory.Exists(ExtractionFolder))
            {
                await informer(["CREATING_EXTRACTION_FOLDER"], new PrintParams(LogConstants.UNPACK));
                ExtractionFolder.CreateDirectoryIfNotExist();
            }
            else
            {
                ExtractionFolder.DeleteDirectoryContent();
            }

            // Get all mods
            var mods = (await Query.All(true)) ?? [];

            // Update unused content
            await UpdateUnusedMods(informer, mods.Where(x => !x.Metadata.Valid).ToArray(), "invalid");
            await UpdateUnusedMods(informer, mods.Where(x => x.Metadata.Valid && !x.Metadata.Enabled).ToArray(), "disabled");

            // Get mods to unpack
            var enabled = mods.Where(x => x.Metadata.Valid && x.Metadata.Enabled).ToArray();
            if (enabled.Length == 0)
            {
                await informer(["NO_VALID_MODS_TO_UNPACK"], new PrintParams(LogConstants.UNPACK));
                return false;
            }

            // Unpack mods
            await informer(["UNPACKING_MODS"], new PrintParams(LogConstants.UNPACK));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseSingleThread)
            {
                // Must respect the order
                foreach (var mod in enabled.OrderBy(mod => mod.Metadata.Order))
                    await UnpackLocal(informer, mod);
            }
            else
            {
                // Must respect the order
                foreach (var collection in enabled
                    .GroupBy(mod => mod.Metadata.Order)
                    .OrderBy(group => group.Key)
                )
                {
                    await Parallel.ForEachAsync(collection, async (mod, token) => await UnpackLocal(informer, mod));
                }
            }

            time.Stop();
            await informer(["UNPACKING_SUCCESS_MODS"], new PrintParams(LogConstants.UNPACK, time.GetHumaneElapsedTime()));

            return true;

            async ValueTask UnpackLocal(Delegates.Log informer, Mod mod)
            {
                if (!(await UnpackInternal(mod)))
                {
                    await informer(["ERROR_UNPACKING_MOD_SINGLE"], new PrintParams(LogConstants.UNPACK, Name: mod.ToString()));
                    return;
                }

                await informer(["UNPACKING_MOD_SINGLE"], new PrintParams(LogConstants.UNPACK, Name: mod.ToString()));
                mod.Metadata.Unpacked = true;

                mod.Update();

                // To deploy on separate file, we need each mod on the corresponding folder
                if (Configuration.Options.DeployOnSeparateFile)
                {
                    var newFolder = Path.Combine(ExtractionFolder, mod.Metadata.Name);
                    newFolder.CreateDirectoryIfNotExist();
                    await mod.File.Extraction.MergeDirectoryAsync(newFolder);
                }
                else
                {
                    await mod.File.Extraction.MergeDirectoryAsync(ExtractionFolder);
                }

                mod.File.Extraction.DeleteDirectoryIfExists();
            }
        }

        /// <see cref="IRepack.GetUnpackedFolder(Delegates.Log?)"/>
        public async ValueTask<string> GetUnpackedFolder(Delegates.Log? informer = null)
        {
            return !await ValidateConfiguration(informer) || !Directory.Exists(ExtractionFolder) ? string.Empty : ExtractionFolder;
        }

        /// <see cref="IRepack.Pack(Delegates.Log)"/>
        public async ValueTask<string[]> Pack(Delegates.Log informer)
        {
            if (!await ValidateConfiguration(informer))
                return [];

            if (!Directory.Exists(ExtractionFolder))
            {
                await informer(["EXTRACTION_FOLDER_NOT_FOUND"], new PrintParams(LogConstants.PACK));
                return [];
            }

            await informer(["PACKING_MODS"], new PrintParams(LogConstants.PACK));
            var time = Stopwatch.StartNew();

            var files = new List<string>();
            if (Configuration.Options.DeployOnSeparateFile)
            {
                if (Configuration.Options.UseSingleThread)
                {
                    foreach (var directory in Directory.GetDirectories(ExtractionFolder))
                        files.Add(await ToolPack(directory));
                }
                else
                {
                    await Parallel.ForEachAsync(Directory.GetDirectories(ExtractionFolder), async (directory, token) =>
                    {
                        files.Add(await ToolPack(directory));
                    });
                }
            }
            else
            {
                files.Add(await ToolPack(ExtractionFolder));
            }

            time.Stop();
            await informer(["PACKING_SUCCESS_MODS"], new PrintParams(LogConstants.PACK, time.GetHumaneElapsedTime()));

            return [.. files.Where(file => !string.IsNullOrEmpty(file))];
        }

        #region Private Methods

        /// <summary>
        ///     Update the status of invalid mods
        /// </summary>
        private async ValueTask UpdateUnusedMods(Delegates.Log informer, Mod[] collection, string alias)
        {
            if (collection.Length == 0)
                return;

            await informer(["UPDATING_MOD_FILES_ALIAS"], new PrintParams(LogConstants.UNPACK, Name: alias));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseSingleThread)
            {
                foreach (var mod in collection)
                {
                    mod.Metadata.Unpacked = false;
                    mod.Update();
                }
            }
            else
            {
                Parallel.ForEach(collection, mod =>
                {
                    mod.Metadata.Unpacked = false;
                    mod.Update();
                });
            }

            time.Stop();
            await informer(["UPDATING_SUCCESS_MOD_FILES_ALIAS"], new PrintParams(LogConstants.UNPACK, time.GetHumaneElapsedTime(), alias));
        }

        /// <summary>
        ///     Validate if the applicatioin configuration is valid
        /// </summary>
        private async ValueTask<bool> ValidateConfiguration(Delegates.Log? informer = null)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                if (informer is not null)
                    await informer(["REPACK_TOOL_IGNORED"], new PrintParams(LogConstants.UNPACK));

                return false;
            }

            if (string.IsNullOrEmpty(Configuration.Folders?.RepackFolder))
            {
                if (informer is not null)
                    await informer(["REPACK_PATH_NOT_FOUND"], new PrintParams(LogConstants.UNPACK));

                return false;
            }

            if (!RepakToolExist())
            {
                if (informer is not null)
                    await informer(["REPACK_EXE_NOT_FOUND"], new PrintParams(LogConstants.UNPACK));

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Unpack a single mod
        /// </summary>
        private async ValueTask<bool> UnpackInternal(Mod mod)
        {
            mod.File.Extraction.DeleteDirectoryIfExists();
            return mod.File.Extension switch
            {
                ".pak" => await ToolUnpack(mod.File),
                ".zip" => await ExtractCompressedFile(mod.File, ".zip"),
                ".rar" => await ExtractCompressedFile(mod.File, ".rar"),
                ".7z" => await ExtractCompressedFile(mod.File, ".7z"),
                _ => false,
            };
        }

        /// <summary>
        ///     Extract compressed file
        /// </summary>
        private async ValueTask<bool> ExtractCompressedFile(FileInformation file, string format)
        {
            switch (format)
            {
                case ".zip":
                    ZipFile.ExtractToDirectory(file.Filepath, file.Extraction);
                    break;

                case ".7z":
                    using (var archive = SevenZipArchive.Open(file.Filepath))
                    {
                        file.Extraction.CreateDirectoryIfNotExist();
                        var reader = archive.ExtractAllEntries();
                        reader.WriteAllToDirectory(file.Extraction, new SharpCompress.Common.ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                    break;

                case ".rar":
                    using (var archive = RarArchive.Open(file.Filepath))
                    {
                        file.Extraction.CreateDirectoryIfNotExist();
                        var reader = archive.ExtractAllEntries();
                        reader.WriteAllToDirectory(file.Extraction, new SharpCompress.Common.ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                    break;

                default: 
                    throw new NotSupportedException("The format is not supported.");
            }

            if (!Directory.Exists(file.Extraction))
                return false;

            if (Directory.Exists(Path.Combine(file.Extraction, GameSettings.GameContentFolder)))
                return true;

            var pakfiles = Directory.GetFiles(file.Extraction, "*.pak");
            if (pakfiles is null || pakfiles.Length == 0)
                return false;

            foreach (var pak in pakfiles)
            {
                var pakinfo = new FileInformation(pak);
                if (!(await ToolUnpack(pakinfo)))
                    return false;

                pakinfo.Extraction.MergeDirectory(file.Extraction);

                // Clean
                pakinfo.Extraction.DeleteDirectoryIfExists();
                pakinfo.Filepath.DeleteFileIfExist();
            }

            return true;
        }

        /// <summary>
        ///     Lookup if the tool exist
        /// </summary>
        public bool RepakToolExist()
        {
            return Directory.Exists(Configuration.Folders.RepackFolder) && File.Exists(Executable);
        }

        #region Paker tool

        /// <summary>
        ///     Extract a pak file
        /// </summary>
        private async ValueTask<bool> ToolUnpack(FileInformation file)
        {
            var arguments = new StringBuilder();
            if (!string.IsNullOrEmpty(GameSettings.AES_KEY))
                arguments.Append($"--aes-key {GameSettings.AES_KEY} ");

            arguments.Append($"unpack \"{file.Filepath}\"");

            if (!await UseTool(arguments.ToString()))
                return false;

            return Directory.Exists(Path.Combine(file.Extraction, GameSettings.GameContentFolder));
        }

        /// <summary>
        ///     Make a pak file
        /// </summary>
        private async ValueTask<string> ToolPack(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return string.Empty;

            var arguments = new StringBuilder();
            if (!string.IsNullOrEmpty(GameSettings.AES_KEY))
                arguments.Append($"--aes-key {GameSettings.AES_KEY} ");

            arguments.Append($"pack \"{folder}\"");

            var file = $"{folder}.pak";
            file.DeleteFileIfExist();

            if (!await UseTool(arguments.ToString()))
                return string.Empty;

            return File.Exists(file) ? file : string.Empty;
        }

        /// <summary>
        ///     Use the tool with the arguments
        /// </summary>
        private async ValueTask<bool> UseTool(string arguments)
        {
            if (!RepakToolExist())
                return false;

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                    return false;

                await process.WaitForExitAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #endregion
    }
}