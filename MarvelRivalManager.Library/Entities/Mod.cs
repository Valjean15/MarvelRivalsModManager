using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Entities
{
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
            File.ProfileFilepath.WriteFileContent(Metadata);
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
                if (file.ExtractionContent.DirectoryContainsSubfolder("UI"))
                    tags.Add(ModTags.UI);

                if (file.ExtractionContent.DirectoryContainsSubfolder("Movies"))
                    tags.Add(ModTags.Movies);

                if (file.ExtractionContent.DirectoryContainsSubfolder("Characters"))
                    tags.Add(ModTags.Character);

                tags.AddRange(SystemTags);
                SystemTags = tags.Distinct().ToArray();
            });

            var filePathTask = Task.Run(() =>
            {
                FilePaths = [.. file.ExtractionContent
                    .GetAllFilesFromDirectory()
                    .Select(path => path.Replace(file.ExtractionContent, string.Empty)
                )];
            });

            await Task.WhenAll(tagTask, filePathTask);
            Store(file);
        }

        private void Store(FileInformation file)
        {
            file.ProfileFilepath.WriteFileContent(this);
        }
    }
}
