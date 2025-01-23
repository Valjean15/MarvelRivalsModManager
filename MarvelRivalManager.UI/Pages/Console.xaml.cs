using MarvelRivalManager.Library.Services.Interface;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Collections.Generic;
using System.Linq;

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
        private readonly IModManager m_manager = Services.Get<IModManager>();
        private readonly IResourcesClient m_resources = Services.Get<IResourcesClient>();
        #endregion

        #region Fields
        private readonly List<string> Logs = [];
        #endregion

        public Console()
        {
            InitializeComponent();
        }

        #region Events

        private void Page_Loaded(object _, RoutedEventArgs __)
        {
            m_environment.Load();
        }

        private async void UnpackButton_Click(object _, RoutedEventArgs __)
        {
            IsLoading(true);
            await m_unpacker.Unpack(m_manager.All().Where(mod => mod.Metadata.Enabled).ToArray(), Print);
            IsLoading(false);
        }

        private async void PatchButton_Click(object _, RoutedEventArgs __)
        {
            IsLoading(true);
            await m_patcher.Patch(Print);
            IsLoading(false);
        }

        private async void UnpatchButton_Click(object _, RoutedEventArgs __)
        {
            IsLoading(true);
            await m_patcher.Unpatch(Print);
            IsLoading(false);
        }

        private async void DownloadButton_Click(object _, RoutedEventArgs __)
        {
            await m_resources.Download(Print);
        }

        private async void DeleteDownloadButton_Click(object _, RoutedEventArgs __)
        {
            await m_resources.Delete(Print);
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

        private void Print(string message)
        {
            Print(message, false);
        }

        private void Print(string message, bool undoLast)
        {
            if (undoLast && Logs.Count > 0)
            {
                Logs[^1] = message;
            }
            else
            {
                Logs.Add(message);
            }
            
            try
            {
                Ouput.IsReadOnly = false;
                Ouput.Document.SetText(new Microsoft.UI.Text.TextSetOptions(), string.Join(System.Environment.NewLine, Logs));
                Ouput.IsReadOnly = true;
            }
            catch
            {

            }
        }

        private void IsLoading(bool loading)
        {
            IsInProgress.IsIndeterminate = loading;
            IsInProgress.Value = loading ? 0 : 100;
        }

        #endregion
    }
}
