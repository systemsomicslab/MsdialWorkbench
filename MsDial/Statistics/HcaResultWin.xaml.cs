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

using Msdial.Dendrogram;
using Msdial.Heatmap;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for HcaResultWin.xaml
    /// </summary>
    public partial class HcaResultWin : Window
    {
        public HcaResultWin(MultivariateAnalysisResult result)
        {
            InitializeComponent();
            this.DataContext = new HcaResultVM(result);
        }
    }

    class HcaResultVM : ViewModelBase
    {
        public DendrogramPlotBean YDendrogramPlotBean
        {
            get => ydendrogramBean;
            set
            {
                if (ydendrogramBean == value) return;
                OnPropertyChanged("YDendrogramPlotBean");
            }
        }

        public HeatmapBean HeatmapBean
        {
            get => heatmapBean;
            set
            {
                if (heatmapBean == value) return;
                OnPropertyChanged("HeatmapBean");
            }
        }

        public HcaResultVM(MultivariateAnalysisResult result)
        {
            ydendrogramBean = new DendrogramPlotBean("", result.StatisticsObject.YLabels, result.Dendrogram, result.Root);
            var rank = ydendrogramBean.Rank;
            var ylabels = rank.Zip(result.StatisticsObject.YLabels, Tuple.Create).OrderBy(p => p.Item1).Select(p => p.Item2);
            var xsize = result.StatisticsObject.XDataMatrix.GetLength(1);
            var ysize = result.StatisticsObject.XDataMatrix.GetLength(0);
            var data = new double[ysize, xsize];
            var yIdx = Enumerable.Range(0, result.StatisticsObject.YLabels.Count)
                .Zip(rank, Tuple.Create).OrderBy(p => p.Item2)
                .Select(p => p.Item1).ToArray();
            for (var i = 0; i < xsize; ++i)
            {
                for(var j = 0; j < ysize; ++j)
                {
                    data[yIdx[j], i] = result.StatisticsObject.XDataMatrix[j, i];
                }
            }
            heatmapBean = new HeatmapBean(data, ylabels, result.StatisticsObject.XLabels);
        }

        // private DendrogramPlotBean xdendrogramBean;
        private DendrogramPlotBean ydendrogramBean;
        private HeatmapBean heatmapBean;
    }
}
