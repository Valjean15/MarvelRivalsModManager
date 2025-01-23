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
    internal class Repack(IEnvironment configuration, IDirectoryCheker cheker) : IRepack
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IDirectoryCheker Cheker = cheker;
        #endregion

        #region Properties
        private string Executable => $"{Configuration?.Folders?.RepackFolder}/repak.exe";
        private string ExtractionFolder => $"{Configuration?.Folders?.RepackFolder}/extraction";
        private string ExtractionFile => $"{ExtractionFolder}.pak";
        #endregion

        /// <see cref="IRepack.CanBeUnPacked(Mod)"/>
        public async ValueTask<bool> CanBeUnPacked(Mod mod)
        {
            return ValidateConfiguration() && await UnpackInternal(mod);
        }

        /// <see cref="IRepack.Unpack(Mod[], Action{string})"/>
        public async ValueTask Unpack(Mod[] mods, Action<string> informer)
        {
            if (!ValidateConfiguration(informer))
                return;

            if (!Directory.Exists(ExtractionFolder))
            {
                informer("Extraction folder do not exist, creating folder...".AsLog(LogConstants.UNPACK));
                ExtractionFolder.CreateDirectoryIfNotExist();
            }
            else
            {
                ExtractionFolder.DeleteDirectoryContent();
            }

            var invalid = mods.Where(x => !x.Metadata.Valid).OrderBy(mod => mod.Metadata.Order).ToArray();
            if (invalid is not null && invalid.Length > 0)
            {
                informer("Some mods are invalid, please validate the metadata...".AsLog(LogConstants.UNPACK));
                foreach (var mod in invalid)
                    informer($"- {mod}".AsLog(LogConstants.UNPACK));
            }

            var valid = mods.Where(x => x.Metadata.Valid).OrderBy(mod => mod.Metadata.Order).ToArray();
            if (valid is null || valid.Length == 0)
            {
                informer("No valids mods to unpack...".AsLog(LogConstants.UNPACK));
                return;
            }

            informer("Unpacking mods...".AsLog(LogConstants.UNPACK));
            var time = Stopwatch.StartNew();

            foreach (var mod in valid)
            {
                informer($"- Unpacking mod {mod}...".AsLog(LogConstants.UNPACK));
                if (!(await UnpackInternal(mod)))
                {
                    informer($"- Failed to unpack mod {mod}".AsLog(LogConstants.UNPACK));
                    continue;
                }

                mod.Metadata.Unpacked = true;
                await mod.SetSystemInformation();
                mod.Update();

                await mod.File.Extraction.MergeDirectoryAsync(ExtractionFolder);
                mod.File.Extraction.DeleteDirectoryIfExists();
            }

            time.Stop();
            informer($"Unpacking mods complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.UNPACK));
        }

        /// <see cref="IRepack.Pack(Action{string}?)"/>
        public string GetUnpackedFolder(Action<string>? informer = null)
        {
            return !ValidateConfiguration(informer) || !Directory.Exists(ExtractionFolder) ? string.Empty : ExtractionFolder;
        }

        /// <see cref="IRepack.Unpack(Mod[], Action{string})"/>
        public async ValueTask<string> Pack(Action<string> informer)
        {
            if (!ValidateConfiguration(informer))
                return string.Empty;

            if (!Directory.Exists(ExtractionFolder))
            {
                informer("Extraction folder do not exist, creating folder...".AsLog(LogConstants.PACK));
                return string.Empty;
            }

            informer("Packing mods...".AsLog(LogConstants.PACK));
            var time = Stopwatch.StartNew();

            var file = await MakePakFile() ? ExtractionFile : string.Empty;

            time.Stop();

            if (File.Exists(file))
            {
                informer($"Packing mods complete - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.PACK));
            }
            else
            {
                informer("Error while packing the mods".AsLog(LogConstants.PACK));
            }

            return file;
        }

        #region Private Methods

        /// <summary>
        ///     Validate if the applicatioin configuration is valid
        /// </summary>
        private bool ValidateConfiguration(Action<string>? informer = null)
        {
            if (string.IsNullOrEmpty(Configuration.Folders?.RepackFolder))
            {
                if (informer is not null)
                    informer("The unpacker executable path is required.".AsLog(LogConstants.UNPACK));

                return false;
            }

            if (!Cheker.RepakToolExist())
            {
                if (informer is not null)
                    informer("The unpacker executable do not exist on the unpacker folder.".AsLog(LogConstants.UNPACK));

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
                ".pak" => await ExtractPakFile(mod.File),
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

            if (Cheker.ModRawStructure(file.Extraction) && Directory.Exists(file.ExtractionContent))
                return true;

            var pakfiles = Directory.GetFiles(file.Extraction, "*.pak");
            if (pakfiles is null || pakfiles.Length == 0)
                return false;

            foreach (var pak in pakfiles)
            {
                var pakinfo = new FileInformation(pak);
                if (!(await ExtractPakFile(pakinfo)))
                    return false;

                pakinfo.Extraction.MergeDirectory(file.Extraction);

                // Clean
                pakinfo.Extraction.DeleteDirectoryIfExists();
                pakinfo.Filepath.DeleteFileIfExist();
            }

            return true;
        }

        /// <summary>
        ///     Extract a pak file
        /// </summary>
        private async ValueTask<bool> ExtractPakFile(FileInformation file)
        {
            if (!Cheker.RepakToolExist())
                return false;

            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Executable,
                    Arguments = $"unpack \"{file.Filepath}\"",
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
        private async ValueTask<bool> MakePakFile()
        {
            if (!Cheker.RepakToolExist() || string.IsNullOrEmpty(ExtractionFolder))
                return false;

            try
            {
                ExtractionFile.DeleteFileIfExist();
                const string AES_KEY = "0C263D8C22DCB085894899C3A3796383E9BF9DE0CBFB08C9BF2DEF2E84F29D74";
                var argument = $"--aes-key {AES_KEY} pack \"{ExtractionFolder}\"";

                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = Executable,
                    Arguments = argument,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process is null)
                    return false;

                await process.WaitForExitAsync();
                return File.Exists(ExtractionFile);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
