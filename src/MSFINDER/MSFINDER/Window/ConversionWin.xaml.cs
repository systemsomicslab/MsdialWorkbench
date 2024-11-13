using Microsoft.Win32;
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
    /// Interaction logic for ConversionWin.xaml
    /// </summary>
    public partial class ConversionWin : Window
    {
        private string targetFormat;

        public ConversionWin(string targetFormat) {
            InitializeComponent();
            this.targetFormat = targetFormat;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Convert_Click(object sender, RoutedEventArgs e) {
            var filepath = this.TextBox_FilePath.Text;
            var folderpath = this.TextBox_FolderPath.Text;

            if (filepath == null || filepath == string.Empty || folderpath == null || folderpath == string.Empty) {
                MessageBox.Show("Please set the file path and export folder", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!System.IO.File.Exists(filepath)) {
                MessageBox.Show("No file at the selected file path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!System.IO.Directory.Exists(folderpath)) {
                MessageBox.Show("No folder at the selected folder path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.targetFormat == "mgf") {
                MgfParser.ConvertMgfToSeparatedMSPs(filepath, folderpath);
            }
            else {
                MspFileParcer.ConvertMspToSeparatedMSPs(filepath, folderpath);
            }

            this.Close();
        }

        private void Click_BrowseFilePath(object sender, RoutedEventArgs e) {

            var ofd = new OpenFileDialog();
            if (this.targetFormat == "mgf") {
                ofd.Filter = "MGF file(*.mgf)|*.mgf";
                ofd.Title = "Import an MGF file";
            }
            else {
                ofd.Filter = "MSP file(*.msp)|*.msp";
                ofd.Title = "Import an MSP file";
            }

            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                this.TextBox_FilePath.Text = ofd.FileName;
            }
        }

        private void Click_BrowseFolderPath(object sender, RoutedEventArgs e) {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose an export folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.TextBox_FolderPath.Text = fbd.SelectedPath;
            }
        }
    }
}
