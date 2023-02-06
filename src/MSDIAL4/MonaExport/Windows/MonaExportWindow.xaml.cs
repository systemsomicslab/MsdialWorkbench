using System.Windows;
using edu.ucdavis.fiehnlab.MonaExport.ViewModels;
using System.Diagnostics;

namespace edu.ucdavis.fiehnlab.MonaExport.Windows {
    /// <summary>
    /// Interaction logic for MonaExportWindow.xaml
    /// </summary>

    public partial class MonaExportWindow : Window {

		public MonaExportWindow(MonaExportWindowVM viewModel) {
            InitializeComponent();

            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => this.Close();
		}

/*		private void NewMDName_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			Debug.WriteLine("sender : " + sender.ToString());
			Debug.WriteLine("args : " + e.Source.ToString());
			Debug.WriteLine("Combo clicked");
            MessageBox.Show("click");
		}*/
	}
}
