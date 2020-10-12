using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls.Ribbon;
using Rfx.Riken.OsakaUniv;

namespace MsdialDimsCoreUiTestApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CollectionViewSource_Ref_Filter(object sender, System.Windows.Data.FilterEventArgs e) {
            var info = e.Item as Ms2Info;
            if (info != null) {
                e.Accepted = info.RefMatched;
            }
        }

        private void CollectionViewSource_Suggested_Filter(object sender, System.Windows.Data.FilterEventArgs e) {
            var info = e.Item as Ms2Info;
            if (info != null) {
                e.Accepted = info.Suggested;
            }
        }

        private void SaveAnnotationResult(object sender, RoutedEventArgs e) {
            var items = PeaksSelector.SelectedValue as IEnumerable<Ms2Info>;
            if (items == null) return;
            using (var writer = new StreamWriter(File.OpenWrite(@"C:\Users\YUKI MATSUZAWA\Desktop\annotation_result.csv"))) {
                writer.WriteLine("Name,Mass,Intensity,RefMatched,Ms2Acquired");
                foreach(var item in items) {
                    writer.WriteLine($"{item.ChromatogramPeakFeature.Name},{item.Mass},{item.Intensity},{item.RefMatched},{item.Ms2Acquired}");
                }
            }
        }
    }
}
