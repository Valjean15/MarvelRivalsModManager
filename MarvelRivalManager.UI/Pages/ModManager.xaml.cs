using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     Manager page of the mods configured
    /// </summary>
    public sealed partial class ModManager : Page
    {
        #region Dependencies

        private readonly IEnvironment m_environment = Services.Get<IEnvironment>();
        private readonly IModManager m_manager = Services.Get<IModManager>();
        private readonly IModDataAccess m_query = Services.Get<IModDataAccess>();

        #endregion

        #region Fields
        public ModViewModel? Selected = null;
        private ModCollection Enabled { get; set; } = [];
        private ModCollection Disabled { get; set; } = [];
        private TagViewModel[] Tags { get; set; } = [];
        #endregion

        public ModManager()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;
        }

        #region Events

        /// <summary>
        ///     Event when the page is loaded first time
        /// </summary>
        private async void Page_Loaded(object _, RoutedEventArgs __)
        {
            IsLoading(true);

            m_environment.Refresh();

            var mods = (await m_query.AllFilepaths(true))
                .Select((filepath, index) => new ModViewModel(index + 1, filepath))
                .OrderBy(mod => mod.Metadata.Order)
                .ThenBy(mod => mod.Metadata.Name)
                .ToArray();

            Enabled = new ModCollection(mods.Where(x => x.Metadata.Enabled));
            Disabled = new ModCollection(mods.Where(x => !x.Metadata.Enabled));

            IsLoading(false);
            Refresh();

            if (Selected is null)
                RefreshTags();
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void OnFilterChanged(object _, TextChangedEventArgs __)
        {
            Refresh();
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void FilterTags_SelectionChanged(ItemsView _, ItemsViewSelectionChangedEventArgs __)
        {
            Refresh();
        }

        #region Collections

        /// <summary>
        ///     Event when the user drop items onto the collection
        /// </summary>
        private async void CollectionLoaded(object _, RoutedEventArgs __)
        {
            // We only need to trigger this event when an mod is selected for edition
            if (Selected is null)
                return;

            var collection = Selected.Metadata.Enabled ? EnabledModsList : DisabledModsList;
            collection.ScrollIntoView(Selected, ScrollIntoViewAlignment.Default);
            collection.UpdateLayout();

            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("BackConnectedAnimation");
            if (animation is not null)
            {
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7))
                {
                    animation.Configuration = new DirectConnectedAnimationConfiguration();
                }

                await collection.TryStartConnectedAnimationAsync(animation, Selected, "ConnectedElement");
            }

            collection.Focus(FocusState.Programmatic);
        }

        /// <summary>
        ///     Event to add a new mod to the collection
        /// </summary>
        private async void CollectionAdd(object sender, RoutedEventArgs _)
        {
            if (sender is not Button button)
                return;

            // Prevent multiple clicks
            button.IsEnabled = false;

            // Build winform dialog to pick a folder
            var picker = new FileOpenPicker();
            InitializeWithWindow.Initialize(picker,
                WindowNative.GetWindowHandle(
                    WindowHelper.GetWindowForElement(this)));

            // Set options for your file picker
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            foreach (var pattern in m_query.SupportedExtentensions())
                picker.FileTypeFilter.Add(pattern);

            IsLoading(true);

            foreach (var file in (await picker.PickMultipleFilesAsync()) ?? [])
            {
                var mod = await m_manager.Add(file.Path.MakeSafeCopy(Path.Combine(m_environment.Folders.ModsEnabled, Path.GetFileName(file.Path))));
                var collection = mod.Metadata.Enabled ? Enabled : Disabled;
                collection.Add(new ModViewModel(collection.Count + 1, mod.File.Filepath));
            }

            IsLoading(false);

            Refresh();
            RefreshTags();
            button.IsEnabled = true;
        }

        /// <summary>
        ///     Event to delete a mod from the collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CollectionMoveSingle(object sender, RoutedEventArgs _)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            var source = mod.Metadata.Enabled ? Enabled : Disabled;
            var target = mod.Metadata.Enabled ? Disabled : Enabled;

            IsLoading(true);

            var (edited, success) = await Move(mod, target.Count + 1);
            if (success)
            {
                source.Remove(mod);
                target.Add(edited);
            }

            IsLoading(false);

            Refresh();
        }

        /// <summary>
        ///     Event to add a new mod to the collection
        /// </summary>
        private void CollectionRemove(object sender, RoutedEventArgs _)
        {
            if (sender is not Button button)
                return;

            var reload = false;

            IsLoading(true);

            foreach (var mod in EnabledModsList.SelectedItems
                .Select(item =>
                {
                    if (item is not ModViewModel mod)
                        return null;
                    return mod;
                })
                .Where(item => item is not null)
                .ToArray())
            {
                m_manager.Delete(mod!);
                Enabled.Remove(mod!);
                reload = true;
            }

            foreach (var mod in DisabledModsList.SelectedItems
                .Select(item =>
                {
                    if (item is not ModViewModel mod)
                        return null;
                    return mod;
                })
                .Where(item => item is not null)
                .ToArray())
            {
                m_manager.Delete(mod!);
                Disabled.Remove(mod!);
                reload = true;
            }

            IsLoading(false);

            if (reload)
            {
                Refresh();
                RefreshTags();
            }

            button.IsEnabled = true;
        }

        /// <summary>
        ///     Event to delete a mod from the collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionDeleteSingle(object sender, RoutedEventArgs _)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            IsLoading(true);

            var collection = mod.Metadata.Enabled ? Enabled : Disabled;
            m_manager.Delete(mod);
            collection.Remove(mod);

            Refresh();
            RefreshTags();
            IsLoading(false);
        }

        /// <summary>
        ///     Event when a mod element is clicked
        /// </summary>
        private void CollectionEditSingle(object sender, RoutedEventArgs _)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            Selected = mod;

            (mod.Metadata.Enabled ? EnabledModsList : DisabledModsList)
                .PrepareConnectedAnimation("ForwardConnectedAnimation", Selected, "ConnectedElement");

            Frame.Navigate(typeof(ModView), Selected, new SuppressNavigationTransitionInfo());
        }

        #region Drag and Drop

        /// <summary>
        ///     Event when the user start dragging items
        /// </summary>
        private void CollectionDragStarting(object _, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count == 0)
            {
                e.Data.RequestedOperation = DataPackageOperation.None;
                e.Cancel = true;
                return;
            }

            var package = new StringBuilder();
            foreach (var item in (e.Items ?? [])
                .Select(item =>
                {
                    if (item is not null && item is ModViewModel mod && mod is not null)
                        return mod;

                    return null;
                })
                .Where(item => item is not null)
            )
            {
                package.AppendFormat("{0},", item!.Index);
            }

            e.Data.SetText(package.ToString().TrimEnd(','));
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        ///     Event when the user drag over items
        /// </summary>
        private void CollectionDragOver(object _, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        /// <summary>
        ///     Event when the user drop items onto the collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CollectionDrop(object sender, DragEventArgs e)
        {
            if (sender is not ListView target
                || !e.DataView.Contains(StandardDataFormats.Text)
                || (target.Name == EnabledModsList.Name ? Disabled : Enabled) is not ModCollection source
                || (target.Name == EnabledModsList.Name ? Enabled : Disabled) is not ModCollection destination)
                return;

            var operation = e.GetDeferral();
            var raw = (await e.DataView.GetTextAsync() ?? string.Empty);

            if (raw
                .Split(',')
                .Select(index =>
                {
                    if (!int.TryParse(index, out var value))
                        return null;

                    return source.First(x => x.Index == value);
                })
                .Where(mod => mod is not null)
                .ToArray() is not ModViewModel[] items || items.Length == 0)
            {
                operation.Complete();
                return;
            }

            IsLoading(true);

            // Calculate the insertion index
            var index = 0;
            if (target.Items.Count != 0 && target.ContainerFromIndex(0) is ListViewItem pivot)
            {
                var height = pivot.ActualHeight + pivot.Margin.Top + pivot.Margin.Bottom;
                index = Math.Min(target.Items.Count - 1, (int)(e.GetPosition(target.ItemsPanelRoot).Y / height));
            }

            var moved = new ConcurrentBag<(ModViewModel original, ModViewModel edited, bool success)>();
            var last = destination.Count == 0 ? 0 : destination.Max(mod => mod.Index);

            for (int pos = items.Length - 1; pos >= 0; pos--)
            {
                last++;

                var item = items[pos]!;
                var (edited, success) = await Move(item, last);
                if (success)
                {
                    source.Remove(item);
                    destination.Insert(index, edited);
                }
            }

            e.AcceptedOperation = DataPackageOperation.Move;
            operation.Complete();

            IsLoading(false);

            Refresh();
        }

        #endregion

        #endregion

        #endregion

        #region Private methods

        /// <summary>
        ///     Move a mod from one collection to another
        /// </summary>
        private async ValueTask<(ModViewModel edited, bool success)> Move(ModViewModel mod, int index)
        {
            var current = mod.Metadata.Enabled;
            mod.Metadata.Enabled = !mod.Metadata.Enabled;
            var edited = await m_manager.Update(mod);
            return (new ModViewModel(index, edited.File.Filepath), current != edited.Metadata.Enabled);
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void Refresh()
        {
            var enabled = Enabled.Select(mod => mod);
            var disabled = Disabled.Select(mod => mod);

            if (FilterTags.SelectedItems.Count > 0)
            {
                var filterTag = FilterTags.SelectedItems
                    .Select(tag => tag as TagViewModel)
                    .Where(tag => tag is not null)
                    .Select(tag => tag?.Value ?? string.Empty)
                    .Where(tag => !string.IsNullOrEmpty(tag))
                    .ToArray()
                    ;

                enabled = enabled.Where(mod => filterTag.All(tag => mod.AllTags.Contains(tag)));
                disabled = disabled.Where(mod => filterTag.All(tag => mod.AllTags.Contains(tag)));
            }

            if (!string.IsNullOrEmpty(Filter.Text))
            {
                enabled = enabled.Where(x => 
                    x.Tags.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Metadata.Name.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase)
                );

                disabled = disabled.Where(x =>
                    x.Tags.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Metadata.Name.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase)
                );
            }

            EnabledModsList.ItemsSource = new ModCollection(enabled);
            DisabledModsList.ItemsSource = new ModCollection(disabled);            
        }

        /// <summary>
        ///     Event that refresh filter tags
        /// </summary>
        private void RefreshTags()
        {
            Tags = Enabled.Concat(Disabled)
                .Select(mod => (mod.Metadata.Tags ?? []).Concat(mod.Metadata.SystemTags ?? []))
                .SelectMany(tags => tags)
                .Where(tag => !string.IsNullOrEmpty(tag))
                .Distinct()
                .OrderBy(tag => tag)
                .Select(tag => new TagViewModel(tag.Trim()))
                .ToArray()
                ;

            FilterTags.ItemsSource = Tags;
        }

        /// <summary>
        ///     Change the loading state of the page
        /// </summary>
        private void IsLoading(bool loading)
        {
            IsInProgress.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;
            IsInProgress.IsIndeterminate = loading;
        }

        #endregion
    }
}