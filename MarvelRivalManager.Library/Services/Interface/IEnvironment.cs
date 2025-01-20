using MarvelRivalManager.Library.Entities;
using Microsoft.Extensions.Configuration;

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

        /// <summary>
        ///     Get folder by kind of mod
        /// </summary>
        public string Get(KindOfMod kind)
        {
            return kind switch
            {
                KindOfMod.Characters => Characters,
                KindOfMod.Audio => Audio,
                KindOfMod.Movies => Movies,
                KindOfMod.UI => Ui,
                _ => string.Empty
            };
        }

        /// <summary>
        ///     Basic structure of the backup folders
        /// </summary>
        public static string[] BasicStructure(KindOfMod kind)
        {
            return kind switch
            {
                KindOfMod.Characters => ["Marvel", "Characters", "VFX"],
                KindOfMod.UI => ["Marvel", "Marvel_LQ", "UI"],
                KindOfMod.Movies => ["Marvel", "Movies", "Movies_HeroSkill", "Movies_Levels"],
                KindOfMod.Audio => ["Marvel", "WwiseAudio", "Wwise"],
                _ => []
            };
        }
    }
}
