using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Media;

using CompMs.Common.DataStructure;

namespace Rfx.Riken.OsakaUniv {
    public class MultivariateAnalysisResult {
        public MultivariateAnalysisResult() { }

        // model set
        public StatisticsObject StatisticsObject { get; set; } = new StatisticsObject();
        public MultivariateAnalysisOption MultivariateAnalysisOption { get; set; } = MultivariateAnalysisOption.Plsda;

        // cv result
        public int NFold { get; set; } = 7;
        public int OptimizedFactor { get; set; } = 0;
        public int OptimizedOrthoFactor { get; set; } = 0;
        public ObservableCollection<double> SsCVs { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Presses { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Totals { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Q2Values { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Q2Cums { get; set; } = new ObservableCollection<double>();


        // modeled set
        public ObservableCollection<double> SsPreds { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> CPreds { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double[]> UPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> TPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> WPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PPreds { get; set; } = new ObservableCollection<double[]>();

        public ObservableCollection<double> Coefficients { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> Vips { get; set; } = new ObservableCollection<double>();
        public ObservableCollection<double> PredictedYs { get; set; } = new ObservableCollection<double>();
        public double Rmsee { get; set; } = 0.0;

        // opls
        public ObservableCollection<double[]> ToPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> WoPreds { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PoPreds { get; set; } = new ObservableCollection<double[]>();
        public double stdevT { get; set; } = 0.0;
        public ObservableCollection<double> StdevFilteredXs { get; set; } = new ObservableCollection<double>();
        public double[,] FilteredXArray { get; set; }
        public ObservableCollection<double[]> PPredCovs { get; set; } = new ObservableCollection<double[]>();
        public ObservableCollection<double[]> PPredCoeffs { get; set; } = new ObservableCollection<double[]>();

        // pca
        public ObservableCollection<double> Contributions { get; set; } = new ObservableCollection<double>();

        // hca
        public DirectedTree XDendrogram { get; set; }
        public DirectedTree YDendrogram { get; set; }
    }
}