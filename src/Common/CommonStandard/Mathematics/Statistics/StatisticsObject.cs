using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.Mathematics.Statistics {
    public class BasicStats {
        public int ID { get; set; }
        public double Average { get; set; }
        public double Stdev { get; set; }
        public string Legend { get; set; }
        public double MinValue { get; set; }
        public double TwentyFiveValue { get; set; }
        public double Median { get; set; }
        public double SeventyFiveValue { get; set; }
        public double MaxValue { get; set; }
    }
    public class StatisticsObject {

        public double[] YVariables { get; set; } = null; // files
        public double[] YTransformed { get; set; } = null;
        public double[] YScaled { get; set; } = null;
        public double YMean { get; set; } = 0;
        public double YStdev { get; set; } = 0;

        public double[,] XDataMatrix { get; set; } = null; // [row] files [column] metabolites
        public double[,] XTransformed { get; set; } = null;
        public double[,] XScaled { get; set; } = null;
        public double[] XMeans { get; set; } = null;
        public double[] XStdevs { get; set; } = null;

        public ObservableCollection<int> YIndexes { get; set; } = new ObservableCollection<int>();
        public ObservableCollection<int> XIndexes { get; set; } = new ObservableCollection<int>();

        public ObservableCollection<string> YLabels { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> YLabels2 { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> XLabels { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<byte[]> YColors { get; set; } = new ObservableCollection<byte[]>(); // [0] R [1] G [2] B [3] A
        public ObservableCollection<byte[]> XColors { get; set; } = new ObservableCollection<byte[]>(); // [0] R [1] G [2] B [3] A

        public ScaleMethod Scale { get; set; } = ScaleMethod.AutoScale;
        public TransformMethod Transform { get; set; } = TransformMethod.None;
        
        public void StatInitialization() {

            int rowSize = this.XDataMatrix.GetLength(0); // files
            int columnSize = this.XDataMatrix.GetLength(1); // metabolites

            this.YTransformed = new double[rowSize];
            this.YScaled = new double[rowSize];

            this.XTransformed = new double[rowSize, columnSize];
            this.XScaled = new double[rowSize, columnSize];

            switch (this.Transform) {
                case TransformMethod.None:
                    this.XTransformed = this.XDataMatrix;
                    //this.YTransformed = this.YVariables;
                    break;
                case TransformMethod.Log10:
                    this.XTransformed = StatisticsMathematics.LogTransform(this.XDataMatrix);
                    //this.YTransformed = StatisticsMathematics.LogTransform(this.YVariables);
                    break;
                case TransformMethod.QuadRoot:
                    this.XTransformed = StatisticsMathematics.QuadRootTransform(this.XDataMatrix);
                    //this.YTransformed = StatisticsMathematics.QuadRootTransform(this.YVariables);
                    break;
                default:
                    break;
            }
            this.YTransformed = this.YVariables;

            double[] xMeans, xStdevs;
            StatisticsMathematics.StatisticsProperties(this.XTransformed, out xMeans, out xStdevs);
            this.XMeans = xMeans;
            this.XStdevs = xStdevs;

            double yMean, yStdev;
            StatisticsMathematics.StatisticsProperties(this.YTransformed, out yMean, out yStdev);
            this.YMean = yMean;
            this.YStdev = yStdev;

            switch (this.Scale) {
                case ScaleMethod.None:
                    //this.YScaled = this.YTransformed;
                    this.XScaled = this.XTransformed;
                    break;
                case ScaleMethod.MeanCenter:
                    //this.YScaled = StatisticsMathematics.MeanCentering(this.YTransformed, this.YMean);
                    this.XScaled = StatisticsMathematics.MeanCentering(this.XTransformed, this.XMeans);
                    break;
                case ScaleMethod.ParetoScale:
                    //this.YScaled = StatisticsMathematics.ParetoScaling(this.YTransformed, this.YMean, this.YStdev);
                    this.XScaled = StatisticsMathematics.ParetoScaling(this.XTransformed, this.XMeans, this.XStdevs);
                    break;
                case ScaleMethod.AutoScale:
                    //this.YScaled = StatisticsMathematics.AutoScaling(this.YTransformed, this.YMean, this.YStdev);
                    this.XScaled = StatisticsMathematics.AutoScaling(this.XTransformed, this.XMeans, this.XStdevs);
                    break;
                default:
                    break;
            }
            this.YScaled = this.YTransformed;

        }

        public double YBackTransform(double scaledY) {
            return scaledY + this.YMean;
            //var backY = scaledY;
            //switch (this.Scale) {
            //    case ScaleMethod.None:
            //        break;
            //    case ScaleMethod.MeanCenter:
            //        backY = scaledY + this.YMean;
            //        break;
            //    case ScaleMethod.ParetoScale:
            //        backY = scaledY * Math.Sqrt(this.YStdev) + this.YMean;
            //        break;
            //    case ScaleMethod.AutoScale:
            //        backY = scaledY * this.YStdev + this.YMean;
            //        break;
            //    default:
            //        break;
            //}

            //switch (this.Transform) {
            //    case TransformMethod.None:
            //        break;
            //    case TransformMethod.Log10:
            //        backY = Math.Pow(10, backY);
            //        break;
            //    case TransformMethod.QuadRoot:
            //        backY = Math.Pow(backY, 4);
            //        break;
            //    default:
            //        break;
            //}

            //return backY;
        }

        public double[] CopyY() {
            var size = this.YScaled.Length;
            var copyY = new double[size];
            for (int i = 0; i < size; i++) {
                copyY[i] = this.YScaled[i];
            }
            return copyY;
        }

        public double[,] CopyX() {
            int rowSize = this.XDataMatrix.GetLength(0); // files
            int columnSize = this.XDataMatrix.GetLength(1); // metabolites
            var copyX = new double[rowSize, columnSize];
            for (int i = 0; i < rowSize; i++) {
                for (int j = 0; j < columnSize; j++) {
                    copyX[i, j] = this.XScaled[i, j];
                }
            }
            return copyX;
        }

        public int RowSize() {
            return this.XDataMatrix.GetLength(0);
        }

        public int ColumnSize() {
            return this.XDataMatrix.GetLength(1);
        }
    }
}
