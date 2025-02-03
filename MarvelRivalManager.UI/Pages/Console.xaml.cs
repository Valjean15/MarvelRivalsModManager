using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;
using MarvelRivalManager.UI.Common;
using MarvelRivalManager.UI.Configuration;
using MarvelRivalManager.UI.Helper;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using static MarvelRivalManager.Library.Entities.Delegates;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     Page dedicated to apply actions
    /// </summary>
    public sealed partial class Console : Page
    {
        #region Dependencies
        private readonly IEnvironment m_environment = Services.Get<IEnvironment>();
        private readonly IRepack m_unpacker = Services.Get<IRepack>();
        private readonly IPatcher m_patcher = Services.Get<IPatcher>();
        private readonly IResourcesClient m_resources = Services.Get<IResourcesClient>();
        #endregion

        #region Fields
        private readonly ConcurrentStack<string> Logs = [];
        private readonly Lock _lock = new();
        #endregion

        public Console()
        {
            InitializeComponent();
        }

        #region Events

        private void Page_Loaded(object _, RoutedEventArgs __)
        {
            m_environment.Refresh();

            // Only check one per session
            if (SessionValues.Get("CHECK_TOOL_VERSION") != "checked")
            {
                Task.Run(async () =>
                {
                    if (await m_resources.NewVersionAvailable(async (keys, @params) => { }))
                        await Print(["REPACK_TOOL_NEW_VERSION_AVAILABLE"], new PrintParams("PACK "));

                    SessionValues.Set("CHECK_TOOL_VERSION", "checked");
                });
            }
        }

        private async void PatchButton_Click(object _, RoutedEventArgs __)
        {
            await Do(async () =>
            {
                if (await m_unpacker.Unpack(Print))
                    await m_patcher.Patch(Print);
            });
        }

        private async void UnpatchButton_Click(object _, RoutedEventArgs __)
        {
            await Do(async () => await m_patcher.Unpatch(Print));
        }

        private async void DownloadButton_Click(object _, RoutedEventArgs __)
        {
            await Do(async () => await m_resources.Download(Print));
        }

        private async void DeleteDownloadButton_Click(object _, RoutedEventArgs __)
        {
            await Do(async () => await m_resources.Delete(Print));
        }

        private void ClearButton_Click(object _, RoutedEventArgs __)
        {
            Ouput.IsReadOnly = false;
            Ouput.Document.SetText(new Microsoft.UI.Text.TextSetOptions(), string.Empty);
            Ouput.IsReadOnly = true;
            Logs.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Execute the action and show the loading indicator
        /// </summary>
        private async ValueTask Do(AsyncAction action)
        {
            IsLoading(true);
            await action();
            IsLoading(false);
        }

        /// <summary>
        ///    Print a message in the console
        /// </summary>
        private async ValueTask Print(string[] keys, PrintParams @params)
        {
            lock (_lock)
            {
                if (@params.UndoLast && !Logs.IsEmpty)
                    Logs.TryPop(out _);

                Logs.Push(LogMessages.Get(keys, @params));
            }

            await this.TryEnqueueAsync(() =>
            {
                Ouput.IsReadOnly = false;
                Ouput.Document.SetText(new Microsoft.UI.Text.TextSetOptions(), string.Join(System.Environment.NewLine, Logs));
                Ouput.IsReadOnly = true;
            });
        }

        /// <summary>
        ///    Show the loading indicator
        /// </summary>
        /// <param name="loading"></param>
        private async void IsLoading(bool loading)
        {
            await this.TryEnqueueAsync(() =>
            {
                IsInProgress.IsIndeterminate = loading;
                IsInProgress.Value = loading ? 0 : 100;
            });
        }

        #endregion
    }
}
