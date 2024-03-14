using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            _grid = GetTemplateChild("PART_Grid") as Grid;

            foreach (var item in Items) {
                AddItem(item);
            }
        }

        private void AddItem(object item) {
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
            
            if (visualItem is not null) {

                if (_grid is not null) {
                    switch (Orientation) {
                        case Orientation.Horizontal:
                            if (_grid.ColumnDefinitions.Count >= 1) {
                                _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, });
                                var splitter = new GridSplitter
                                {
                                    ResizeDirection = GridResizeDirection.Columns,
                                    Width = 3d,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Background = Brushes.LightGray,
                                };
                                Grid.SetColumn(splitter, _grid.ColumnDefinitions.Count - 1);
                                _grid.Children.Add(splitter);
                            }
                            _grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d, GridUnitType.Star), });
                            Grid.SetColumn(visualItem, _grid.ColumnDefinitions.Count - 1);
                            _grid.Children.Add(visualItem);
                            break;
                        case Orientation.Vertical:
                            if (_grid.RowDefinitions.Count >= 1) {
                                _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto, });
                                var splitter = new GridSplitter
                                {
                                    ResizeDirection = GridResizeDirection.Rows,
                                    Height = 3d,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Background = Brushes.LightGray,
                                };
                                Grid.SetRow(splitter, _grid.RowDefinitions.Count - 1);
                                _grid.Children.Add(splitter);
                            }
                            _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d, GridUnitType.Star), });
                            Grid.SetRow(visualItem, _grid.RowDefinitions.Count - 1);
                            _grid.Children.Add(visualItem);
                            break;
                    }
                }
            }
        }
    }
}
