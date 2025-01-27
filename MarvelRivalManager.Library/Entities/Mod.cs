using MarvelRivalManager.Library.Util;
using System.Text.Json.Serialization;

namespace MarvelRivalManager.Library.Entities
{
    public class ModTags
    {
        #region Tags

        public static readonly Dictionary<string, string> Formats = new()
        {
            { ".pak", "pak" },
            { ".rar", "rar" },
            { ".7z", "7z" },
            { ".zip", "zip" }
        };

        public static readonly Dictionary<string, string> Categories = new()
        {
            { "UI", "UI" },
            { "Movies", "movies" },
            { "Characters", "character" },
            { "Wwise", "audio" }
        };

        public static readonly Dictionary<string, string> SubCategories = new()
        {
            { "Meshes", "model" },
            { "Weapons", "weapons" },
            { "Textures", "textures" }
        };

        #endregion

        public static string[] GetCategoryTag(FileInformation file)
        {
            if (!Directory.Exists(file.ExtractionContent))
                return [];

            var tags = new List<string>();
            if (file.ExtractionContent.DirectoryContainsSubfolder("UI"))
                tags.Add(Categories["UI"]);

            if (file.ExtractionContent.DirectoryContainsSubfolder("Movies"))
                tags.Add(Categories["Movies"]);

            if (file.ExtractionContent.DirectoryContainsSubfolder("Characters"))
                tags.Add(Categories["Characters"]);

            if (file.ExtractionContent.DirectoryContainsSubfolder("WwiseAudio") || file.ExtractionContent.DirectoryContainsSubfolder("Wwise"))
                tags.Add(Categories["Wwise"]);

            return [.. tags];
        }

        public static string[] GetFormatTag(FileInformation file)
        {
            return Formats.TryGetValue(file.Extension, out var format) ? [format] : [];
        }

        public static string[] GetSubCategoryTag(FileInformation file)
        {
            if (!Directory.Exists(file.ExtractionContent))
                return [];

            var tags = new List<string>();

            if (file.ExtractionContent.DirectoryContainsSubfolder("Meshes"))
                tags.Add(SubCategories["Meshes"]);

            if (file.ExtractionContent.DirectoryContainsSubfolder("Weapons"))
                tags.Add(SubCategories["Weapons"]);

            if (file.ExtractionContent.DirectoryContainsSubfolder("Textures"))
                tags.Add(SubCategories["Textures"]);

            return [.. tags];
        }
    }

    /// <summary>
    ///     Structure of a mod
    /// </summary>
    public class Mod
    {
        public Mod()
        {
            Metadata = new Metadata();
            File = new FileInformation(string.Empty);
        }

        public Mod(string filepath)
        {
            File = new FileInformation(filepath);
            Metadata = new Metadata(File);
        }

        public Metadata Metadata { get; set; }
        public FileInformation File { get; set; }

        public async ValueTask SetSystemInformation()
        {
            await Metadata.SetSystemInformation(File);
        }

        public void Update()
        {
            Metadata.Update(File);
        }

        public void Delete()
        {
            File.Filepath.DeleteFileIfExist();
            File.ProfileFilepath.DeleteFileIfExist();

            if (!string.IsNullOrEmpty(Metadata.Logo))
                Metadata.Logo.DeleteFileIfExist();
        }

        public override string ToString()
        {
            return $"[{File.Extension}] | {Metadata.Name}";
        }
    }

    /// <summary>
    ///     Partial information of the mod extracted from the file
    /// </summary>
    public class FileInformation(string filepath)
    {
        public string Filepath { get; set; } = filepath;
        public string Filename => Path.GetFileNameWithoutExtension(Filepath);
        public string Location => Path.GetDirectoryName(Filepath) ?? string.Empty;
        public string Extension => Path.GetExtension(Filepath);
        public string ProfileFilepath => Path.Combine(Location, "profiles", $"{Filename}.json");
        public string ProfileFilename => Path.GetFileNameWithoutExtension(ProfileFilepath);
        public string ProfileLocation => Path.GetDirectoryName(ProfileFilepath) ?? string.Empty;
        public string ProfileExtension => Path.GetExtension(ProfileFilepath);
        public string Extraction => Path.Combine(Location, Filename);
        public string ExtractionContent => Path.Combine(Extraction, "Marvel/Content");
        public string ImagesLocation => Path.Combine(Location, "images");

        override public string ToString()
        {
            return $"{Filename} [{Extension}]";
        }
    }

    /// <summary>
    ///     Metadata related to the mod
    /// </summary>
    public class Metadata
    {
        /// <summary>
        ///    Constructor only needed for deserialize values
        /// </summary>
        public Metadata()
        {
            
        }

        public Metadata(FileInformation file)
        {
            var stored = file.ProfileFilepath.DeserializeFileContent<Metadata>();
            if (stored is not null)
            {
                Order = stored.Order;
                Logo = stored.Logo;
                SystemTags = stored.SystemTags;
                Tags = stored.Tags;
                FilePaths = stored.FilePaths;
                Name = stored.Name;
                Enabled = stored.Enabled;
                Unpacked = stored.Unpacked;
                Active = stored.Active;
                Valid = stored.Valid;
            }
            else
            {
                Name = file.Filename;
                Update(file);
            }
        }

        [JsonPropertyName("Order")]
        public int Order { get; set; }

        [JsonPropertyName("Logo")]
        public string Logo { get; set; } = string.Empty;

        [JsonPropertyName("SystemTags")]
        public string[] SystemTags { get; set; } = [];

        [JsonPropertyName("Tags")]
        public string[] Tags { get; set; } = [];

        [JsonPropertyName("FilePaths")]
        public string[] FilePaths { get; set; } = [];

        [JsonPropertyName("Name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Enabled")]
        public bool Enabled { get; set; } = false;

        [JsonPropertyName("Unpacked")]
        public bool Unpacked { get; set; } = false;

        [JsonPropertyName("Active")]
        public bool Active { get; set; } = false;

        [JsonPropertyName("Valid")]
        public bool Valid { get; set; } = false;

        /// <summary>
        ///     Set the system information of the mod
        /// </summary>
        public async ValueTask SetSystemInformation(FileInformation file)
        {
            var tagTask = Task.Run(() =>
            {
                var tags = new List<string>();

                tags.AddRange(ModTags.GetFormatTag(file));
                tags.AddRange(ModTags.GetCategoryTag(file));
                tags.AddRange(ModTags.GetSubCategoryTag(file));

                SystemTags = tags.Distinct().ToArray();
            });

            var filePathTask = Task.Run(() =>
            {
                if (Directory.Exists(file.ExtractionContent))
                {
                    FilePaths = [.. file.ExtractionContent
                        .GetAllFilesFromDirectory()
                        .Select(path => path.Replace(file.ExtractionContent, string.Empty)
                    )];
                }
            });

            await Task.WhenAll(tagTask, filePathTask);
        }

        /// <summary>
        ///     Update the metadata of the mod
        /// </summary>
        public void Update(FileInformation file)
        {
            Path.Combine(file.Location, "profiles").CreateDirectoryIfNotExist();
            file.ProfileFilepath.WriteFileContent(this);
        }
    }
}
