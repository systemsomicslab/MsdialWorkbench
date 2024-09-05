using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for CreateSpectrumFileWindow.xaml
    /// </summary>
    public partial class CreateSpectrumFileWindow : Window
    {
        MainWindowVM mainWindowVM;

        public CreateSpectrumFileWindow(MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindowVM = mainWindowVM;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_FolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBox_FolderPath.Text = fbd.SelectedPath;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new CreateSpectrumFileVM(this.mainWindowVM, this);
        }
    }
}
