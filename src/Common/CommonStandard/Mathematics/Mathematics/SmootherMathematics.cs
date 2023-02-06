using CompMs.Common.Mathematics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Mathematics.Basic {
    public sealed class SmootherMathematics
    {
        private SmootherMathematics() { }

        public static double[] Lowess(double[] x, double[] y, double span, int iter)
        {
            int includeNumber = (int)(x.Length * span);
            double[] g = new double[x.Length]; for (int i = 0; i < x.Length; i++) { g[i] = 1; }

            List<List<double[]>> neighborPointsList = new List<List<double[]>>();
            List<double[]> neighborPoints;
            for (int i = 0; i < x.Length; i++)
            {
                neighborPoints = getNeighborhoodPoints(x, i, includeNumber);
                neighborPointsList.Add(neighborPoints);
            }
            double median;
            double[] xValue, yValue, wValue, yPredArray, yResiduals;
            yResiduals = yPredArray = new double[y.Length];

            for (int i = 0; i < iter; i++)
            {
                for (int j = 0; j < neighborPointsList.Count; j++)
                {
                    xValue = new double[neighborPointsList[j].Count];
                    yValue = new double[neighborPointsList[j].Count];
                    wValue = new double[neighborPointsList[j].Count];
                    
                    for (int k = 0; k < neighborPointsList[j].Count; k++)
                    {
                        xValue[k] = neighborPointsList[j][k][2];
                        yValue[k] = y[(int)neighborPointsList[j][k][0]];
                        wValue[k] = neighborPointsList[j][k][5] * g[(int)neighborPointsList[j][k][0]];
                    }
                    
                    yPredArray[j] = RegressionMathematics.WeightedLeastSquaresRegression(wValue, xValue, yValue, "linear")[1];
                }

                yResiduals = BasicMathematics.getResidualArrayByAbs(y, yPredArray);
                median = BasicMathematics.Median(yResiduals);

                for (int j = 0; j < g.Length; j++)
                {
                    if (yResiduals[j] < 6 * median) g[j] = Math.Pow(1 - Math.Pow(yResiduals[j] / 6 / median, 2), 2);
                    else g[j] = 0;
                }
            }

            return yPredArray;
        }

        public static double[] Loess(double[] x, double[] y, double span, int iter)
        {
            int includeNumber = (int)(x.Length * span);
            double[] g = new double[x.Length]; for (int i = 0; i < x.Length; i++) { g[i] = 1; }

            List<List<double[]>> neighborPointsList = new List<List<double[]>>();
            List<double[]> neighborPoints;
            for (int i = 0; i < x.Length; i++)
            {
                neighborPoints = getNeighborhoodPoints(x, i, includeNumber);
                neighborPointsList.Add(neighborPoints);
            }
            double median;
            double[] xValue, yValue, wValue, yPredArray, yResiduals;
            yResiduals = yPredArray = new double[y.Length];

            for (int i = 0; i < iter; i++)
            {
                for (int j = 0; j < neighborPointsList.Count; j++)
                {
                    xValue = new double[neighborPointsList[j].Count];
                    yValue = new double[neighborPointsList[j].Count];
                    wValue = new double[neighborPointsList[j].Count];
                    
                    for (int k = 0; k < neighborPointsList[j].Count; k++)
                    {
                        xValue[k] = neighborPointsList[j][k][2];
                        yValue[k] = y[(int)neighborPointsList[j][k][0]];
                        wValue[k] = neighborPointsList[j][k][5] * g[(int)neighborPointsList[j][k][0]];
                    }
                    yPredArray[j] = RegressionMathematics.WeightedLeastSquaresRegression(wValue, xValue, yValue, "quand")[2];
                }

                yResiduals = BasicMathematics.getResidualArrayByAbs(y, yPredArray);
                median = BasicMathematics.Median(yResiduals);

                for (int j = 0; j < g.Length; j++)
                {
                    if (yResiduals[j] < 6 * median) g[j] = Math.Pow(1 - Math.Pow(yResiduals[j] / 6 / median, 2), 2);
                    else g[j] = 0;
                }
            }
            return yPredArray;
        }

        public static double[] Spline(double[] x, double[] y, double yp1, double ypn)
        {
            double[] y2 = new double[x.Length];
            double[] u = new double[x.Length - 1];

            if (yp1 > 0.99 * Math.Pow(2, 30)) { y2[0] = 0; u[0] = 0; }
            else { y2[0] = -0.5; u[0] = (3.0 / (x[1] - x[0])) * ((y[1] - y[0]) / (x[1] - x[0]) - yp1); }

            double sig = 0, p = 0;
            for (int i = 1; i < x.Length - 1; i++)
            {
                sig = (x[i] - x[i - 1]) / (x[i + 1] - x[i - 1]);
                p = sig * y2[i - 1] + 2.0;
                y2[i] = (sig - 1.0) / p;
                u[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]) - (y[i] - y[i - 1]) / (x[i] - x[i - 1]);
                u[i] = (6.0 * u[i] / (x[i + 1] - x[i - 1]) - sig * u[i - 1]) / p;
            }

            double qn = 0, un = 0;
            if (ypn > 0.99 * Math.Pow(2, 30)) { qn = 0.0; un = 0.0; }
            else { qn = 0.5; un = (3.0 / (x[x.Length - 1] - x[x.Length - 2])) * (ypn - (y[y.Length - 1] - y[x.Length - 2]) / (x[x.Length - 1] - x[x.Length - 2])); }

            y2[y2.Length - 1] = (un - qn * u[y.Length - 2]) / (qn * y2[y.Length - 2] + 1.0);
            for (int k = x.Length - 2; k >= 0; k--)
            {
                y2[k] = y2[k] * y2[k + 1] + u[k];
            }
            return y2;
        }

        public static double Splint(double[] xa, double[] ya, double[] y2a, double x)
        {
            int klo = 0, khi = xa.Length - 1, k;
            double h, b, a;

            while (khi - klo > 1)
            {
                k = (int)((khi + klo) / 2);
                if (xa[k] > x) khi = k;
                else klo = k;
            }

            h = xa[khi] - xa[klo];
            if (h == 0.0) 
            { 
                Console.WriteLine("Bad xa input to toutine splint"); return 0; 
            }

            a = (xa[khi] - x) / h;
            b = (x - xa[klo]) / h;

            return a * ya[klo] + b * ya[khi] + ((a * a * a - a) * y2a[klo] + (b * b * b - b) * y2a[khi]) * (h * h) / 6.0;
        }

        private static List<double[]> getNeighborhoodPoints(double[] x, int index, int neighborNumber)
        {
            List<double[]> neighborCandidate = new List<double[]>();
            for (int i = -neighborNumber; i <= neighborNumber; i++)
            {
                if (index + i < 0) continue;
                if (index + i > x.Length - 1) break;
                neighborCandidate.Add(new double[] { index + i, x[index + i], x[index + i] - x[index], Math.Abs(x[index + i] - x[index]) });
            }
            neighborCandidate = neighborCandidate.OrderBy(n => n[3]).ToList();

            List<double[]> neighborList = new List<double[]>();
            for (int i = 0; i < neighborNumber; i++)
                neighborList.Add(new double[] { neighborCandidate[i][0], neighborCandidate[i][1], neighborCandidate[i][2], neighborCandidate[i][3], 0, 0 });

            double maxDistance = neighborList[neighborList.Count - 1][3];
            for (int i = 0; i < neighborList.Count; i++)
            {
                if (maxDistance != 0)
                {
                    neighborList[i][4] = neighborList[i][3] / maxDistance;
                    neighborList[i][5] = Math.Pow(1 - Math.Pow(neighborList[i][4], 3), 3);
                }
            }
            neighborList = neighborList.OrderBy(n => n[0]).ToList();

            return neighborList;
        }

        public static double GetMinimumLowessSpan(int sampleSize)
        {
            double sSize = (double)sampleSize;
            double lamda = 1.5;

            double minOptSize = (lamda + 1.0) / sSize;
            minOptSize = Math.Round(minOptSize, 2) + 0.01;

            if (minOptSize > 1) return 1.0;

            return minOptSize;
        }

        public static double GetOptimalLowessSpanByCrossValidation(IReadOnlyList<double> x, IReadOnlyList<double> y, double minSpan, double spanStep, int iter, int fold)
        {
            var optSpan = 0.7;
            var minDevi = double.MaxValue;
            if (iter < 3) iter = 3; if (fold < 2) fold = 7;

            var span = minSpan;
            if (x.Count == 0) return span;
            var minTrainSpan = getMinimumTrainSpan(x, fold);
            if (span < minTrainSpan) span = minTrainSpan;

            while (span <= 1.0)
            {
                var counter = 0;
                var devi = 0.0;
                
                for (int i = 0; i < fold; i++)
                {
                    var xTrain = StatisticsMathematics.GetCrossValidationTrainArray(x, i, fold);
                    var xTest = StatisticsMathematics.GetCrossValidationTestArray(x, i, fold);

                    if (xTrain == null || xTest == null) continue;
                    if (xTrain.Length < 2) continue;

                    var yTrain = StatisticsMathematics.GetCrossValidationTrainArray(y, i, fold);
                    var yTest = StatisticsMathematics.GetCrossValidationTestArray(y, i, fold);

                    if (yTrain == null || yTest == null) continue;


                    var yLoessPreArray = SmootherMathematics.Lowess(xTrain, yTrain, span, iter);
                    var ySplineDeviArray = SmootherMathematics.Spline(xTrain, yLoessPreArray, double.MaxValue, double.MaxValue);
                    var baseQcValue = yTrain[0];
                    var fittedValue = 0.0;
                    var aveQcValue = BasicMathematics.Mean(yTrain);

                    for (int j = 0; j < xTest.Length; j++)
                    {
                        fittedValue = SmootherMathematics.Splint(xTrain, yLoessPreArray, ySplineDeviArray, xTest[j]);
                        if (fittedValue <= 0) fittedValue = aveQcValue;
                        devi += Math.Pow(fittedValue - yTest[j], 2);
                    }
                    devi = devi / (double)xTest.Length;
                    counter++;
                }

                if (counter != 0)
                {
                    devi = devi / counter;
                    if (minDevi > devi) { minDevi = devi; optSpan = span; }
                }
                span += spanStep;
            }
            
            return Math.Round(optSpan, 3);
        }

        private static double getMinimumTrainSpan(IReadOnlyList<double> x, int fold)
        {
            var xTrain = StatisticsMathematics.GetCrossValidationTrainArray(x, 0, fold);
            var minSpan = GetMinimumLowessSpan(xTrain.Length);
            return minSpan;
        }
    }
}
