using Microsoft.UI.Xaml.Controls;

namespace MarvelRivalManager.UI.Pages.Dialogs
{
    public sealed partial class TextInputDialog : ContentDialog
    {
        public TextInputDialog()
        {
            InitializeComponent();
        }

        public string GetInput()
        {
            return TextInput.Text;
        }
    }
}
