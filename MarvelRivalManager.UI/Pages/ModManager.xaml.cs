using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using MarvelRivalManager.UI.Configuration;
using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.Pages.Dialogs;
using MarvelRivalManager.UI.ViewModels;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly IProfileManager m_profiles = Services.Get<IProfileManager>();

        #endregion

        #region Fields

        public Profile[] All = [];
        private Profile Current = new();

        public ModViewModel? Selected = null;
        private ModCollection Enabled = [];
        private ModCollection Disabled = [];

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

            // Load the current profile
            if (m_environment is AppEnvironment environment)
            {
                ProfileCommandBar.Visibility = environment.Options.UseMultipleProfiles ? Visibility.Visible : Visibility.Collapsed;

                if (!Directory.Exists(environment.Folders.Collections))
                {
                    m_environment.Folders.Collections = environment.Default().Folders.Collections;
                    environment.Update(m_environment);
                }

                Current = await m_profiles.GetCurrent();
                All = await m_profiles.All(true);

                ProfileCombobox.ItemsSource = All;
                ProfileCombobox.SelectedItem = All.FirstOrDefault(x => x.Metadata.Active);
            }

            IsLoading(false);

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
        private void FilterTags_OnSelectionChange(object _, Components.MultipleTagEventArg __)
        {
            Refresh();
        }

        #region Profiles

        /// <summary>
        ///     Event when the user change the current profile
        /// </summary>
        private async void Profile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.FirstOrDefault() is not Profile selected)
                return;

            IsLoading(true);

            // Same profile to load
            if (!selected.Name.Equals(Current.Name))
            {
                // Update the current profile
                Current.Metadata.Active = false;
                await m_profiles.Update(Current);
            }

            // Load the profile
            await m_profiles.Load(selected);
            Current = selected;

            // Reload mod lists
            var mods = (await m_query.AllFilepaths(true))
                .Select((filepath, index) => new ModViewModel(index + 1, filepath))
                .OrderBy(mod => mod.Metadata.Order)
                .ThenBy(mod => mod.Metadata.Name)
                .ToArray();

            Enabled = new ModCollection(mods.Where(x => x.Metadata.Enabled));
            Disabled = new ModCollection(mods.Where(x => !x.Metadata.Enabled));

            Refresh();
            RefreshTags();

            IsLoading(false);
        }

        /// <summary>
        ///     Event to add a new profile
        /// </summary>
        private async void ProfileAdd(object sender, RoutedEventArgs e)
        {
            IsLoading(true);

            var dialog = new TextInputDialog
            {
                XamlRoot = XamlRoot,
                Title = "Enter the profile name",
                PrimaryButtonText = "Add",
            };

            var result = await dialog.ShowAsync();
            var value = dialog.GetInput();

            if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(value))
            {
                // Create the new profile
                var created = await m_profiles.Create(value, []);
                var all = await m_profiles.All();
                var selected = all.First(profile => profile.Name.Equals(created.Name));

                // Reload the profiles combobox
                ProfileCombobox.ItemsSource = all;
                ProfileCombobox.SelectedItem = selected;
            }

            IsLoading(false);
        }

        /// <summary>
        ///     Event to delete the current profile
        /// </summary>
        private async void ProfileDelete(object sender, RoutedEventArgs e)
        {
            IsLoading(true);

            // Delete the current profile
            await m_profiles.Delete(Current);

            // Load other profile
            All = await m_profiles.All(true);
            if (All.Length == 0) All = [Current];
            Current = await m_profiles.GetCurrent();

            // Reload the profiles combobox
            ProfileCombobox.ItemsSource = All;
            ProfileCombobox.SelectedItem = Current;

            IsLoading(false);
        }

        #endregion

        #region Command Bar

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

            UpdateProfile();

            IsLoading(false);

            Refresh();
            RefreshTags();
            button.IsEnabled = true;
        }

        /// <summary>
        ///     Event when a mod is evaluated via the unpacker
        /// </summary>
        private async void CollectionEvaluate(object sender, RoutedEventArgs _)
        {
            if (sender is not Button button)
                return;

            var reload = false;

            IsLoading(true);

            foreach (var mod in EnabledModsList.GetSelected()
                .Concat(DisabledModsList.GetSelected())
                .ToArray())
            {
                await m_manager.Evaluate(mod!);
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
        ///     Event to add a new mod to the collection
        /// </summary>
        private void CollectionRemove(object sender, RoutedEventArgs _)
        {
            if (sender is not Button button)
                return;

            var reload = false;

            IsLoading(true);

            foreach (var mod in EnabledModsList.GetSelected()
                .Concat(DisabledModsList.GetSelected())
                .ToArray())
            {
                m_manager.Delete(mod!);
                (mod!.Metadata.Enabled ? Enabled : Disabled).Remove(mod!);
                reload = true;
            }

            IsLoading(false);

            if (reload)
            {
                UpdateProfile();
                Refresh();
                RefreshTags();
            }

            button.IsEnabled = true;
        }

        #endregion

        #region Collections

        /// <summary>
        ///     Event when the user drop items onto the collection
        /// </summary>
        private async void ModCollection_OnCollectionLoaded(object _, RoutedEventArgs __)
        {
            // We only need to trigger this event when an mod is selected for edition
            if (Selected is null)
                return;

            var collection = (Selected.Metadata.Enabled ? EnabledModsList : DisabledModsList).View();
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
        ///     Event to delete a mod from the collection
        /// </summary>
        private async void ModCollection_OnMove(object sender, Components.SingleModEventArg e)
        {
            var source = e.Mod.Metadata.Enabled ? Enabled : Disabled;
            var target = e.Mod.Metadata.Enabled ? Disabled : Enabled;

            IsLoading(true);

            var (edited, success) = await Move(e.Mod, target.Count + 1);
            if (success)
            {
                source.Remove(e.Mod);
                target.Add(edited);
            }

            UpdateProfile();
            IsLoading(false);

            Refresh();
        }

        /// <summary>
        ///     Event when a mod element is clicked
        /// </summary>
        private void ModCollection_OnEdit(object _, Components.SingleModEventArg e)
        {
            Selected = e.Mod;

            (e.Mod.Metadata.Enabled ? EnabledModsList : DisabledModsList).View()
                .PrepareConnectedAnimation("ForwardConnectedAnimation", Selected, "ConnectedElement");

            Frame.Navigate(typeof(ModView), Selected, new SuppressNavigationTransitionInfo());
        }

        /// <summary>
        ///     Event when a mod is evaluated via the unpacker
        /// </summary>
        private async void ModCollection_OnEvaluate(object _, Components.SingleModEventArg e)
        {
            IsLoading(true);

            await m_manager.Evaluate(e.Mod);

            Refresh();
            RefreshTags();
            IsLoading(false);
        }

        /// <summary>
        ///     Event to delete a mod from the collection
        /// </summary>
        private void ModCollection_OnDelete(object _, Components.SingleModEventArg e)
        {
            IsLoading(true);

            var collection = e.Mod.Metadata.Enabled ? Enabled : Disabled;
            m_manager.Delete(e.Mod);
            collection.Remove(e.Mod);

            UpdateProfile();
            Refresh();
            RefreshTags();
            IsLoading(false);
        }

        /// <summary>
        ///     Event when the user drop items onto the collection
        /// </summary>
        private async void ModCollection_OnDropItems(object sender, Components.DropModEventArg e)
        {
            if (sender is not FrameworkElement target)
                return;

            var source = target.Name == EnabledModsList.Name ? Disabled : Enabled;
            var destination = target.Name == EnabledModsList.Name ? Enabled : Disabled;

            var items = e.Indexes.Select(index => source.First(x => x.Index == index)).ToArray();
            if (items.Length == 0)
                return;

            IsLoading(true);

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
                    destination.Insert(e.Position, edited);
                }
            }

            UpdateProfile();
            IsLoading(false);

            Refresh();
        }

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
        ///     Update the current profile
        /// </summary>
        private async void UpdateProfile()
        {
            Current.Metadata.Selected = Enabled.Select(mod => mod.File.Filename).ToArray();
            Current = await m_profiles.Update(Current);
        }

        /// <summary>
        ///     Event that filter the mods lists
        /// </summary>
        private void Refresh()
        {
            var enabled = Enabled.Select(mod => mod);
            var disabled = Disabled.Select(mod => mod);

            var tags = FilterTags.GetSelected();
            if (tags.Length > 0)
            {
                var filterTag = tags.Select(tag => tag.Value).ToArray();
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

            EnabledModsList.Load(new ModCollection(enabled));
            DisabledModsList.Load(new ModCollection(disabled));
        }

        /// <summary>
        ///     Event that refresh filter tags
        /// </summary>
        private void RefreshTags()
        {
            FilterTags.Load(Enabled.Concat(Disabled).ToArray());
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