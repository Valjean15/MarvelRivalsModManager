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
        ///    Mega folder used to store the backup game content
        /// </summary>
        public string MegaFolder { get; set; } = string.Empty;

        /// <summary>
        ///    Download folder used to store the downloaded resources
        /// </summary>
        public string DownloadFolder { get; set; } = string.Empty;

        /// <summary>
        ///     Mods folder used to store the mods
        /// </summary>
        public string ModsEnabled { get; set; } = string.Empty;

        /// <summary>
        ///     Disabled mods folder used to store the disabled mods
        /// </summary>
        public string ModsDisabled { get; set; } = string.Empty;

        /// <summary>
        ///     Folder of the repak tool
        /// </summary>
        public string RepackFolder { get; set; } = string.Empty;
    }
}
