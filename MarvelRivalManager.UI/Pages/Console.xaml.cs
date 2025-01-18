using MarvelRivalManager.Library.Entities;
using MarvelRivalManager.Library.Services.Interface;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System.Linq;
using System.Text;

namespace MarvelRivalManager.UI.Pages
{
    /// <summary>
    ///     Page dedicated to apply actions
    /// </summary>
    public sealed partial class Console : Page
    {
        #region Dependencies
        private readonly IUnpacker m_unpacker = Services.Get<IUnpacker>();
        private readonly IPatcher m_patcher = Services.Get<IPatcher>();
        private readonly IModManager m_manager = Services.Get<IModManager>();
        #endregion

        #region Fields
        private readonly StringBuilder Logs = new();
        private Mod[] Mods = [];
        #endregion

        public Console()
        {
            InitializeComponent();
        }

        #region Events

        private void Page_Loaded(object _, RoutedEventArgs __)
        {
            Mods = m_manager.All().Where(mod => mod.Metadata.Enabled).ToArray();
        }

        private async void UnpackButton_Click(object sender, RoutedEventArgs e)
        {
            IsLoading(true);
            await m_unpacker.Unpack(Mods, Print);
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

            var kind = item.Tag switch
            {
                "characters" => KindOfMod.Characters,
                "movies" => KindOfMod.Movies,
                "ui" => KindOfMod.UI,
                _ => KindOfMod.All,
            };

            IsLoading(true);
            await m_patcher.HardRestore(kind, Print);
            IsLoading(false);
        }

        private void EnableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            var kind = item.Tag switch
            {
                "characters" => KindOfMod.Characters,
                "movies" => KindOfMod.Movies,
                "ui" => KindOfMod.UI,
                _ => KindOfMod.All,
            };

            m_patcher.Toggle(kind, true, Print);
        }

        private void DisableButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem item)
                return;

            var kind = item.Tag switch
            {
                "characters" => KindOfMod.Characters,
                "movies" => KindOfMod.Movies,
                "ui" => KindOfMod.UI,
                _ => KindOfMod.All,
            };

            m_patcher.Toggle(kind, false, Print);
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
            Logs.AppendLine(message);
            Ouput.IsReadOnly = false;
            Ouput.Document.SetText(new Microsoft.UI.Text.TextSetOptions(), Logs.ToString());
            Ouput.IsReadOnly = true;
        }

        private void IsLoading(bool loading)
        {
            IsInProgress.IsIndeterminate = loading;
            IsInProgress.Value = loading ? 0 : 100;
        }

        #endregion
    }
}
