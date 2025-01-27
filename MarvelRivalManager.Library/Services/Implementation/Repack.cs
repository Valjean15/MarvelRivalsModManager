using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

using System.Diagnostics;
using System.IO.Compression;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IRepack"/>
    internal class Repack(IEnvironment configuration, IModDataAccess query) : IRepack
    {
        private const string AES_KEY = "0C263D8C22DCB085894899C3A3796383E9BF9DE0CBFB08C9BF2DEF2E84F29D74";

        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IModDataAccess Query = query;
        #endregion

        #region Properties
        private string Executable => $"{Configuration?.Folders?.RepackFolder}/repak.exe";
        private string ExtractionFolder => $"{Configuration?.Folders?.RepackFolder}/extraction";
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

        /// <see cref="IRepack.Unpack(Delegates.Print)"/>
        public async ValueTask<bool> Unpack(Delegates.Print informer)
        {
            if (!await ValidateConfiguration(informer))
                return false;

            // Remove old content
            if (!Directory.Exists(ExtractionFolder))
            {
                await informer("Extraction folder do not exist, creating folder...".AsLog(LogConstants.UNPACK));
                ExtractionFolder.CreateDirectoryIfNotExist();
            }
            else
            {
                ExtractionFolder.DeleteDirectoryContent();
            }

            // Get all mods
            var mods = (await Query.All(true))?.OrderBy(mod => mod.Metadata.Order)?.ToArray() ?? [];

            // Update unused content
            await UpdateUnusedMods(informer, mods.Where(x => !x.Metadata.Valid).ToArray(), "invalid");
            await UpdateUnusedMods(informer, mods.Where(x => x.Metadata.Valid && !x.Metadata.Enabled).ToArray(), "disabled");

            // Get mods to unpack
            var enabled = mods.Where(x => x.Metadata.Valid && x.Metadata.Enabled).ToArray();
            if (enabled.Length == 0)
            {
                await informer("No valids mods to unpack...".AsLog(LogConstants.UNPACK));
                return false;
            }

            // Unpack mods
            await informer("Unpacking mods...".AsLog(LogConstants.UNPACK));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseParallelLoops)
            {
                await Parallel.ForEachAsync(enabled, async (mod, token) => await UnpackLocal(informer, mod));
            }
            else
            {
                foreach (var mod in enabled)
                    await UnpackLocal(informer, mod);
            }
            
            time.Stop();
            await informer($"Unpacking mods complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.UNPACK));

            return true;

            async ValueTask UnpackLocal(Delegates.Print informer, Mod mod)
            {
                await informer($"- Unpacking mod {mod}...".AsLog(LogConstants.UNPACK));
                if (!(await UnpackInternal(mod)))
                {
                    await informer($"- Failed to unpack mod {mod}".AsLog(LogConstants.UNPACK));
                    return;
                }

                mod.Metadata.Unpacked = true;

                if (Configuration.Options.EvaluateOnUpdate)
                    await mod.SetSystemInformation();

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

        /// <see cref="IRepack.GetUnpackedFolder(Delegates.Print?)"/>
        public async ValueTask<string> GetUnpackedFolder(Delegates.Print? informer = null)
        {
            return !await ValidateConfiguration(informer) || !Directory.Exists(ExtractionFolder) ? string.Empty : ExtractionFolder;
        }

        /// <see cref="IRepack.Pack(Delegates.Print)"/>
        public async ValueTask<string[]> Pack(Delegates.Print informer)
        {
            if (!await ValidateConfiguration(informer))
                return [];

            if (!Directory.Exists(ExtractionFolder))
            {
                await informer("Extraction folder do not exist".AsLog(LogConstants.PACK));
                return [];
            }

            await informer("Packing mods...".AsLog(LogConstants.PACK));
            var time = Stopwatch.StartNew();

            var files = new List<string>();
            if (Configuration.Options.DeployOnSeparateFile)
            {
                if (Configuration.Options.UseParallelLoops)
                {
                    await Parallel.ForEachAsync(Directory.GetDirectories(ExtractionFolder), async (directory, token) =>
                    {
                        files.Add(await ToolPack(directory));
                    });
                }
                else
                {
                    foreach (var directory in Directory.GetDirectories(ExtractionFolder))
                        files.Add(await ToolPack(directory));
                }
            }
            else
            {
                files.Add(await ToolPack(ExtractionFolder));
            }

            time.Stop();
            await informer($"Packing mods complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PACK));

            return [.. files.Where(file => !string.IsNullOrEmpty(file))];
        }

        #region Private Methods

        /// <summary>
        ///     Update the status of invalid mods
        /// </summary>
        private async ValueTask UpdateUnusedMods(Delegates.Print informer, Mod[] collection, string alias)
        {
            if (collection.Length == 0)
                return;

            await informer($"Updating {alias} mods status...".AsLog(LogConstants.UNPACK));
            var time = Stopwatch.StartNew();

            if (Configuration.Options.UseParallelLoops)
            {
                await Parallel.ForEachAsync(collection, async (mod, token) => await Update(mod));
            }
            else
            {
                foreach (var mod in collection)
                    await Update(mod);
            }

            time.Stop();
            await informer($"Updating {alias} mods status complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.UNPACK));

            static async Task Update(Mod mod)
            {
                mod.Metadata.Unpacked = false;
                mod.Update();
            }
        }

        /// <summary>
        ///     Validate if the applicatioin configuration is valid
        /// </summary>
        private async ValueTask<bool> ValidateConfiguration(Delegates.Print? informer = null)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                if (informer is not null)
                    await informer("The Repack tool is ignored on the settings option section".AsLog(LogConstants.UNPACK));

                return false;
            }

            if (string.IsNullOrEmpty(Configuration.Folders?.RepackFolder))
            {
                if (informer is not null)
                    await informer("The unpacker executable path is required.".AsLog(LogConstants.UNPACK));

                return false;
            }

            if (!RepakToolExist())
            {
                if (informer is not null)
                    await informer("The unpacker executable do not exist on the unpacker folder.".AsLog(LogConstants.UNPACK));

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

            if (Directory.Exists(file.ExtractionContent))
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
            if (!RepakToolExist())
                return false;

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Executable,
                    Arguments = $"--aes-key {AES_KEY} unpack \"{file.Filepath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                    return false;

                await process.WaitForExitAsync();
                return Directory.Exists(file.ExtractionContent);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Make a pak file
        /// </summary>
        private async ValueTask<string> ToolPack(string folder)
        {
            var file = $"{folder}.pak";

            if (!RepakToolExist() || string.IsNullOrEmpty(folder))
                return string.Empty;

            try
            {
                file.DeleteFileIfExist();
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Executable,
                    Arguments = $"--aes-key {AES_KEY} pack \"{folder}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                    return string.Empty;

                await process.WaitForExitAsync();
                return File.Exists(file) ? file : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        #endregion
    }
}