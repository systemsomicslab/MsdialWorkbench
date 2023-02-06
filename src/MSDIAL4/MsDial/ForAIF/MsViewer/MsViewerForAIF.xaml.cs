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
using Msdial.Lcms.Dataprocess.Algorithm;

namespace Rfx.Riken.OsakaUniv.MsViewer
{
    /// <summary>
    /// MsViewerForAIF.xaml の相互作用ロジック
    /// </summary>
    public partial class MsViewerWithReferenceForAIF : Window
    {
        public MsViewerWithReferenceForAifVM MsViewerWithReferenceForAifVM { get; set; }
        public enum MS2focus { DecVsRef, RawVsDec }
        public MS2focus Ms2Focus = MS2focus.DecVsRef;

        public MsViewerWithReferenceForAIF() {
            InitializeComponent();
        }

        public MsViewerWithReferenceForAIF(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, List<string> nameList, int peakId) {
            InitializeComponent();
           MsViewerWithReferenceForAifVM = new MsViewerWithReferenceForAifVM(this, commonProp, decResList, peakId);
            
            initialize(commonProp, decResList, nameList);
        }


        private void initialize(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, List<string> nameList) {
            this.Width = commonProp.NumDec * 400;

            for (var i = 0; i < commonProp.NumDec; i++) {
                this.MainViewerForDec.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                var newGrid = new Grid() {
                    HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(newGrid, i);

                var msUC = new MassSpectrogramDecVsRefUserControl(commonProp, decResList[i], MsViewerWithReferenceForAifVM.Pab, nameList[i], MsViewerWithReferenceForAifVM.ReferenceMassSpectrogramList[i], i);
                msUC.MassSpectrogramDecVsRefUserControlVM.SetScores(MsViewerWithReferenceForAifVM.CompNameList[i], MsViewerWithReferenceForAifVM.ScoresList[i]);
                newGrid.Children.Add(msUC);
                this.MainViewerForDec.Children.Add(newGrid);


                this.MainViewerForRaw.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                var newGrid2 = new Grid() {
                    HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(newGrid2, i);

                var msUC2 = new MassSpectrogramRawVsDecUC(commonProp, decResList[i], MsViewerWithReferenceForAifVM.Pab, nameList[i], i);
                newGrid2.Children.Add(msUC2);
                this.MainViewerForRaw.Children.Add(newGrid2);

                if (i > 0) {
                    var newGridSp = new GridSplitter() {
                        Width = 3, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Stretch, Background = Brushes.Gray
                    };
                    Grid.SetColumn(newGridSp, i);
                    Grid.SetRowSpan(newGridSp, 1);
                    this.MainViewerForRaw.Children.Add(newGridSp);

                    var newGridSp2 = new GridSplitter() {
                        Width = 3, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Stretch, Background = Brushes.Gray
                    };
                    Grid.SetColumn(newGridSp2, i);
                    Grid.SetRowSpan(newGridSp2, 1);
                    this.MainViewerForDec.Children.Add(newGridSp2);
                }


            }
        }

        public void Refresh(List<MS2DecResult> decResList, int peakId) {
                this.MsViewerWithReferenceForAifVM.Refresh(decResList, peakId);
        }

        public void RefreshReference(MspFormatCompoundInformationBean msp) {
            this.MsViewerWithReferenceForAifVM.RefreshReference(msp);
        }

        public void FileChange(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, int peakId) {
            this.MsViewerWithReferenceForAifVM.FileChange(commonProp, decResList, peakId);
        }



        private void TabControl_MsView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (((TabControl)sender).SelectedIndex == 0) {
                this.Ms2Focus = MS2focus.DecVsRef;
                this.MsViewerWithReferenceForAifVM.RefreshDecVsRef();
            }
            if (((TabControl)sender).SelectedIndex == 1) {
                this.Ms2Focus = MS2focus.RawVsDec;
                this.MsViewerWithReferenceForAifVM.RefreshRawVsDec();
            }
        }
    }
    public class MsViewerWithReferenceForAifVM:ViewModelBase {

        public List<MassSpectrogramBean> ReferenceMassSpectrogramList { get; set; }
        public List<string> CompNameList { get; set; }
        public List<List<float>> ScoresList { get; set; }
        public AifViewControlCommonProperties CommonProp { get; set; }
        private MsViewerWithReferenceForAIF msViewerWithReferenceForAif;
        private int peakID;
        public PeakAreaBean Pab { get; set; }
        private List<MS2DecResult> mS2DecResList { get; set; }

