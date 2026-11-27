using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        // A key typed into a selected cell starts the edit, but a template column does not pass that key on to the editing TextBox.
        // Put it there, so the cell takes what was typed, as DataGridTextColumn does.
        private void EicDataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e) {
            if (e.EditingEventArgs is not TextCompositionEventArgs { Text.Length: > 0 } input) {
                return;
            }
            e.EditingElement.ApplyTemplate();
            if (FindDescendant<TextBox>(e.EditingElement) is TextBox textBox) {
                StartWith(textBox, input.Text);
            }
            else {
                e.EditingElement.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => {
                    if (FindDescendant<TextBox>(e.EditingElement) is TextBox loaded) {
                        StartWith(loaded, input.Text);
                    }
                });
            }
        }

        private static void StartWith(TextBox textBox, string text) {
            textBox.Text = text;
            textBox.CaretIndex = text.Length;
            textBox.Focus();
        }

        private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) {
                    return t;
                }
                if (FindDescendant<T>(child) is T found) {
                    return found;
                }
            }
            return null;
        }
    }
}
