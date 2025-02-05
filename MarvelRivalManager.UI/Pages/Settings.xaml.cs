using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel;

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
        private string AppVersion
        {
            get
            {
                if (!NativeHelper.IsAppPackaged)
                    return "Portable";

                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
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
            m_environment.Update(m_environment);
        }

        /// <summary>
        ///     Update values of the environment. The object is updated due the two way binding
        /// </summary>
        private void ToggleButton_Click(object _, RoutedEventArgs __)
        {
            m_environment.Update(m_environment);
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
    }
}
