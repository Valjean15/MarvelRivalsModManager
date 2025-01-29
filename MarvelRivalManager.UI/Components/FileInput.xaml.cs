using MarvelRivalManager.UI.Helper;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using Windows.Storage.Pickers;

using WinRT.Interop;

namespace MarvelRivalManager.UI.Components
{
    /// <summary>
    ///     Component dedicated to pick a folder from the user's file system.
    /// </summary>
    public sealed partial class FileInput : UserControl
    {
        #region Constants

        private const string ACTIVITY_ID = "FolderPickedNotificationId";

        #endregion

        #region Handlers

        /// <summary>
        ///     Event of change in the selected folder.
        /// </summary>
        public event EventHandler<string>? OnChange;

        #endregion

        #region Dependencies

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(FileInput), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(FileInput), new PropertyMetadata(string.Empty));

        #endregion

        #region Properties

        /// <summary>
        ///     Label to be displayed in the component.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        /// <summary>
        ///     Value of the selected folder.
        /// </summary>
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        #endregion

        public FileInput()
        {
            InitializeComponent();
        }

        #region Events

        /// <summary>
        ///     Event when the user click on button to search new folder
        /// </summary>
        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            // Prevent multiple clicks
            button.IsEnabled = false;

            // Build winform dialog to pick a folder
            var picker = new FolderPicker();
            InitializeWithWindow.Initialize(picker, 
                WindowNative.GetWindowHandle(
                    WindowHelper.GetWindowForElement(this)));

            // Filter kind of input
            picker.SuggestedStartLocation = PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            // Wait for user to pick a folder
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {                
                var valueChanged = !Selected.Text.Equals(folder.Path, StringComparison.InvariantCultureIgnoreCase);
                Selected.Text = folder.Path;
                Value = Selected.Text;

                if (valueChanged && OnChange is not null)
                    OnChange(this, Value);
            }
            else
            {
                // User cancelled the operation - Restore previous state
                Selected.Text = Value;
            }

            button.IsEnabled = true;
            UIHelper.AnnounceActionForAccessibility(button, Selected.Text, ACTIVITY_ID);
        }

        #endregion
    }
}
