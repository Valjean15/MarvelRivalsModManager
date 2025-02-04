namespace MarvelRivalManager.Library.Entities
{
    /// <summary>
    ///     Get settings of the game
    /// </summary>
    public class GameSetting
    {
        public Dictionary<string, string> Formats => new()
        {
            { ".pak", "pak" },
            { ".rar", "rar" },
            { ".7z", "7z" },
            { ".zip", "zip" }
        };

        public Dictionary<string, string> Categories => new()
        {
            { "UI", "UI" },
            { "Movies", "movies" },
            { "Characters", "character" },
            { "Wwise", "audio" },
            { "WwiseAudio", "audio" }
        };

        public Dictionary<string, string> SubCategories => new()
        {
            { "Meshes", "model" },
            { "Weapons", "weapons" },
            { "Characters/*/Texture", "textures" },
            { "Characters/*/Textures", "textures" }
        };

        public string[] SupportedExtentensions => [".pak", ".zip", ".7z", ".rar"];

        public string AES_KEY => "0C263D8C22DCB085894899C3A3796383E9BF9DE0CBFB08C9BF2DEF2E84F29D74";

        public string GameContentFolder => "Marvel/Content";

        public string PakFilesFolder => "Paks";
    }
}
