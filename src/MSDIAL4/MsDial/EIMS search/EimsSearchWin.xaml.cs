using Msdial.Gcms.Dataprocess.Algorithm;
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
    /// Interaction logic for EimsSearchWin.xaml
    /// </summary>
    public partial class EimsSearchWin : Window
    {
        private AnalysisFileBean analysisFile;
        private AlignmentResultBean alignmentResult;

        private List<MspFormatCompoundInformationBean> mspDB;
        private MS1DecResult ms1DecResult;
        private AnalysisParamOfMsdialGcms param;

        private int focusedPeakId;

        public EimsSearchWin()
        {
            InitializeComponent();
        }

        public EimsSearchWin(AlignmentResultBean alignmentResult, int focusedAlignmentPeakId, AnalysisParamOfMsdialGcms param, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB)
        {
            InitializeComponent();
            
            this.alignmentResult = alignmentResult;
            this.focusedPeakId = focusedAlignmentPeakId;
            this.ms1DecResult = ms1DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.DataContext = new EimsSearchVM(alignmentResult, focusedAlignmentPeakId, param, ms1DecResult, mspDB);

            var spectrumVM = GetReverseMassspectrogramVM(this.ms1DecResult, ((EimsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(spectrumVM);
            
            labelRefresh();
        }

        public EimsSearchWin(AnalysisFileBean analysisFile, int focusedPeakId, AnalysisParamOfMsdialGcms param, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB)
        {
            InitializeComponent();
            
            this.analysisFile = analysisFile;
            this.focusedPeakId = focusedPeakId;
            this.ms1DecResult = ms1DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.DataContext = new EimsSearchVM(analysisFile, focusedPeakId, param, ms1DecResult, mspDB);

            var spectrumVM = GetReverseMassspectrogramVM(this.ms1DecResult, ((EimsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(spectrumVM);
            
            labelRefresh();
        }

        private void Button_ReAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (this.TextBox_RetentionTimeTolerance.Text == string.Empty || this.TextBox_MassTolerance.Text == string.Empty || this.TextBox_RetentionIndexTolerance.Text == string.Empty)
            {
                MessageBox.Show("The search tolerance should be included in textbox.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((EimsSearchVM)this.DataContext).SetCompoundSearchReferenceViewModelCollection();
        }

        private void Button_Confidence_Click(object sender, RoutedEventArgs e)
        {
            int mspID = ((EimsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((EimsSearchVM)this.DataContext).SelectedReferenceViewModelId;
            
            this.ms1DecResult.MetaboliteName = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
            setIdentificationProperty(this.ms1DecResult, selectedRowId, mspID);
           
            if (((EimsSearchVM)this.DataContext).PeakViewer)
            {
                
            }
            else
            {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
                setIdentificationProperty(this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId], selectedRowId, mspID);
                //setIdentificationProperty(this.ms1DecResult, selectedRowId, mspID);
            }

            this.Close();
        }

        private void Button_Unsettled_Click(object sender, RoutedEventArgs e)
        {
            int mspID = ((EimsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((EimsSearchVM)this.DataContext).SelectedReferenceViewModelId;
            this.ms1DecResult.MetaboliteName = "Unsettled: " + ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
            setIdentificationProperty(this.ms1DecResult, selectedRowId, mspID);
            if (((EimsSearchVM)this.DataContext).PeakViewer)
            {
               
            }
            else
            {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = "Unsettled: " + ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
                setIdentificationProperty(this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId], selectedRowId, mspID);

                //this.ms1DecResult.MetaboliteName = "Unsettled: " + ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
                //setIdentificationProperty(this.ms1DecResult, selectedRowId, mspID);
            }
           
            this.Close();
        }

        private void Button_Unknown_Click(object sender, RoutedEventArgs e)
        {
            int selectedRowId = ((EimsSearchVM)this.DataContext).SelectedReferenceViewModelId;
            this.ms1DecResult.MetaboliteName = string.Empty;
            setIdentificationProperty(this.ms1DecResult, selectedRowId, -1);
            if (((EimsSearchVM)this.DataContext).PeakViewer)
            {
                
            }
            else
            {
                this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName = string.Empty;
                setIdentificationProperty(this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId], selectedRowId, -1);

                //this.ms1DecResult.MetaboliteName = string.Empty;
                //setIdentificationProperty(this.ms1DecResult, selectedRowId, -1);
            }
            
            this.Close();
        }

        private void setIdentificationProperty(MS1DecResult ms1DecResult, int selectedRowId, int mspID)
        {
            ms1DecResult.MspDbID = mspID;
            if (mspID >= 0)
            {
                ms1DecResult.MetaboliteName = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
                ms1DecResult.EiSpectrumSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].EiSpectraSimilarity * 1000;
                ms1DecResult.DotProduct = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].DotProduct * 1000;
                ms1DecResult.ReverseDotProduct = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].ReverseDotProduct * 1000;
                ms1DecResult.PresencePersentage = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].PresenseSimilarity * 1000;
                ms1DecResult.TotalSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].TotalSimilarity * 1000;
                ms1DecResult.RetentionTimeSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionTimeSimlarity * 1000;
                ms1DecResult.RetentionIndexSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionIndexSimilarity * 1000;
            }
            else
            {
                ms1DecResult.MetaboliteName = string.Empty;
                ms1DecResult.EiSpectrumSimilarity = -1;
                ms1DecResult.DotProduct = -1;
                ms1DecResult.ReverseDotProduct = -1;
                ms1DecResult.PresencePersentage = -1;
                ms1DecResult.RetentionTimeSimilarity = -1;
                ms1DecResult.RetentionIndexSimilarity = -1;
                ms1DecResult.TotalSimilarity = -1;
            }
        }

        private void setIdentificationProperty(AlignmentPropertyBean property, int selectedRowId, int mspID)
        {
            property.LibraryID = mspID;

            if (mspID >= 0)
            {
                property.MetaboliteName = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
                property.EiSpectrumSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].EiSpectraSimilarity * 1000;
                property.MassSpectraSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].DotProduct * 1000;
                property.ReverseSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].ReverseDotProduct * 1000;
                property.FragmentPresencePercentage = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].PresenseSimilarity * 1000;
                property.TotalSimilairty = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].TotalSimilarity * 1000;
                property.RetentionTimeSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionTimeSimlarity * 1000;
                property.RetentionIndexSimilarity = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionIndexSimilarity * 1000;
                property.IsManuallyModifiedForAnnotation = true;
            }
            else
            {
                property.MetaboliteName = string.Empty;
                property.RetentionTimeSimilarity = -1;
                property.RetentionIndexSimilarity = -1;
                property.MassSpectraSimilarity = -1;
                property.ReverseSimilarity = -1;
                property.FragmentPresencePercentage = -1;
                property.EiSpectrumSimilarity = -1;
                property.TotalSimilairty = -1;
                property.IsManuallyModifiedForAnnotation = true;
            }

        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void labelRefresh()
        {
            this.Label_SelectedLibraryID.Content = ((EimsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((EimsSearchVM)this.DataContext).SelectedReferenceId;
            int mspID = ((EimsSearchVM)this.DataContext).SelectedReferenceId;

            if (((EimsSearchVM)this.DataContext).PeakViewer)
            {
                this.Label_PeakInformation_AnnotatedMetabolite.Content = getCompoundName(this.ms1DecResult.MspDbID);
                this.Label_PeakInformation_RiSimilarity.Content = this.ms1DecResult.RetentionIndexSimilarity;
                this.Label_PeakInformation_RtSimilarity.Content = this.ms1DecResult.RetentionTimeSimilarity;
                this.Label_PeakInformation_EiSimilarity.Content = this.ms1DecResult.EiSpectrumSimilarity;
                this.Label_PeakInformation_DotProduct.Content = this.ms1DecResult.DotProduct;
                this.Label_PeakInformation_ReverseDotProduct.Content = this.ms1DecResult.ReverseDotProduct;
                this.Label_PeakInformation_PresentPercentage.Content = this.ms1DecResult.PresencePersentage;
                this.Label_PeakInformation_TotalScore.Content = this.ms1DecResult.TotalSimilarity;
            }
            else
            {
                this.Label_PeakInformation_AnnotatedMetabolite.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName;
                this.Label_PeakInformation_RiSimilarity.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionIndexSimilarity;
                this.Label_PeakInformation_RtSimilarity.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionTimeSimilarity;
                this.Label_PeakInformation_EiSimilarity.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].EiSpectrumSimilarity;
                this.Label_PeakInformation_DotProduct.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MassSpectraSimilarity;
                this.Label_PeakInformation_ReverseDotProduct.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].ReverseSimilarity;
                this.Label_PeakInformation_PresentPercentage.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].FragmentPresencePercentage;
                this.Label_PeakInformation_TotalScore.Content = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].TotalSimilairty;
            }
        }

        private void labelRefresh(int selectedRowId)
        {
            this.Label_SelectedLibraryID.Content = ((EimsSearchVM)this.DataContext).SelectedReferenceId;

            this.Label_PeakInformation_AnnotatedMetabolite.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].CompoundName;
            this.Label_PeakInformation_RtSimilarity.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionTimeSimlarity * 1000;
            this.Label_PeakInformation_RiSimilarity.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].RetentionIndexSimilarity * 1000;
            this.Label_PeakInformation_EiSimilarity.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].EiSpectraSimilarity * 1000;
            this.Label_PeakInformation_DotProduct.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].DotProduct * 1000;
            this.Label_PeakInformation_ReverseDotProduct.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].ReverseDotProduct * 1000;
            this.Label_PeakInformation_PresentPercentage.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].PresenseSimilarity * 1000;
            this.Label_PeakInformation_TotalScore.Content = ((EimsSearchVM)this.DataContext).EimsSearchReferenceVMs[selectedRowId].TotalSimilarity * 1000;
        }

        private MassSpectrogramViewModel GetReverseMassspectrogramVM(MS1DecResult ms1DecResult, int libraryId)
        {
            float targetRt = ms1DecResult.RetentionTime;
            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(ms1DecResult);

            string graphTitle = "";

            if (this.mspDB != null && this.mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(this.mspDB, libraryId);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 2.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, 0, targetRt, graphTitle);
        }

        private MassSpectrogramBean getMassSpectrogramBean(MS1DecResult ms1DecResult)
        {
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            var masslist = new List<double[]>();

            if (ms1DecResult.Spectrum.Count == 0)
            {
                masslist.Add(new double[] { 0, 0 });
            }
            else
            {
                for (int i = 0; i < ms1DecResult.Spectrum.Count; i++)
                    masslist.Add(new double[] { ms1DecResult.Spectrum[i].Mz, ms1DecResult.Spectrum[i].Intensity });
            }

            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(Brushes.Black, 2.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private MassSpectrogramBean getReferenceSpectra(List<MspFormatCompoundInformationBean> mspDB, int libraryID)
        {
            var masslist = new ObservableCollection<double[]>();
            var massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (libraryID < 0) return new MassSpectrogramBean(Brushes.Red, 1.0, null);

            for (int i = 0; i < mspDB[libraryID].MzIntensityCommentBeanList.Count; i++)
            {
                masslist.Add(new double[] { (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Mz, (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Intensity });
                massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Mz, Intensity = (double)mspDB[libraryID].MzIntensityCommentBeanList[i].Intensity, Label = mspDB[libraryID].MzIntensityCommentBeanList[i].Comment });
            }


            return new MassSpectrogramBean(Brushes.Red, 2.0, masslist, massSpectrogramDisplayLabelCollection);
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            var window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            var window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void DataGrid_LibraryInformation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;

            ((EimsSearchVM)this.DataContext).SelectedReferenceId = ((EimsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).MspID;
            ((EimsSearchVM)this.DataContext).SelectedReferenceViewModelId = ((EimsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).Id;

            var mass2SpectrogramViewModel = GetReverseMassspectrogramVM(this.ms1DecResult, ((EimsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

            labelRefresh(((EimsSearchVM)this.DataContext).SelectedReferenceViewModelId);
        }

        private string getCompoundName(int id)
        {
            if (this.mspDB == null || this.mspDB.Count - 1 < id || id < 0) return "Unknown";
            else return this.mspDB[id].Name;
        }

    }
}
