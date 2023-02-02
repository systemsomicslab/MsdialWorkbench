using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for BrowsePeakDetectiomMethodWin.xaml
    /// </summary>
    public partial class BrowseSmootherAndBaselineCorrectionWin : Window {
        public BrowseSmootherAndBaselineCorrectionWin(ChromatogramAlignedEicUI chromatogramsView) {
            InitializeComponent();
            this.DataContext = new BrowseSmootherAndBaselineCorrectionVM(chromatogramsView);
        }


    }

    public class BrowseSmootherAndBaselineCorrectionVM : ViewModelBase {
        public ChromatogramAlignedEicUI ChromatogramsView { get; set; }

        public BrowseSmootherAndBaselineCorrectionVM(ChromatogramAlignedEicUI chromatogramsView) {
            this.ChromatogramsView = chromatogramsView;
        }
    }
}
