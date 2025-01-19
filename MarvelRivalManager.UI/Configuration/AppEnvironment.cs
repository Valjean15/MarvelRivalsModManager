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
        public void Update(IEnv environment);
    }

    /// <see cref="IAppEnvironment"/>
    public class AppEnvironment : Env, IAppEnvironment
    {
        #region Constants

        private const string UserSettingsFolderName = "MarvelRivalModManager";
        private const string UserSettingsFileName = "MarvelRivalModManager/usersettings.json";

        #endregion

        #region Fields

        /// <summary>
        ///     Folder were the user modified settings are saved
        /// </summary>
        private readonly string UserSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), UserSettingsFolderName);

        /// <summary>
        ///     File were the user modified settings are saved
        /// </summary>
        private readonly string UserSettingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), UserSettingsFileName);

        #endregion

        /// <summary>
        ///     Read the user settings from were saved
        /// </summary>
        public override IAppEnvironment Load()
        {
            // Read data from the user settings
            UserSettingsFolder.CreateDirectoryIfNotExist();
            var stored = UserSettingsFile.DeserializeFileContent<AppEnvironment>();

            if (stored is null)
            {
                var disabled = Path.Combine(UserSettingsFolder, "disabled");
                var enabled = Path.Combine(UserSettingsFolder, "enabled");
                var unpacker = Path.Combine(UserSettingsFolder, "unpacker");

                disabled.CreateDirectoryIfNotExist();
                enabled.CreateDirectoryIfNotExist();
                unpacker.CreateDirectoryIfNotExist();

                // Create default user settings
                Folders = new Folders()
                {
                    GameContent = SteamFolderLookup.GetGameFolderByRelativePath(Folders.GameContent),
                    ModsDisabled = disabled,
                    ModsEnabled = enabled,
                    UnpackerExecutable = unpacker,

                    // User defined values
                    BackupResources = new BackupFolders()
                    {
                        Characters = string.Empty,
                        Ui = string.Empty,
                        Movies = string.Empty,
                    }
                };

                Update(this);
            }

            var values = UserSettingsFile.DeserializeFileContent<AppEnvironment>()!;
            Folders = values.Folders ?? new();
            return values;
        }

        /// <summary>
        ///     Write on the user settings the new values
        /// </summary>
        public void Update(IEnv environment)
        {
            UserSettingsFile.WriteFileContent(environment);
        }
    }
}