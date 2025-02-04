using MarvelRivalManager.Library.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MarvelRivalManager.UI.ViewModels
{
    public class ModInputViewModel : ModViewModel
    {
        public ModInputViewModel(int index, string filepath) : base(index, filepath)
        {
            InputTags = string.Join(", ", Metadata.Tags);
        }

        public string InputTags { get; set; }

        #region Read
        public string SystemTags => string.Join(", ", Metadata.SystemTags);

        public string Files
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var file in Metadata.FilePaths)
                {
                    builder.AppendLine(file);
                }

                return builder.ToString();
            }
        }

        #endregion
    }

    public class ModViewModel(int index, string filepath) : Mod(filepath)
    {
        public int Index { get; set; } = index;

        #region View properties
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(Metadata.Name))
                    return Metadata.Name;

                if (!string.IsNullOrEmpty(File.Filename))
                    return File.Filename;

                return "Unknown";
            }
        }
        public string[] AllTags => Metadata.Tags
            .Concat(Metadata.SystemTags)
            .Distinct()
            .Where(tag => !string.IsNullOrEmpty(tag))
            .ToArray();
        public string Tags
        {
            get
            {
                var value = string.Join(", ", AllTags.Where(tag => !string.IsNullOrWhiteSpace(tag)));
                return string.IsNullOrEmpty(value) ? "[No tags]" : value;
            }
        }
        public string Status
        {
            get
            {
                return string.Join(", ", [
                    Metadata.Valid ? "Valid" : "Invalid",
                    Metadata.Unpacked ? "Unpacked" : "Packed",
                    Metadata.Active ? "Active" : "Inactive"
                ]);
            }
        }
        public bool Invalid => !Metadata.Valid;
        public string NonNullableLogo => string.IsNullOrEmpty(Metadata.Logo) ? "ms-appx:///Assets/DefaultImage.jpg" : Metadata.Logo;
        public bool NoHasLogo => string.IsNullOrEmpty(Metadata.Logo);
        public bool HasLogo => !string.IsNullOrEmpty(Metadata.Logo);
        #endregion
    }

    public partial class ModCollection : ObservableCollection<ModViewModel>
    {
        public ModCollection() : base()
        {

        }

        public ModCollection(IEnumerable<ModViewModel> collection) : base()
        {
            foreach (var mod in collection ?? [])
            {
                Add(mod);
            }
        }

        public override string ToString()
        {
            return $"Length: [{Count}]";
        }
    }
}