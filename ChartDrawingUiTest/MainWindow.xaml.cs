using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using ChartDrawingUiTest.Default;
using ChartDrawingUiTest.LineChart;
using ChartDrawingUiTest.Scatter;
using ChartDrawingUiTest.Dendrogram;
using ChartDrawingUiTest.Heatmap;
using ChartDrawingUiTest.Compound;
using ChartDrawingUiTest.Chromatogram;

namespace ChartDrawingUiTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pageType = new Dictionary<string, Type>
            {
                {"Default1", typeof(Default1) },
                {"LineChart1", typeof(LineChart1) },
                {"LineChart2", typeof(LineChart2) },
                {"LineChart3", typeof(LineChart3) },
                {"LineChart4", typeof(LineChart4) },
                {"Scatter1", typeof(Scatter1) },
                {"Scatter2", typeof(Scatter2) },
                {"Scatter3", typeof(Scatter3) },
                {"Scatter4", typeof(Scatter4) },
                {"Dendrogram1", typeof(Dendrogram1) },
                {"Dendrogram2", typeof(Dendrogram2) },
                {"Dendrogram3", typeof(Dendrogram3) },
                {"Dendrogram4", typeof(Dendrogram4) },
                {"Dendrogram5", typeof(Dendrogram5) },
                {"DigraphView1", typeof(DigraphView1) },
                {"Heatmap1", typeof(Heatmap1) },
                {"Heatmap2", typeof(Heatmap2) },
                {"Heatmap3", typeof(Heatmap3) },
                {"Heatmap4", typeof(Heatmap4) },
                {"Clustermap1", typeof(Clustermap1) },
                {"Clustermap2", typeof(Clustermap2) },
                {"LineAndScatter1", typeof(LineAndScatter1) },
                {"LineAndScatter2", typeof(LineAndScatter2) },
                {"Chromatogram1", typeof(Chromatogram1) },
                // {"DrawingTest1", typeof(DrawingTest1) },
                // {"BindingTest1", typeof(BindingTest1) },
                // {"ClipTest1", typeof(ClipTest1) },
            };
            names = pageType.Keys.ToList();
            navbar.ItemsSource = names;
            pageMemo = new Dictionary<string, Page>();
            pageMemo[names[0]] = (Page)Activator.CreateInstance(pageType[names[0]]);
            sampleFrame.Navigate(pageMemo[names[0]]);
        }

        private List<string> names;
        private Dictionary<string, Page> pageMemo;
        private Dictionary<string, Type> pageType;

        private void navbar_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string item = (string)navbar.SelectedItem;
            if (!pageMemo.ContainsKey(item))
                pageMemo[item] = (Page)Activator.CreateInstance(pageType[item]); 
            sampleFrame.Navigate(pageMemo[item]);
        }
    }
}
