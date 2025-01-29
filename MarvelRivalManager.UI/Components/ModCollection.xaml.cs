using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Linq;

using Windows.ApplicationModel.DataTransfer;

using Collection = MarvelRivalManager.UI.ViewModels.ModCollection;
using Mod = MarvelRivalManager.UI.ViewModels.ModViewModel;

namespace MarvelRivalManager.UI.Components
{
    public record class SingleModEventArg(Collection Collection, Mod Mod);
    public record class DropModEventArg(int[] Indexes, int Position);

    /// <summary>
    ///     Component dedicated to display a collection of mods.
    /// </summary>
    public sealed partial class ModCollection : UserControl
    {
        #region Handlers

        public event EventHandler<SingleModEventArg>? OnMove;
        public event EventHandler<SingleModEventArg>? OnEdit;
        public event EventHandler<SingleModEventArg>? OnEvaluate;
        public event EventHandler<SingleModEventArg>? OnDelete;

        public event EventHandler<DropModEventArg>? OnDropItems;
        public event EventHandler<RoutedEventArgs>? OnCollectionLoaded;

        #endregion

        #region Dependencies

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ModCollection), new PropertyMetadata(string.Empty));

        #endregion

        #region Properties - Bindable

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        #endregion

        #region Properties - Private

        private Collection Original = [];

        #endregion

        public ModCollection()
        {
            InitializeComponent();
        }

        #region Public Methods

        public void Load(Mod[] collection)
        {
            Load(new Collection(collection));
        }

        public void Load(Collection collection)
        {
            Original = collection;
            ModCollectionList.ItemsSource = collection;
        }

        public void Filter(Func<Mod, bool> filter)
        {
            ModCollectionList.ItemsSource = Original.Where(filter);
        }

        public Mod[] GetSelected()
        {
            return ModCollectionList.SelectedItems.Cast<Mod>().ToArray();
        }

        public ListView View()
        {
            return ModCollectionList;
        }

        #endregion

        private void ModCollectionList_Loaded(object sender, RoutedEventArgs e)
        {
            if (OnCollectionLoaded is not null)
                OnCollectionLoaded(this, e);
        }

        private void CollectionControl_Loaded(object sender, RoutedEventArgs e)
        {
            ModCollectionList.CanReorderItems = OnDropItems is not null;
            ModCollectionList.CanDrag = OnDropItems is not null;
            ModCollectionList.AllowDrop = OnDropItems is not null;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Grid content)
                return;

            foreach (var item in new Dictionary<string, Visibility>
            {
                ["ContextMenu_Move"] = OnMove is not null ? Visibility.Visible : Visibility.Collapsed,
                ["ContextMenu_Edit"] = OnEdit is not null ? Visibility.Visible : Visibility.Collapsed,
                ["ContextMenu_Evaluate"] = OnEvaluate is not null ? Visibility.Visible : Visibility.Collapsed,
                ["ContextMenu_Delete_Separator"] = OnDelete is not null ? Visibility.Visible : Visibility.Collapsed,
                ["ContextMenu_Delete"] = OnDelete is not null ? Visibility.Visible : Visibility.Collapsed
            })
            {
                var element = content.FindName(item.Key);
                if (element is not FrameworkElement ui)
                    return;

                ui.Visibility = item.Value;
            }
        }

        #region Context Menu

        private void OnMoveHandler(object sender, RoutedEventArgs _)
        {
            if (sender is not FrameworkElement element || element.DataContext is not Mod mod)
                return;

            if (OnMove is not null)
            {
                OnMove(this, new SingleModEventArg(Original, mod));
            }
        }

        private void OnEditHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not Mod mod)
                return;

            if (OnEdit is not null)
            {
                OnEdit(this, new SingleModEventArg(Original, mod));
            }
        }

        private void OnEvaluateHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not Mod mod)
                return;

            if (OnEvaluate is not null)
            {
                OnEvaluate(this, new SingleModEventArg(Original, mod));
            }
        }

        private void OnDeleteHandler(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.DataContext is not Mod mod)
                return;

            if (OnDelete is not null)
            {
                OnDelete(this, new SingleModEventArg(Original, mod));
            }
        }

        #endregion

        #region Drag and Drop

        private void ModCollectionList_DragItemsStarting(object _, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count == 0)
            {
                e.Data.RequestedOperation = DataPackageOperation.None;
                e.Cancel = true;
                return;
            }

            var package = string.Join(',', (e.Items ?? [])
                .Select(item => item is null || item is not Mod mod || mod is null ? null : mod)
                .Where(item => item is not null)
                .Select(item => item!.Index));

            e.Data.SetText(package);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }

        private void ModCollectionList_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Move;
        }

        private async void ModCollectionList_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ListView target || !e.DataView.Contains(StandardDataFormats.Text))
                return;

            var operation = e.GetDeferral();
            var indexes = (await e.DataView.GetTextAsync() ?? string.Empty)
                .Split(',')
                .Select(raw =>
                {
                    if (int.TryParse(raw, out var index))
                        return index;

                    return -1;
                })
                .Where(index => index >= 0)
                .ToArray()
                ;

            if (OnDropItems is not null)
            {
                // Calculate the insertion index
                var position = 0;
                if (target.Items.Count != 0 && target.ContainerFromIndex(0) is ListViewItem pivot)
                {
                    var height = pivot.ActualHeight + pivot.Margin.Top + pivot.Margin.Bottom;
                    position = Math.Min(target.Items.Count - 1, (int)(e.GetPosition(target.ItemsPanelRoot).Y / height));
                }

                e.AcceptedOperation = DataPackageOperation.Move;
                OnDropItems(this, new DropModEventArg(indexes, position));
            }

            operation.Complete();
        }

        #endregion
    }
}
