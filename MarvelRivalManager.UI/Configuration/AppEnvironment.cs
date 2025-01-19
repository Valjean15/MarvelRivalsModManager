using IEnv = MarvelRivalManager.Library.Services.Interface.IEnvironment;
using Env = MarvelRivalManager.Library.Services.Implementation.Environment;

using MarvelRivalManager.Library.Util;

using System.IO;
using System;

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
        #region Fields

        /// <summary>
        ///     Folder were the user modified settings are saved
        /// </summary>
        private readonly string UserSettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "usersettings.json");
        
        #endregion

        /// <summary>
        ///     Read the user settings from were saved
        /// </summary>
        public override IAppEnvironment Load()
        {
            var values = UserSettingsPath.DeserializeFileContent<AppEnvironment>() ?? this;
            Folders = values.Folders ?? new();
            return values;
        }

        /// <summary>
        ///     Write on the user settings the new values
        /// </summary>
        public void Update(IEnv environment)
        {
            UserSettingsPath.WriteFileContent(environment);
        }
    }
}