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
    public partial class MassSpectrogramRawVsDecUC : UserControl
    {
        public MassSpectrogramRawVsDecVM MassSpectrogramRawVsDecVM { get; set; }

        public MassSpectrogramRawVsDecUC() {
            InitializeComponent();
            MassSpectrogramRawVsDecVM = new MassSpectrogramRawVsDecVM();
            this.DataContext = MassSpectrogramRawVsDecVM;
        }
        public MassSpectrogramRawVsDecUC(AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, int id) {
            InitializeComponent();
            MassSpectrogramRawVsDecVM = new MassSpectrogramRawVsDecVM(this, commonProp, decRes, pab, name, id);
            this.DataContext = MassSpectrogramRawVsDecVM;
        }

        public void buttonClick_MsFinderSearchPeakViewerDec(object sender, RoutedEventArgs e) {
            this.MassSpectrogramRawVsDecVM.RunMsFinder(true);
        }

        public void buttonClick_MsFinderSearchPeakViewerRaw(object sender, RoutedEventArgs e) {
            this.MassSpectrogramRawVsDecVM.RunMsFinder(false);
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
            window = new SaveSpectraTableAsWin(target, this.MassSpectrogramRawVsDecVM.Pab, this.MassSpectrogramRawVsDecVM.CommonProp.ProjectProperty, this.MassSpectrogramRawVsDecVM.CommonProp.MspDB);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        private void contextMenu_CopySpectraTableAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopySpectraTableAsWin window = null;
            window = new CopySpectraTableAsWin(target, this.MassSpectrogramRawVsDecVM.Pab, this.MassSpectrogramRawVsDecVM.CommonProp.ProjectProperty, this.MassSpectrogramRawVsDecVM.CommonProp.MspDB);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }
    }
    public partial class MassSpectrogramRawVsDecVM: ViewModelBase
    {
        private MassSpectrogramRawVsDecUC massSpectrogramRawVsDecUC;

        private string graphTitle;
        private MS2DecResult peakViewMS2DecResult;
        public AifViewControlCommonProperties CommonProp { get; set; }
        private int id;
        public PeakAreaBean Pab { get; set; }
        public string GraphTitle { get { return graphTitle; } set { if (graphTitle == value) return; graphTitle = value; OnPropertyChanged("GraphTitle"); } }
        private bool propertyCheck;

        public MassSpectrogramRawVsDecVM() { }

        public MassSpectrogramRawVsDecVM(MassSpectrogramRawVsDecUC massSpectrogramRawVsDecUC, AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, int id) {
            this.massSpectrogramRawVsDecUC = massSpectrogramRawVsDecUC;
            this.CommonProp = commonProp;
            this.id = id;
            this.Pab = pab;
            this.peakViewMS2DecResult = decRes;
            graphTitle = name;

            Refresh();
        }
        
        public void RunMsFinder(bool check) {
            if(check)
                UtilityForAIF.SendToMsFinderProgram(CommonProp, peakViewMS2DecResult, Pab, graphTitle, id);
            else
                UtilityForAIF.SendToMsFinderProgram(CommonProp, Pab, graphTitle, id);
        }

        public void FileChange(AifViewControlCommonProperties commonProp, MS2DecResult ms2dec, PeakAreaBean pab) {
            this.CommonProp = commonProp;
            this.peakViewMS2DecResult = ms2dec;
            this.Pab = pab;

            Refresh();
        }


        public void Refresh(MS2DecResult ms2dec, PeakAreaBean pab) {
            this.peakViewMS2DecResult = ms2dec;
            this.Pab = pab;
            Refresh();
        }

        public void Refresh() {
            var msVmRaw = UtilityForAIF.GetMs2RawMassspectrogramViewModel(this.CommonProp, Pab, id);
            var msVmDec = UtilityForAIF.GetMs2MassspectrogramViewModel(this.peakViewMS2DecResult);
            msVmRaw.PropertyChanged -= rawMassSpectrogramViewModel_PropertyChanged;
            msVmRaw.PropertyChanged += rawMassSpectrogramViewModel_PropertyChanged;
            msVmDec.PropertyChanged -= deconvolutedMassSpectrogramViewModel_PropertyChanged;
            msVmDec.PropertyChanged += deconvolutedMassSpectrogramViewModel_PropertyChanged;

            this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content = new MassSpectrogramUI(msVmRaw);
            this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content = new MassSpectrogramUI(msVmDec);
        }

        private void rawMassSpectrogramViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "DisplayRangeMassMin") {
                if (this.propertyCheck == true) { this.propertyCheck = false; return; }
                this.propertyCheck = true;

                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax;
                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin;
                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void deconvolutedMassSpectrogramViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "DisplayRangeMassMin") {
                if (this.propertyCheck == true) { this.propertyCheck = false; return; }
                this.propertyCheck = true;

                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax;
                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.DecMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin;
                ((MassSpectrogramUI)this.massSpectrogramRawVsDecUC.RawMassSpectrogramUI.Content).RefreshUI();
            }
        }

    }

}
