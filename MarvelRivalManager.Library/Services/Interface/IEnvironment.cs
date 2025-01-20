namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Configuration of the application
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        ///     Get the configuration
        /// </summary>
        public IEnvironment Load();

        /// <summary>
        ///     Folder paths
        /// </summary>
        public Folders Folders { get; set; }
    }

    /// <summary>
    ///     Folders paths
    /// </summary>
    public class Folders
    {
        /// <summary>
        ///     Game content used to apply mods
        /// </summary>
        public string GameContent { get; set; } = string.Empty;

        /// <summary>
        ///     Game content backup used to restore the original game content
        /// </summary>
        public BackupFolders BackupResources { get; set; } = new();

        /// <summary>
        ///     Mods folder used to store the mods
        /// </summary>
        public string ModsEnabled { get; set; } = string.Empty;

        /// <summary>
        ///     Disabled mods folder used to store the disabled mods
        /// </summary>
        public string ModsDisabled { get; set; } = string.Empty;

        /// <summary>
        ///     Unpacker executable path
        /// </summary>
        public string UnpackerExecutable { get; set; } = string.Empty;
    }

    public class BackupFolders
    {
        /// <summary>
        ///    Game content backup for Characters
        /// </summary>
        public string Characters { get; set; } = string.Empty;

        /// <summary>
        ///     Game content backup for UI
        /// </summary>
        public string Ui { get; set; } = string.Empty;

        /// <summary>
        ///     Game content backup for Movies
        /// </summary>
        public string Movies { get; set; } = string.Empty;

        /// <summary>
        ///     Game content backup for Audio
        /// </summary>
        public string Audio { get; set; } = string.Empty;
    }
}
