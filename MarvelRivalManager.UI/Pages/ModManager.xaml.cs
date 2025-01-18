using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using System;
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

        private readonly IModManager m_manager = Services.Get<IModManager>();

        #endregion

        #region Fields
        public ModViewModel? Selected = null;
        private ModCollection Enabled { get; set; } = [];
        private ModCollection Disabled { get; set; } = [];
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
        private void Page_Loaded(object _, RoutedEventArgs __)
        {
            var mods = m_manager.All().Select((mod, index) => new ModViewModel(index + 1, mod)).ToArray();

            Enabled = new ModCollection(mods.Where(x => x.Values.Metadata.Enabled));
            Disabled = new ModCollection(mods.Where(x => !x.Values.Metadata.Enabled));
            Refresh();
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void OnFilterChanged(object _, TextChangedEventArgs __)
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

            var collection = Selected.Values.Metadata.Enabled ? EnabledModsList : DisabledModsList;
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
        private async void CollectionAdd(object sender, RoutedEventArgs e)
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

            foreach (var pattern in m_manager.SupportedExtentensions())
                picker.FileTypeFilter.Add(pattern);

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                foreach (var file in files)
                {
                    var mod = await m_manager.Add(file.Path);
                    var collection = mod.Metadata.Enabled ? Enabled : Disabled;
                    collection.Add(new ModViewModel(collection.Count + 1, mod));
                }
            }

            Refresh();
            button.IsEnabled = true;
        }

        /// <summary>
        ///     Event to delete a mod from the collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CollectionMoveSingle(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            var source = mod.Values.Metadata.Enabled ? Enabled : Disabled;
            var target = mod.Values.Metadata.Enabled ? Disabled : Enabled;

            var (edited, success) = await Move(mod, target.Count + 1);
            if (success)
            {
                source.Remove(mod);
                target.Add(edited);
            }

            Refresh();
        }

        /// <summary>
        ///     Event to add a new mod to the collection
        /// </summary>
        private void CollectionRemove(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

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
                m_manager.Delete(mod!.Values);
                Enabled.Remove(mod);
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
                m_manager.Delete(mod!.Values);
                Disabled.Remove(mod);
            }

            Refresh();
            button.IsEnabled = true;
        }

        /// <summary>
        ///     Event to delete a mod from the collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CollectionDeleteSingle(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            var collection = mod.Values.Metadata.Enabled ? Enabled : Disabled;
            m_manager.Delete(mod.Values);
            collection.Remove(mod);
            Refresh();
        }

        /// <summary>
        ///     Event when a mod element is clicked
        /// </summary>
        private void CollectionEditSingle(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not ModViewModel mod)
                return;

            Selected = mod;

            (mod.Values.Metadata.Enabled ? EnabledModsList : DisabledModsList)
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

            // Calculate the insertion index
            var index = 0;
            if (target.Items.Count != 0 && target.ContainerFromIndex(0) is ListViewItem pivot)
            {
                var height = pivot.ActualHeight + pivot.Margin.Top + pivot.Margin.Bottom;
                index = Math.Min(target.Items.Count - 1, (int)(e.GetPosition(target.ItemsPanelRoot).Y / height));
            }

            for (int pos = items.Length - 1; pos >= 0; pos--)
            {
                var item = items[pos]!;
                var (edited, success) = await Move(item, destination.Count + 1);
                if (success)
                {
                    source.Remove(item);
                    destination.Insert(index, edited);
                }
            }

            e.AcceptedOperation = DataPackageOperation.Move;
            operation.Complete();

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
            var status = mod.Values.Metadata.Enabled;
            var edited = await(mod.Values.Metadata.Enabled ? m_manager.Disable(mod.Values) : m_manager.Enable(mod.Values));
            return (new ModViewModel(index, edited), status != edited.Metadata.Enabled);
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void Refresh()
        {
            if (!string.IsNullOrEmpty(Filter.Text))
            {
                EnabledModsList.ItemsSource = new ModCollection(
                    Enabled.Where(x => x.Tags.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Values.Metadata.Name.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase)
                ));
                DisabledModsList.ItemsSource = new ModCollection(
                    Disabled.Where(x => x.Tags.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase) ||
                    x.Values.Metadata.Name.Contains(Filter.Text, StringComparison.CurrentCultureIgnoreCase)
                ));
            }
            else
            {
                EnabledModsList.ItemsSource = Enabled;
                DisabledModsList.ItemsSource = Disabled;
            }
        }

        #endregion
    }
}