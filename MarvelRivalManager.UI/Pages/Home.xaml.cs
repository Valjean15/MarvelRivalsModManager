using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

using System;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     Main layout of the application
    /// </summary>
    public sealed partial class Home : Window
    {
        #region Constants
        private static Type Settings => typeof(Settings);
        private static string PageNamespace => Settings?.Namespace ?? string.Empty;
        #endregion

        public Home()
        {
            InitializeComponent();
            navigation.SelectedItem = DefaultPage;
            SystemBackdrop = new MicaBackdrop();
        }

        #region Events

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var type =
                args.IsSettingsSelected ? Settings :
                args.SelectedItem is NavigationViewItem tab ? GetPageType(tab) :
                default(Type?);

            // Update UI
            if (type is not null)
            {
                container.Navigate(type, null, new DrillInNavigationTransitionInfo());
                navigation.Header = GetPageName(type);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Get page name by type
        /// </summary>
        private static string GetPageName(Type type)
        {
            return type.Name switch
            {
                "Settings" => "Settings",
                "ModManager" => "Mods",
                "Console" => "Actions",
                _ => string.Empty,
            };
        }

        /// <summary>
        ///     Try to get the Type of a page using the tag of the NavigationViewItem
        /// </summary>
        private static Type? GetPageType(NavigationViewItem tab)
        {
            return Type.GetType($"{PageNamespace}.{tab.Tag?.ToString() ?? string.Empty}");
        }

        #endregion
    }
}
