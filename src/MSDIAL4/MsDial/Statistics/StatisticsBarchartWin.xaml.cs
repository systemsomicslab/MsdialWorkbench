using Common.BarChart;
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
    /// Interaction logic for StatisticsBarchartWin.xaml
    /// </summary>
    public partial class StatisticsBarchartWin : Window
    {
        public StatisticsBarchartWin(BarChartUI barChartView)
        {
            InitializeComponent();
            this.DataContext = new StatisticsBarchartVM(barChartView);
        }
    }

    public class StatisticsBarchartVM : ViewModelBase {
        public BarChartUI BarChartView { get; set; }

        public StatisticsBarchartVM(BarChartUI barChartView) {
            this.BarChartView = barChartView;
        }
    }
}
