using MarvelRivalManager.Library.Entities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace MarvelRivalManager.UI.ViewModels
{
    public class ModInputViewModel : ModViewModel
    {
        public ModInputViewModel(int index, Mod mod) : base(index, mod)
        {
            InputTags = string.Join(", ", Values.Metadata.Tags);
        }

        public string InputTags { get; set; }

        #region Read
        public string SystemTags => string.Join(", ", Values.Metadata.SystemTags);

        public string Files { 
            get 
            {
                var builder = new StringBuilder();
                foreach (var file in Values.Metadata.FilePaths)
                {
                    builder.AppendLine(file);
                }

                return builder.ToString();
            } 
        }

        #endregion
    }

    public class ModViewModel(int index, Mod mod)
    {
        public int Index { get; set; } = index;
        public Mod Values { get; set; } = mod;

        #region View properties
        public string Name
        {
            get
            {
                if (!string.IsNullOrEmpty(Values.Metadata.Name))
                    return Values.Metadata.Name;

                if (!string.IsNullOrEmpty(Values.File.Filename))
                    return Values.File.Filename;

                return "Unknown";
            }
        }

        public string Tags
        {
            get
            {
                var value = string.Join(", ", Values.Metadata.Tags.Concat(Values.Metadata.SystemTags)
                    .Where(tag => !string.IsNullOrWhiteSpace(tag)));
                return string.IsNullOrEmpty(value) ? "[No tags]" : value;
            }
        }

        public string Status
        {
            get
            {
                return string.Join(", ", [
                    Values.Metadata.Valid ? "Valid" : "Invalid",
                    Values.Metadata.Unpacked ? "Unpacked" : "Packed",
                    Values.Metadata.Active ? "Active" : "Inactive"
                ]);
            }
        }

        public bool Invalid => !Values.Metadata.Valid;
        public string NullableLogo => string.IsNullOrEmpty(Values.Metadata.Logo) ? "ms-appx:///Assets/DefaultImage.jpg" : Values.Metadata.Logo;
        public bool NoHasLogo => string.IsNullOrEmpty(Values.Metadata.Logo);
        public bool HasLogo => !string.IsNullOrEmpty(Values.Metadata.Logo);
        #endregion

        public override string ToString()
        {
            return Values.ToString();
        }
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