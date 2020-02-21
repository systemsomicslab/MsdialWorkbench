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
using Msdial.Lcms.Dataprocess.Algorithm;

namespace Rfx.Riken.OsakaUniv.MsViewer
{
    /// <summary>
    /// Ms2ChromatogramForAIF.xaml の相互作用ロジック
    /// </summary>
    public partial class Ms2ChromatogramForAIF : Window
    {
        public Ms2ChromatogramForAifVM Ms2ChromatogramForAifVM { get; set; }
        public Ms2ChromatogramForAIF() {
            InitializeComponent();
        }
        

        public Ms2ChromatogramForAIF(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, List<string> nameList, int peakId) {
            InitializeComponent();
            Ms2ChromatogramForAifVM = new Ms2ChromatogramForAifVM(this, commonProp, decResList, peakId);

            initialize(commonProp, decResList, nameList);
        }


        private void initialize(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, List<string> nameList) {
            this.Width = commonProp.NumDec * 400;

            for (var i = 0; i < commonProp.NumDec; i++) {
                this.MainViewer.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                var newGrid = new Grid() {
                    HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch
                };
                Grid.SetColumn(newGrid, i);
                var chromUC = new Ms2ChromatogramUC(commonProp, decResList[i], Ms2ChromatogramForAifVM.Pab, nameList[i], i);
                newGrid.Children.Add(chromUC);
                this.MainViewer.Children.Add(newGrid);
                if(i > 0) {
                    var newGridSp = new GridSplitter() {
                        Width = 3, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Stretch, Background = Brushes.Gray
                    };
                    Grid.SetColumn(newGridSp, i);
                    Grid.SetRowSpan(newGridSp, 1);
                    this.MainViewer.Children.Add(newGridSp);
                }

            }
        }

        public void Refresh(List<MS2DecResult> decResList, int peakId) {
            this.Ms2ChromatogramForAifVM.Refresh(decResList, peakId);
        }

        public void FileChange(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, int peakId) {
            this.Ms2ChromatogramForAifVM.FileChange(commonProp, decResList, peakId);
        }
    }

    public class Ms2ChromatogramForAifVM
    {
        private Ms2ChromatogramForAIF ms2ChromatogramForAif;
        public AifViewControlCommonProperties CommonProp { get; set; }
        private int peakID;
        public PeakAreaBean Pab { get; set; }
        private List<MS2DecResult> mS2DecResList;

        public Ms2ChromatogramForAifVM() { }

        public Ms2ChromatogramForAifVM(Ms2ChromatogramForAIF ms2ChromatogramForAIF, AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, int peakId) {
            this.ms2ChromatogramForAif = ms2ChromatogramForAIF;
            this.CommonProp = commonProp;
            this.mS2DecResList = decResList;
            this.peakID = peakId;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
        }


        public void FileChange(AifViewControlCommonProperties commonProp, List<MS2DecResult> decResList, int peakId) {
            this.CommonProp = commonProp;
            this.peakID = peakId;
            this.mS2DecResList = decResList;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                var i = j > 0 ? (j + 1) / 2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.ms2ChromatogramForAif.MainViewer, j);
                var chromUC = (Ms2ChromatogramUC)VisualTreeHelper.GetChild(grid, 0);
                chromUC.Ms2ChromatogramVM.FileChange(commonProp, this.mS2DecResList[i], this.Pab);
                if (j > 0) j++;
            }
        }

        
        public void Refresh(List<MS2DecResult> decResList, int peakId) {
            this.peakID = peakId;
            this.mS2DecResList = decResList;
            this.Pab = CommonProp.AnalysisFile.PeakAreaBeanCollection[this.peakID];
            Refresh();
        }

        public void Refresh() {
            for (var j = 0; j < (CommonProp.NumDec * 2 - 1); j++) {
                var i = j > 0 ? (j + 1) / 2 : 0;
                var grid = (Grid)VisualTreeHelper.GetChild(this.ms2ChromatogramForAif.MainViewer, j);
                var chromUC = (Ms2ChromatogramUC)VisualTreeHelper.GetChild(grid, 0);
                chromUC.Ms2ChromatogramVM.Refresh(this.mS2DecResList[i], this.Pab);
                if (j > 0) j++;
            }
        }

    }
}
