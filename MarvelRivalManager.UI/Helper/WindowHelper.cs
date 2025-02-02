using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Windows.Storage;
using WinRT.Interop;
using Microsoft.UI.Windowing;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     Helper class to allow the app to find the Window that contains an
    ///     arbitrary UIElement (GetWindowForElement). To do this, we keep track
    ///     of all active Windows.  The app code must call WindowHelper.CreateWindow
    ///     rather than "new Window" so we can keep track of all the relevant
    ///     windows.  In the future, we would like to support this in platform APIs.
    /// </summary>
    public class WindowHelper
    {
        #region Fields

        /// <summary>
        ///     Readonly active windows
        /// </summary>
        static public List<Window> ActiveWindows { get { return _activeWindows; } }

        /// <summary>
        ///     Internal active windows
        /// </summary>
        static private List<Window> _activeWindows = [];

        #endregion

        /// <summary>
        ///     Create a new Window and track it.
        /// </summary>
        static public Window CreateWindow()
        {
            Window newWindow = new Window
            {
                SystemBackdrop = new MicaBackdrop()
            };
            TrackWindow(newWindow);
            return newWindow;
        }

        /// <summary>
        ///     Track a window so we can find it later.
        /// </summary>
        static public void TrackWindow(Window window)
        {
            window.Closed += (sender, args) => {
                _activeWindows.Remove(window);
            };
            _activeWindows.Add(window);
        }

        /// <summary>
        ///     Get the AppWindow for a Window.
        /// </summary>
        static public AppWindow GetAppWindow(Window window)
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(wndId);
        }

        /// <summary>
        ///     Get the Window for an AppWindow.
        /// </summary>
        static public Window GetWindowForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return window;
                    }
                }
            }
            return null!;
        }

        /// <summary>
        ///     Get dpi for an element
        /// </summary>
        static public double GetRasterizationScaleForElement(UIElement element)
        {
            if (element.XamlRoot != null)
            {
                foreach (Window window in _activeWindows)
                {
                    if (element.XamlRoot == window.Content.XamlRoot)
                    {
                        return element.XamlRoot.RasterizationScale;
                    }
                }
            }
            return 0.0;
        }

        /// <summary>
        ///     Get the local folder for the app.
        /// </summary>
        /// <returns></returns>
        static public StorageFolder GetAppLocalFolder()
        {
            StorageFolder localFolder;
            if (!NativeHelper.IsAppPackaged)
            {
                localFolder = Task.Run(async () => await StorageFolder.GetFolderFromPathAsync(AppContext.BaseDirectory)).Result;
            }
            else
            {
                localFolder = ApplicationData.Current.LocalFolder;
            }
            return localFolder;
        }
    }
}
