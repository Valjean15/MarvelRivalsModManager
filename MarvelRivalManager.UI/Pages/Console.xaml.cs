using MarvelRivalManager.Library.Entities;
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
        private readonly IUnpacker m_unpacker = Services.Get<IUnpacker>();
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

        private async void UnpackButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading(true);
            await m_unpacker.Unpack(m_manager.All().Where(mod => mod.Metadata.Enabled).ToArray(), Print);
            IsLoading(false);
        }

        private async void PatchButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading(true);
            await m_patcher.Patch(Print);
            IsLoading(false);
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            IsLoading(true);
            await m_patcher.HardRestore(GetKindFromTag(item.Tag?.ToString() ?? string.Empty), Print);
            IsLoading(false);
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            m_patcher.Toggle(GetKindFromTag(item.Tag?.ToString() ?? string.Empty), true, Print);
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            m_patcher.Toggle(GetKindFromTag(item.Tag?.ToString() ?? string.Empty), false, Print);
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            var tag = item.Tag?.ToString() ?? string.Empty;
            if (tag.Equals("unpacker"))
            {
                await m_resources.Unpacker(Print);
            } 
            else
            {
                await m_resources.Download(GetKindFromTag(tag), Print);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button)
                return;

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

        private KindOfMod GetKindFromTag(string tag)
        {
            return tag switch
            {
                "characters" => KindOfMod.Characters,
                "movies" => KindOfMod.Movies,
                "ui" => KindOfMod.UI,
                "audio" => KindOfMod.Audio,
                _ => KindOfMod.All,
            };
        }

        #endregion
    }
}
