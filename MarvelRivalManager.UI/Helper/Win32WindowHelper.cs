using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using static MarvelRivalManager.UI.Common.Win32;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     Helper class to handle the window size of a window.
    /// </summary>
    /// <param name="window"></param>
    internal class Win32WindowHelper(Window window)
    {
        #region Private fields

        private static WinProc? NewWndProc = null;
        private static nint OldWndProc = nint.Zero;

        private POINT? MinWindowSize = null;
        private POINT? MaxWindowSize = null;

        private readonly Window Window = window;

        #endregion

        /// <summary>
        ///     Set a maximum and minimum size for the window.
        /// </summary>
        public void SetWindowMinMaxSize(POINT? minWindowSize = null, POINT? maxWindowSize = null)
        {
            MinWindowSize = minWindowSize;
            MaxWindowSize = maxWindowSize;

            var hwnd = GetWindowHandleForCurrentWindow(Window);

            NewWndProc = new WinProc(WndProc);
            OldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, NewWndProc);
        }

        /// <summary>
        ///     Get the window handle for the current window.
        /// </summary>
        private static nint GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        /// <summary>
        ///     Get the DPI for the window.
        /// </summary>
        private nint WndProc(nint hWnd, WindowMessage Msg, nint wParam, nint lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    if (MinWindowSize != null)
                    {
                        minMaxInfo.ptMinTrackSize.x = (int)(MinWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMinTrackSize.y = (int)(MinWindowSize.Value.y * scalingFactor);
                    }
                    if (MaxWindowSize != null)
                    {
                        minMaxInfo.ptMaxTrackSize.x = (int)(MaxWindowSize.Value.x * scalingFactor);
                        minMaxInfo.ptMaxTrackSize.y = (int)(MaxWindowSize.Value.y * scalingFactor);
                    }

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(OldWndProc, hWnd, Msg, wParam, lParam);
        }

        /// <summary>
        ///     Set the window long pointer.
        /// </summary>
        private nint SetWindowLongPtr(nint hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (nint.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new nint(SetWindowLong32(hWnd, nIndex, newProc));
        }

        /// <summary>
        ///     Point on the screen
        /// </summary>
        internal struct POINT
        {
            public int x;
            public int y;
        }

        /// <summary>
        ///     Struct to handle the min max info.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
    }
}
