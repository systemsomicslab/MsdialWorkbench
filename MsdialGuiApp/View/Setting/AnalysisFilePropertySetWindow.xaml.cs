using CompMs.App.Msdial.ViewModel.Setting;
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
using System.Windows.Shapes;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for AnalysisFilePropertySetWin.xaml
    /// </summary>
    public partial class AnalysisFilePropertySetWindow : Window
    {
        public AnalysisFilePropertySetWindow() {
            InitializeComponent();
        }

        private void dg_DragOver(object sender, DragEventArgs e) {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private void dg_Drop(object sender, DragEventArgs e) {
            var files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            var vm = (AnalysisFilePropertySetViewModel)this.DataContext;
            vm.Drop(files);
        }
    }
}
