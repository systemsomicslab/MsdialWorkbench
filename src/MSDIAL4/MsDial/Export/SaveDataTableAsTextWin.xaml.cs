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
using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Algorithm;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// SaveDataTableAsTextWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveDataTableAsTextWin : Window
    {
        private object target;
        //private PrincipalComponentAnalysisResult pcaBean;
        private PeakAreaBean peakAreaBean;
        private MS1DecResult ms1DecResult;
        private MultivariateAnalysisResult maResult;

        public SaveDataTableAsTextWin()
        {
            InitializeComponent();
        }

        public SaveDataTableAsTextWin(object target)
        {
            InitializeComponent();
            this.target = target;
        }

        public SaveDataTableAsTextWin(MultivariateAnalysisResult maResult) {
            InitializeComponent();
            this.maResult = maResult;
        }

        //public SaveDataTableAsTextWin(PrincipalComponentAnalysisResult pcaBean)
        //{
        //    InitializeComponent();
        //    this.pcaBean = pcaBean;
        //}

        public SaveDataTableAsTextWin(object target, PeakAreaBean peakAreaBean)
        {
            InitializeComponent();
            this.target = target;
            this.peakAreaBean = peakAreaBean;
        }

        public SaveDataTableAsTextWin(object target, MS1DecResult ms1DecResult)
        {
            InitializeComponent();
            this.target = target;
            this.ms1DecResult = ms1DecResult;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (this.TextBox_ExportFilePath.Text == string.Empty)
            {
                MessageBox.Show("Select an export file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string saveDataTableFormat = System.IO.Path.GetExtension(this.TextBox_ExportFilePath.Text);
            string saveDataTableFilePath = this.TextBox_ExportFilePath.Text;

            //if (this.pcaBean != null)
            //    TableExportUtility.SavePcaTableAsTextFormat(saveDataTableFilePath, this.pcaBean);
            //else 
            if (this.maResult != null)
                TableExportUtility.SaveMultivariableResultTableAsTextFormat(saveDataTableFilePath, this.maResult);
            else if (this.peakAreaBean != null)
                TableExportUtility.SaveChromatogramTableAsText(saveDataTableFilePath, this.target, this.peakAreaBean);
            else if (this.ms1DecResult != null)
                TableExportUtility.SaveChromatogramTableAsText(saveDataTableFilePath, this.target, this.ms1DecResult);
            else
                TableExportUtility.SaveChromatogramTableAsText(saveDataTableFilePath, this.target, this.peakAreaBean);

            this.Close();
        }

        private void Click_ExportFilePathSelect(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "*.txt";
            sfd.Filter = "Test format(*.txt)|*.txt;";
            sfd.Title = "Save file dialog";
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog(this) == true)
            {
                this.TextBox_ExportFilePath.Text = sfd.FileName;
            }
        }
    }
}
