namespace MarvelRivalManager.Library.Services.Interface
{
    /// <summary>
    ///     Configuration of the application
    /// </summary>
    public interface IEnvironment
    {
        /// <summary>
        ///     Refresh the configuration
        /// </summary>
        public IEnvironment Refresh();

        /// <summary>
        ///     Folder paths
        /// </summary>
        public Folders Folders { get; set; }

        /// <summary>
        ///     Application behavior options
        /// </summary>
        public Options Options { get; set; }
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

    /// <summary>
    ///     Application behavior options
    /// </summary>
    public class Options
    {
        /// <summary>
        ///     Indicate to use parallel loops
        /// </summary>
        public bool UseParallelLoops { get; set; }

        /// <summary>
        ///    Refers to deploy all the mods on separate files when patching the game
        /// </summary>
        public bool DeployOnSeparateFile { get; set; }

        /// <summary>
        ///    Refer to skip all actions related to unpacking the mods
        /// </summary>
        public bool IgnorePackerTool { get; set; }

        /// <summary>
        ///     Refers to make an evaluation of the mods when the user toggle the status (enable/disable)
        /// </summary>
        public bool EvaluateOnUpdate { get; set; }
    }
}