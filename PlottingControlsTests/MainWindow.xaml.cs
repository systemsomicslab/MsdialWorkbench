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

using PlottingControlsTests.Base;
using PlottingControlsTests.Dendrogram;

namespace PlottingControlsTests
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ps = new List<(string name, Page page)> {
                ("PlottingBase", new BaseTest()),
                ("Dendrogram1", new DendrogramTest1()),
                ("Dendrogram2", new DendrogramTest2()),
                ("Dendrogram3", new DendrogramTest3())

            };
            navbar.ItemsSource = ps.Select(p => p.name);
        }

        private List<(string name, Page page)> ps;

        private void navbar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            sampleFrame.Navigate(ps[navbar.SelectedIndex].page);
        }
    }
}
