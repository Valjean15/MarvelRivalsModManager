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

        public string GameContentFolder => "Marvel/Content";

        public string PakFilesFolder => "Paks";

        public string PatchPakFilesFormat => "Patch_-Windows_";
    }
}
