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
    /// AnalysisParameterSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AnalysisParameterSettingWindow : Window
    {
        MainWindowVM mainWindowVM;
        AnalysisParameterSettingVM analysisParameterSettingVM;

        public AnalysisParameterSettingWindow(MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindowVM = mainWindowVM;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ComboBox_ElementRatioCheck.ItemsSource = new string[] { "Common range (99.7%)", "Extended range (99.99%)", "No restriction" };
            this.ComboBox_ElementRatioCheck.SelectedIndex = (int)this.mainWindowVM.DataStorageBean.AnalysisParameter.CoverRange;

            this.DataContext = new AnalysisParameterSettingVM(this, mainWindowVM);
            this.analysisParameterSettingVM = (AnalysisParameterSettingVM)this.DataContext;
            if (this.analysisParameterSettingVM.MassToleranceType == MassToleranceType.Da)
                this.RadioButton_DaAsTolerance.IsChecked = true;
            else
                this.RadioButton_PpmAsTolerance.IsChecked = true;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ComboBox_ElementRatioCheck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.ElementRatioCheck = (CoverRange)((ComboBox)sender).SelectedIndex;
        }

        private void RadioButton_DaAsTolerance_Checked(object sender, RoutedEventArgs e)
        {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.MassToleranceType = MassToleranceType.Da;
        }

        private void RadioButton_PpmAsTolerance_Checked(object sender, RoutedEventArgs e)
        {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.MassToleranceType = MassToleranceType.Ppm;
        }

        private void Checkbox_AsRetentionIndex_Checked(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.RetentionType = RetentionType.RI;
        }

        private void Checkbox_AsRetentionIndex_Unchecked(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.RetentionType = RetentionType.RT;
        }

        private void Button_UserDefinedDb_Browse_Click(object sender, RoutedEventArgs e)
        {
            if (this.analysisParameterSettingVM == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "TEXT file(*.txt)|*.txt";
            ofd.Title = "Import your own structure database";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                this.analysisParameterSettingVM.UserDefinedDbFilePath = ofd.FileName;
            }
        }

        private void Button_UserDefinedSpectralDb_Browse_Click(object sender, RoutedEventArgs e)
        {
            if (this.analysisParameterSettingVM == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "MSP file(*.msp)|*.msp";
            ofd.Title = "Import your own MSP format file";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                this.analysisParameterSettingVM.UserDefinedSpectralDbFilePath = ofd.FileName;
            }
        }

        private void Button_RtSmilesDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt";
            ofd.Title = "Import your RT and SMILES dictionary";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                this.analysisParameterSettingVM.RtSmilesDictionaryFilepath = ofd.FileName;
            }
        }

        private void Button_RtSmilesDictionaryFilepath_Load_Click(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;
            if (this.analysisParameterSettingVM.RtSmilesDictionaryFilepath == null) return;
            if (this.analysisParameterSettingVM.RtSmilesDictionaryFilepath == string.Empty) return;

            var window = new RtPredictionSummaryWin();
            var mvvm = new RtPredictionVM(this.analysisParameterSettingVM);
            if (mvvm.Result == null) return;

            window.DataContext = mvvm;
            window.Show();
        }

        private void Button_AdductSet_Click(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;
            this.analysisParameterSettingVM.OpenSetAdductTypeWindow();
        }
        
        private void Button_RtInChiKeyDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt";
            ofd.Title = "Import your RT and InChIKey dictionary";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                this.analysisParameterSettingVM.RtInChIKeyDictionaryFilepath = ofd.FileName;
            }
        }

        private void Button_CcsAdductInChiKeyDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e) {
            if (this.analysisParameterSettingVM == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt";
            ofd.Title = "Import your CCS, Adduct and InChIKey dictionary";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                this.analysisParameterSettingVM.CcsAdductInChIKeyDictionaryFilepath = ofd.FileName;
            }
        }
    }
}
