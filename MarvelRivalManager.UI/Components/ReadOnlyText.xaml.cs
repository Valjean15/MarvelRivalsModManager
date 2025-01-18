using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MarvelRivalManager.UI.Components
{
    /// <summary>
    ///     Component to show readonly information
    /// </summary>
    public sealed partial class ReadOnlyText : UserControl
    {
        #region Dependencies

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(ReadOnlyText), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(ReadOnlyText), new PropertyMetadata(string.Empty));

        #endregion

        #region Properties

        /// <summary>
        ///     Label to be displayed in the component.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        /// <summary>
        ///     Value of the selected folder.
        /// </summary>
        public string Value
        {
            get { return (string)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        #endregion

        public ReadOnlyText()
        {
            InitializeComponent();
        }
    }
}
