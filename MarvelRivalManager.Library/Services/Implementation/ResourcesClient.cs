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
    internal class ResourcesClient(IEnvironment configuration, IDirectoryCheker cheker) : IResourcesClient
    {
        #region Constants
        private const string DOWNLOAD = "DOWN";
        #endregion

        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IDirectoryCheker Cheker = cheker;
        private readonly MegaApiClient Client = new();
        #endregion

        /// <see cref="IResourcesClient.Download(KindOfMod, Action{string})"/>
        public async ValueTask<bool> Download(KindOfMod kind, Action<string> informer)
        {
            if (kind.Equals(KindOfMod.All))
            {
                foreach (var category in new KindOfMod[] { KindOfMod.Characters, KindOfMod.Audio, KindOfMod.UI, KindOfMod.Movies })
                {
                    if (!await Download(category, informer))
                        return false;
                }
                return true;
            }

            var resource = Configuration.Folders.BackupResources.Get(kind);
            if (string.IsNullOrEmpty(resource))
            {
                informer($"Resource folder {kind} is not defined".AsLog(DOWNLOAD));
                return false;
            }

            if (!Directory.Exists(resource))
            {
                informer($"Resource do not exist folder {kind}, creating folder".AsLog(DOWNLOAD));
                resource.CreateDirectoryIfNotExist();
            }

            if (string.IsNullOrEmpty(await Download(kind.ToString(), resource, informer)))
                return false;

            informer("Validating the resource has correct structure".AsLog(DOWNLOAD));
            if (!Cheker.BackupResource(kind))
            {
                informer("The resource has incorrect structure".AsLog(DOWNLOAD));
                return false;
            }

            return true;
        }

        /// <see cref="IResourcesClient.Unpacker(Action{string})"/>
        public async ValueTask<bool> Unpacker(Action<string> informer)
        {
            var resource = Configuration.Folders.UnpackerExecutable;
            if (string.IsNullOrEmpty(resource))
            {
                informer($"Unpacker folder is not defined".AsLog(DOWNLOAD));
                return false;
            }

            if (!Directory.Exists(resource))
            {
                informer($"Unpacker folder do not exist, creating folder".AsLog(DOWNLOAD));
                resource.CreateDirectoryIfNotExist();
            }

            if (string.IsNullOrEmpty(await Download("Unpacker", resource, informer)))
                return false;

            informer("Validating if the unpacker exists".AsLog(DOWNLOAD));
            if (!Cheker.UnpackerExist())
            {
                informer("The unpacker cannot be found".AsLog(DOWNLOAD));
                return false;
            }

            return true;
        }

        #region Private methods

        /// <summary>
        ///     Download a resource from the service
        /// </summary>
        private async ValueTask<string> Download(string resource, string folder, Action<string> informer)
        {
            Stopwatch? time = null;

            try
            {
                // Login into the service
                informer("Login into service...".AsLog(DOWNLOAD));
                time = Stopwatch.StartNew();

                await Client.LoginAnonymousAsync();

                time.Stop();
                informer($"Login completed - {time.GetHumaneElapsedTime()}".AsLog(DOWNLOAD));

                time.Reset();

                // Download the backup resource
                time.Start();
                informer("Downloading the backup resource...".AsLog(DOWNLOAD));

                INode? toDownload = null;
                var downloadedFile = Path.Combine(Configuration.Folders.DownloadFolder, resource);
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
                    informer($"Resource {resource} not found in the service".AsLog(DOWNLOAD));
                    time.Stop();
                    return string.Empty;
                }
                else
                {
                    await Client.DownloadFileAsync(toDownload, downloadedFile, new Progress<double>((percentage) =>
                    {
                        informer($"Download of the resource {resource} - {percentage}%".AsLog(DOWNLOAD));
                    }));
                }

                time.Stop();
                informer($"Download of the resource completed - {time.GetHumaneElapsedTime()}".AsLog(DOWNLOAD));

                time.Reset();

                // Move the download to resource folder
                time.Start();
                informer($"Moving the download to resource folder for {resource}".AsLog(DOWNLOAD));

                var downloadedFileInFolder = downloadedFile.MakeSafeMove(Path.Combine(folder, resource));

                time.Stop();
                informer($"Moving the resource completed - {time.GetHumaneElapsedTime()}".AsLog(DOWNLOAD));

                time.Reset();

                // Decompress the download to resource folder
                time.Start();
                informer($"Decompressing the download for resource {resource}".AsLog(DOWNLOAD));

                using var archive = RarArchive.Open(downloadedFileInFolder);
                var reader = archive.ExtractAllEntries();
                reader.WriteAllToDirectory(folder, new SharpCompress.Common.ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });

                time.Stop();
                informer($"Decompressing the files completed - {time.GetHumaneElapsedTime()}".AsLog(DOWNLOAD));

                return folder;
            }
            catch
            {
                informer("Error occurred trying to download backup resource".AsLog(DOWNLOAD));
                return string.Empty;
            }
            finally
            {
                // For any error we stop the watch
                time?.Stop();

                informer("Logout from service...".AsLog(DOWNLOAD));
                await Client.LogoutAsync();
            }
        }

        #endregion
    }
}
