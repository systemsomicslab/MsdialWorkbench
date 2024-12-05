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
    /// Interaction logic for BatchExportWin.xaml
    /// </summary>
    public partial class BatchExportWin : Window
    {
        private MainWindow mainWindow;
        private MainWindowVM mainWindowVM;
        private bool isSingleFileExport;

        public BatchExportWin(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.TextBox_Number.Text = "10";
            this.mainWindow = mainWindow;
            this.mainWindowVM = mainWindowVM;
            this.isSingleFileExport = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose an export folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBox_FolderPath.Text = fbd.SelectedPath;
            }
        }

        private void Button_Export(object sender, RoutedEventArgs e)
        {
            if (this.isSingleFileExport && 
                (this.TextBox_FolderPath.Text == null || this.TextBox_FolderPath.Text == string.Empty ||
                this.TextBox_Number.Text == null || this.TextBox_Number.Text == string.Empty))
            {
                MessageBox.Show("Choose a folder and add the number for exporting the candidates", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            int number = 10;
            if (int.TryParse(this.TextBox_Number.Text, out number)){}

            Mouse.OverrideCursor = Cursors.Wait;

            if (this.isSingleFileExport)
                ExportUtility.BatchExport(this.mainWindow, this.mainWindowVM, this.TextBox_FolderPath.Text, number);
            else
                ExportUtility.BatchExportForEachFile(this.mainWindow, this.mainWindowVM, this.TextBox_FolderPath.Text, number);

            Mouse.OverrideCursor = null;
            this.Close();
        }

        private void CheckBox_ExportToSingleFile_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            this.isSingleFileExport = true;
            this.TextBox_Number.IsEnabled = true;
        }

        private void CheckBox_ExportToSingleFile_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            this.isSingleFileExport = false;
            this.TextBox_Number.IsEnabled = false;
        }
    }
}
