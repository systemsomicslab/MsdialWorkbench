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
    /// Ms2ChromatogramUC.xaml の相互作用ロジック
    /// </summary>
    public partial class Ms2ChromatogramUC : UserControl
    {
        public Ms2ChromatogramVM Ms2ChromatogramVM { get; set; }
        public Ms2ChromatogramUC() {
            InitializeComponent();
        }

        public Ms2ChromatogramUC(AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, int id) {
            InitializeComponent();
            this.Ms2ChromatogramVM = new Ms2ChromatogramVM(this, commonProp, decRes, pab, name, id);
            this.DataContext = Ms2ChromatogramVM;
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

        private void contextMenu_SaveChromatogramTableAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveDataTableAsTextWin window = null;
            window = new SaveDataTableAsTextWin(target, this.Ms2ChromatogramVM.Pab);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }
    }

    public class Ms2ChromatogramVM : ViewModelBase
    {
        private Ms2ChromatogramUC ms2ChromatogramUC;
        private string graphTitle;
        private MS2DecResult peakViewMS2DecResult;
        public AifViewControlCommonProperties CommonProp { get; set; }
        private int id;
        public PeakAreaBean Pab { get; set; }
        public string GraphTitle { get { return graphTitle; } set { if (graphTitle == value) return; graphTitle = value; OnPropertyChanged("GraphTitle"); } }
        private List<SolidColorBrush> solidColorBrushList = new List<SolidColorBrush>() { Brushes.Blue, Brushes.Red, Brushes.Green
            , Brushes.DarkBlue, Brushes.DarkRed, Brushes.DarkGreen, Brushes.DeepPink, Brushes.OrangeRed
            , Brushes.Purple, Brushes.Crimson, Brushes.DarkGoldenrod, Brushes.Black, Brushes.BlanchedAlmond
            , Brushes.BlueViolet, Brushes.Brown, Brushes.BurlyWood, Brushes.CadetBlue, Brushes.Aquamarine
            , Brushes.Yellow, Brushes.Crimson, Brushes.Chartreuse, Brushes.Chocolate, Brushes.Coral
            , Brushes.CornflowerBlue, Brushes.Cornsilk, Brushes.Crimson, Brushes.Cyan, Brushes.DarkCyan
            , Brushes.DarkKhaki, Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid
            , Brushes.DarkSalmon, Brushes.DarkSeaGreen, Brushes.DarkSlateBlue, Brushes.DarkSlateGray
            , Brushes.DarkTurquoise, Brushes.DeepSkyBlue, Brushes.DodgerBlue, Brushes.Firebrick, Brushes.FloralWhite
            , Brushes.ForestGreen, Brushes.Fuchsia, Brushes.Gainsboro, Brushes.GhostWhite, Brushes.Gold
            , Brushes.Goldenrod, Brushes.Gray, Brushes.Navy, Brushes.DarkGreen, Brushes.Lime
            , Brushes.MediumBlue };

        public Ms2ChromatogramVM() { }

        public Ms2ChromatogramVM(Ms2ChromatogramUC ms2ChromatogramUC, AifViewControlCommonProperties commonProp, MS2DecResult decRes, PeakAreaBean pab, string name, int id) {
            this.ms2ChromatogramUC = ms2ChromatogramUC;
            this.CommonProp = commonProp;
            this.id = id;
            this.Pab = pab;
            this.peakViewMS2DecResult = decRes;
            GraphTitle = name;

            Refresh();
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
            var chromVMRaw = UtilityForAIF.GetMs2ChromatogramViewModelForMsViewer(CommonProp.Spectrum, CommonProp.ProjectProperty, this.Pab, CommonProp.Param, CommonProp.ProjectProperty.ExperimentID_AnalystExperimentInformationBean, this.peakViewMS2DecResult, MrmChromatogramView.raw, solidColorBrushList, id);
            var chromVMDec = UtilityForAIF.GetMs2ChromatogramViewModelForMsViewer(CommonProp.Spectrum, CommonProp.ProjectProperty, this.Pab, CommonProp.Param, CommonProp.ProjectProperty.ExperimentID_AnalystExperimentInformationBean, this.peakViewMS2DecResult, MrmChromatogramView.component, solidColorBrushList, id);
            this.ms2ChromatogramUC.MS2ChromatogramRawUI.Content = new ChromatogramMrmUI(chromVMRaw);
            this.ms2ChromatogramUC.MS2ChromatogramDecUI.Content = new ChromatogramMrmUI(chromVMDec);
        }
    }
}
