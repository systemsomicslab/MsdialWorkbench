using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AlignmentResultExportVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private string exportFolderPath;
        private Window window;

        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private int selectedAnalysisFileID;
        private int selectedAlignmentFileID;

        private bool rawDatamatrix;
        private bool normalizedDatamatrix;
        private bool representativeSpectra;
        private bool sampleAxisDeconvolution;
        private bool peakIdMatrix;
        private bool retentionTimeMatrix;
        private bool mzMatrix;
        private bool msmsIncludedMatrix;
        private bool deconvolutedPeakAreaDataMatrix;
        private bool uniqueMs;
        private bool peakareaMatrix;
        private bool isFilteringOptionForIsotopeLabeledTracking;
        private bool parameter;
        private bool gnpsExport;
        private bool molecularNetworkingExport;
        private bool blankFilter;
        private bool isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
        private bool snMatrixExport;
        private bool isExportAsMzTabM;

        private float massTolerance;
        private ExportSpectraFileFormat exportSpectraFileFormat;
        private ExportspectraType exportSpectraType;

        public AlignmentResultExportVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.alignmentFiles = mainWindow.AlignmentFiles;
            this.analysisFiles = mainWindow.AnalysisFiles;
            this.window = window;

            initialization();
        }

        private void initialization() {
            this.selectedAlignmentFileID = 0;

            var alignmentResultID = this.mainWindow.FocusedAlignmentFileID;
            var alignmentResults = this.alignmentFiles;

            if (alignmentResults == null || alignmentResults.Count == 0) return;
            if (alignmentResultID < 0) return;
            if (alignmentResultID > alignmentResults.Count - 1) return;

            this.selectedAlignmentFileID = alignmentResultID;

            this.selectedAnalysisFileID = 0;

            if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
                var refAnalysisFileID = this.mainWindow.AnalysisParamForLC.NonLabeledReferenceID;
                if (refAnalysisFileID < 0 && refAnalysisFileID < this.analysisFiles.Count)
                    this.selectedAnalysisFileID = refAnalysisFileID;
            }

            var projectProp = this.mainWindow.ProjectProperty;
            ExportFolderPath = projectProp.ExportFolderPath;
            RawDatamatrix = projectProp.RawDatamatrix;
            NormalizedDatamatrix = projectProp.NormalizedDatamatrix;
            RepresentativeSpectra = projectProp.RepresentativeSpectra;
            SampleAxisDeconvolution = projectProp.SampleAxisDeconvolution;
            PeakIdMatrix = projectProp.PeakIdMatrix;
            RetentionTimeMatrix = projectProp.RetentionTimeMatrix;
            MzMatrix = projectProp.MzMatrix;
            MsmsIncludedMatrix = projectProp.MsmsIncludedMatrix;
            DeconvolutedPeakAreaDataMatrix = projectProp.DeconvolutedPeakAreaDataMatrix;
            UniqueMs = projectProp.UniqueMs;
            PeakareaMatrix = projectProp.PeakareaMatrix;
            IsFilteringOptionForIsotopeLabeledTracking = projectProp.IsFilteringOptionForIsotopeLabeledTracking;
            Parameter = projectProp.Parameter;
            GnpsExport = projectProp.GnpsExport;
            MolecularNetworkingExport = projectProp.MolecularNetworkingExport;
            BlankFilter = projectProp.BlankFilter;
            IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            SnMatrixExport = projectProp.SnMatrixExport;
            IsExportAsMzTabM = projectProp.IsExportedAsMzTabM;
            MassTolerance = projectProp.MassToleranceForMs2Export;
            ExportSpectraFileFormat = projectProp.ExportSpectraFileFormat;
            ExportSpectraType = projectProp.ExportSpectraType;
            if (massTolerance <= 0) {
                this.MassTolerance = 0.05F;
                this.RawDatamatrix = true;
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (ClosingMethod() == true)
            {
                window.DialogResult = true;
                window.Close();
            }
        }

        private bool ClosingMethod()
        {
            if (this.exportFolderPath == null || this.exportFolderPath == string.Empty)
            {
                MessageBox.Show("Select an export folder.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (!System.IO.Directory.Exists(this.exportFolderPath)) {
                MessageBox.Show("There is no such a directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (this.sampleAxisDeconvolution == true && this.massTolerance < 0) { MessageBox.Show("If you do sample axis de-convolution method, please add the mass tolerance.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; }
            copyExportParametersToProjectProperty();

            
            if (IsExportAsMzTabM && !RawDatamatrix && !NormalizedDatamatrix && !PeakareaMatrix) {
                MessageBox.Show("For mzTab-M export, please check at least one of peak height, area, and normalized data table exports.", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return false;
            }

            try {
                if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
                    if (this.mainWindow.ProjectProperty.IsLabPrivateVersion && this.mainWindow.ProjectProperty.IsLabPrivateVersionTada) {
                        if (this.mainWindow.AnalysisParamForLC.IsIonMobility) {
                            DataExportLcUtility.AlignmentResultLipidomicsExportAtIonMobility(this.mainWindow,selectedAlignmentFileID, -1);
                        }
                        else {
                            DataExportLcUtility.AlignmentResultLipidomicsExport(this.mainWindow, selectedAlignmentFileID, this.selectedAnalysisFileID);
                        }
                        return true;
                    }


                    if (this.mainWindow.AnalysisParamForLC.IsIonMobility) {
                        DataExportLcUtility.AlignmentResultExportAtIonMobility(this.mainWindow, selectedAlignmentFileID, -1);
                    }
                    else if (isFilteringOptionForIsotopeLabeledTracking == false)
                        DataExportLcUtility.AlignmentResultExport(this.mainWindow, selectedAlignmentFileID, -1);
                    else
                        DataExportLcUtility.AlignmentResultExport(this.mainWindow, selectedAlignmentFileID, this.selectedAnalysisFileID);

                    if (this.gnpsExport) {
                        DataExportLcUtility.GnpsResultExport(this.mainWindow, selectedAlignmentFileID, this.selectedAnalysisFileID);
                    }
                }
                else
                {
                    DataExportGcUtility.AlignmentResultExport(this.mainWindow, selectedAlignmentFileID);
                }
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error exporting alignment data.\n" + ex.Message + "\n\nFull Exception:\n" + ex.StackTrace, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
			}

            return true;
        }

        private void copyExportParametersToProjectProperty() {
            var projectProp = this.mainWindow.ProjectProperty;
            projectProp.ExportFolderPath = ExportFolderPath;
            projectProp.RawDatamatrix = RawDatamatrix;
            projectProp.NormalizedDatamatrix = NormalizedDatamatrix;
            projectProp.RepresentativeSpectra = RepresentativeSpectra;
            projectProp.SampleAxisDeconvolution = SampleAxisDeconvolution;
            projectProp.PeakIdMatrix = PeakIdMatrix;
            projectProp.RetentionTimeMatrix = RetentionTimeMatrix;
            projectProp.MzMatrix = MzMatrix;
            projectProp.MsmsIncludedMatrix = MsmsIncludedMatrix;
            projectProp.DeconvolutedPeakAreaDataMatrix = DeconvolutedPeakAreaDataMatrix;
            projectProp.UniqueMs = UniqueMs;
            projectProp.PeakareaMatrix = PeakareaMatrix;
            projectProp.IsFilteringOptionForIsotopeLabeledTracking = IsFilteringOptionForIsotopeLabeledTracking;
            projectProp.Parameter = Parameter;
            projectProp.GnpsExport = GnpsExport;
            projectProp.MolecularNetworkingExport = MolecularNetworkingExport;
            projectProp.BlankFilter = BlankFilter;
            projectProp.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            projectProp.SnMatrixExport = SnMatrixExport;
            projectProp.IsExportedAsMzTabM = IsExportAsMzTabM;

            projectProp.MassToleranceForMs2Export = MassTolerance;
            projectProp.ExportSpectraFileFormat = ExportSpectraFileFormat;
            projectProp.ExportSpectraType = ExportSpectraType;
        }

        #region properties
        [Required(ErrorMessage = "Choose a folder path.")]
        public string ExportFolderPath
        {
            get { return exportFolderPath; }
            set { if (exportFolderPath == value) return; exportFolderPath = value; OnPropertyChanged("ExportFilePath"); }
        }


        public int SelectedAlignmentFileID
        {
            get { return selectedAlignmentFileID; }
            set { if (selectedAlignmentFileID == value) return; selectedAlignmentFileID = value; OnPropertyChanged("SelectedAlignmentFileID"); }
        }

        public int SelectedAnalysisFileID
        {
            get { return selectedAnalysisFileID; }
            set { if (selectedAnalysisFileID == value) return; selectedAnalysisFileID = value; OnPropertyChanged("SelectedAnalysisFileID"); }
        }

        public Window Window
        {
            get { return window; }
            set { window = value; }
        }

        public ObservableCollection<AlignmentFileBean> AlignmentFiles
        {
            get { return alignmentFiles; }
            set { if (alignmentFiles == value) return; alignmentFiles = value; OnPropertyChanged("AlignmentFiles"); }
        }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles
        {
            get { return analysisFiles; }
            set { if (analysisFiles == value) return; analysisFiles = value; OnPropertyChanged("AnalysisFiles"); }
        }
        public bool RawDatamatrix
        {
            get { return rawDatamatrix; }
            set { if (rawDatamatrix == value) return; rawDatamatrix = value; OnPropertyChanged("RawDatamatrix"); }
        }

        public bool NormalizedDatamatrix
        {
            get { return normalizedDatamatrix; }
            set { if (normalizedDatamatrix == value) return; normalizedDatamatrix = value; OnPropertyChanged("NormalizedDatamatrix"); }
        }

        public bool RepresentativeSpectra
        {
            get { return representativeSpectra; }
            set { if (representativeSpectra == value) return; representativeSpectra = value; OnPropertyChanged("RepresentativeSpectra"); }
        }

        public bool SampleAxisDeconvolution
        {
            get { return sampleAxisDeconvolution; }
            set { if (sampleAxisDeconvolution == value) return; sampleAxisDeconvolution = value; OnPropertyChanged("SampleAxisDeconvolution"); }
        }

        public bool PeakIdMatrix
        {
            get { return peakIdMatrix; }
            set { if (peakIdMatrix == value) return; peakIdMatrix = value; OnPropertyChanged("PeakIdMatrix"); }
        }

        public bool Parameter
        {
            get { return parameter; }
            set { if (parameter == value) return; parameter = value; OnPropertyChanged("Parameter"); }
        }

        public bool RetentionTimeMatrix
        {
            get { return retentionTimeMatrix; }
            set { if (retentionTimeMatrix == value) return; retentionTimeMatrix = value; OnPropertyChanged("RetentionTimeMatrix"); }
        }

        public bool MzMatrix
        {
            get { return mzMatrix; }
            set { if (mzMatrix == value) return; mzMatrix = value; OnPropertyChanged("MzMatrix"); }
        }

        public bool MsmsIncludedMatrix
        {
            get { return msmsIncludedMatrix; }
            set { if (msmsIncludedMatrix == value) return; msmsIncludedMatrix = value; OnPropertyChanged("MsmsIncludedMatrix"); }
        }

        public float MassTolerance
        {
            get { return massTolerance; }
            set { if (massTolerance == value) return; massTolerance = value; OnPropertyChanged("MassTolerance"); }
        }

        public ExportSpectraFileFormat ExportSpectraFileFormat
        {
            get { return exportSpectraFileFormat; }
            set { if (exportSpectraFileFormat == value) return; exportSpectraFileFormat = value; OnPropertyChanged("ExportSpectraFileFormat"); }
        }

        public ExportspectraType ExportSpectraType
        {
            get { return exportSpectraType; }
            set { if (exportSpectraType == value) return; exportSpectraType = value; OnPropertyChanged("ExportSpectraType"); }
        }


        public bool DeconvolutedPeakAreaDataMatrix
        {
            get { return deconvolutedPeakAreaDataMatrix; }
            set { if (deconvolutedPeakAreaDataMatrix == value) return; deconvolutedPeakAreaDataMatrix = value; OnPropertyChanged("DeconvolutedPeakAreaDataMatrix"); }
        }

        public bool UniqueMs
        {
            get { return uniqueMs; }
            set { if (uniqueMs == value) return; uniqueMs = value; OnPropertyChanged("UniqueMs"); }
        }

        public bool PeakareaMatrix
        {
            get { return peakareaMatrix; }
            set { if (peakareaMatrix == value) return; peakareaMatrix = value; OnPropertyChanged("PeakareaMatrix"); }
        }

        public bool IsFilteringOptionForIsotopeLabeledTracking
        {
            get { return isFilteringOptionForIsotopeLabeledTracking; }
            set { if (isFilteringOptionForIsotopeLabeledTracking == value) return; isFilteringOptionForIsotopeLabeledTracking = value; OnPropertyChanged("IsFilteringOptionForIsotopeLabeledTracking"); }
        }

        public bool GnpsExport {
            get { return gnpsExport; }
            set { if (gnpsExport == value) return; gnpsExport = value; OnPropertyChanged("GnpsExport"); }
        }

        public bool MolecularNetworkingExport {
            get {
                return molecularNetworkingExport;
            }

            set {
                if (molecularNetworkingExport == value) return; molecularNetworkingExport = value; OnPropertyChanged("MolecularNetworkingExport");
            }
        }

        public bool BlankFilter {
            get {
                return blankFilter;
            }

            set {
                if (blankFilter == value) return; blankFilter = value; OnPropertyChanged("BlankFilter");
            }
        }

        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get {
                return isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            }

            set {
                if (isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples == value) return; isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value; OnPropertyChanged("IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples");
            }
        }

        public bool SnMatrixExport {
            get {
                return snMatrixExport;
            }

            set {
                if (snMatrixExport == value) return; snMatrixExport = value; OnPropertyChanged("SnMatrixExport");
            }
        }

        public bool IsExportAsMzTabM {
            get {
                return isExportAsMzTabM;
            }

            set {
                if (isExportAsMzTabM == value) return; isExportAsMzTabM = value; OnPropertyChanged("IsExportAsMzTabM");
            }
        }

        #endregion
    }
}