        public MsViewerWithReferenceForAifVM(MsViewerWithReferenceForAIF msViewerWithReferenceForAif, AifViewControlCommonProperties commonProp, List<MS2DecResult> mS2DecResList, int peakId) {
            this.msViewerWithReferenceForAif = msViewerWithReferenceForAif;
            this.CommonProp = commonProp;
            this.peakID = peakId;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
            this.mS2DecResList = mS2DecResList;
            setReferenceInformation();
        }

        public void FileChange(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, int peakId) {
            this.CommonProp = commonProp;
            this.peakID = peakId;
            this.mS2DecResList = decResList;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
            setReferenceInformation();
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                var i = j > 0 ? (j + 1) / 2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.msViewerWithReferenceForAif.MainViewerForRaw, j);
                var msUC = (MassSpectrogramRawVsDecUC)VisualTreeHelper.GetChild(grid, 0);
                msUC.MassSpectrogramRawVsDecVM.FileChange(CommonProp, this.mS2DecResList[i], this.Pab);

                var grid2 = (Grid)VisualTreeHelper.GetChild(this.msViewerWithReferenceForAif.MainViewerForDec, j);
                var msUC2 = (MassSpectrogramDecVsRefUserControl)VisualTreeHelper.GetChild(grid2, 0);
                msUC2.MassSpectrogramDecVsRefUserControlVM.FileChange(CommonProp, this.mS2DecResList[i], ReferenceMassSpectrogramList[i], this.Pab, CompNameList[i], ScoresList[i]);
                if (j > 0) j++;
            }

        }


        public void Refresh(List<MS2DecResult> decResList, int peakId) {
            this.peakID = peakId;
            this.mS2DecResList = decResList;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
            setReferenceInformation();
            RefreshDecVsRef();
            RefreshRawVsDec();
        }

