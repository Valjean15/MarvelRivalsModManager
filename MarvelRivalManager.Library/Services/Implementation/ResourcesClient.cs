using CG.Web.MegaApiClient;

using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

using SharpCompress.Archives.Rar;
using SharpCompress.Readers;

using System.Diagnostics;

namespace MarvelRivalManager.Library.Services.Implementation
{
    /// <see cref="IResourcesClient"/>
    internal class ResourcesClient(IEnvironment configuration, IRepack repack) : IResourcesClient
    {
        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Repack = repack;
        private readonly MegaApiClient Client = new();
        #endregion

        /// <see cref="IResourcesClient.Delete(Delegates.PrintAndUndo)"/>
        public async ValueTask<bool> Delete(Delegates.PrintAndUndo informer)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer("The Repack tool is ignored on the settings option section".AsLog(LogConstants.DOWNLOAD), false);
                return true;
            }

            var resource = Configuration.Folders.RepackFolder;
            if (string.IsNullOrEmpty(resource))
            {
                await informer("Repack folder is not defined".AsLog(LogConstants.DOWNLOAD), false);
                return false;
            }

            if (!Directory.Exists(resource))
            {
                await informer("Repack folder do not exist, creating folder. No need to delete.".AsLog(LogConstants.DOWNLOAD), false);
                resource.CreateDirectoryIfNotExist();
                return true;
            }

            await informer("Deleting repack folder content".AsLog(LogConstants.DOWNLOAD), false);
            resource.DeleteDirectoryContent();
            return true;
        }

        /// <see cref="IResourcesClient.Download(Delegates.PrintAndUndo, CancellationToken?)"/>
        public async ValueTask<bool> Download(Delegates.PrintAndUndo informer, CancellationToken? cancellationToken = null)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer("The Repack tool is ignored on the settings option section".AsLog(LogConstants.DOWNLOAD), false);
                return true;
            }

            var resource = Configuration.Folders.RepackFolder;
            if (string.IsNullOrEmpty(resource))
            {
                await informer("Repack folder is not defined".AsLog(LogConstants.DOWNLOAD), false);
                return false;
            }

            if (!Directory.Exists(resource))
            {
                await informer("Repack folder do not exist, creating folder".AsLog(LogConstants.DOWNLOAD), false);
                resource.CreateDirectoryIfNotExist();
            }

            // Already downloaded, no need to download again
            if (await Repack.IsAvailable())
            {
                await informer("Resource folder Repack already downloaded".AsLog(LogConstants.DOWNLOAD), false);
                return true;
            }

            if (string.IsNullOrEmpty(await Download("Repack", resource, informer, cancellationToken)))
                return false;

            await informer("Validating if the Repack exists".AsLog(LogConstants.DOWNLOAD), false);
            if (!await Repack.IsAvailable())
            {
                await informer("The Repack cannot be found".AsLog(LogConstants.DOWNLOAD), false);
                return false;
            }

            await informer("The Repack was found".AsLog(LogConstants.DOWNLOAD), false);
            return true;
        }

        #region Private methods

        /// <summary>
        ///     Download a resource from the service
        /// </summary>
        private async ValueTask<string> Download(string name, string folder, Delegates.PrintAndUndo informer, CancellationToken? cancellationToken)
        {
            Stopwatch? time = null;
            string? downloadedFile = null;
            string? downloadedFileInFolder = null;

            var resource = $"{name}.rar";
            var extractedFolder = Path.Combine(folder, name);

            try
            {
                // Login into the service
                await informer("Login into service...".AsLog(LogConstants.DOWNLOAD), false);
                time = Stopwatch.StartNew();

                await Client.LoginAnonymousAsync();

                time.Stop();
                await informer($"Login completed - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.DOWNLOAD), false);

                time.Reset();

                // Download the backup resource
                time.Start();
                await informer($"Downloading the {resource} resource...".AsLog(LogConstants.DOWNLOAD), false);

                INode? toDownload = null;
                downloadedFile = Path.Combine(Configuration.Folders.DownloadFolder, resource);
                foreach (var node in Client.GetNodesFromLink(new Uri(Configuration.Folders.MegaFolder)).Where(node => node is not null && node.Type.Equals(NodeType.File)))
                {
                    if (node!.Name.Equals(resource, StringComparison.InvariantCultureIgnoreCase))
                    {
                        toDownload = node;
                        break;
                    }
                }

                if (toDownload is null)
                {
                    await informer($"Resource {resource} not found in the service".AsLog(LogConstants.DOWNLOAD), false);
                    time.Stop();
                    return string.Empty;
                }
                else
                {
                    downloadedFile.DeleteFileIfExist();

                    await Client.DownloadFileAsync(toDownload, downloadedFile, new Progress<double>((percentage) =>
                    {
                        if(Math.Round(percentage, 0) % 10 == 0)
                            informer($"Download of the resource {resource} - {Math.Round(percentage, 3)}%".AsLog(LogConstants.DOWNLOAD), true);

                    }), cancellationToken);
                }

                time.Stop();
                await informer($"Download of the resource completed - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.DOWNLOAD), false);

                time.Reset();

                // Move the download to resource folder
                time.Start();
                await informer($"Moving the download to resource folder for {resource}".AsLog(LogConstants.DOWNLOAD), false);

                downloadedFileInFolder = downloadedFile.MakeSafeMove(Path.Combine(folder, resource));

                time.Stop();
                await informer($"Moving the resource completed - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.DOWNLOAD), false);

                time.Reset();

                // Decompress the download to resource folder
                time.Start();
                await informer($"Decompressing the download for resource {resource}".AsLog(LogConstants.DOWNLOAD), false);

                using (var archive = RarArchive.Open(downloadedFileInFolder))
                {
                    var reader = archive.ExtractAllEntries();
                    reader.WriteAllToDirectory(folder, new SharpCompress.Common.ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }

                downloadedFileInFolder.DeleteFileIfExist();
                if (Directory.Exists(extractedFolder))
                {
                    await extractedFolder.MergeDirectoryAsync(folder);
                    extractedFolder.DeleteDirectoryIfExists();
                }

                time.Stop();
                await informer($"Decompressing the files completed - {time.GetHumaneElapsedTime()}".AsLog(LogConstants.DOWNLOAD), false);

                return folder;
            }
            catch (Exception ex)
            {
                await informer($"Error occurred trying to download backup resource => {ex.Message}".AsLog(LogConstants.DOWNLOAD), false);
                return string.Empty;
            }
            finally
            {
                // For any error we stop the watch
                time?.Stop();
                downloadedFile?.DeleteFileIfExist();
                downloadedFileInFolder?.DeleteFileIfExist();
                extractedFolder.DeleteDirectoryIfExists();

                await informer("Logout from service...".AsLog(LogConstants.DOWNLOAD), false);
                await Client.LogoutAsync();
            }
        }

        #endregion
    }
}
