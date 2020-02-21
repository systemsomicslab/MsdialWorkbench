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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Msdial.Lcms.Dataprocess.Algorithm;

namespace Rfx.Riken.OsakaUniv.MsViewer
{
    /// <summary>
    /// MassSpectrogramRawVsDecUC.xaml の相互作用ロジック
    /// </summary>
    public partial class MassSpectrogramAlignmentUC : UserControl
    {
        public MassSpectrogramAlignmentVM MassSpectrogramAlignmentVM { get; set; }

        public MassSpectrogramAlignmentUC() {
            InitializeComponent();
            MassSpectrogramAlignmentVM = new MassSpectrogramAlignmentVM(this);
            this.DataContext = MassSpectrogramAlignmentVM;
        }
        public MassSpectrogramAlignmentUC(AifViewControlCommonProperties commonProp, MS2DecResult decRes, AlignmentPropertyBean alignmentProperty, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            InitializeComponent();
            var g = this.Grid_all;
            g.Children.RemoveAt(3);
            g.RowDefinitions.RemoveAt(3);
            MassSpectrogramAlignmentVM = new MassSpectrogramAlignmentVM(this, commonProp, decRes, alignmentProperty, name, referenceSpectraBean, id);
            this.DataContext = MassSpectrogramAlignmentVM;
        }

        public MassSpectrogramAlignmentUC(AifViewControlCommonProperties commonProp, CorrDecResult decRes, AlignmentPropertyBean alignmentProperty, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            InitializeComponent();
            MassSpectrogramAlignmentVM = new MassSpectrogramAlignmentVM(this, commonProp, decRes, alignmentProperty, name, referenceSpectraBean, id);
            this.DataContext = MassSpectrogramAlignmentVM;
        }


        public void buttonClick_MsFinderSearchAlignment(object sender, RoutedEventArgs e) {
            this.MassSpectrogramAlignmentVM.RunMsFinder();
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

    }

    public class MassSpectrogramAlignmentVM : ViewModelBase
    {
        private MassSpectrogramAlignmentUC massSpectrogramUserControl;

        private string graphTitle;
        private MS2DecResult mS2DecResult;
        private CorrDecResult correlDecRes;
        private MassSpectrogramBean referenceSpectraBean;
        public AifViewControlCommonProperties CommonProp { get; set; }
        private int id;
        public AlignmentPropertyBean AlignmentProperty { get; set; }
        private string compoundName;
        private float totalScore;
        private float mzScore;
        private float rtScore;
        private float ms2TotalScore;
        private float scoreDot;
        private float scoreRev;
        private float scoreMatch;
        private float scoreSimpleDot;
        public string GraphTitle { get { return graphTitle; } set { if (graphTitle == value) return; graphTitle = value; OnPropertyChanged("GraphTitle"); } }
        public string CompoundName { get { return compoundName; } set { if (compoundName == value) return; compoundName = value; OnPropertyChanged("CompoundName"); } }
        public float TotalScore { get { return totalScore; } set { if (totalScore == value) return; totalScore = value; OnPropertyChanged("TotalScore"); } }
        public float MzScore { get { return mzScore; } set { if (mzScore == value) return; mzScore = value; OnPropertyChanged("MzScore"); } }
        public float RtScore { get { return rtScore; } set { if (rtScore == value) return; rtScore = value; OnPropertyChanged("RtScore"); } }
        public float Ms2TotalScore { get { return ms2TotalScore; } set { if (ms2TotalScore == value) return; ms2TotalScore = value; OnPropertyChanged("Ms2TotalScore"); } }
        public float ScoreDot { get { return scoreDot; } set { if (scoreDot == value) return; scoreDot = value; OnPropertyChanged("ScoreDot"); } }
        public float ScoreRev { get { return scoreRev; } set { if (scoreRev == value) return; scoreRev = value; OnPropertyChanged("ScoreRev"); } }
        public float ScoreMatch { get { return scoreMatch; } set { if (scoreMatch == value) return; scoreMatch = value; OnPropertyChanged("ScoreMatch"); } }
        public float ScoreSimpleDot { get { return scoreSimpleDot; } set { if (scoreSimpleDot == value) return; scoreSimpleDot = value; OnPropertyChanged("ScoreSimpleDot"); } }
        public List<double[]> Peaks { get { var tmp = new List<double[]>(); foreach (var p in correlDecRes.PeakMatrix) { tmp.Add(new double[] { p.Mz, p.Intensity, p.Correlation, p.Count, p.StDevRatio, p.Score }); } return tmp; } }
        public MassSpectrogramAlignmentVM(MassSpectrogramAlignmentUC massSpectrogramUserControl) {
            this.massSpectrogramUserControl = massSpectrogramUserControl;
            this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI();
        }

