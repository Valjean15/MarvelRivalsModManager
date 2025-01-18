using MarvelRivalManager.Library.Util;
using System.Text.Json;

namespace MarvelRivalManager.Library.Entities
{
    public static class ModOptions
    {
        /// <summary>
        ///     Json serialized options for the settings
        /// </summary>
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    }

    public class ModTags
    {
        public const string PakFile = "pak";
        public const string CompressedFile = "compressed";
        public const string UI = "UI";
        public const string Movies = "movies";
        public const string Character = "character";
    }

    /// <summary>
    ///     Structure of a mod
    /// </summary>
    public class Mod
    {
        public Mod(string filepath)
        {
            File = new FileInformation(filepath);
            Metadata = new Metadata(File);
        }

        public Metadata Metadata { get; set; }
        public FileInformation File { get; set; }

        public void Update()
        {
            System.IO.File.WriteAllText(File.ProfileFilepath, JsonSerializer.Serialize(Metadata, ModOptions.JsonOptions));
        }

        public void Delete()
        {
            if (System.IO.File.Exists(File.Filepath))
                System.IO.File.Delete(File.Filepath);

            if (System.IO.File.Exists(File.ProfileFilepath))
                System.IO.File.Delete(File.ProfileFilepath);

            if (!string.IsNullOrEmpty(Metadata.Logo) && System.IO.File.Exists(Metadata.Logo))
                System.IO.File.Delete(Metadata.Logo);
        }

        public override string ToString()
        {
            return $"[{Metadata.Order}] [{File.Extension}] | {Metadata.Name}";
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
        public Metadata()
        {
            
        }

        public Metadata(FileInformation file)
        {
            if (File.Exists(file.ProfileFilepath))
            {
                var stored = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(file.ProfileFilepath));
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
            }
            else
            {
                Name = file.Filename;
                SystemTags = [file.Extension.Equals(".pak") ? ModTags.PakFile : ModTags.CompressedFile];
                Store(file);
            }
        }

        public int Order { get; set; }
        public string Logo { get; set; } = string.Empty;
        public string[] SystemTags { get; set; } = [];
        public string[] Tags { get; set; } = [];
        public string[] FilePaths { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public bool Enabled { get; set; } = false;
        public bool Unpacked { get; set; } = false;
        public bool Active { get; set; } = false;
        public bool Valid { get; set; } = false;

        public async ValueTask Update(FileInformation file)
        {
            var tagTask = Task.Run(() =>
            {
                var tags = new List<string>();
                if (file.ExtractionContent.ContainSubFolder("UI"))
                    tags.Add(ModTags.UI);

                if (file.ExtractionContent.ContainSubFolder("Movies"))
                    tags.Add(ModTags.Movies);

                if (file.ExtractionContent.ContainSubFolder("Characters"))
                    tags.Add(ModTags.Character);

                tags.AddRange(SystemTags);
                SystemTags = tags.Distinct().ToArray();
            });

            var filePathTask = Task.Run(() =>
            {
                FilePaths = [.. file.ExtractionContent
                    .GetAllFilePaths()
                    .Select(path => path.Replace(file.ExtractionContent, string.Empty)
                )];
            });

            await Task.WhenAll(tagTask, filePathTask);
            Store(file);
        }

        private void Store(FileInformation file)
        {
            File.WriteAllText(file.ProfileFilepath, JsonSerializer.Serialize(this, ModOptions.JsonOptions));
        }
    }
}
