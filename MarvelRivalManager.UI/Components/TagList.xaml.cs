using MarvelRivalManager.UI.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace MarvelRivalManager.UI.Components
{
    public record class MultipleTagEventArg(string[] Tags);

    public sealed partial class TagList : UserControl
    {
        #region Handlers

        public event EventHandler<MultipleTagEventArg>? OnSelectionChange;

        #endregion

        public TagList()
        {
            InitializeComponent();
        }

        #region Public Methods

        public void Load(ModViewModel[] mods)
        {
            Load((mods ?? [])
                .Select(mod => (mod.Metadata.Tags ?? []).Concat(mod.Metadata.SystemTags ?? []))
                .SelectMany(tags => tags)
                .Where(tag => !string.IsNullOrEmpty(tag))
                .ToArray()
            );
        }

        public void Load(string[] tags)
        {
            FilterTags.ItemsSource = (tags ?? [])
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .Select(tag => new TagViewModel(tag.Trim()))
                .ToArray()
                ;
        }

        public TagViewModel[] GetSelected()
        {
            return FilterTags.SelectedItems
                    .Select(tag => tag as TagViewModel)
                    .Where(tag => tag is not null && !string.IsNullOrEmpty(tag.Value))
                    .ToArray()!
                    ;
        }

        #endregion

        private void FilterTags_SelectionChanged(ItemsView _, ItemsViewSelectionChangedEventArgs __)
        {
            if (OnSelectionChange is not null)
                OnSelectionChange(this, new MultipleTagEventArg(GetSelected()
                    .Select(tag => tag?.Value ?? string.Empty)
                    .Where(tag => !string.IsNullOrEmpty(tag))
                    .ToArray()));
        }
    }
}
