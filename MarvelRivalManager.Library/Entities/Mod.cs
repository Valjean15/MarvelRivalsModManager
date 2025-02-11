using MarvelRivalManager.Library.Util;

namespace MarvelRivalManager.Library.Entities
{
    /// <summary>
    ///     Structure of a mod
    /// </summary>
    public class Mod
    {
        public FileInformation File { get; set; }
        public Metadata Metadata { get; set; }

        public Mod(string filepath)
        {
            File = new FileInformation(filepath);
            Metadata = new Metadata(File);
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
    ///     Information related to the file
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
                IgnoreUnpackage = stored.IgnoreUnpackage;
            }
            else
            {
                Name = file.Filename;
                Update(file);
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
        public bool IgnoreUnpackage { get; set; }

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