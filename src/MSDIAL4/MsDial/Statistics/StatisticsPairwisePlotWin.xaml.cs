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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for StatisticsPairwisePlotWin.xaml
    /// </summary>
    public partial class StatisticsPairwisePlotWin : Window
    {
        public StatisticsPairwisePlotWin(PairwisePlotUI pairwisePlotView)
        {
            InitializeComponent();
            this.DataContext = new StatisticsPairwisePlotVM(pairwisePlotView);
        }
    }

    public class StatisticsPairwisePlotVM : ViewModelBase {
        public PairwisePlotUI PairwisePlotView { get; set; }

        public StatisticsPairwisePlotVM(PairwisePlotUI pairwisePlotView) {
            this.PairwisePlotView = pairwisePlotView;
        }
    }
}