        // alignment
        public MassSpectrogramAlignmentVM(MassSpectrogramAlignmentUC massSpectrogramUserControl, AifViewControlCommonProperties commonProp, MS2DecResult decRes, AlignmentPropertyBean alignmentProperty, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            this.massSpectrogramUserControl = massSpectrogramUserControl;
            this.CommonProp = commonProp;
            this.id = id;
            this.AlignmentProperty = alignmentProperty;
            this.mS2DecResult = decRes;
            GraphTitle = name;
            this.referenceSpectraBean = referenceSpectraBean;

            Refresh();
        }

        // correlation
        public MassSpectrogramAlignmentVM(MassSpectrogramAlignmentUC massSpectrogramUserControl, AifViewControlCommonProperties commonProp, CorrDecResult decRes, AlignmentPropertyBean alignmentProperty, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            this.massSpectrogramUserControl = massSpectrogramUserControl;
            this.CommonProp = commonProp;
            this.id = id;
            this.AlignmentProperty = alignmentProperty;
            this.correlDecRes = decRes;
            GraphTitle = name;
            this.referenceSpectraBean = referenceSpectraBean;
           // this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.TopMarginForLabel = 60;

            RefreshCorrel();
        }

        public void SetScores(string compName, List<float> scores) {
            CompoundName = compName;
            TotalScore = scores[0];
            MzScore = scores[1];
            RtScore = scores[2];
            Ms2TotalScore = scores[3];
            ScoreDot = scores[4];
            ScoreRev = scores[5];
            ScoreMatch = scores[6];
            ScoreSimpleDot = scores[7];
        }

        public void RunMsFinder() {
            if (this.correlDecRes == null)
                UtilityForAIF.SendToMsFinderProgram(CommonProp, mS2DecResult, AlignmentProperty, graphTitle, id);
            else
                UtilityForAIF.SendToMsFinderProgram(CommonProp, correlDecRes, AlignmentProperty, graphTitle, id);

        }

        public void FileChange(AifViewControlCommonProperties commonProp, MS2DecResult ms2dec, MassSpectrogramBean referenceSpectraBean, AlignmentPropertyBean alignmentProperty, string compName, List<float> scores) {
            CommonProp = commonProp;
            this.mS2DecResult = ms2dec;
            this.AlignmentProperty = alignmentProperty;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            Refresh();

        }


        public void Refresh() {
            var msVm = UtilityForAIF.GetMs2MassspectrogramViewModel(this.mS2DecResult, this.referenceSpectraBean, "Precursor: " + Math.Round(this.AlignmentProperty.CentralAccurateMass, 4));
            this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(msVm);
        }

        public void Refresh(MS2DecResult ms2dec, MassSpectrogramBean referenceSpectraBean, AlignmentPropertyBean alignmentProperty, string compName, List<float> scores) {
            this.mS2DecResult = ms2dec;
            this.AlignmentProperty = alignmentProperty;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            Refresh();
        }

        public void Refresh(MassSpectrogramBean referenceSpectraBean, string compName, List<float> scores, bool correlCheck = false) {
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            if (correlCheck)
                RefreshCorrel();
            else
                Refresh();
        }

        // for correlation
        public void FileChange(AifViewControlCommonProperties commonProp, CorrDecResult decRes, MassSpectrogramBean referenceSpectraBean, AlignmentPropertyBean alignmentProperty, string compName, List<float> scores) {
            CommonProp = commonProp;
            this.correlDecRes = decRes;
            this.AlignmentProperty = alignmentProperty;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            RefreshCorrel();

        }

        public void Refresh(CorrDecResult decRes, MassSpectrogramBean referenceSpectraBean, AlignmentPropertyBean alignmentProperty, string compName, List<float> scores) {
            this.correlDecRes = decRes;
            this.AlignmentProperty = alignmentProperty;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            RefreshCorrel();
        }

        public void RefreshCorrel(MassSpectrogramBean referenceSpectraBean) {
            this.referenceSpectraBean = referenceSpectraBean;
            RefreshCorrel();
        }

        public void RefreshCorrel() {
            OnPropertyChanged("Peaks");
            var msVm = UtilityForAIF.GetMs2MassspectrogramViewModel(this.correlDecRes, this.CommonProp.Param.AnalysisParamOfMsdialCorrDec, this.AlignmentProperty, this.referenceSpectraBean, "Precursor: " + Math.Round(this.AlignmentProperty.CentralAccurateMass, 4));
            this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(msVm, 20);
        }


    }
}
