using CompMs.App.Msdial.Model.DataObj;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Interaction logic for SmallClassBarChart.xaml
    /// </summary>
    public partial class SmallClassBarChart : UserControl
    {
        public SmallClassBarChart() {
            InitializeComponent();

            Unloaded += SmallClassBarChart_Unloaded;
        }

        private void SmallClassBarChart_Unloaded(object sender, RoutedEventArgs e) {
            (DataContext as IDisposable)?.Dispose();
            Unloaded -= SmallClassBarChart_Unloaded;
        }

        public static readonly DependencyProperty ClassBrushProperty =
            DependencyProperty.Register(
                nameof(ClassBrush), typeof(IBrushMapper<BarItem>), typeof(SmallClassBarChart),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<BarItem>(Brushes.Blue)));

        public IBrushMapper<BarItem> ClassBrush {
            get => (IBrushMapper<BarItem>)GetValue(ClassBrushProperty); 
            set => SetValue(ClassBrushProperty, value);
        }
    }
}
