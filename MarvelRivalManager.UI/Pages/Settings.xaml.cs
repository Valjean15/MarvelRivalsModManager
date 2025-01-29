using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Configuration;
using MarvelRivalManager.UI.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     Page dedicated to update the user settings of the application
    /// </summary>
    public sealed partial class Settings : Page
    {
        #region Dependencies
        private readonly IEnvironment m_environment = Services.Get<IEnvironment>().Refresh();
        #endregion

        #region Fields
        private string SelectedTheme => ThemeHelper.ActualTheme.ToString();
        private bool CanChangeTheme => NativeHelper.IsAppPackaged;
        #endregion

        public Settings()
        {
            InitializeComponent();
        }

        #region Events

        /// <summary>
        ///     Update values of the environment. The object is updated due the two way binding
        /// </summary>
        private void Update(object _, string __)
        {
            UpdateSettings();
        }

        /// <summary>
        ///     Update values of the environment. The object is updated due the two way binding
        /// </summary>
        private void ToggleButton_Click(object _, RoutedEventArgs __)
        {
            UpdateSettings();
        }

        /// <summary>
        ///     Toggle the theme of the application
        /// </summary>
        private void ToggleThemeButton_Click(object _, RoutedEventArgs __)
        {
            if (!CanChangeTheme)
                return;

            var next = ThemeHelper.ActualTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            ThemeHelper.Update(next);
            ToggleThemeButton.Content = next.ToString();
        }

        #endregion

        #region Private methods

        /// <summary>
        ///     Update the settings of the application
        /// </summary>
        private void UpdateSettings()
        {
            if (m_environment is AppEnvironment environment)
                environment.Update(m_environment);
        }

        #endregion
    }
}
