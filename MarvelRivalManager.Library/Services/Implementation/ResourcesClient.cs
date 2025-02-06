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
        #region Constants
        private const string TOOL_VERSION_KEY = "RepakVersion";
        #endregion

        #region Dependencies
        private readonly IEnvironment Configuration = configuration;
        private readonly IRepack Repack = repack;
        private readonly MegaApiClient Client = new();
        #endregion

        /// <see cref="IResourcesClient.Delete(Delegates.Log)"/>
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

        /// <see cref="IResourcesClient.Download(Delegates.Log, bool, CancellationToken?)"/>
        public async ValueTask<bool> Download(Delegates.Log informer, bool update, CancellationToken? cancellationToken = null)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer(["REPACK_TOOL_IGNORED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            if (string.IsNullOrEmpty(Configuration.Folders.RepackFolder))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_DEFINED"], new PrintParams(LogConstants.DOWNLOAD));
                return false;
            }

            if (!Directory.Exists(Configuration.Folders.RepackFolder))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_FOUND"], new PrintParams(LogConstants.DOWNLOAD));
                Configuration.Folders.RepackFolder.CreateDirectoryIfNotExist();
            }

            if (await Repack.IsAvailable() && (!update || !await NewVersionAvailable(Delegates.EmptyLog)))
            {
                await informer(["REPACK_TOOL_ALREADY_DOWNLOADED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            if (!await Download("Repak", Configuration.Folders.RepackFolder, informer, cancellationToken))
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

        /// <see cref="IResourcesClient.Download(Delegates.Log, CancellationToken?)"/>
        public async ValueTask<bool> Download(Delegates.Log informer, CancellationToken? cancellationToken = null)
        {
            return await Download(informer, true, cancellationToken);
        }

        /// <see cref="IResourcesClient.NewVersionAvailable(Delegates.Log)"/>
        public async ValueTask<bool> NewVersionAvailable(Delegates.Log informer)
        {
            if (Configuration.Options.IgnorePackerTool)
            {
                await informer(["REPACK_TOOL_IGNORED"], new PrintParams(LogConstants.DOWNLOAD));
                return true;
            }

            if (string.IsNullOrEmpty(Configuration.Folders.RepackFolder))
            {
                await informer(["REPACK_TOOL_FOLDER_NOT_DEFINED"], new PrintParams(LogConstants.DOWNLOAD));
                return false;
            }

            Configuration.Variables.TryGetValue(TOOL_VERSION_KEY, out var raw);
            _ = Version.TryParse(raw, out var current);

            if (!await Login(informer))
            {
                await LogOut(informer);
                return false;
            }

            var last = (await GetLastVersion("Repak", informer)).Version;
            await LogOut(informer);

            return last is not null && (current is null || last > current);
        }

        #region Private methods

        /// <summary>
        ///     Download a resource from the service
        /// </summary>
        private async ValueTask<bool> Download(string remote, string local, Delegates.Log informer, CancellationToken? cancellationToken)
        {
            if (!await Login(informer))
            {
                await LogOut(informer);
                return false;
            }

            var (lastVersion, version) = (await GetLastVersion(remote, informer));
            if (lastVersion is null || version is null)
            {
                await LogOut(informer);
                return false;
            }

            // Download file
            var filename = $"{remote}-{lastVersion.Name}";
            var downloaded = Path.Combine(Configuration.Folders.DownloadFolder, filename);
            downloaded.DeleteFileIfExist();

            if (!await Measure(informer, async (time) =>
            {
                await Client.DownloadFileAsync(lastVersion, downloaded, new Progress<double>((percentage) =>
                {
                    if (Math.Round(percentage, 0) % 10 == 0)
                        informer(["CLIENT_RESOURCE_PROGRESS"], new PrintParams(
                            LogConstants.DOWNLOAD,
                            Name: $"{remote} - {Math.Round(percentage, 3)}%",
                            UndoLast: true
                            ));

                }), cancellationToken);

                time.Stop();
                await informer(["CLIENT_RESOURCE_DOWNLOADED"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                return true;
            }))
            {
                await LogOut(informer);
                return false;
            }

            // Logout, we don't need the service anymore
            await LogOut(informer);

            // Move the downloaded file to resource folder
            var movedDownloadedFile = await Measure(informer, async (time) =>
            {
                await informer(["CLIENT_RESOURCE_MOVING"], new PrintParams(LogConstants.DOWNLOAD, Name: remote));

                var movedDownloadedFile = downloaded.MakeSafeMove(Path.Combine(local, filename));
                time.Stop();
                await informer(["CLIENT_RESOURCE_MOVING_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                return movedDownloadedFile;
            });
            if (string.IsNullOrWhiteSpace(movedDownloadedFile))
                return false;

            // Decompress the downloaded file into the resource folder
            if (!await Measure(informer, async (time) =>
            {
                await informer(["CLIENT_RESOURCE_DECOMPRESS"], new PrintParams(LogConstants.DOWNLOAD, Name: remote));

                using (var archive = RarArchive.Open(movedDownloadedFile))
                {
                    var reader = archive.ExtractAllEntries();
                    reader.WriteAllToDirectory(local, new SharpCompress.Common.ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }

                movedDownloadedFile.DeleteFileIfExist();
                time.Stop();

                await informer(["CLIENT_RESOURCE_DECOMPRESS_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                return true;
            }))
                return false;

            SaveVersion(version, TOOL_VERSION_KEY);

            return true;
        }

        /// <summary>
        ///     Wrapper to measure the time of an action
        /// </summary>
        private async ValueTask<T?> Measure<T>(Delegates.Log informer, Delegates.AsyncActionWithTimer<T> action)
        {
            var time = Stopwatch.StartNew();

            try
            {
                return await action(time);
            }
            catch (Exception ex)
            {
                await informer(["CLIENT_RESOURCE_ERROR"], new PrintParams(LogConstants.DOWNLOAD, Name: ex.Message));

                time?.Stop();
                await LogOut(informer);

                return default;
            }
        }

        /// <summary>
        ///     Login into the service
        /// </summary>
        private async ValueTask<bool> Login(Delegates.Log informer)
        {
            return await Measure(informer, async (Stopwatch time) =>
            {
                await informer(["CLIENT_LOGIN"], new PrintParams(LogConstants.DOWNLOAD));
                await Client.LoginAnonymousAsync();

                time.Stop();
                await informer(["CLIENT_LOGIN_COMPLETE"], new PrintParams(LogConstants.DOWNLOAD, time.GetHumaneElapsedTime()));

                return true;
            });
        }

        /// <summary>
        ///     Logout from the service
        /// </summary>
        private async ValueTask LogOut(Delegates.Log informer)
        {
            if (Client.IsLoggedIn)
            {
                await informer(["CLIENT_LOGOUT"], new PrintParams(LogConstants.DOWNLOAD));
                await Client.LogoutAsync();
            }  
        }

        /// <summary>
        ///     Get the last version of a resource
        /// </summary>
        private async ValueTask<(INode? Node, Version? Version)> GetLastVersion(string file, Delegates.Log informer)
        {
            (INode? Node, Version? Version) empty = (null, null);

            return await Measure(informer, async (Stopwatch time) =>
            {
                await informer(["CLIENT_RESOURCE_LOOKUP"], new PrintParams(LogConstants.DOWNLOAD, Name: file));

                if (string.IsNullOrEmpty(Configuration.Folders.MegaFolder))
                {
                    time.Stop();
                    await informer(["CLIENT_RESOURCE_FOLDER_NOT_DEFINED"], new PrintParams(LogConstants.DOWNLOAD));
                    return empty;
                }

                var all = Client.GetNodesFromLink(new Uri(Configuration.Folders.MegaFolder));
                var directory = all.FirstOrDefault(node => node is not null && node.Type == NodeType.Directory && node.Name.Equals(file, StringComparison.InvariantCultureIgnoreCase));
                var resources = directory is null ? [] : all.Where(node => node.ParentId == directory?.Id && node.Type == NodeType.File);

                if (!resources.Any())
                {
                    time.Stop();
                    await informer(["CLIENT_RESOURCE_NOT_FOUND"], new PrintParams(LogConstants.DOWNLOAD, Name: file));
                    return empty;
                }

                var last = resources
                    .Select(node =>
                    {
                        _ = Version.TryParse(Path.GetFileNameWithoutExtension(node.Name), out var version);
                        return (node, version);
                    })
                    .OrderByDescending(resource => resource.version)
                    .First();

                time.Stop();
                return last;
            });
        }

        /// <summary>
        ///     The tool version
        /// </summary>
        private void SaveVersion(Version version, string key)
        {
            Configuration.Refresh();
            Configuration.Variables[key] = version.ToString();
            Configuration.Update(Configuration);
        }

        #endregion
    }
}
