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
        public async ValueTask<bool> Delete(Delegates.Log informer)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer(["REPACK_TOOL_IGNORED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            var resource = Configuration.Folders.RepackFolder;
            if (string.IsNullOrEmpty(resource))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_DEFINED"], new PrintParams(LogConstants.DOWNLOAD));
                return false;
            }

            if (!Directory.Exists(resource))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_FOUND", "SKIPPING_DELETE"], new PrintParams(LogConstants.DOWNLOAD));
                resource.CreateDirectoryIfNotExist();
                return true;
            }

            await informer(["REPACK_TOOL_FOLDER_DELETED"], new PrintParams(LogConstants.DOWNLOAD));
            resource.DeleteDirectoryContent();
            return true;
        }

        /// <see cref="IResourcesClient.Download(Delegates.PrintAndUndo, CancellationToken?)"/>
        public async ValueTask<bool> Download(Delegates.Log informer, CancellationToken? cancellationToken = null)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer(["REPACK_TOOL_IGNORED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            var resource = Configuration.Folders.RepackFolder;
            if (string.IsNullOrEmpty(resource))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_DEFINED"], new PrintParams(LogConstants.DOWNLOAD));
                return false;
            }

            if (!Directory.Exists(resource))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_FOUND"], new PrintParams(LogConstants.DOWNLOAD));
                resource.CreateDirectoryIfNotExist();
            }

            // Already downloaded, no need to download again
            if (await Repack.IsAvailable())
            {
                await informer(["REPACK_TOOL_ALREADY_DOWNLOADED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            if (string.IsNullOrEmpty(await Download("Repack", resource, informer, cancellationToken)))
                return false;

            await informer(["REPACK_TOOL_VALIDATING_DOWNLOAD"], new PrintParams(LogConstants.DOWNLOAD));
            if (!await Repack.IsAvailable())
            {
                await informer(["REPACK_TOOL_DOWNLOAD_WAS_INVALID"], new PrintParams(LogConstants.DOWNLOAD));
                return false;
            }

            await informer(["REPACK_TOOL_DOWNLOAD_WAS_VALID"], new PrintParams(LogConstants.DOWNLOAD));
            return true;
        }

        #region Private methods

        /// <summary>
        ///     Download a resource from the service
        /// </summary>
        private async ValueTask<string> Download(string name, string folder, Delegates.Log informer, CancellationToken? cancellationToken)
        {
            Stopwatch? time = null;
            string? downloadedFile = null;
            string? downloadedFileInFolder = null;

            var resource = $"{name}.rar";
            var extractedFolder = Path.Combine(folder, name);

            try
            {
                // Login into the service
                await informer(["CLIENT_LOGIN"], new PrintParams(LogConstants.DOWNLOAD));
                time = Stopwatch.StartNew();

                await Client.LoginAnonymousAsync();

                time.Stop();
                await informer(["CLIENT_LOGIN_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                time.Reset();

                // Download the backup resource
                time.Start();
                await informer(["CLIENT_RESOURCE_LOOKUP"], new PrintParams(LogConstants.DOWNLOAD, Name: name));

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
                    await informer(["CLIENT_RESOURCE_NOT_FOUND"], new PrintParams(LogConstants.DOWNLOAD, Name: name));
                    time.Stop();
                    return string.Empty;
                }
                else
                {
                    downloadedFile.DeleteFileIfExist();

                    await Client.DownloadFileAsync(toDownload, downloadedFile, new Progress<double>((percentage) =>
                    {
                        if(Math.Round(percentage, 0) % 10 == 0)
                            informer(["CLIENT_RESOURCE_PROGRESS"], new PrintParams(
                                LogConstants.DOWNLOAD, 
                                Name: $"{resource} - {Math.Round(percentage, 3)}%",
                                UndoLast: true
                                ));

                    }), cancellationToken);
                }

                time.Stop();
                await informer(["CLIENT_RESOURCE_DOWNLOADED"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                time.Reset();

                // Move the download to resource folder
                time.Start();
                await informer(["CLIENT_RESOURCE_MOVING"], new PrintParams(LogConstants.DOWNLOAD, Name: name));

                downloadedFileInFolder = downloadedFile.MakeSafeMove(Path.Combine(folder, resource));

                time.Stop();
                await informer(["CLIENT_RESOURCE_MOVING_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                time.Reset();

                // Decompress the download to resource folder
                time.Start();
                await informer(["CLIENT_RESOURCE_DECOMPRESS"], new PrintParams(LogConstants.DOWNLOAD, Name: name));

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
                await informer(["CLIENT_RESOURCE_DECOMPRESS_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                return folder;
            }
            catch (Exception ex)
            {
                await informer(["CLIENT_RESOURCE_ERROR"], new PrintParams(LogConstants.DOWNLOAD, ex.Message));
                return string.Empty;
            }
            finally
            {
                // For any error we stop the watch
                time?.Stop();
                downloadedFile?.DeleteFileIfExist();
                downloadedFileInFolder?.DeleteFileIfExist();
                extractedFolder.DeleteDirectoryIfExists();

                await informer(["CLIENT_LOGOUT"], new PrintParams(LogConstants.DOWNLOAD));
                await Client.LogoutAsync();
            }
        }

        #endregion
    }
}
