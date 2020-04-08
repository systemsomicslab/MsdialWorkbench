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
using CompMs.RawDataHandler.Core;

namespace Rfx.Riken.OsakaUniv.MsViewer
{
    /// <summary>
    /// ScrollMs1ChromatogramForAif.xaml の相互作用ロジック
    /// </summary>
    public partial class ScrollMs1ChromatogramForAif : Window
    {
        private AifViewControlCommonProperties CommonProp;
        public ScrollMs1ChromatogramForAif() {
            InitializeComponent();
        }
        public ScrollMs1ChromatogramForAif(AifViewControlCommonProperties commonProp, AlignedData alignedEicData) {
            InitializeComponent();
            CommonProp = commonProp;
            var stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var chromatUI = new ChromatogramXicUI();
            var vms = UtilityForAIF.GetAlignedEicChromatogramList(alignedEicData, new AlignmentPropertyBean(), commonProp.AnalysisFiles, commonProp.ProjectProperty, commonProp.Param);

            for (var i = 0; i < alignedEicData.NumAnalysisFiles; i++) {
                chromatUI = new ChromatogramXicUI(vms[i]);
                var chromatogramGrid = new Grid() { Height = 150 , HorizontalAlignment = HorizontalAlignment.Stretch };
                chromatogramGrid.Children.Add(chromatUI);
                stackPanel.Children.Add(chromatogramGrid);
            }
            this.Scroll.Content = stackPanel;
            this.Scroll.ScrollToTop();
            this.Scroll.UpdateLayout();

        }

        public void FileChange(AifViewControlCommonProperties commonPorp, AlignedData alignedEicData, int peakID) {
            this.CommonProp = commonPorp;
            Refresh(alignedEicData, peakID);
        }

        public void Refresh(AlignedData alignedEicData, int peakID) {
            var stackPanel = new StackPanel() { Orientation = Orientation.Vertical };
            var chromatUI = new ChromatogramXicUI();
            var vms = UtilityForAIF.GetAlignedEicChromatogramList(alignedEicData, new AlignmentPropertyBean(), CommonProp.AnalysisFiles, CommonProp.ProjectProperty, CommonProp.Param);

            for (var i = 0; i < alignedEicData.NumAnalysisFiles; i++) {
                chromatUI = new ChromatogramXicUI(vms[i]);
                var chromatogramGrid = new Grid() { Height = 150, HorizontalAlignment = HorizontalAlignment.Stretch };
                chromatogramGrid.Children.Add(chromatUI);
                stackPanel.Children.Add(chromatogramGrid);
            }
            this.Scroll.Content = stackPanel;
            this.Scroll.UpdateLayout();
        }

    }
}
