using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
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
    /// MassSpectrogram.xaml の相互作用ロジック
    /// </summary>
    public partial class MassSpectrogramDecVsRefUserControl : UserControl
    {
        public MassSpectrogramDecVsRefUserControlVM MassSpectrogramDecVsRefUserControlVM { get; set; }
        public MassSpectrogramDecVsRefUserControl() {
            InitializeComponent();
            MassSpectrogramDecVsRefUserControlVM = new MassSpectrogramDecVsRefUserControlVM(this);
            this.DataContext = MassSpectrogramDecVsRefUserControlVM;
        }
        public MassSpectrogramDecVsRefUserControl(AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            InitializeComponent();
            MassSpectrogramDecVsRefUserControlVM = new MassSpectrogramDecVsRefUserControlVM(this, commonProp, decRes, pab, name, referenceSpectraBean, id);
            this.DataContext = MassSpectrogramDecVsRefUserControlVM;
        }

        public void buttonClick_MsFinderSearchPeakViewer(object sender, RoutedEventArgs e) {
            this.MassSpectrogramDecVsRefUserControlVM.RunMsFinder();
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

        private void contextMenu_SaveSpectraTableAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
            SaveSpectraTableAsWin window = null;
            window = new SaveSpectraTableAsWin(target, this.MassSpectrogramDecVsRefUserControlVM.Pab, this.MassSpectrogramDecVsRefUserControlVM.CommonProp.ProjectProperty, this.MassSpectrogramDecVsRefUserControlVM.CommonProp.MspDB);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        private void contextMenu_CopySpectraTableAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopySpectraTableAsWin window = null;
            window = new CopySpectraTableAsWin(target, this.MassSpectrogramDecVsRefUserControlVM.Pab, this.MassSpectrogramDecVsRefUserControlVM.CommonProp.ProjectProperty, this.MassSpectrogramDecVsRefUserControlVM.CommonProp.MspDB);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }
    }

    public class MassSpectrogramDecVsRefUserControlVM : ViewModelBase
    {
        private MassSpectrogramDecVsRefUserControl massSpectrogramUserControl;

        private string graphTitle;
        private MS2DecResult peakViewMS2DecResult;
        private MassSpectrogramBean referenceSpectraBean;
        public AifViewControlCommonProperties CommonProp { get; set; }
        private int id;
        public PeakAreaBean Pab { get; set; }
        private string compoundName;
        private float totalScore;
        private float mzScore;
        private float rtScore;
        private float ms2TotalScore;
        private float scoreDot;
        private float scoreRev;
        private float scoreMatch;
        public string GraphTitle { get { return graphTitle; } set { if (graphTitle == value) return; graphTitle = value; OnPropertyChanged("GraphTitle"); } }
        public string CompoundName { get { return compoundName; }  set { if (compoundName == value) return; compoundName = value; OnPropertyChanged("CompoundName"); } }
        public float TotalScore { get { return totalScore; } set { if (totalScore == value) return; totalScore = value; OnPropertyChanged("TotalScore"); } }
        public float MzScore { get { return mzScore; } set { if (mzScore == value) return; mzScore = value; OnPropertyChanged("MzScore"); } }
        public float RtScore { get { return rtScore; } set { if (rtScore == value) return; rtScore = value; OnPropertyChanged("RtScore"); } }
        public float Ms2TotalScore { get { return ms2TotalScore; } set { if (ms2TotalScore == value) return; ms2TotalScore = value; OnPropertyChanged("Ms2TotalScore"); } }
        public float ScoreDot { get { return scoreDot; } set { if (scoreDot == value) return; scoreDot = value; OnPropertyChanged("ScoreDot"); } }
        public float ScoreRev { get { return scoreRev; } set { if (scoreRev == value) return; scoreRev = value; OnPropertyChanged("ScoreRev"); } }
        public float ScoreMatch { get { return scoreMatch; } set { if (scoreMatch == value) return; scoreMatch = value; OnPropertyChanged("ScoreMatch"); } }

        public MassSpectrogramDecVsRefUserControlVM(MassSpectrogramDecVsRefUserControl massSpectrogramUserControl) {
            this.massSpectrogramUserControl = massSpectrogramUserControl;
            this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI();
        }

        public MassSpectrogramDecVsRefUserControlVM(MassSpectrogramDecVsRefUserControl massSpectrogramUserControl, AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, MassSpectrogramBean referenceSpectraBean, int id) {
            this.massSpectrogramUserControl = massSpectrogramUserControl;
            this.CommonProp = commonProp;
            this.id = id;
            this.Pab = pab;
            this.peakViewMS2DecResult = decRes;
            GraphTitle = name;
            this.referenceSpectraBean = referenceSpectraBean;
            
            Refresh();
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

        }

        public void RunMsFinder() {
            UtilityForAIF.SendToMsFinderProgram(CommonProp, peakViewMS2DecResult, Pab, GraphTitle, id);
        }

        public void FileChange(AifViewControlCommonProperties commonProp, MS2DecResult ms2dec, MassSpectrogramBean referenceSpectraBean, PeakAreaBean pab, string compName, List<float> scores) {
            CommonProp = commonProp;
            this.peakViewMS2DecResult = ms2dec;
            this.Pab = pab;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            Refresh();

        }


        public void Refresh() {
            var msVm = UtilityForAIF.GetMs2MassspectrogramViewModel(this.peakViewMS2DecResult, this.referenceSpectraBean, "Precursor: " + Math.Round(this.peakViewMS2DecResult.Ms1AccurateMass, 4));
            this.massSpectrogramUserControl.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(msVm);
        }

        public void Refresh(MS2DecResult ms2dec, MassSpectrogramBean referenceSpectraBean, PeakAreaBean pab, string compName, List<float> scores) {
            this.peakViewMS2DecResult = ms2dec;
            this.Pab = pab;
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            Refresh();
        }

        public void Refresh(MassSpectrogramBean referenceSpectraBean, string compName, List<float> scores) {
            this.referenceSpectraBean = referenceSpectraBean;
            SetScores(compName, scores);
            Refresh();
        }

    }
}
