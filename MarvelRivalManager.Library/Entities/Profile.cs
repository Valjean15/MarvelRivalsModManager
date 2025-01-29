using MarvelRivalManager.Library.Util;
using System.Text.Json.Serialization;

namespace MarvelRivalManager.Library.Entities
{
    /// <summary>
    ///     Represent a profile, which is a collection of mods
    /// </summary>
    public class Profile
    {
        public Profile()
        {
            
        }

        public Profile(string filepath)
        {
            Filepath = filepath;
            Load();
        }

        public ProfileMetadata Metadata { get; set; } = new();

        public string Filepath { get; set; } = string.Empty;

        override public string ToString() => Metadata.Name;

        #region Readonly prop

        public string Name => Metadata.Name;

        #endregion

        public void Load()
        {
            if (File.Exists(Filepath))
                Metadata = Filepath.DeserializeFileContent<ProfileMetadata>() ?? new ();
        }

        public void Update()
        {
            Metadata.Update(Filepath);
        }
    }

    public class ProfileMetadata 
    {
        public ProfileMetadata()
        {

        }

        [JsonPropertyName("Active")]
        public bool Active { get; set; } = false;

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName(name: "Selected")]
        public string[] Selected { get; set; } = [];

        public void Update(string file)
        {
            file.WriteFileContent(this);
        }
    }
}