using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// StoreMsAnnotationTagsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class StoreMsAnnotationTagsWin : Window
    {
        private MainWindow mainWindow;
        private ObservableCollection<PeakAreaBean> peakAreaCollection;
        private AlignmentResultBean alignmentResult;
        private List<MS1DecResult> ms1DecResults;
        private int peakID;
        private ExportspectraType exportMsType;
        private MatExportOption exportOption;

        public StoreMsAnnotationTagsWin(MainWindow mainWindow, ObservableCollection<PeakAreaBean> peakAreaCollection, int peakID)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.peakAreaCollection = peakAreaCollection;
            this.peakID = peakID;

            this.TextBox_ExportFolderPath.Text = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath;
            this.ComboBox_Type.ItemsSource = new string[] { "Profile", "Centroid", "Deconvoluted" };
            this.ComboBox_ExportOption.ItemsSource = new string[] { "Only the foucsed peak", "Mono isotopic ions", "All unknowns",
                "All peaks", "Identified peaks", "MS/MS acquired ions", "Mono isotopic & MS/MS acquired ions" };

            if (this.mainWindow.ProjectProperty.MethodType == MethodType.diMSMS) { this.ComboBox_Type.SelectedIndex = 2; this.exportMsType = ExportspectraType.deconvoluted; }
            else { this.ComboBox_Type.SelectedIndex = 1; this.exportMsType = ExportspectraType.centroid; }

            this.ComboBox_ExportOption.SelectedIndex = 0; this.exportOption = MatExportOption.OnlyFocusedPeak;
        }

        public StoreMsAnnotationTagsWin(MainWindow mainWindow, AlignmentResultBean alignmnetResult, int peakID)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.alignmentResult = alignmnetResult;
            this.peakID = peakID;

            this.TextBox_ExportFolderPath.Text = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath;
            this.ComboBox_Type.ItemsSource = new string[] { "Profile", "Centroid", "Deconvoluted" };
            this.ComboBox_Type.IsEnabled = false;

            this.ComboBox_ExportOption.ItemsSource = new string[] { "Only the foucsed peak", "Mono isotopic ions", "All unknowns",
                "All peaks", "Identified peaks", "MS/MS acquired ions", "Mono isotopic & MS/MS acquired ions" };

            this.ComboBox_ExportOption.SelectedIndex = 0; this.exportOption = MatExportOption.OnlyFocusedPeak;
        }

        public StoreMsAnnotationTagsWin(MainWindow mainWindow, List<MS1DecResult> ms1DecResults, int peakID)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.ms1DecResults = ms1DecResults;
            this.peakID = peakID;

            this.TextBox_ExportFolderPath.Text = mainWindow.ProjectProperty.MsAnnotationTagsFolderPath;
            this.ComboBox_Type.ItemsSource = new string[] { "Profile", "Centroid", "Deconvoluted" };
            this.ComboBox_Type.IsEnabled = false;

            this.ComboBox_ExportOption.ItemsSource = new string[] { "Only the foucsed peak", "Mono isotopic ions", "All unknowns", "All peaks", "Identified peaks" };

            this.ComboBox_ExportOption.SelectedIndex = 0; this.exportOption = MatExportOption.OnlyFocusedPeak;
        }

        private void Click_ExportFolderPathSelect(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose an export folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBox_ExportFolderPath.Text = fbd.SelectedPath;
            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (this.TextBox_ExportFolderPath.Text == string.Empty)
            {
                MessageBox.Show("Select an export folder path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            mainWindow.ProjectProperty.MsAnnotationTagsFolderPath = this.TextBox_ExportFolderPath.Text;

            Mouse.OverrideCursor = Cursors.Wait;

            if (this.peakAreaCollection != null && this.peakAreaCollection.Count > 0)
                DataExportLcUtility.MsAnnotationTagExport(this.mainWindow, this.peakAreaCollection, this.peakID, this.exportMsType, this.exportOption);
            else if (this.alignmentResult != null && this.alignmentResult.AlignmentPropertyBeanCollection != null && this.alignmentResult.AlignmentPropertyBeanCollection.Count > 0) {
                //for plant chemical annotaion project
                if (this.mainWindow.AnalysisParamForLC != null && this.mainWindow.AnalysisParamForLC.TrackingIsotopeLabels) {
                    ResultExportLcUtility.ExportIsotopeTrackingResultAsMatFormatFile(this.mainWindow.ProjectProperty.MsAnnotationTagsFolderPath,
                        this.mainWindow.ProjectProperty, this.mainWindow.AnalysisParamForLC, this.mainWindow.MspDB,
                        this.alignmentResult, this.mainWindow.AlignViewDecFS, this.mainWindow.AlignViewDecSeekPoints);
                }
                else {
                    DataExportLcUtility.MsAnnotationTagExport(this.mainWindow, this.alignmentResult.AlignmentPropertyBeanCollection, this.peakID, this.exportOption);
                }
                //DataExportLcUtility.MsAnnotationTagExport(this.mainWindow, this.alignmentResult.AlignmentPropertyBeanCollection, this.peakID, this.exportOption);
            }
            else if (this.ms1DecResults != null && this.ms1DecResults.Count != 0)
                DataExportGcUtility.MsfinderTagExport(this.mainWindow, this.ms1DecResults, this.peakID, this.exportOption);

            Mouse.OverrideCursor = null;

            this.Close();
        }

        private void ComboBox_MassSpecType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mainWindow.ProjectProperty.MethodType == MethodType.ddMSMS && ((ComboBox)sender).SelectedIndex == 2)
            {
                MessageBox.Show("In data depeondent mode, you cannot choose the deconvoluton output.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            this.exportMsType = (ExportspectraType)((ComboBox)sender).SelectedIndex;
        }

        private void ComboBox_ExportOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    this.exportOption = MatExportOption.OnlyFocusedPeak;
                    break;
                case 1:
                    this.exportOption = MatExportOption.UnknownPeaksWithoutIsotope;
                    break;
                case 2:
                    this.exportOption = MatExportOption.UnknownPeaks;
                    break;
                case 3:
                    this.exportOption = MatExportOption.AllPeaks;
                    break;
                case 4:
                    this.exportOption = MatExportOption.IdentifiedPeaks;
                    break;
                case 5:
                    this.exportOption = MatExportOption.MsmsPeaks;
                    break;
                case 6:
                    this.exportOption = MatExportOption.MonoisotopicAndMsmsPeaks;
                    break;
                default:
                    this.exportOption = MatExportOption.OnlyFocusedPeak;
                    break;
            }
        }
    }
}
