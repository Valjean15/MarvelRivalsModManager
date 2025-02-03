using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;

using System.IO;
using System;

using BaseAppEnvironment = MarvelRivalManager.Library.Services.Implementation.Environment;
using System.Collections.Concurrent;

namespace MarvelRivalManager.UI.Configuration
{
    /// <summary>
    ///     Session values
    /// </summary>
    public static class SessionValues
    {
        private static ConcurrentDictionary<string, string> _localization = new();

        public static string Get(string key)
        {
            return _localization.GetOrAdd(key, string.Empty);
        }

        public static string Set(string key, string value)
        {
            _localization.AddOrUpdate(key, value, (k, v) => value);
            return value;
        }
    }

    /// <see cref="BaseAppEnvironment"/>
    public class AppEnvironment : BaseAppEnvironment, IEnvironment
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

        /// <see cref="IEnvironment.Refresh"/>
        public override IEnvironment Refresh()
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
            Variables = values.Variables ?? [];

            return values;
        }

        /// <see cref="IEnvironment.Update(IEnvironment)"/>
        public override void Update(IEnvironment environment)
        {
            UserSettingsFile.WriteFileContent(environment);
        }

        /// <see cref="IEnvironment.Default"/>
        public override IEnvironment Default()
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