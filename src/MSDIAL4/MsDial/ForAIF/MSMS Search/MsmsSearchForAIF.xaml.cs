using Msdial.Lcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    /// CompoundSearchWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MsmsSearchForAIF : Window
    {
        private AnalysisFileBean analysisFile;
        private AlignmentResultBean alignmentResult;

        private List<MspFormatCompoundInformationBean> mspDB;

        private List<MS2DecResult> ms2DecResultList;

        private AnalysisParametersBean param;

        private AifViewControlForPeakTrigger checker;
        private int numDec;
        private int focusedPeakId;

        public MsmsSearchForAIF() {
            InitializeComponent();
        }

        public MsmsSearchForAIF(AlignmentResultBean alignmentResult, int focusedAlignmentPeakId, AnalysisParametersBean param, List<MS2DecResult> ms2DecResultList, List<MspFormatCompoundInformationBean> mspDB, AifViewControlForPeakTrigger checker) {
            InitializeComponent();

            this.alignmentResult = alignmentResult;
            this.focusedPeakId = focusedAlignmentPeakId;
            this.ms2DecResultList = ms2DecResultList;
            this.numDec = ms2DecResultList.Count;
            this.mspDB = mspDB;
            this.param = param;
            this.checker = checker;
            this.DataContext = new MsmsSearchForAifVM(alignmentResult, focusedAlignmentPeakId, param, ms2DecResultList, mspDB);
        }
        
        public MsmsSearchForAIF(AnalysisFileBean analysisFile, int focusedPeakId, AnalysisParametersBean param, List<MS2DecResult> ms2DecResultList, List<MspFormatCompoundInformationBean> mspDB, AifViewControlForPeakTrigger checker) {
            InitializeComponent();

            this.analysisFile = analysisFile;
            this.focusedPeakId = focusedPeakId;
            this.ms2DecResultList = ms2DecResultList;
            this.numDec = ms2DecResultList.Count;
            this.mspDB = mspDB;
            this.param = param;
            this.checker = checker;
            this.DataContext = new MsmsSearchForAifVM(analysisFile, focusedPeakId, param, ms2DecResultList, mspDB);
        }

        public void Refresh(List<MS2DecResult> ms2DecResultList, int focusedPeakId) {
            this.focusedPeakId = focusedPeakId;
            this.ms2DecResultList = ms2DecResultList;
            if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                ((MsmsSearchForAifVM)this.DataContext).Refresh(this.analysisFile, focusedPeakId, ms2DecResultList, mspDB);
            }
            else {
                ((MsmsSearchForAifVM)this.DataContext).Refresh(this.alignmentResult, focusedPeakId, ms2DecResultList, mspDB);
            }
        }

        private void Button_Import_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult result = MessageBox.Show(this, "If you change a msp file, you cannot select confidence/unsettled butotn\r\nDo you want to continue?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK) {
                this.mspDB = null;
                Button_Confidence.IsEnabled = false;
                Button_Unsettled.IsEnabled = false;
                var ofd = new Microsoft.Win32.OpenFileDialog();
 
                ofd.Filter = "all files(*)|*";
                ofd.Title = "Import a project file";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == true) {
                    System.Windows.Input.Mouse.OverrideCursor = Cursors.Wait;
                    var w = new ShortMessageWindow("Loading msp file");
                    w.Owner = this;
                    w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    w.Show();
                    this.mspDB = MspFileParcer.MspFileReader(ofd.FileName);
                    if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                        ((MsmsSearchForAifVM)this.DataContext).Refresh(this.analysisFile, focusedPeakId, ms2DecResultList, mspDB);
                    }
                    else {
                        ((MsmsSearchForAifVM)this.DataContext).Refresh(this.alignmentResult, focusedPeakId, ms2DecResultList, mspDB);
                    }
                    w.Close();
                    System.Windows.Input.Mouse.OverrideCursor = null;
                }
            }
        }

        private void Button_ReAnalysis_Click(object sender, RoutedEventArgs e) {
            if (this.TextBox_RetentionTimeTolerance.Text == string.Empty || this.TextBox_Ms1Tolerance.Text == string.Empty || this.TextBox_Ms2Tolerance.Text == string.Empty) {
                MessageBox.Show("The search tolerance should be included in textbox.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ((MsmsSearchForAifVM)this.DataContext).ChangeParam(this.param);
            ((MsmsSearchForAifVM)this.DataContext).SetCompoundSearchReferenceViewModelCollection();
        }

        private void Button_Confidence_Click(object sender, RoutedEventArgs e) {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;
            int libraryID = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceViewModelId;

            if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryID = libraryID;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MetaboliteName = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].RtSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].AccurateMassSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].ReverseSearchSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MassSpectraSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].PresenseSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].PresenseSimilarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].TotalScore = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000;
                for (var i = 0; i < numDec; i++) {
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryIDList[i] = -1;
                }
                checker.LibSearchPeak = true;
            }
            else {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].LibraryID = libraryID;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].ReverseSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MassSpectraSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].FragmentPresencePercentage = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].PresenseSimilarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionTimeSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].AccurateMassSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].TotalSimilairty = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000;
                checker.LibSearchAlignment = true;
            }
        }

        private void Button_Unsettled_Click(object sender, RoutedEventArgs e) {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;
            int libraryID = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceViewModelId;

            if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryID = libraryID;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MetaboliteName = "Unsettled: " + ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].RtSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].AccurateMassSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].ReverseSearchSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MassSpectraSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].PresenseSimilarityValue = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].PresenseSimilarity * 1000;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].TotalScore = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000;
                for (var i = 0; i < numDec; i++) {
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryIDList[i] = -1;
                }
                checker.LibSearchPeak = true;
            }
            else {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].LibraryID = libraryID;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = "Unsettled: " + ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].ReverseSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MassSpectraSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].FragmentPresencePercentage = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].PresenseSimilarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionTimeSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].AccurateMassSimilarity = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].TotalSimilairty = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000;
                checker.LibSearchAlignment = true;
            }
        }

        private void Button_Unknown_Click(object sender, RoutedEventArgs e) {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;
            int libraryID = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceViewModelId;

            if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryID = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MetaboliteName = string.Empty;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].RtSimilarityValue = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].AccurateMassSimilarity = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].ReverseSearchSimilarityValue = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MassSpectraSimilarityValue = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].PresenseSimilarityValue = -1;
                this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].TotalScore = -1;
                for (var i = 0; i < numDec; i++) {
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryIDList[i] = -1;
                }
                checker.LibSearchPeak = true;
            }
            else {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].LibraryID = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = string.Empty;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].ReverseSimilarity = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MassSpectraSimilarity = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].FragmentPresencePercentage = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionTimeSimilarity = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].AccurateMassSimilarity = -1;
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].TotalSimilairty = -1;
                checker.LibSearchAlignment = true;
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void labelRefresh(int selectedRowId) {
            /*        this.Label_SelectedLibraryID.Content = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;

                    this.Label_PeakInformation_AnnotatedMetabolite.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
                    this.Label_PeakInformation_RtSimilarity.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000;
                    this.Label_PeakInformation_Ms1Similarity.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000;
                    this.Label_PeakInformation_DotProduct.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000;
                    this.Label_PeakInformation_ReverseDotProduct.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000;
                    this.Label_PeakInformation_TotalScore.Content = ((MsmsSearchForAifVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000;
              */
        }

        private void DataGrid_LibraryInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;

            ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId = ((MsmsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).LibraryId;
            ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceViewModelId = ((MsmsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).Id;

            if (((MsmsSearchForAifVM)this.DataContext).PeakViewer) {
                var Lib = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;
                checker.LibraryPeak = this.mspDB[Lib];
            }
            else {
                var Lib = ((MsmsSearchForAifVM)this.DataContext).SelectedReferenceId;
                checker.LibraryAlignment = this.mspDB[Lib];
            }


        }
    }
}
