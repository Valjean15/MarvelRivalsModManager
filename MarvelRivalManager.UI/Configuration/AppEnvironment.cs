using IEnv = MarvelRivalManager.Library.Services.Interface.IEnvironment;
using Env = MarvelRivalManager.Library.Services.Implementation.Environment;

using MarvelRivalManager.Library.Util;

using System.IO;
using System;
using MarvelRivalManager.Library.Services.Interface;

namespace MarvelRivalManager.UI.Configuration
{
    /// <summary>
    ///     Extension of the configuration of the application
    /// </summary>
    public interface IAppEnvironment : IEnv
    {
        /// <summary>
        ///     Write on the user settings the new values
        /// </summary>
        public void Update(IEnv environment);

        /// <summary>
        ///     Get default configuration
        /// </summary>
        public IEnv Default();
    }

    /// <see cref="IAppEnvironment"/>
    public class AppEnvironment : Env, IAppEnvironment
    {
        #region Constants

        private const string UserSettingsFolderName = "MarvelRivalModManager";
        private const string UserSettingsFileName = "MarvelRivalModManager/usersettings_1.json";

        #endregion

        #region Fields

        /// <summary>
        ///     Folder were the user modified settings are saved
        /// </summary>
        private readonly string UserSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), UserSettingsFolderName);

        /// <summary>
        ///     File were the user modified settings are saved
        /// </summary>
        private readonly string UserSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), UserSettingsFileName);

        #endregion

        /// <see cref="IEnv.Refresh"/>
        public override IAppEnvironment Refresh()
        {
            // Read data from the user settings
            UserSettingsFolder.CreateDirectoryIfNotExist();
            var stored = UserSettingsFile.DeserializeFileContent<AppEnvironment>();

            if (stored is null)
            {
                var @default = Default();
                Options = @default.Options;
                Folders = @default.Folders;

                Update(this);
            }

            var values = UserSettingsFile.DeserializeFileContent<AppEnvironment>()!;
            
            Folders = values.Folders ?? new();
            Options = values.Options ?? new();

            return values;
        }

        /// <see cref="IAppEnvironment.Update(IEnv)"/>
        public void Update(IEnv environment)
        {
            UserSettingsFile.WriteFileContent(environment as AppEnvironment);
        }

        /// <see cref="IAppEnvironment.Default"/>
        public IEnv Default()
        {
            var disabled = Path.Combine(UserSettingsFolder, "disabled");
            var enabled = Path.Combine(UserSettingsFolder, "enabled");
            var repak = Path.Combine(UserSettingsFolder, "repak");
            var download = Path.Combine(UserSettingsFolder, "download");
            var collections = Path.Combine(UserSettingsFolder, "collections");

            disabled.CreateDirectoryIfNotExist();
            enabled.CreateDirectoryIfNotExist();
            repak.CreateDirectoryIfNotExist();
            download.CreateDirectoryIfNotExist();
            collections.CreateDirectoryIfNotExist();

            return new AppEnvironment
            {
                Folders = new Folders()
                {
                    GameContent = SteamFolderLookup.GetGameFolderByRelativePath(Folders.GameContent),
                    MegaFolder = Folders.MegaFolder,
                    Collections = collections,
                    DownloadFolder = download,
                    ModsDisabled = disabled,
                    ModsEnabled = enabled,
                    RepackFolder = repak
                },
                Options = new Options()
            };
        }
    }
}