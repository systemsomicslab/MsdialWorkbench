using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for IsotopeTrackSettingView.xaml
    /// </summary>
    public partial class IsotopeTrackSettingView : UserControl
    {
        public IsotopeTrackSettingView() {
            InitializeComponent();
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) {
            var fbd = new OpenFileDialog
            {
                Title = "Import target formulas library",
                Filter = "Text file(*.txt)|*.txt;",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (fbd.ShowDialog() == true) {
                TextBox_TargetFormulaLibraryFilePath.Text = fbd.FileName;
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = CheckBox_UseTargetLibrary.IsChecked ?? false;
        }
    }
}