        public void RefreshDecVsRef() { 
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                var i = j > 0 ? (j + 1)/2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.msViewerWithReferenceForAif.MainViewerForDec, j);
                var msUC = (MassSpectrogramDecVsRefUserControl)VisualTreeHelper.GetChild(grid, 0);
                msUC.MassSpectrogramDecVsRefUserControlVM.Refresh(this.mS2DecResList[i], ReferenceMassSpectrogramList[i], this.Pab, CompNameList[i], ScoresList[i]);
                if (j > 0) j++;
            }
        }

        public void RefreshRawVsDec() {
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                    var i = j > 0 ? (j + 1) / 2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.msViewerWithReferenceForAif.MainViewerForRaw, j);
                var msUC = (MassSpectrogramRawVsDecUC)VisualTreeHelper.GetChild(grid, 0);
                msUC.MassSpectrogramRawVsDecVM.Refresh(this.mS2DecResList[i], this.Pab);
                if (j > 0) j++;
            }
        }

        public void RefreshReference(MspFormatCompoundInformationBean msp) {
            ScoresList = new List<List<float>>();
            MassSpectrogramBean referenceMassSpectrogram = UtilityForAIF.GetReferenceSpectra(msp, Brushes.Red);
            for (var i = 0; i < CommonProp.NumDec; i++) {
                var ms2peaks = new ObservableCollection<double[]>(this.mS2DecResList[i].MassSpectra);
                ScoresList.Add(UtilityForAIF.GetScores(CommonProp.Param, msp, ms2peaks, Pab.AccurateMass, Pab.RtAtPeakTop));
            }
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                var i = j > 0 ? (j + 1) / 2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.msViewerWithReferenceForAif.MainViewerForDec, j);
                var msUC = (MassSpectrogramDecVsRefUserControl)VisualTreeHelper.GetChild(grid, 0);
                msUC.MassSpectrogramDecVsRefUserControlVM.Refresh(referenceMassSpectrogram, msp.Name, ScoresList[i]);
                if (j > 0) j++;
            }

        }



        private void setReferenceInformation() {
            CompNameList = new List<string>();
            ScoresList = new List<List<float>>();
            ReferenceMassSpectrogramList = new List<MassSpectrogramBean>();
            var idList = this.Pab.LibraryIDList;

            if (CommonProp.MspDB == null || CommonProp.MspDB.Count == 0 || idList == null || idList.Count == 0) {
                setNoAnnotation();
                return;
            }
            var checkAnnotation = false;
            for (var i = 0; i < CommonProp.NumDec; i++) {
                if (idList[i] < 0) {
                    checkAnnotation = true; break;
                }
            }
            if (checkAnnotation && this.Pab.LibraryID >= 0) {
                if (this.Pab.LibraryID < 0) { setNoAnnotation(); }
                if (this.Pab.LibraryID > CommonProp.MspDB.Count - 1) { setNoAnnotation(); }
                var msp = CommonProp.MspDB[this.Pab.LibraryID];
                var reference = UtilityForAIF.GetReferenceSpectra(msp, Brushes.Red);
                for (var i = 0; i < CommonProp.NumDec; i++) {
                    var ms2peaks = new ObservableCollection<double[]>(this.mS2DecResList[i].MassSpectra);
                    CompNameList.Add(Pab.MetaboliteName);
                    ScoresList.Add(UtilityForAIF.GetScores(CommonProp.Param, msp, ms2peaks, Pab.AccurateMass, Pab.RtAtPeakTop));
                    ReferenceMassSpectrogramList.Add(reference);
                }
            }
            else {
                for (var i = 0; i < CommonProp.NumDec; i++) {
                    if (idList[i] < 0) { UtilityForAIF.SetNoReferense(CompNameList, ScoresList, ReferenceMassSpectrogramList); ; continue; }
                    if (idList[i] > CommonProp.MspDB.Count - 1) { UtilityForAIF.SetNoReferense(CompNameList, ScoresList, ReferenceMassSpectrogramList); ; continue; }
                    var msp = CommonProp.MspDB[idList[i]];
                    var ms2peaks = new ObservableCollection<double[]>(this.mS2DecResList[i].MassSpectra);
                    CompNameList.Add(msp.Name);
                    ScoresList.Add(UtilityForAIF.GetScores(CommonProp.Param, msp, ms2peaks, Pab.AccurateMass, Pab.RtAtPeakTop));
                    ReferenceMassSpectrogramList.Add(UtilityForAIF.GetReferenceSpectra(msp, Brushes.Red));
                }
            }
        }
        private void setNoAnnotation() {
            for (var i = 0; i < CommonProp.NumDec; i++) {
                UtilityForAIF.SetNoReferense(CompNameList, ScoresList, ReferenceMassSpectrogramList);
            }
        }
    }


    /*        private void initialize(MainWindow mainWindow) {
                this.Width = NumDec * 300;

                for (var i = 0; i < NumDec; i++) {
                    this.MainViewer.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                    MultiBinding bndWidth = new MultiBinding() { };
                   bndWidth.Converter = new ConverterToDevidedValue();
                    bndWidth.Bindings.Add(new Binding("ActualWidth") { ElementName = "MainViewer" });
                    bndWidth.Bindings.Add(new Binding() { Source = NumDec });
                    var bndHeight = new Binding("ActualHeight") { ElementName = "MainViewer" };
                    var newGrid = new Grid() {
                        //    Width = this.Width / NumDec, Height = this.Height,
                        Background = System.Windows.Media.Brushes.Violet,
                        HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
                    };
                    //BindingOperations.SetBinding(newGrid, Grid.WidthProperty, bndWidth);
                    //BindingOperations.SetBinding(newGrid, Grid.HeightProperty, bndHeight);

                    Grid.SetColumn(newGrid, i);
                    newGrid.Children.Add(new MassSpectrogramUserControl());
                    //                var msUC = new MassSpectrogramUserControl(mainWindow.AnalysisFiles[mainWindow.FocusedFileID].AnalysisFilePropertyBean.DeconvolutionFilePathList[i],
                    //                  mainWindow.ProjectProperty.ExperimentID_AnalystExperimentInformationBean[mainWindow.ProjectProperty.Ms2LevelIdList[i]].Name, mainWindow.FocusedPeakID);
                    //            newGrid.Children.Add(msUC);
                    this.MainViewer.Children.Add(newGrid);

                }
            }

            public void SetValue() {
                for (var i = 0; i < NumDec; i++) {
                    Grid newGrid = (Grid)VisualTreeHelper.GetChild(this.MainViewer, i);
                    var a = (MassSpectrogramUserControl)VisualTreeHelper.GetChild(newGrid, 0);
                    a.Content =  new MassSpectrogramUserControl(mainWindow.AnalysisFiles[mainWindow.FocusedFileID].AnalysisFilePropertyBean.DeconvolutionFilePathList[i],
                      mainWindow.ProjectProperty.ExperimentID_AnalystExperimentInformationBean[mainWindow.ProjectProperty.Ms2LevelIdList[i]].Name, mainWindow.FocusedPeakID);
                    System.Diagnostics.Debug.WriteLine("w : " + newGrid.ActualHeight + ", h : " + newGrid.ActualWidth);
                    this.UpdateLayout();
                }
            }
            }

        public class ConverterToDevidedValue : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture) {
            System.Diagnostics.Debug.WriteLine("type" + value[1].GetType() + " " +  value[1]);
            var val = (double)value[0];
            var num = (int)value[1];
            if (num > 0) return val / (double)num;
            else return 0;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture) {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }

            */

}



