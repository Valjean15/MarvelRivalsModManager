using MarvelRivalManager.UI.Helper;
using MarvelRivalManager.UI.ViewModels;
using Microsoft.UI.Xaml;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

using System;

using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     View dedicated to the modification of a mod.
    /// </summary>
    public sealed partial class ModView : Page
    {
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

            Mod = new ModInputViewModel(input.Index, input.Values);

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

        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            Mod!.Values.Metadata.Tags = Mod.InputTags?.Split(", ") ?? [];

            // Move image to the right location
            if (!string.IsNullOrEmpty(Mod.Values.Metadata.Logo))
            {
                if (!System.IO.Directory.Exists(Mod.Values.File.ImagesLocation))
                    System.IO.Directory.CreateDirectory(Mod.Values.File.ImagesLocation);

                var filename = System.IO.Path.GetFileName(Mod.Values.Metadata.Logo);
                var target = System.IO.Path.Combine(Mod.Values.File.ImagesLocation, filename);
                System.IO.File.Move(Mod.Values.Metadata.Logo, target, true);
                Mod.Values.Metadata.Logo = target;
            }

            Mod.Values.Update();
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
                Mod!.Values.Metadata.Logo = file.Path;
            }

            LogoPlaceholder.Source = new BitmapImage(new Uri(Mod!.NullableLogo));
            LogoPlaceholder.Visibility = Mod!.HasLogo ? Visibility.Visible : Visibility.Collapsed;
            ConnectedElement.Visibility = Mod!.NoHasLogo ? Visibility.Visible : Visibility.Collapsed;

            button.IsEnabled = true;
        }

        #endregion
    }
}
