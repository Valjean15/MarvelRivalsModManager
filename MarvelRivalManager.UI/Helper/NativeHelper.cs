using System.Runtime.InteropServices;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     Helper class related to native methods of windows DLL's.
    /// </summary>
    internal class NativeHelper
    {
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        [DllImport("api-ms-win-appmodel-runtime-l1-1-1", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern uint GetCurrentPackageId(ref int pBufferLength, out byte pBuffer);

        /// <summary>
        ///     Check if the application is packaged.
        /// </summary>
        public static bool IsAppPackaged
        {
            get
            {
                var bufferSize = 0;
                return !(GetCurrentPackageId(ref bufferSize, out _) == APPMODEL_ERROR_NO_PACKAGE);
            }
        }
    }
}