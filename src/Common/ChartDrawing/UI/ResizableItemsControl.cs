using System;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.Graphics.UI
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI;assembly=CompMs.Graphics.UI"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ResizableItemsControl/>
    ///
    /// </summary>
    public class ResizableItemsControl : ItemsControl
    {
        static ResizableItemsControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizableItemsControl), new FrameworkPropertyMetadata(typeof(ResizableItemsControl)));
        }

        private Grid? _grid;

        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(
                nameof(Orientation),
                typeof(Orientation),
                typeof(ResizableItemsControl),
                new FrameworkPropertyMetadata(Orientation.Vertical));

        public Orientation Orientation {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (oldValue is INotifyCollectionChanged oldCollection) {
                oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
            }

            base.OnItemsSourceChanged(oldValue, newValue);

            if (newValue is INotifyCollectionChanged newCollection) {
                newCollection.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            SetCurrentItems();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        InsertItem(e.NewItems[i], (e.NewStartingIndex + i) * 2);
                    }
                    ResetIndex(e.NewStartingIndex * 2);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems) {
                        RemoveItemAt(e.OldStartingIndex * 2);
                    }
                    ResetIndex(Math.Max(e.OldStartingIndex * 2 - 1, 0));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems) {
                        RemoveItemAt(e.OldStartingIndex * 2);
                    }
                    for (int i = 0; i < e.NewItems.Count; i++) {
                        InsertItem(e.NewItems[i], (e.NewStartingIndex + i) * 2);
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    ResetIndex(e.NewStartingIndex * 2);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _grid?.Children.Clear();
                    SetCurrentItems();
                    break;
            }
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            _grid = GetTemplateChild("PART_Grid") as Grid;

            SetCurrentItems();
        }

        private void SetCurrentItems() {
            if (_grid is not null) {
                foreach (var item in Items) {
                    InsertItem(item, _grid.Children.Count);
                }
            }
        }

        private void InsertItem(object item, int index) {
            var visualItem = ItemTemplate?.LoadContent() as FrameworkElement;
            if (visualItem is not null) {
                visualItem.DataContext = item;
            }
            else {
                var container = GetContainerForItemOverride();
                switch (container) {
                    case ContentPresenter presenter:
                        presenter.Content = item;
                        visualItem = presenter;
                        break;
                    case ContentControl content:
                        content.Content = item;
                        visualItem = content;
                        break;
                    case null:
                        return;
                }
            }

            if (visualItem is not null && _grid is not null) {
                if (index > _grid.Children.Count) {
                    index = _grid.Children.Count;
                }
                switch (Orientation) {
                    case Orientation.Horizontal:
                        if (index % 2 == 0) {
                            if (visualItem is ContentPresenter presenter && presenter.Content is IContainerNode node) {
                                ColumnDefinition columndef = new() { DataContext = node, Width = node.Width, };
                                Binding binding = new(nameof(IContainerNode.Width))
                                {
                                    Mode = BindingMode.TwoWay,
                                };
                                columndef.SetBinding(ColumnDefinition.WidthProperty, binding);
                                _grid.ColumnDefinitions.Insert(index, columndef);
                            }
                            else {
                                _grid.ColumnDefinitions.Insert(index, new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star), });
                            }
                            Grid.SetColumn(visualItem, index);
                            _grid.Children.Insert(index, visualItem);
                            if (_grid.ColumnDefinitions.Count >= 2) {
                                _grid.ColumnDefinitions.Insert(index + 1, new ColumnDefinition { Width = GridLength.Auto, });
                                var splitter = new GridSplitter
                                {
                                    ResizeDirection = GridResizeDirection.Columns,
                                    Width = 3d,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Background = Brushes.LightGray,
                                };
                                Grid.SetColumn(splitter, index + 1);
                                _grid.Children.Insert(index + 1, splitter);
                            }
                        }
                        else {
                            _grid.ColumnDefinitions.Insert(index, new ColumnDefinition { Width = GridLength.Auto, });
                            var splitter = new GridSplitter
                            {
                                ResizeDirection = GridResizeDirection.Columns,
                                Width = 3d,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Stretch,
                                Background = Brushes.LightGray,
                            };
                            Grid.SetColumn(splitter, index);
                            _grid.Children.Insert(index, splitter);
                            if (visualItem is ContentPresenter presenter && presenter.Content is IContainerNode node) {
                                ColumnDefinition columndef = new() { DataContext = node, Width = node.Width, };
                                Binding binding = new(nameof(IContainerNode.Width))
                                {
                                    Mode = BindingMode.TwoWay,
                                };
                                columndef.SetBinding(ColumnDefinition.WidthProperty, binding);
                                _grid.ColumnDefinitions.Insert(index + 1, columndef);
                            }
                            else {
                                _grid.ColumnDefinitions.Insert(index + 1, new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star), });
                            }
                            Grid.SetColumn(visualItem, index + 1);
                            _grid.Children.Insert(index + 1, visualItem);
                        }
                        break;
                    case Orientation.Vertical:
                        if (index % 2 == 0) {
                            if (visualItem is ContentPresenter presenter && presenter.Content is IContainerNode node) {
                                RowDefinition rowdef = new() { DataContext = node, Height = node.Height, };
                                Binding binding = new(nameof(IContainerNode.Height))
                                {
                                    Mode = BindingMode.TwoWay,
                                };
                                rowdef.SetBinding(RowDefinition.HeightProperty, binding);
                                _grid.RowDefinitions.Insert(index, rowdef);
                            }
                            else {
                                _grid.RowDefinitions.Insert(index, new RowDefinition { Height = new GridLength(1d, GridUnitType.Star), });
                            }
                            Grid.SetRow(visualItem, index);
                            _grid.Children.Insert(index, visualItem);
                            if (_grid.RowDefinitions.Count >= 2) {
                                _grid.RowDefinitions.Insert(index + 1, new RowDefinition { Height = GridLength.Auto, });
                                var splitter = new GridSplitter
                                {
                                    ResizeDirection = GridResizeDirection.Rows,
                                    Height = 3d,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Background = Brushes.LightGray,
                                };
                                Grid.SetRow(splitter, index + 1);
                                _grid.Children.Insert(index + 1, splitter);
                            }
                        }
                        else {
                            _grid.RowDefinitions.Insert(index, new RowDefinition { Height = GridLength.Auto, });
                            var splitter = new GridSplitter
                            {
                                ResizeDirection = GridResizeDirection.Rows,
                                Height = 3d,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Stretch,
                                Background = Brushes.LightGray,
                            };
                            Grid.SetRow(splitter, index);
                            _grid.Children.Insert(index, splitter);
                            if (visualItem is ContentPresenter presenter && presenter.Content is IContainerNode node) {
                                RowDefinition rowdef = new() { DataContext = node, Height = node.Height, };
                                Binding binding = new(nameof(IContainerNode.Height))
                                {
                                    Mode = BindingMode.TwoWay,
                                };
                                rowdef.SetBinding(RowDefinition.HeightProperty, binding);
                                _grid.RowDefinitions.Insert(index + 1, rowdef);
                            }
                            else {
                                _grid.RowDefinitions.Insert(index + 1, new RowDefinition { Height = new GridLength(1d, GridUnitType.Star), });
                            }
                            Grid.SetRow(visualItem, index + 1);
                            _grid.Children.Insert(index + 1, visualItem);
                        }
                        break;
                }
            }
        }

        private void RemoveItemAt(int index) {
            if ((uint)index < _grid.Children.Count && _grid is not null) {
                if (Orientation == Orientation.Horizontal) {
                    RemoveColumnDefinition(index);
                }
                else {
                    RemoveRowDefinition(index);
                }
            }
        }

        private void RemoveColumnDefinition(int index) {
            if (_grid.ColumnDefinitions.Count >= 1) {
                _grid.ColumnDefinitions.RemoveAt(index);
                _grid.Children.RemoveAt(index);
                if (_grid.ColumnDefinitions.Count >= 1) {
                    if (index > 0) {
                        _grid.ColumnDefinitions.RemoveAt(index - 1);
                        _grid.Children.RemoveAt(index - 1);
                    }
                    else {
                        _grid.ColumnDefinitions.RemoveAt(index);
                        _grid.Children.RemoveAt(index);
                    }
                }
            }
        }

        private void RemoveRowDefinition(int index) {
            if (_grid.RowDefinitions.Count >= 1) {
                _grid.RowDefinitions.RemoveAt(index);
                _grid.Children.RemoveAt(index);
                if (_grid.RowDefinitions.Count >= 1) {
                    if (index > 0) {
                        _grid.RowDefinitions.RemoveAt(index - 1);
                        _grid.Children.RemoveAt(index - 1);
                    }
                    else {
                        _grid.RowDefinitions.RemoveAt(index);
                        _grid.Children.RemoveAt(index);
                    }
                }
            }
        }

        private void ResetIndex(int index) {
            for (int i = index; i < _grid.Children.Count; i++) {
                switch (Orientation) {
                    case Orientation.Horizontal:
                        Grid.SetColumn(_grid.Children[i], i);
                        break;
                    case Orientation.Vertical:
                        Grid.SetRow(_grid.Children[i], i);
                        break;
                }
            }
        }
    }
}
