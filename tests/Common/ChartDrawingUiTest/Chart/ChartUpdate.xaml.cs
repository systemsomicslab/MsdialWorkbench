using CompMs.Graphics.Core.Base;
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

namespace ChartDrawingUiTest.Chart
{
    /// <summary>
    /// Interaction logic for ChartUpdate.xaml
    /// </summary>
    public partial class ChartUpdate : Page
    {
        public ChartUpdate()
        {
            InitializeComponent();
        }

        private void Button_Click03(object sender, RoutedEventArgs e)
        {
            var axis = this.Resources["HorizontalAxis"] as IAxisManager;
            axis.Focus(new AxisRange(0, 3));
        }

        private void Button_Click12(object sender, RoutedEventArgs e)
        {
            var axis = this.Resources["HorizontalAxis"] as IAxisManager;
            axis.Focus(new AxisRange(1, 2));
        }

        private void Button_Click13(object sender, RoutedEventArgs e)
        {
            var axis = this.Resources["HorizontalAxis"] as IAxisManager;
            axis.Focus(new AxisRange(1, 3));
        }

        private void Button_Click02(object sender, RoutedEventArgs e)
        {
            var axis = this.Resources["HorizontalAxis"] as IAxisManager;
            axis.Focus(new AxisRange(0, 2));
        }
    }
}
