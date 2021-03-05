using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using ChartDrawingUiTest.LineChart;
using ChartDrawingUiTest.AreaChart;
using ChartDrawingUiTest.Scatter;
using ChartDrawingUiTest.LineSpectrum;
using ChartDrawingUiTest.Dendrogram;
using ChartDrawingUiTest.Heatmap;
using ChartDrawingUiTest.Bar;
using ChartDrawingUiTest.Chromatogram;
using ChartDrawingUiTest.DataGrid;
using ChartDrawingUiTest.Controls;
using ChartDrawingUiTest.UI;
using ChartDrawingUiTest.Chart;

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
            Type[] pages = {
                typeof(LineChart4),
                typeof(AreaChart1),
                typeof(Scatter3),
                typeof(Scatter4),
                typeof(LineSpectrum1),
                typeof(LineSpectrum2),
                typeof(Dendrogram5),
                typeof(Heatmap4),
                typeof(Bar1),
                typeof(Chromatogram1),
                typeof(FileOpenControl1),
                typeof(DataGrid1),
                typeof(RangeSlider),
                typeof(ChartUpdate),
                typeof(TestPage),
                typeof(DependencyPropertyTest),
            };
            pageType = pages.ToDictionary(type => type.Name);
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
