using CompMs.App.Msdial.ViewModel.Chart;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.Chart
{
    /// <summary>
    /// Interaction logic for CheckChromatogramsView.xaml
    /// </summary>
    public partial class CheckChromatogramsView : UserControl
    {
        public CheckChromatogramsView() {
            InitializeComponent();
        }

        // Text with tabs or line breaks (e.g. cells copied from Excel) is pasted as rows of the EIC list.
        private void EicQueryTextBox_Pasting(object sender, DataObjectPastingEventArgs e) {
            if (sender is not TextBox { DataContext: EicQueryViewModel row } textBox
                || !int.TryParse(textBox.Tag as string, out var column)
                || FindAncestor<ItemsControl>(textBox)?.DataContext is not EicSettingViewModel setting
                || !e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true)
                || e.SourceDataObject.GetData(DataFormats.UnicodeText, true) is not string text
                || text.IndexOfAny(['\t', '\r', '\n']) < 0) {
                return;
            }
            e.CancelCommand();
            var failed = setting.Paste(row, column, text);
            if (failed.Count > 0) {
                MessageBox.Show(Window.GetWindow(this),
                    $"These lines could not be read and were skipped: {string.Join(", ", failed)}.{System.Environment.NewLine}Exact mass and mass tolerance must be positive numbers.",
                    "Paste", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Enter moves to the same column of the next row, like a spreadsheet.
        private void EicQueryTextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && sender is TextBox textBox) {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                e.Handled = true;
            }
        }

        private static T? FindAncestor<T>(DependencyObject? d) where T : DependencyObject {
            while (d is not null) {
                d = VisualTreeHelper.GetParent(d);
                if (d is T t) {
                    return t;
                }
            }
            return null;
        }
    }
}
