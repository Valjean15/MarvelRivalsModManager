using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace MarvelRivalManager.UI.Helper
{
    /// <summary>
    ///     This class contains helper methods for the UI.
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        ///     Run an async method that affects the UI.
        /// </summary>
        public async static ValueTask TryEnqueueAsync(this DependencyObject view, Action action)
        {
            await Task.Run(() =>
            {
                view.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        action();
                    }
                    catch
                    {
                        // Left blank intentionally
                    }
                });
            });
        }
    }
}
