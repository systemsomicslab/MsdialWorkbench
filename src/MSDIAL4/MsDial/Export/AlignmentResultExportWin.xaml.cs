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
    /// AlignmentResultExportWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AlignmentResultExportWin : Window
    {
        private MainWindow mainWindow;

        public AlignmentResultExportWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //this.ComboBox_ExportFormat.ItemsSource = Enum.GetValues(typeof(ExportSpectraFileFormat));
            this.ComboBox_ExportFormat.ItemsSource = new string[] { "mgf", "msp" };
            //this.ComboBox_MassSpecType.ItemsSource = Enum.GetValues(typeof(ExportspectraType));

          //  this.DataContext = new AlignmentResultExportVM(this.mainWindow, this) { RawDatamatrix = true };
            this.DataContext = new AlignmentResultExportVM(this.mainWindow, this);

            this.ComboBox_ExportFormat.SelectedIndex = 1;
            //if (this.mainWindow.ProjectProperty.MethodType == MethodType.diMSMS)
            //    this.ComboBox_MassSpecType.SelectedIndex = 2;
            //else
            //    this.ComboBox_MassSpecType.SelectedIndex = 1;

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI)
            {
                //this.ComboBox_MassSpecType.SelectedIndex = 2;
                this.Checbox_MsmsIncludedMatrix.IsEnabled = false;
                this.Checbox_FilteringForIsotopeTrackResult.IsEnabled = false;
                this.Checbox_GnpsExport.IsEnabled = false;
            }
        }

        private void Button_ExportFolderPath_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ((AlignmentResultExportVM)this.DataContext).ExportFolderPath = fbd.SelectedPath;
                this.TextBox_ExportFolderPath.Text = fbd.SelectedPath;
            }
        }

        private void ComboBox_ExportFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AlignmentResultExportVM)this.DataContext).ExportSpectraFileFormat = (ExportSpectraFileFormat)((ComboBox)sender).SelectedIndex;
        }

        //private void ComboBox_MassSpecType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
        //        if (this.mainWindow.ProjectProperty.MethodType == MethodType.ddMSMS && ((ComboBox)sender).SelectedIndex == 2) {
        //            MessageBox.Show("In data dependent  mode, you cannot choose the deconvolution output.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
        //            return;
        //        }
        //        if (this.mainWindow.ProjectProperty.MethodType == MethodType.diMSMS && ((ComboBox)sender).SelectedIndex == 1) {
        //            MessageBox.Show("In data indepeondent mode, you cannot choose the centroid output.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
        //            return;
        //        }
        //    }
        //    ((AlignmentResultExportVM)this.DataContext).ExportSpectraType = (ExportspectraType)((ComboBox)sender).SelectedIndex;
        //}

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            this.Close();
        }

        //private void Checbox_RawDatamatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RawDatamatrix = true;
        //}

        //private void Checbox_RawDatamatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RawDatamatrix = false;
        //}

        //private void Checbox_NormalizedDatamatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).NormalizedDatamatrix = true;

        //}

        //private void Checbox_NormalizedDatamatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).NormalizedDatamatrix = false;

        //}

        //private void Checbox_RepresentativeSpectra_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RepresentativeSpectra = true;

        //}

        //private void Checbox_RepresentativeSpectra_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RepresentativeSpectra = false;
        //}

        //private void Checbox_SampleAxisDeconvolution_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).SampleAxisDeconvolution = true;
        //}

        //private void Checbox_SampleAxisDeconvolution_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).SampleAxisDeconvolution = false;
        //}

        private void ComboBox_AlignmentFileName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AlignmentResultExportVM)this.DataContext).SelectedAlignmentFileID = ((ComboBox)sender).SelectedIndex;
        }

        //private void Checbox_PeakIDmatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).PeakIdMatrix = true;
        //}

        //private void Checbox_PeakIDmatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).PeakIdMatrix = false;
        //}

        //private void Checbox_RetentionTimematrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RetentionTimeMatrix = true;
        //}

        //private void Checbox_RetentionTimematrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).RetentionTimeMatrix = false;
        //}

        //private void Checbox_MassMatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MzMatrix = true;
        //}

        //private void Checbox_MassMatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MzMatrix = false;
        //}

        //private void Checbox_MsmsIncluded_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MsmsIncludedMatrix = true;
        //}

        //private void Checbox_MsmsIncluded_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MsmsIncludedMatrix = false;
        //}

        //private void Checbox_DeconvolutedPeakAreaMatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).DeconvolutedPeakAreaDataMatrix = true;
        //}

        //private void Checbox_DeconvolutedPeakAreaMatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).DeconvolutedPeakAreaDataMatrix = false;
        //}

        //private void Checbox_UniqueMs_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).UniqueMs = true;
        //}

        //private void Checbox_UniqueMs_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).UniqueMs = false;
        //}

        //private void Checbox_PeakAreaMatrix_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).PeakareaMatrix = true;
        //}

        //private void Checbox_PeakAreaMatrix_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).PeakareaMatrix = false;
        //}

        //private void Checbox_FilteringForIsotopeTrackResult_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsFilteringOptionForIsotopeLabeledTracking = true;
        //}

        //private void Checbox_FilteringForIsotopeTrackResult_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsFilteringOptionForIsotopeLabeledTracking = false;
        //}

        private void ComboBox_AnalysisFileName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AlignmentResultExportVM)this.DataContext).SelectedAnalysisFileID = ((ComboBox)sender).SelectedIndex;
        }

        //private void Checbox_Parameter_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).Parameter = true;
        //}

        //private void Checbox_Parameter_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).Parameter = false;
        //}

        //private void Checbox_GnpsExport_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).GnpsExport = false;
        //}

        //private void Checbox_GnpsExport_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).GnpsExport = true;
        //}

        //private void Checbox_MolecularNetworkingEdges_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MolecularNetworkingExport = true;
        //}

        //private void Checbox_MolecularNetworkingEdges_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).MolecularNetworkingExport = false;
        //}

        //private void Checbox_FilteringByBlank_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).BlankFilter = false;
        //}

        //private void Checbox_FilteringByBlank_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).BlankFilter = true;
        //}

        //private void Checbox_ReplaceZeroToHalfValueOverSamples_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = true;
        //}

        //private void Checbox_ReplaceZeroToHalfValueOverSamples_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = false;
        //}

        //private void Checbox_SnExport_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).SnMatrixExport = false;
        //}

        //private void Checbox_SnExport_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).SnMatrixExport = true;
        //}

        //private void Checbox_ExportAsMzTabM_Checked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsExportAsMzTabM = true;
        //}

        //private void Checbox_ExportAsMzTabM_Unchecked(object sender, RoutedEventArgs e) {
        //    if (this.DataContext == null) return;
        //    ((AlignmentResultExportVM)this.DataContext).IsExportAsMzTabM = false;
        //}
    }
}
