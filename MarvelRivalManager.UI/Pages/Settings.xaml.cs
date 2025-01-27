using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Configuration;
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

        public Settings()
        {
            InitializeComponent();
        }

        #region Events

        /// <summary>
        ///     Update values of the environment. The object is updated due the two way binding
        /// </summary>
        private void Update(object sender, string value)
        {
            if (m_environment is AppEnvironment environment)
                environment.Update(m_environment);
        }

        /// <summary>
        ///     Update values of the environment. The object is updated due the two way binding
        /// </summary>
        private void ToggleButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (m_environment is AppEnvironment environment)
                environment.Update(m_environment);
        }

        #endregion
    }
}
