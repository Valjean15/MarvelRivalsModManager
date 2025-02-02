using Microsoft.UI.Xaml;
using Windows.Storage;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     Class providing functionality around switching and restoring theme settings
    /// </summary>
    public static class ThemeHelper
    {
        #region Constants

        private const string SelectedAppThemeKey = "SelectedAppTheme";

        #endregion

        /// <summary>
        ///     Gets the current actual theme of the app based on the requested theme of the
        ///     root element, or if that value is Default, the requested theme of the Application.
        /// </summary>
        public static ElementTheme ActualTheme
        {
            get
            {
                foreach (var window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement && rootElement.RequestedTheme != ElementTheme.Default)
                    {
                        return rootElement.RequestedTheme;
                    }
                }

                return EnumHelper.GetEnum<ElementTheme>(Application.Current.RequestedTheme.ToString());
            }
        }

        /// <summary>
        ///     Gets or sets (with LocalSettings persistence) the RequestedTheme of the root element.
        /// </summary>
        public static ElementTheme RootTheme
        {
            get
            {
                foreach (Window window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        return rootElement.RequestedTheme;
                    }
                }

                return ElementTheme.Default;
            }
            set
            {
                foreach (Window window in WindowHelper.ActiveWindows)
                {
                    if (window.Content is FrameworkElement rootElement)
                    {
                        rootElement.RequestedTheme = value;
                    }
                }

                if (NativeHelper.IsAppPackaged)
                {
                    ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey] = value.ToString();
                }
            }
        }

        /// <summary>
        ///     Initialize the theme settings
        /// </summary>
        public static void Initialize()
        {
            if (NativeHelper.IsAppPackaged)
            {
                string? savedTheme = ApplicationData.Current.LocalSettings.Values[SelectedAppThemeKey]?.ToString();

                if (savedTheme != null)
                {
                    RootTheme = EnumHelper.GetEnum<ElementTheme>(savedTheme);
                }
            }
        }

        /// <summary>
        ///     Update the theme settings
        /// </summary>
        public static void Update(ElementTheme next)
        {
            if (NativeHelper.IsAppPackaged)
            {
                ApplicationData.Current.LocalSettings.Values.Remove(SelectedAppThemeKey);
                ApplicationData.Current.LocalSettings.Values.Add(SelectedAppThemeKey, next.ToString());
                RootTheme = next;
            }
        }
    }
}
