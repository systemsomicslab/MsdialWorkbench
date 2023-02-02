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

namespace ChartDrawingUiTest.LineSpectrum
{
    /// <summary>
    /// Interaction logic for LineSpectrum1.xaml
    /// </summary>
    public partial class LineSpectrum1 : Page
    {
        public LineSpectrum1()
        {
            InitializeComponent();
        }

        public void DataFilter(object sender, FilterEventArgs e)
        {
            if (((DataPoint)e.Item).Y >= 0)
                e.Accepted = true;
            else
                e.Accepted = false;
        }
    }
}
