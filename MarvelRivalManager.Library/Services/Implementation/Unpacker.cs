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
    /// <see cref="IUnpacker"/>
    internal class Unpacker(IEnvironment configuration) : IUnpacker
    {
        #region Constants

        private const string MERGE = "MERGE";
        private const string UNPACK = "UNPCK";

        #endregion

        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        #endregion

        #region Properties
        public string ExecutableFile { get; set; } = string.Empty;
        public string ExtractionFolder { get; set; } = string.Empty;
        #endregion

        /// <see cref="IUnpacker.StoreMetadata(Mod)"/>
        public async ValueTask<bool> StoreMetadata(Mod mod)
        {
            return ValidateConfiguration() && await UnpackInternal(mod);
        }

        /// <see cref="IUnpacker.Unpack(Mod[], Action{string})"/>
        public async ValueTask Unpack(Mod[] mods, Action<string> informer)
        {
            if (!ValidateConfiguration(informer))
                return;

            if (!File.Exists(ExecutableFile))
            {
                informer("The unpacker executable do not exist.".AsLog(UNPACK));
                return;
            }

            if (!Directory.Exists(ExtractionFolder))
            {
                informer("Extraction folder do not exist, creating folder...".AsLog(UNPACK));
                ExtractionFolder.CreateDirectoryIfNotExist();
            }
            else
            {
                ExtractionFolder.DeleteDirectoryContent();
            }

            var invalid = mods.Where(x => !x.Metadata.Valid).OrderBy(mod => mod.Metadata.Order).ToArray();
            if (invalid is not null && invalid.Length > 0)
            {
                informer("Some mods are invalid, please validate the metadata...".AsLog(UNPACK));
                foreach (var mod in invalid)
                    informer($"- {mod}".AsLog(UNPACK));
            }

            var valid = mods.Where(x => x.Metadata.Valid).OrderBy(mod => mod.Metadata.Order).ToArray();
            if (valid is null || valid.Length == 0)
            {
                informer("No valids mods to unpack...".AsLog(UNPACK));
                return;
            }

            informer("Unpacking mods...".AsLog(UNPACK));

            foreach (var mod in valid)
            {
                informer($"- Unpacking mod {mod}...".AsLog(UNPACK));
                if (!(await UnpackInternal(mod)))
                {
                    informer($"- Failed to unpack mod {mod}".AsLog(UNPACK));
                    continue;
                }

                informer($"- Updating metadata of mod {mod}...".AsLog(UNPACK));
                mod.Metadata.Unpacked = true;
                await mod.Metadata.Update(mod.File);

                informer($"- Merging mod {mod}...".AsLog(MERGE));
                await mod.File.ExtractionContent.MergeDirectoryAsync(ExtractionFolder);
                mod.File.Extraction.DeleteDirectoryIfExists();
            }
        }

        /// <see cref="IUnpacker.GetExtractionFolder"/>
        public string GetExtractionFolder()
        {
            return !ValidateConfiguration() || !Directory.Exists(ExtractionFolder) ? string.Empty : ExtractionFolder;
        }

        #region Private Methods

        /// <summary>
        ///     Validate if the applicatioin configuration is valid
        /// </summary>
        private bool ValidateConfiguration(Action<string>? informer = null)
        {
            var configuration = Configuration.Load();
            if (string.IsNullOrEmpty(configuration?.Folders?.UnpackerExecutable))
            {
                if (informer is not null)
                    informer("The unpacker executable path is required.".AsLog(UNPACK));

                return false;
            }

            // Properties
            ExecutableFile = $"{configuration.Folders.UnpackerExecutable}/repak.exe";
            ExtractionFolder = $"{configuration.Folders.UnpackerExecutable}/extraction";

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

            // The mod contain the raw files
            if (Directory.Exists(file.ExtractionContent))
                return true;

            var pak = Directory.GetFiles(file.Extraction, "*.pak").FirstOrDefault();
            if (string.IsNullOrEmpty(pak))
                return false;

            var pakinfo = new FileInformation(pak);
            if (!(await ExtractPakFile(pakinfo)))
                return false;

            pakinfo.Extraction.MergeDirectory(file.Extraction);
            return true;
        }

        /// <summary>
        ///     Extract a pak file
        /// </summary>
        private async ValueTask<bool> ExtractPakFile(FileInformation file)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ExecutableFile,
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

        #endregion
    }
}
