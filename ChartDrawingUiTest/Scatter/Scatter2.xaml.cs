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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChartDrawingUiTest.Scatter
{
    /// <summary>
    /// Scatter2.xaml の相互作用ロジック
    /// </summary>
    public partial class Scatter2 : Page
    {
        public Scatter2()
        {
            InitializeComponent();
        }

        private void scatter_Loaded(object sender, RoutedEventArgs e)
        {
            scatter.ChartArea = scatter.InitialArea;
        }
    }
}
