using CompMs.App.Msdial.ViewModel.Statistics;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CompMs.App.Msdial.View.Statistics
{
    /// <summary>
    /// Interaction logic for InternalStandardSetView.xaml
    /// </summary>
    public partial class InternalStandardSetView : UserControl
    {
        public InternalStandardSetView() {
            InitializeComponent();
        }

        private void FinishClose(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                window.DialogResult = true;
            }
            window.Close();
        }

        private void CancelClose(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                window.DialogResult = false;
            }
            window.Close();
        }
    }

    internal sealed class SelectedAndBellowRowsConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var datagrid = (DataGrid)value;
            if (datagrid is null) {
                return default;
            }
            var current = (NormalizationSpotPropertyViewModel)datagrid.CurrentItem;
            var currentIndex = datagrid.Items.IndexOf(current);
            var targets = datagrid.Items.Cast<NormalizationSpotPropertyViewModel>().Skip(currentIndex);
            return (current, targets);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}
