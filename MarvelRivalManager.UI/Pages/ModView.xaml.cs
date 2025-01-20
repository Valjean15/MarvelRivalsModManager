using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.Library.Util;
using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.ViewModels;
using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using System;
using System.Linq;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     View dedicated to the modification of a mod.
    /// </summary>
    public sealed partial class ModView : Page
    {
        #region Dependencies

        private readonly IModManager m_manager = Services.Get<IModManager>();

        #endregion

        public ModInputViewModel? Mod { get; set; } = null;
        public ModView()
        {
            InitializeComponent();
            GoBackButton.Loaded += GoBackButton_Loaded;
        }

        #region Events

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is not ModViewModel input)
                return;

            Mod = new ModInputViewModel(input.Index, input.File.Filepath);

            FilePaths.IsReadOnly = false;
            FilePaths.Document.SetText(new Microsoft.UI.Text.TextSetOptions(), Mod.Files);
            FilePaths.IsReadOnly = true;

            var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation("ForwardConnectedAnimation");
            animation?.TryStart(ConnectedElement, [CoordinatedElement]);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("BackConnectedAnimation", ConnectedElement);
        }

        private void GoBackButton_Loaded(object sender, RoutedEventArgs e)
        {
            GoBackButton.Focus(FocusState.Programmatic);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private async void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Mod!.Metadata.Tags = (Mod.InputTags?.Split(", ") ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            // Move image to the right location
            if (!string.IsNullOrEmpty(Mod.Metadata.Logo))
            {
                Mod.File.ImagesLocation.CreateDirectoryIfNotExist();
                Mod.Metadata.Logo = 
                    Mod.Metadata.Logo.MakeSafeCopy(
                        System.IO.Path.Combine(Mod.File.ImagesLocation, System.IO.Path.GetFileName(Mod.Metadata.Logo)));
            }

            await m_manager.Update(Mod);
            Frame.GoBack();
        }

        private async void ChangeLogoClick(object sender, RoutedEventArgs e)
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

            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".png");

            var file = await picker.PickSingleFileAsync();
            if (file is not null && !string.IsNullOrEmpty(file.Path))
            {
                Mod!.Metadata.Logo = file.Path;
            }

            LogoPlaceholder.Source = new BitmapImage(new Uri(Mod!.NonNullableLogo));
            LogoPlaceholder.Visibility = Mod!.HasLogo ? Visibility.Visible : Visibility.Collapsed;
            ConnectedElement.Visibility = Mod!.NoHasLogo ? Visibility.Visible : Visibility.Collapsed;

            button.IsEnabled = true;
        }

        #endregion
    }
}
