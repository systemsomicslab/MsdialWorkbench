using CompMs.Graphics.Core.Base;
using System;
using System.CodeDom;
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
    /// Scatter3.xaml の相互作用ロジック
    /// </summary>
    public partial class Scatter3 : Page
    {
        public Scatter3()
        {
            InitializeComponent();
        }

        public void DataFilter(object sender, FilterEventArgs e)
        {
            if (e.Item != null && ((DataPoint)e.Item).Y >= 0)
                e.Accepted = true;
            else
                e.Accepted = false;
        }

    }
}
