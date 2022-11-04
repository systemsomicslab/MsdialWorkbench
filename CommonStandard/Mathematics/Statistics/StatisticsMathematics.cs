using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Mathematics.Matrix;

namespace CompMs.Common.Mathematics.Statistics {
    public sealed class StatisticsMathematics
    {
        private StatisticsMathematics() { }

        public static double[] GetCrossValidationTrainArray(IReadOnlyList<double> x, int index, int fold)
        {
            var list = new List<double>();
            for (int i = 0; i < x.Count; i++)
            {
                var rem = i % fold;
                if (rem != index) list.Add(x[i]);
            }

            if (list.Count > 0) return list.ToArray();
            else return null;
        }

        public static double[] GetCrossValidationTestArray(IReadOnlyList<double> x, int index, int fold)
        {
            var list = new List<double>();
            for (int i = 0; i < x.Count; i++)
            {
                var rem = i % fold;
                if (rem == index) list.Add(x[i]);
            }

            if (list.Count > 0) return list.ToArray();
            else return null;
        }

        public static double[] PolynomialFitting(double[] w, double[] x, double[] y, int degree)
        {
            if (degree == 1)
            {
                double[,] inverseMatrix = new double[2, 2];
                double a, b, c, d, detA;
                double coefficientA, coefficientB;

                a = 0; b = 0; c = 0; d = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    a += Math.Pow(x[i], 2) * w[i];
                    b += x[i] * w[i];
                    c += x[i] * w[i];
                    d += w[i];
                }
                detA = a * d - b * c;
                coefficientA = coefficientB = 0;

                if (detA != 0)
                {
                    inverseMatrix[0, 0] = d / detA;
                    inverseMatrix[0, 1] = (-1) * b / detA;
                    inverseMatrix[1, 0] = (-1) * c / detA;
                    inverseMatrix[1, 1] = a / detA;

                    a = b = 0;
                    for (int j = 0; j < y.Length; j++)
                    {
                        a += y[j] * x[j] * w[j];
                        b += y[j] * w[j];
                    }

                    coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                    coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                }
                else
                {
                    double sum = 0;
                    for (int j = 0; j < y.Length; j++)
                    {
                        sum += y[j];
                    }
                    coefficientA = 0;
                    coefficientB = sum / y.Length;
                }
                return new double[] { coefficientA, coefficientB };
            }
            else if (degree == 2)
            {
                double a11, a12, a13, a21, a22, a23, a31, a32, a33, detA, a, b, c;
                double[,] inverseMatrix = new double[3, 3];
                double coefficientA, coefficientB, coefficientC;
                a11 = 0; a12 = 0; a13 = 0; a21 = 0; a22 = 0; a23 = 0; a31 = 0; a32 = 0; a33 = 0;
                for (int i = 0; i < x.Length; i++)
                {
                    a11 += Math.Pow(x[i], 4) * w[i];
                    a12 += Math.Pow(x[i], 3) * w[i];
                    a13 += Math.Pow(x[i], 2) * w[i];
                    a21 += Math.Pow(x[i], 3) * w[i];
                    a22 += Math.Pow(x[i], 2) * w[i];
                    a23 += x[i] * w[i];
                    a31 += Math.Pow(x[i], 2) * w[i];
                    a32 += x[i] * w[i];
                    a33 += w[i];
                }
                detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;

                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                if (detA != 0)
                {
                    inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
                    inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
                    inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
                    inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
                    inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
                    inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
                    inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
                    inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
                    inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

                    for (int i = 0; i < y.Length; i++)
                    {
                        a += y[i] * Math.Pow(x[i], 2) * w[i];
                        b += y[i] * x[i] * w[i];
                        c += y[i] * w[i];
                    }

                    coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                    coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                    coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;
                }
                else
                {
                    double sum = 0;
                    for (int i = 0; i < y.Length; i++) { sum += y[i]; }
                    coefficientA = 0;
                    coefficientB = 0;
                    coefficientC = sum / y.Length;
                }
                return new double[] { coefficientA, coefficientB, coefficientC };
            }
            else
            {
                return null;
            }
        }

        public static void StatisticsProperties(double[,] dataArray, out double[] mean, out double[] stdev) {

            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            mean = new double[columnSize];
            stdev = new double[columnSize];
            double sum;

            for (int i = 0; i < columnSize; i++) {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += dataArray[j, i]; }
                mean[i] = sum / rowSize;
            }

            for (int i = 0; i < columnSize; i++) {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += Math.Pow(dataArray[j, i] - mean[i], 2); }
                stdev[i] = Math.Sqrt(sum / (rowSize - 1));
            }
        }

        public static void StatisticsProperties(double[] dataArray, out double mean, out double stdev) {
            int size = dataArray.Length;

            mean = 0.0;
            stdev = 0.0;
            var sum = 0.0;
            for (int i = 0; i < size; i++) {
                sum += dataArray[i];
            }
            mean = sum / size;

            sum = 0.0;
            for (int i = 0; i < size; i++) {
                sum += Math.Pow(dataArray[i] - mean, 2);
            }
            stdev = Math.Sqrt(sum / (size - 1));
        }

        #region scaling
        public static double[,] MeanCentering(double[,] dataArray)
        {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            double[] mean = new double[columnSize];
            double sum;

            for (int i = 0; i < columnSize; i++)
            {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += dataArray[j, i]; }
                mean[i] = sum / rowSize;
            }

            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++)
                    resultArray[j, i] = dataArray[j, i] - mean[i];

            return resultArray;
        }

        public static double[,] MeanCentering(double[,] dataArray, double[] mean) {

            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];

            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++)
                    resultArray[j, i] = dataArray[j, i] - mean[i];

            return resultArray;
        }


        public static double[] MeanCentering(double[] dataArray) {
            var sum = 0.0;
            for (int i = 0; i < dataArray.Length; i++) {
                sum += dataArray[i];
            }
            var mean = sum / (double)dataArray.Length;

            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++)
                processedArray[i] = dataArray[i] - mean;
            return processedArray;
        }

        public static double[] MeanCentering(double[] dataArray, double mean) {
            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++)
                processedArray[i] = dataArray[i] - mean;
            return processedArray;
        }

        public static double[,] ParetoScaling(double[,] dataArray)
        {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            double[] mean = new double[columnSize];
            double[] stdev = new double[columnSize];
            double sum;

            for (int i = 0; i < columnSize; i++)
            {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += dataArray[j, i]; }
                mean[i] = sum / rowSize;
            }

            for (int i = 0; i < columnSize; i++)
            {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += Math.Pow(dataArray[j, i] - mean[i], 2); }
                stdev[i] = Math.Sqrt(sum / (rowSize - 1));
            }

            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++)
                {
                    if (stdev[i] != 0)
                        resultArray[j, i] = (dataArray[j, i] - mean[i]) / Math.Sqrt(stdev[i]);
                    else
                        resultArray[j, i] = (dataArray[j, i] - mean[i]);
                }

            return resultArray;
        }

        public static double[,] ParetoScaling(double[,] dataArray, double[] mean, double[] stdev) {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            var resultArray = new double[rowSize, columnSize];
            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++) {
                    if (stdev[i] != 0)
                        resultArray[j, i] = (dataArray[j, i] - mean[i]) / Math.Sqrt(stdev[i]);
                    else
                        resultArray[j, i] = (dataArray[j, i] - mean[i]);
                }

            return resultArray;
        }

        public static double[] ParetoScaling(double[] dataArray) {
            var sum = 0.0;
            var size = (double)dataArray.Length;
            for (int i = 0; i < dataArray.Length; i++) {
                sum += dataArray[i];
            }
            var mean = sum / size;

            sum = 0.0;
            for (int i = 0; i < dataArray.Length; i++) {
                sum += Math.Pow(dataArray[i] - mean, 2);
            }
            var stdev = Math.Sqrt(sum / (size - 1.0));

            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++) {
                if (stdev == 0) {
                    processedArray[i] = dataArray[i] - mean;
                }
                else {
                    processedArray[i] = (dataArray[i] - mean) / Math.Sqrt(stdev);
                }
            }
            return processedArray;
        }

        public static double[] ParetoScaling(double[] dataArray, double mean, double stdev) {
            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++) {
                if (stdev == 0) {
                    processedArray[i] = dataArray[i] - mean;
                }
                else {
                    processedArray[i] = (dataArray[i] - mean) / Math.Sqrt(stdev);
                }
            }
            return processedArray;
        }

        public static double[,] AutoScaling(double[,] dataArray)
        {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            double[] mean = new double[columnSize];
            double[] stdev = new double[columnSize];
            double sum;

            for (int i = 0; i < columnSize; i++)
            {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += dataArray[j, i]; }
                mean[i] = sum / rowSize;
            }

            for (int i = 0; i < columnSize; i++)
            {
                sum = 0;
                for (int j = 0; j < rowSize; j++) { sum += Math.Pow(dataArray[j, i] - mean[i], 2); }
                stdev[i] = Math.Sqrt(sum / (rowSize - 1));
            }

            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++)
                {
                    if (stdev[i] != 0)
                        resultArray[j, i] = (dataArray[j, i] - mean[i]) / stdev[i];
                    else
                        resultArray[j, i] = (dataArray[j, i] - mean[i]);

                }

            return resultArray;
        }

        public static double[,] AutoScaling(double[,] dataArray, double[] mean, double[] stdev) {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            for (int i = 0; i < columnSize; i++)
                for (int j = 0; j < rowSize; j++) {
                    if (stdev[i] != 0)
                        resultArray[j, i] = (dataArray[j, i] - mean[i]) / stdev[i];
                    else
                        resultArray[j, i] = (dataArray[j, i] - mean[i]);

                }

            return resultArray;
        }

        public static double[] AutoScaling(double[] dataArray) {
            var sum = 0.0;
            var size = (double)dataArray.Length;
            for (int i = 0; i < dataArray.Length; i++) {
                sum += dataArray[i];
            }
            var mean = sum / size;

            sum = 0.0;
            for (int i = 0; i < dataArray.Length; i++) {
                sum += Math.Pow(dataArray[i] - mean, 2);
            }
            var stdev = Math.Sqrt(sum / (size - 1.0));

            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++) {
                if (stdev == 0) {
                    processedArray[i] = dataArray[i] - mean;
                }
                else {
                    processedArray[i] = (dataArray[i] - mean) / stdev;
                }
            }
            return processedArray;
        }

        public static double[] AutoScaling(double[] dataArray, double mean, double stdev) {
            var processedArray = new double[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++) {
                if (stdev == 0) {
                    processedArray[i] = dataArray[i] - mean;
                }
                else {
                    processedArray[i] = (dataArray[i] - mean) / stdev;
                }
            }
            return processedArray;
        }


        public static double[,] LogTransform(double[,] dataArray)
        {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            for (int i = 0; i < rowSize; i++) {
                for (int j = 0; j < columnSize; j++) {
                    if (dataArray[i, j] > 0)
                        resultArray[i, j] = Math.Log10(dataArray[i, j]);
                    else
                        resultArray[i, j] = 0;
                }
            }
            return resultArray;
        }

        public static double[] LogTransform(double[] dataArray) {
            var size = dataArray.Length;
            var resultArray = new double[size];
            for (int i = 0; i < size; i++) {
                if (dataArray[i] > 0)
                    resultArray[i] = Math.Log10(dataArray[i]);
                else
                    resultArray[i] = 0;
            }
            return resultArray;
        }

        public static double[,] QuadRootTransform(double[,] dataArray)
        {
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);

            double[,] resultArray = new double[rowSize, columnSize];
            for (int i = 0; i < rowSize; i++) {
                for (int j = 0; j < columnSize; j++) {
                    if (dataArray[i, j] > 0)
                        resultArray[i, j] = Math.Sqrt(Math.Sqrt(dataArray[i, j]));
                    else
                        resultArray[i, j] = 0;
                }
            }
            return resultArray;
        }

        public static double[] QuadRootTransform(double[] dataArray) {
            var size = dataArray.Length;
            var resultArray = new double[size];
            for (int i = 0; i < size; i++) {
                if (dataArray[i] > 0)
                    resultArray[i] = Math.Sqrt(Math.Sqrt(dataArray[i]));
                else
                    resultArray[i] = 0;
            }
            return resultArray;
        }
        #endregion

        public static MultivariateAnalysisResult OrthogonalProjectionsToLatentStructures(StatisticsObject statObject,
            MultivariateAnalysisOption plsoption, int component = -1) {
            var dataArray = statObject.CopyX();
            var yArray = statObject.CopyY();

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites
            var maxLV = rowSize;
            var nFold = getOptimalFoldValue(rowSize);

            var ss = new List<double>();
            var press = new List<double>();
            var total = new List<double>();
            var q2 = new List<double>();
            var q2cum = new List<double>();
            var biofactor = 1; // must be 1
            var orthofactor = 1; // should be optimized
            OplsCrossValidation(yArray, dataArray, maxLV, nFold,
               out ss, out press, out total, out q2, out q2cum, out orthofactor);

            if (component > 0) orthofactor = component;
            if (orthofactor > rowSize * 0.5 - 1) orthofactor = (int)(rowSize * 0.5 - 1);
            if (orthofactor < 1) orthofactor = 1;

            // again, initialization
            dataArray = statObject.CopyX();
            yArray = statObject.CopyY();

            double[] ssPred, cPred;
            double[,] wPred, pPred, tPred, uPred, woPred, poPred, toPred, filteredArray;

            OplsModeling(biofactor, orthofactor, yArray, dataArray,
                out ssPred, out wPred, out pPred, out cPred, out tPred, out uPred,
                out woPred, out poPred, out toPred, out filteredArray);

            //for (int i = 0; i < columnSize; i++) {
            //    Debug.WriteLine("Loadings\t" + wPred[0, i] + "\t" + pPred[0, i]);
            //}

            // calculate coefficients
            var coeffVector = GetPlsCoefficients(biofactor, pPred, wPred, cPred);

            // calculate vips
            var vip = GetVips(wPred, ssPred);

            // calculate stdev of filtered x array
            var stdevFilteredXs = GetStdevOfFilteredXArray(filteredArray);

            // yPred
            var yPred = GetPredictedYvariables(coeffVector, statObject, woPred, poPred);
            var ySumofSqure = BasicMathematics.ErrorOfSquareVs2(statObject.YVariables, yPred);
            var rmsee = Math.Sqrt(ySumofSqure / (rowSize - biofactor - 1));

            var plsresult = new MultivariateAnalysisResult() {
                StatisticsObject = statObject,
                MultivariateAnalysisOption = plsoption,
                NFold = nFold,
                SsCVs = new ObservableCollection<double>(ss),
                Presses = new ObservableCollection<double>(press),
                Totals = new ObservableCollection<double>(total),
                Q2Values = new ObservableCollection<double>(q2),
                Q2Cums = new ObservableCollection<double>(q2cum),
                OptimizedFactor = biofactor,
                OptimizedOrthoFactor = orthofactor,
                SsPreds = new ObservableCollection<double>(ssPred),
                CPreds = new ObservableCollection<double>(cPred),
                Coefficients = new ObservableCollection<double>(coeffVector),
                Vips = new ObservableCollection<double>(vip),
                PredictedYs = new ObservableCollection<double>(yPred),
                Rmsee = rmsee,
                StdevFilteredXs = new ObservableCollection<double>(stdevFilteredXs),
                FilteredXArray = filteredArray
            };

            for (int i = 0; i < orthofactor; i++) {
                var woArray = new double[columnSize];
                var poArray = new double[columnSize];
                for (int j = 0; j < columnSize; j++) {
                    woArray[j] = woPred[i, j];
                    poArray[j] = poPred[i, j];
                }
                plsresult.WoPreds.Add(woArray);
                plsresult.PoPreds.Add(poArray);
            }

            for (int i = 0; i < orthofactor; i++) {
                var toArray = new double[rowSize];
                for (int j = 0; j < rowSize; j++) {
                    toArray[j] = toPred[i, j];
                }
                plsresult.ToPreds.Add(toArray);
            }

            for (int i = 0; i < biofactor; i++) {
                var uArray = new double[rowSize];
                var tArray = new double[rowSize];
                for (int j = 0; j < rowSize; j++) {
                    uArray[j] = uPred[i, j];
                    tArray[j] = tPred[i, j];
                }
                plsresult.UPreds.Add(uArray);
                plsresult.TPreds.Add(tArray);

                if (i == 0) {
                    plsresult.stdevT = BasicMathematics.Stdev(tArray);
                }
            }

            for (int i = 0; i < biofactor; i++) {
                var wArray = new double[columnSize];
                var pArray = new double[columnSize];
                var pcoeffArray = new double[columnSize];
                var pcovArray = new double[columnSize];
                for (int j = 0; j < columnSize; j++) {
                    wArray[j] = wPred[i, j];
                    pArray[j] = pPred[i, j];

                    for (int k = 0; k < rowSize; k++) {
                        pcovArray[j] += tPred[i, k] * filteredArray[k, j] / (rowSize - 1);
                    }

                    //pcoeffArray[j] = pArray[j] / plsresult.StdevFilteredXs[j] / plsresult.stdevT;
                    pcoeffArray[j] = pcovArray[j] / plsresult.StdevFilteredXs[j] / plsresult.stdevT;
                }
                plsresult.WPreds.Add(wArray);
                plsresult.PPreds.Add(pArray);
                plsresult.PPredCoeffs.Add(pcoeffArray);
                plsresult.PPredCovs.Add(pcovArray);
            }

            return plsresult;
        }

        private static double[] GetStdevOfFilteredXArray(double[,] dataArray) {
            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var stdevs = new double[columnSize];
            for (int i = 0; i < columnSize; i++) {
                var xArray = new double[rowSize];
                for (int j = 0; j < rowSize; j++) {
                    xArray[j] = dataArray[j, i];
                }
                stdevs[i] = BasicMathematics.Stdev(xArray);
            }

            return stdevs;
        }

        #region pls
        public static MultivariateAnalysisResult PartialLeastSquares(StatisticsObject statObject,
            MultivariateAnalysisOption plsoption,
            int component = -1) {
            if (plsoption == MultivariateAnalysisOption.Oplsda || plsoption == MultivariateAnalysisOption.Oplsr)
                return OrthogonalProjectionsToLatentStructures(statObject, plsoption, component);

            var dataArray = statObject.CopyX();
            var yArray = statObject.CopyY();

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites
            var maxLV = rowSize;
            var nFold = getOptimalFoldValue(rowSize);

            var ss = new List<double>();
            var press = new List<double>();
            var total = new List<double>();
            var q2 = new List<double>();
            var q2cum = new List<double>();
            var optfactor = 1;

            PlsCrossValidation(yArray, dataArray, maxLV, nFold,
                out ss, out press, out total, out q2, out q2cum, out optfactor);

            if (component > 0) optfactor = component;
            if (optfactor < 2) optfactor = 2;

            // again, initialization
            dataArray = statObject.CopyX();
            yArray = statObject.CopyY();

            double[] ssPred, cPred;
            double[,] wPred, pPred, tPred, uPred;

            PlsModeling(optfactor, yArray, dataArray, out ssPred, out wPred, out pPred, out cPred, out tPred, out uPred);

            // calculate coefficients
            var coeffVector = GetPlsCoefficients(optfactor, pPred, wPred, cPred);

            // calculate vips
            var vip = GetVips(wPred, ssPred);

            // yPred
            var yPred = GetPredictedYvariables(coeffVector, statObject);
            var ySumofSqure = BasicMathematics.ErrorOfSquareVs2(statObject.YVariables, yPred);
            var rmsee = Math.Sqrt(ySumofSqure / (rowSize - optfactor - 1));

            var maResult = new MultivariateAnalysisResult() {
                StatisticsObject = statObject,
                MultivariateAnalysisOption = plsoption,
                NFold = nFold,
                SsCVs = new ObservableCollection<double>(ss),
                Presses = new ObservableCollection<double>(press),
                Totals = new ObservableCollection<double>(total),
                Q2Values = new ObservableCollection<double>(q2),
                Q2Cums = new ObservableCollection<double>(q2cum),
                OptimizedFactor = optfactor,
                SsPreds = new ObservableCollection<double>(ssPred),
                CPreds = new ObservableCollection<double>(cPred),
                Coefficients = new ObservableCollection<double>(coeffVector),
                Vips = new ObservableCollection<double>(vip),
                PredictedYs = new ObservableCollection<double>(yPred),
                Rmsee = rmsee
            };

            for (int i = 0; i < optfactor; i++) {
                var wArray = new double[columnSize];
                var pArray = new double[columnSize];
                for (int j = 0; j < columnSize; j++) {
                    wArray[j] = wPred[i, j];
                    pArray[j] = pPred[i, j];
                }
                maResult.WPreds.Add(wArray);
                maResult.PPreds.Add(pArray);
            }

            for (int i = 0; i < optfactor; i++) {
                var uArray = new double[rowSize];
                var tArray = new double[rowSize];
                for (int j = 0; j < rowSize; j++) {
                    uArray[j] = uPred[i, j];
                    tArray[j] = tPred[i, j];
                }
                maResult.UPreds.Add(uArray);
                maResult.TPreds.Add(tArray);
            }

            return maResult;
        }

        private static int getOptimalFoldValue(int rowSize) {
            if (rowSize < 7) return rowSize;
            else return 7;
        }

        private static double[] GetPredictedYvariables(double[] coeffVector, StatisticsObject statObject) {

            var yPreds = new double[statObject.RowSize()];
            for (int i = 0; i < statObject.RowSize(); i++) {
                var s = 0.0;
                for (int j = 0; j < statObject.ColumnSize(); j++) {
                    s += coeffVector[j] * statObject.XScaled[i, j];
                }
                yPreds[i] = statObject.YBackTransform(s);
            }

            return yPreds;
        }

        private static double[] GetPredictedYvariables(double[] coeffVector,
            StatisticsObject statObject, double[,] woPred, double[,] poPred) {
            var yPreds = new double[statObject.RowSize()];
            for (int i = 0; i < statObject.RowSize(); i++) {

                // x vector
                var xnew = new double[statObject.ColumnSize()];
                for (int j = 0; j < statObject.ColumnSize(); j++) {
                    xnew[j] = statObject.XScaled[i, j];
                }

                xnew = convertToFilteredX(xnew, woPred, poPred);

                var s = 0.0;
                for (int j = 0; j < statObject.ColumnSize(); j++) {
                    s += coeffVector[j] * xnew[j];
                }
                yPreds[i] = statObject.YBackTransform(s);
            }

            return yPreds;
        }

        private static double[] convertToFilteredX(double[] xnew, double[,] woPred, double[,] poPred) {

            for (int j = 0; j < woPred.GetLength(0); j++) {
                var tneworth = 0.0;
                var wo2 = 0.0;
                for (int k = 0; k < xnew.Length; k++) {
                    tneworth += xnew[k] * woPred[j, k];
                    wo2 += Math.Pow(woPred[j, k], 2);
                }
                tneworth /= wo2;

                for (int k = 0; k < xnew.Length; k++) {
                    xnew[k] = xnew[k] - poPred[j, k] * tneworth;
                }
            }

            return xnew;
        }

        public static double[] GetVips(double[,] wPred, double[] ssPred) {
            var optfactor = wPred.GetLength(0); // optfactor
            var columnSize = wPred.GetLength(1); // metabolites

            var vip = new double[columnSize];
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < optfactor; j++) {
                    s += Math.Pow(wPred[j, i], 2) * (ssPred[j] - ssPred[j + 1]) * columnSize / (ssPred[0] - ssPred[optfactor]);
                }
                vip[i] = Math.Sqrt(s);
            }
            return vip;
        }

        public static double[] GetPlsCoefficients(int optfactor, double[,] pPred,
            double[,] wPred, double[] cPred) {
            var columnSize = pPred.GetLength(1); // metabolites

            var coeffVector = new double[columnSize];
            #region // calculate coefficients

            var pwMatrix = new double[optfactor, optfactor];
            for (int i = 0; i < optfactor; i++) {
                for (int j = 0; j < optfactor; j++) {
                    var s = 0.0;
                    for (int k = 0; k < columnSize; k++) {
                        s += pPred[i, k] * wPred[j, k];
                    }
                    pwMatrix[i, j] = s;
                }
            }

            var pwMatrixLU = MatrixCalculate.MatrixDecompose(pwMatrix);
            var pwMatrixInv = MatrixCalculate.MatrixInverse(pwMatrixLU);

            var wStar = new double[optfactor, columnSize];
            for (int i = 0; i < optfactor; i++) {
                for (int j = 0; j < columnSize; j++) {
                    var s = 0.0;
                    for (int k = 0; k < optfactor; k++) {
                        s += wPred[k, j] * pwMatrixInv[k, i];
                    }
                    wStar[i, j] = s;
                }
            }

            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < optfactor; j++) {
                    s += wStar[j, i] * cPred[j];
                }
                coeffVector[i] = s;
            }
            #endregion

            return coeffVector;
        }

        public static void OplsModeling(int biofactor, int orthofactor, double[] yArray, double[,] dataArray,
            out double[] ssPred, out double[,] wPred, out double[,] pPred,
            out double[] cPred, out double[,] tPred, out double[,] uPred,
            out double[,] woPred, out double[,] poPred, out double[,] toPred,
            out double[,] filteredXMatrix) {
            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var ssPredList = new List<double>();
            var cPredList = new List<double>();
            var wPredList = new List<double[]>();
            var pPredList = new List<double[]>();
            var tPredList = new List<double[]>();
            var uPredList = new List<double[]>();
            var woPredList = new List<double[]>();
            var poPredList = new List<double[]>();
            var toPredList = new List<double[]>();

            var originalY = new double[yArray.Length];
            for (int i = 0; i < yArray.Length; i++) originalY[i] = yArray[i];

            var currentSS = BasicMathematics.SumOfSquare(yArray);
            ssPredList.Add(currentSS);

            double[] wfirst = getWeightLoading(yArray, dataArray); // to keep weight loading for the first trial of pls
            // modeling
            #region // orthogonal field modeling
            for (int i = 0; i < orthofactor; i++) {

                // t: score vector calculation
                // u: y score vector calculation
                // p: loading vector calculation
                // w: weight (X) factor calculation
                // c: weight (Y) factor calculation
                // to: score vector calculation
                // po: loading vector calculation
                // wo: weight (X) factor calculation

                double[] u, t, p, w, to, po, wo;
                double c;

                OplsVectorsCalculations(yArray, dataArray, wfirst, out u, out t, out c, out p,
                    out wo, out to, out po);
                dataArray = PlsMatrixUpdate(to, po, dataArray);
                yArray = PlsMatrixUpdate(to, c, yArray);
                //Debug.WriteLine("C value\t" + c);
                woPredList.Add(wo); poPredList.Add(po); toPredList.Add(to);
            }
            #endregion

            // save filtered x array
            filteredXMatrix = dataArray;

            // ss value calc
            currentSS = BasicMathematics.SumOfSquare(yArray);
            ssPredList.Add(currentSS);

            // finally, pls modeling is performed to the filtered matrix by othrogonal components
            double[] uf, tf, pf, wf, tof, pof, wof;
            double cf;
            OplsVectorsCalculations(yArray, dataArray, wfirst, out uf, out tf, out cf, out pf,
                out wof, out tof, out pof);
            wPredList.Add(wfirst); pPredList.Add(pf); cPredList.Add(cf); tPredList.Add(tf); uPredList.Add(uf);

            ssPred = ssPredList.ToArray();
            cPred = cPredList.ToArray();
            wPred = new double[biofactor, columnSize];
            pPred = new double[biofactor, columnSize];
            for (int i = 0; i < biofactor; i++) {
                for (int j = 0; j < columnSize; j++) {
                    wPred[i, j] = wPredList[i][j];
                    pPred[i, j] = pPredList[i][j];
                }
            }

            tPred = new double[biofactor, rowSize];
            uPred = new double[biofactor, rowSize];
            for (int i = 0; i < biofactor; i++) {
                for (int j = 0; j < rowSize; j++) {
                    tPred[i, j] = tPredList[i][j];
                    uPred[i, j] = uPredList[i][j];
                }
            }

            woPred = new double[orthofactor, columnSize];
            poPred = new double[orthofactor, columnSize];
            for (int i = 0; i < orthofactor; i++) {
                for (int j = 0; j < columnSize; j++) {
                    woPred[i, j] = woPredList[i][j];
                    poPred[i, j] = poPredList[i][j];
                }
            }

            toPred = new double[orthofactor, rowSize];
            for (int i = 0; i < orthofactor; i++) {
                for (int j = 0; j < rowSize; j++) {
                    toPred[i, j] = toPredList[i][j];
                }
            }
        }

        private static void PlsModeling(int optfactor, double[] yArray, double[,] dataArray,
            out double[] ssPred, out double[,] wPred, out double[,] pPred, out double[] cPred,
            out double[,] tPred, out double[,] uPred) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var ssPredList = new List<double>();
            var cPredList = new List<double>();
            var wPredList = new List<double[]>();
            var pPredList = new List<double[]>();
            var tPredList = new List<double[]>();
            var uPredList = new List<double[]>();

            var currentSS = BasicMathematics.SumOfSquare(yArray);
            ssPredList.Add(currentSS);

            // modeling
            #region // modeling
            for (int i = 0; i < optfactor; i++) {
                // t: score vector calculation
                // u: y score vector calculation
                // p: loading vector calculation
                // w: weight (X) factor calculation
                // c: weight (Y) factor calculation

                double[] u, t, p, w;
                double c;

                PlsVectorsCalculations(yArray, dataArray, out u, out w, out t, out c, out p);
                dataArray = PlsMatrixUpdate(t, p, dataArray);
                yArray = PlsMatrixUpdate(t, c, yArray);

                wPredList.Add(w); pPredList.Add(p); cPredList.Add(c); tPredList.Add(t); uPredList.Add(u);

                currentSS = BasicMathematics.SumOfSquare(yArray);
                ssPredList.Add(currentSS);
            }
            #endregion

            ssPred = ssPredList.ToArray();
            cPred = cPredList.ToArray();
            wPred = new double[optfactor, columnSize];
            pPred = new double[optfactor, columnSize];
            for (int i = 0; i < optfactor; i++) {
                for (int j = 0; j < columnSize; j++) {
                    wPred[i, j] = wPredList[i][j];
                    pPred[i, j] = pPredList[i][j];
                }
            }

            tPred = new double[optfactor, rowSize];
            uPred = new double[optfactor, rowSize];
            for (int i = 0; i < optfactor; i++) {
                for (int j = 0; j < rowSize; j++) {
                    tPred[i, j] = tPredList[i][j];
                    uPred[i, j] = uPredList[i][j];
                }
            }
        }

        private static void OplsCrossValidation(double[] yArray, double[,] dataArray, int maxLV, int nFold,
          out List<double> ss, out List<double> press, out List<double> total,
          out List<double> q2, out List<double> q2cum, out int orthofactor) {

            ss = new List<double>();
            press = new List<double>();
            total = new List<double>();
            q2 = new List<double>();
            q2cum = new List<double>();
            orthofactor = 1;

            double[] w = getWeightLoading(yArray, dataArray); // this w is fixed for opls modeling

            for (int i = 0; i < maxLV; i++) {
                var currentSS = BasicMathematics.SumOfSquare(yArray);
                ss.Add(currentSS);
                var currentPress = PlsPressCalculation(nFold, dataArray, yArray); // same in opls
                press.Add(currentPress);
                q2.Add(1 - press[i] / ss[i]);
                if (i == 0) {
                    total.Add(press[i] / ss[i]);
                    q2cum.Add(1 - press[i] / ss[i]);
                }
                else {
                    total.Add(total[i - 1] * press[i] / ss[i]);
                    q2cum.Add(1 - total[i - 1] * press[i] / ss[i]);
                }

                if (isOptimaized(yArray.Length, ss, press, total, q2, q2cum, out orthofactor)) {
                    orthofactor--; // because the optimal value includes the biological componemnt
                    break;
                }

                // t: score vector calculation
                // u: y score vector calculation
                // p: loading vector calculation
                // w: weight (X) factor calculation
                // c: weight (Y) factor calculation
                // to: score vector calculation
                // po: loading vector calculation
                // wo: weight (X) factor calculation

                double[] u, t, p, to, po, wo;
                double c;

                OplsVectorsCalculations(yArray, dataArray, w, out u, out t, out c, out p,
                    out wo, out to, out po);
                dataArray = PlsMatrixUpdate(to, po, dataArray);
                yArray = PlsMatrixUpdate(to, c, yArray);
            }
        }

        private static double[] getWeightLoading(double[] yArray, double[,] dataArray) {
            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var uScalar = 0.0;
            var u = new double[yArray.Length];

            // score initialize
            for (int i = 0; i < yArray.Length; i++) {
                u[i] = yArray[i];
                uScalar += Math.Pow(u[i], 2);
            }

            var w = new double[columnSize]; // weight (X) factor calculation
            #region // weight factor calculation
            // weight factor initialization
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < rowSize; j++) {
                    s += dataArray[j, i] * u[j];
                }
                w[i] = s / uScalar;
            }

            // weight scalar
            var wScalar = BasicMathematics.RootSumOfSquare(w);

            // weight vector
            for (int i = 0; i < columnSize; i++)
                w[i] = w[i] / wScalar;
            #endregion

            return w;
        }

        public static void PlsCrossValidation(double[] yArray, double[,] dataArray, int maxLV, int nFold,
            out List<double> ss, out List<double> press, out List<double> total,
            out List<double> q2, out List<double> q2cum, out int optfactor) {

            ss = new List<double>();
            press = new List<double>();
            total = new List<double>();
            q2 = new List<double>();
            q2cum = new List<double>();
            optfactor = 1;

            for (int i = 0; i < maxLV; i++) {
                var currentSS = BasicMathematics.SumOfSquare(yArray);
                ss.Add(currentSS);
                var currentPress = PlsPressCalculation(nFold, dataArray, yArray);
                press.Add(currentPress);
                q2.Add(1 - press[i] / ss[i]);
                if (i == 0) {
                    total.Add(press[i] / ss[i]);
                    q2cum.Add(1 - press[i] / ss[i]);
                }
                else {
                    total.Add(total[i - 1] * press[i] / ss[i]);
                    q2cum.Add(1 - total[i - 1] * press[i] / ss[i]);
                }

                if (isOptimaized(yArray.Length, ss, press, total, q2, q2cum, out optfactor)) {
                    break;
                }

                // t: score vector calculation
                // u: y score vector calculation
                // p: loading vector calculation
                // w: weight (X) factor calculation
                // c: weight (Y) factor calculation

                double[] u, t, p, w;
                double c;

                PlsVectorsCalculations(yArray, dataArray, out u, out w, out t, out c, out p);
                dataArray = PlsMatrixUpdate(t, p, dataArray);
                yArray = PlsMatrixUpdate(t, c, yArray);
            }
        }

        private static bool isOptimaized(int size, List<double> ss, List<double> press,
            List<double> total, List<double> q2, List<double> q2cum, out int optfactor) {
            var latestQ2cum = q2cum[q2cum.Count - 1];
            var latestQ2 = q2[q2.Count - 1];

            // rule 1
            if (size > 100) {
                if (latestQ2cum <= 0.0) {
                    optfactor = q2.Count - 1;
                    if (optfactor == 0) return true;
                    ss.RemoveAt(ss.Count - 1);
                    press.RemoveAt(press.Count - 1);
                    total.RemoveAt(total.Count - 1);
                    q2.RemoveAt(q2.Count - 1);
                    q2cum.RemoveAt(q2cum.Count - 1);

                    return true;
                }
            }
            else {
                if (latestQ2cum <= 0.05) {
                    optfactor = q2.Count - 1;
                    if (optfactor == 0) return true;
                    ss.RemoveAt(ss.Count - 1);
                    press.RemoveAt(press.Count - 1);
                    total.RemoveAt(total.Count - 1);
                    q2.RemoveAt(q2.Count - 1);
                    q2cum.RemoveAt(q2cum.Count - 1);

                    return true;
                }
            }

            // rule 2
            var limit = 0.0;
            if (size >= 25) {
                limit = size * 0.2 * 0.01;
            }
            else {
                limit = Math.Sqrt(size) * 0.01;
            }

            if (latestQ2 < limit) {
                optfactor = q2.Count - 1;
                if (optfactor == 0) return true;
                ss.RemoveAt(ss.Count - 1);
                press.RemoveAt(press.Count - 1);
                total.RemoveAt(total.Count - 1);
                q2.RemoveAt(q2.Count - 1);
                q2cum.RemoveAt(q2cum.Count - 1);

                return true;
            }

            // rule N4
            var explained = total[total.Count - 1] / total[0];
            if (explained < 0.01) {
                optfactor = q2.Count - 1;
                if (optfactor == 0) return true;
                ss.RemoveAt(ss.Count - 1);
                press.RemoveAt(press.Count - 1);
                total.RemoveAt(total.Count - 1);
                q2.RemoveAt(q2.Count - 1);
                q2cum.RemoveAt(q2cum.Count - 1);

                return true;
            }

            // rule N5 defined by Hiroshi
            if (q2cum.Count > 1) {
                var diff = Math.Abs(q2cum[q2cum.Count - 1] - q2cum[q2cum.Count - 2]) * 100;
                if (diff < 2) {
                    optfactor = q2.Count - 1;
                    return true;
                }
            }
            optfactor = q2.Count;
            return false;
        }

        private static double[,] PlsMatrixUpdate(double[] t, double[] p, double[,] dataArray) {
            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var nArray = new double[rowSize, columnSize];
            for (int i = 0; i < rowSize; i++) {
                for (int j = 0; j < columnSize; j++) {
                    nArray[i, j] = dataArray[i, j] - t[i] * p[j];
                }
            }
            return nArray;
        }

        private static double[] PlsMatrixUpdate(double[] t, double c, double[] yArray) {
            var size = yArray.Length;
            var nArray = new double[size];

            for (int i = 0; i < size; i++) {
                nArray[i] = yArray[i] - t[i] * c;
            }
            return nArray;
        }


        private static void OplsVectorsCalculations(double[] yArray, double[,] dataArray, double[] w,
            out double[] u, out double[] t, out double c, out double[] p,
            out double[] wo, out double[] to, out double[] po) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            u = new double[rowSize];
            //w = new double[columnSize];
            t = new double[rowSize];
            c = 0.0;
            p = new double[columnSize];

            //PlsVectorsCalculations(yArray, dataArray, out u, out w, out t, out c, out p);
            t = new double[rowSize]; // score vector calculation
            #region // score vector 
            // score initialization
            for (int i = 0; i < rowSize; i++) {
                var s = 0.0;
                for (int j = 0; j < columnSize; j++) {
                    s += dataArray[i, j] * w[j];
                }
                t[i] = s;
            }

            #endregion

            // score scalar
            var tScalar = BasicMathematics.SumOfSquare(t);

            Debug.WriteLine("Y array\t" + String.Join("\t", yArray));
            Debug.WriteLine("T array\t" + String.Join("\t", t));

            c = BasicMathematics.InnerProduct(yArray, t) / tScalar; // weight (Y) factor calculation
            p = new double[columnSize]; // loading vector calculation

            #region // loading vector calculation
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < rowSize; j++) {
                    s += dataArray[j, i] * t[j];
                }
                p[i] = s / tScalar;
            }
            #endregion
            for (int i = 0; i < rowSize; i++) {
                u[i] = yArray[i] / c;
            }

            // wo calc
            wo = new double[columnSize];
            var wp = BasicMathematics.InnerProduct(w, p);
            var w2 = BasicMathematics.SumOfSquare(w);
            for (int i = 0; i < columnSize; i++) {
                wo[i] = p[i] - wp / w2 * w[i];
            }

            var wonorm = BasicMathematics.RootSumOfSquare(wo);
            for (int i = 0; i < columnSize; i++) {
                wo[i] /= wonorm;
            }


            // score initialization
            to = new double[rowSize];
            for (int i = 0; i < rowSize; i++) {
                var s = 0.0;
                for (int j = 0; j < columnSize; j++) {
                    s += dataArray[i, j] * wo[j];
                }
                to[i] = s;
            }

            // score scalar
            var toScalar = BasicMathematics.SumOfSquare(to);

            po = new double[columnSize]; // loading vector calculation

            #region // loading vector calculation
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < rowSize; j++) {
                    s += dataArray[j, i] * to[j];
                }
                po[i] = s / toScalar;
            }
            #endregion
        }

        public static void PlsVectorsCalculations(double[] yArray, double[,] dataArray,
            out double[] u, out double[] w, out double[] t, out double c, out double[] p) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            var uScalar = 0.0;
            u = new double[yArray.Length];

            // score initialize
            for (int i = 0; i < yArray.Length; i++) {
                u[i] = yArray[i];
                uScalar += Math.Pow(u[i], 2);
            }

            w = new double[columnSize]; // weight (X) factor calculation
            #region // weight factor calculation
            // weight factor initialization
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < rowSize; j++) {
                    s += dataArray[j, i] * u[j];
                }
                w[i] = s / uScalar;
            }

            // weight scalar
            var wScalar = BasicMathematics.RootSumOfSquare(w);

            // weight vector
            for (int i = 0; i < columnSize; i++)
                w[i] = w[i] / wScalar;
            #endregion

            t = new double[rowSize]; // score vector calculation
            #region // score vector 
            // score initialization
            for (int i = 0; i < rowSize; i++) {
                var s = 0.0;
                for (int j = 0; j < columnSize; j++) {
                    s += dataArray[i, j] * w[j];
                }
                t[i] = s;
            }

            #endregion

            // score scalar
            var tScalar = BasicMathematics.SumOfSquare(t);

            c = BasicMathematics.InnerProduct(yArray, t) / tScalar; // weight (Y) factor calculation
            p = new double[columnSize]; // loading vector calculation

            #region // loading vector calculation
            for (int i = 0; i < columnSize; i++) {
                var s = 0.0;
                for (int j = 0; j < rowSize; j++) {
                    s += dataArray[j, i] * t[j];
                }
                p[i] = s / tScalar;
            }
            #endregion
        }


        public static double PlsPressCalculation(int cvNumber, int cvFold, double[,] dataArray, double[] yArray) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites

            double[,] trainMatrix, testMatrix;
            double[] trainYvalues, testYvalues;
            DivideMatrixToTrainTest(yArray, dataArray, cvFold, cvNumber, out trainMatrix, out testMatrix, out trainYvalues, out testYvalues);

            double[] uTrain, tTrain, pTrain, wTrain;
            double cTrain;

            PlsVectorsCalculations(trainYvalues, trainMatrix, out uTrain, out wTrain, out tTrain, out cTrain, out pTrain);

            var testSize = testYvalues.Length;
            var tTest = new double[testSize];
            for (int i = 0; i < testSize; i++) {
                var s = 0.0;
                for (int j = 0; j < columnSize; j++) {
                    s += testMatrix[i, j] * wTrain[j];
                }
                tTest[i] = s;
            }

            var yPred = new double[testSize];
            for (int i = 0; i < testSize; i++) {
                yPred[i] = cTrain * tTest[i];
            }
            var press = BasicMathematics.ErrorOfSquareVs2(testYvalues, yPred);
            return press;
        }

        public static double PlsPressCalculation(int cvFold, double[,] dataArray, double[] yArray) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites
            var press = 0.0;
            for (int i = 0; i < cvFold; i++) {
                press += PlsPressCalculation(i, cvFold, dataArray, yArray);
            }

            return press;
        }

        public static void DivideMatrixToTrainTest(double[] yArray, double[,] dataArray, int cvFold, int cvNumber,
            out double[,] trainMatrix, out double[,] testMatrix, out double[] trainYvalues, out double[] testYvalues) {

            var rowSize = dataArray.GetLength(0); // files
            var columnSize = dataArray.GetLength(1); // metabolites
            var testSize = 0;

            for (int i = 0; i < rowSize; i++) {
                var isTestData = i % cvFold == cvNumber ? true : false;
                if (isTestData) testSize++;
            }

            trainMatrix = new double[rowSize - testSize, columnSize];
            testMatrix = new double[testSize, columnSize];

            trainYvalues = new double[rowSize - testSize];
            testYvalues = new double[testSize];

            var trainCounter = 0;
            var testCounter = 0;

            for (int i = 0; i < rowSize; i++) {
                var isTestData = i % cvFold == cvNumber ? true : false;
                if (isTestData) {
                    for (int j = 0; j < columnSize; j++) {
                        testMatrix[testCounter, j] = dataArray[i, j];
                    }
                    testYvalues[testCounter] = yArray[i];
                    testCounter++;
                }
                else {
                    for (int j = 0; j < columnSize; j++) {
                        trainMatrix[trainCounter, j] = dataArray[i, j];
                    }
                    trainYvalues[trainCounter] = yArray[i];
                    trainCounter++;
                }
            }
        }
        #endregion

        public static MultivariateAnalysisResult PrincipalComponentAnalysis(StatisticsObject statObject,
            MultivariateAnalysisOption option,
            int maxPC = 5)
        {
            var dataArray = statObject.XScaled;
            int rowSize = dataArray.GetLength(0);
            int columnSize = dataArray.GetLength(1);
            if (rowSize < maxPC) maxPC = rowSize;

            //PrincipalComponentAnalysisResult pcaBean = new PrincipalComponentAnalysisResult() {
            //    ScoreIdCollection = statObject.YIndexes,
            //    LoadingIdCollection = statObject.XIndexes,
            //    ScoreLabelCollection = statObject.YLabels,
            //    LoadingLabelCollecction = statObject.XLabels,
            //    ScoreBrushCollection = statObject.YColors,
            //    LoadingBrushCollection = statObject.XColors
            //};

            double[,] tpMatrix = new double[rowSize, columnSize];
            double[] mean, var, scoreOld, scoreNew, loading;
            double sum, maxVar, threshold, scoreScalar, scoreVar, loadingScalar, contributionOriginal = 1;
            int maxVarID;

            var contributions = new ObservableCollection<double>();
            var scores = new ObservableCollection<double[]>();
            var loadings = new ObservableCollection<double[]>();

            for (int i = 0; i < maxPC; i++)
            {
                mean = new double[columnSize];
                var = new double[columnSize];
                scoreOld = new double[rowSize];
                scoreNew = new double[rowSize];
                loading = new double[columnSize];

                for (int j = 0; j < columnSize; j++)
                {
                    sum = 0;
                    for (int k = 0; k < rowSize; k++) { sum += dataArray[k, j]; }
                    mean[j] = sum / rowSize;
                }

                for (int j = 0; j < columnSize; j++)
                {
                    sum = 0;
                    for (int k = 0; k < rowSize; k++) { sum += Math.Pow(dataArray[k, j] - mean[j], 2); }
                    var[j] = sum / (rowSize - 1);
                }
                if (i == 0) contributionOriginal = BasicMathematics.Sum(var);

                maxVar = BasicMathematics.Max(var);
                maxVarID = Array.IndexOf(var, maxVar);

                for (int j = 0; j < rowSize; j++)
                    scoreOld[j] = dataArray[j, maxVarID];

                threshold = double.MaxValue;
                while (threshold > 0.00000001)
                {
                    scoreScalar = BasicMathematics.SumOfSquare(scoreOld);
                    for (int j = 0; j < columnSize; j++)
                    {
                        sum = 0;
                        for (int k = 0; k < rowSize; k++) { sum += dataArray[k, j] * scoreOld[k]; }
                        loading[j] = sum / scoreScalar;
                    }

                    loadingScalar = BasicMathematics.RootSumOfSquare(loading);
                    for (int j = 0; j < columnSize; j++) { loading[j] = loading[j] / loadingScalar; }

                    for (int j = 0; j < rowSize; j++)
                    {
                        sum = 0;
                        for (int k = 0; k < columnSize; k++) { sum += dataArray[j, k] * loading[k]; }
                        scoreNew[j] = sum;
                    }

                    threshold = BasicMathematics.RootSumOfSquare(scoreNew, scoreOld);
                    for (int j = 0; j < scoreNew.Length; j++) { scoreOld[j] = scoreNew[j]; }
                }

                for (int j = 0; j < columnSize; j++)
                {
                    for (int k = 0; k < rowSize; k++)
                    {
                        tpMatrix[k, j] = scoreNew[k] * loading[j];
                        dataArray[k, j] = dataArray[k, j] - tpMatrix[k, j];
                    }
                }
                scoreVar = BasicMathematics.Var(scoreNew);
                contributions.Add(scoreVar / contributionOriginal * 100);
                scores.Add(scoreNew);
                loadings.Add(loading);
                //pcaBean.ContributionCollection.Add(scoreVar / contributionOriginal * 100);
                //pcaBean.ScorePointsCollection.Add(scoreNew);
                //pcaBean.LoadingPointsCollection.Add(loading);
            }

            var maResult = new MultivariateAnalysisResult() {
                StatisticsObject = statObject,
                MultivariateAnalysisOption = option,
                NFold = 0,
                Contributions = contributions,
                TPreds = scores,
                PPreds = loadings
            };

            return maResult;
        }

        private static double[,] matrixPreprocessing(StatisticsObject statObject) {

            var dataArray = statObject.XDataMatrix;
            switch (statObject.Transform) {
                case TransformMethod.None:
                    break;
                case TransformMethod.Log10:
                    dataArray = LogTransform(dataArray);
                    break;
                case TransformMethod.QuadRoot:
                    dataArray = QuadRootTransform(dataArray);
                    break;
                default:
                    break;
            }

            switch (statObject.Scale) {
                case ScaleMethod.None:
                    break;
                case ScaleMethod.MeanCenter:
                    dataArray = MeanCentering(dataArray);
                    break;
                case ScaleMethod.ParetoScale:
                    dataArray = ParetoScaling(dataArray);
                    break;
                case ScaleMethod.AutoScale:
                    dataArray = AutoScaling(dataArray);
                    break;
                default:
                    break;
            }

            return dataArray;
        }

        private static double[] responsePreprocessing(StatisticsObject statObject) {
            var dataArray = statObject.YVariables;
            switch (statObject.Transform) {
                case TransformMethod.None:
                    break;
                case TransformMethod.Log10:
                    dataArray = LogTransform(dataArray);
                    break;
                case TransformMethod.QuadRoot:
                    dataArray = QuadRootTransform(dataArray);
                    break;
                default:
                    break;
            }

            switch (statObject.Scale) {
                case ScaleMethod.None:
                    break;
                case ScaleMethod.MeanCenter:
                    dataArray = MeanCentering(dataArray);
                    break;
                case ScaleMethod.ParetoScale:
                    dataArray = ParetoScaling(dataArray);
                    break;
                case ScaleMethod.AutoScale:
                    dataArray = AutoScaling(dataArray);
                    break;
                default:
                    break;
            }

            return dataArray;
        }

        public static MultivariateAnalysisResult HierarchicalClusterAnalysis(StatisticsObject statObject) {
            var n = statObject.XDataMatrix.GetLength(0);
            var m = statObject.XDataMatrix.GetLength(1);
            var transposeMatrix = new double[m, n];
            for (int i = 0; i < n; ++i) for (int j = 0; j < m; ++j)
                    transposeMatrix[j, i] = statObject.XDataMatrix[i, j];
            var result = new MultivariateAnalysisResult()
            {
                StatisticsObject = statObject,
                MultivariateAnalysisOption = MultivariateAnalysisOption.Hca,
                XDendrogram = ClusteringWard2Distance(statObject.XDataMatrix, CalculatePearsonCorrelationDistance),
                YDendrogram = ClusteringWard2Distance(transposeMatrix, CalculatePearsonCorrelationDistance),
                // Root = tree.Count - 1,
                // DistanceMatrix = distMatrix
            };

            return result;
        }

        public static DirectedTree ClusteringWardDistance(
            double[,] dataMatrix,
            Func<IEnumerable<double>, IEnumerable<double>, double> distanceFunc
            )
        {
            var n = dataMatrix.GetLength(0);
            var m = dataMatrix.GetLength(1);
            var clusters = new List<List<List<double>>>(n * 2 - 1);

            var tree = new DirectedTree(n * 2 - 1);
            for(int i = 0; i < n; ++i)
            {
                var vec = new List<double>(m);
                for(int j = 0; j < m; ++j)
                {
                    vec.Add(dataMatrix[i, j]);
                }
                clusters.Add(new List<List<double>> { vec });
            }

            var dists = new LinkedList<(double d, int i, int j)>();
            var distMatrix = new double[n, n];
            for (int i = 0; i < n; ++i)
            {
                distMatrix[i, i] = 0;
                for (int j = i + 1; j < n; ++j)
                {
                    var d = CalculateWardDistance(clusters[i], clusters[j], distanceFunc);
                    dists.AddLast((d, i, j ));
                    distMatrix[i, j] = distMatrix[j, i] = d;
                }
            }

            // clustering
            var existsParent = Enumerable.Repeat(false, n*2-1).ToArray();
            var heights = Enumerable.Repeat(0.0, n*2-1).ToArray();
            for (int k = 0; k < n - 1; ++k)
            {
                (double d, int i, int j) = dists.Min();
                clusters.Add(clusters[i].Concat(clusters[j]).ToList());
                heights[n + k] = d;
                tree.AddEdge(n + k, i, d - heights[i]);
                tree.AddEdge(n + k, j, d - heights[j]);
                existsParent[i] = existsParent[j] = true;
                for (int l = 0; l < n + k; ++l)
                {
                    if (!existsParent[l])
                    {
                        dists.AddLast((
                            CalculateWardDistance(clusters[l], clusters[n + k], distanceFunc),
                            l, n + k
                        ));
                    }
                }
                var node = dists.First;
                var loop = dists.Count;
                for(int l = 0; l<loop; ++l)
                {
                    var cur = node;
                    node = node.Next;
                    if (existsParent[cur.Value.i] || existsParent[cur.Value.j])
                    {
                        dists.Remove(cur);
                    }
                }
            }
            return tree;
        }

        public static DirectedTree ClusteringWard2Distance(
            double[,] dataMatrix,
            Func<IEnumerable<double>, IEnumerable<double>, double> distanceFunc
            )
        {
            var n = dataMatrix.GetLength(0);
            var m = dataMatrix.GetLength(1);

            var jagData = new List<List<double>>(n);
            for (int i = 0; i < n; ++i)
            {
                var l = new List<double>(m);
                for (int j = 0; j < m; ++j)
                    l.Add(dataMatrix[i, j]);
                jagData.Add(l);
            }

            var q = new PriorityQueue<(double, int, int)>((a, b) => a.CompareTo(b));
            var memo = new Dictionary<(int, int), double>();
            var nodes = new HashSet<int>(Enumerable.Range(0, n));
            var ws = new List<int>(n * 2 - 1);
            var heights = new List<double>(n * 2 - 1);
            var result = new DirectedTree(n * 2 - 1);
            var next = n;

            for(int i = 0; i < n; ++i)
            {
                ws.Add(1);
                heights.Add(0);
                for(int j = i+1; j < n; ++j)
                {
                    var d = distanceFunc(jagData[i], jagData[j]);
                    memo[(i, j)] = d * d;
                    memo[(j, i)] = d * d;
                    q.Push((d, i, j));
                }
            }
            
            while (q.Length > 0)
            {
                (double d, int i, int j) = q.Pop();
                if (nodes.Contains(i) && nodes.Contains(j))
                {
                    result.AddEdge(next, i, d - heights[i]);
                    result.AddEdge(next, j, d - heights[j]);
                    heights.Add(d);
                    ws.Add(ws[i] + ws[j]);

                    nodes.Remove(i);
                    nodes.Remove(j);
                    foreach(var k in nodes)
                    {
                        var newd = memo[(i, k)] * (ws[i] + ws[k]) / (ws[i] + ws[j] + ws[k])
                                 + memo[(j, k)] * (ws[j] + ws[k]) / (ws[i] + ws[j] + ws[k])
                                 - memo[(i, j)] * ws[k] / (ws[i] + ws[j] + ws[k]);
                        memo[(k, next)] = newd;
                        memo[(next, k)] = newd;
                        q.Push((Math.Sqrt(newd), k, next));
                    }

                    nodes.Add(next++);
                }
            }

            return result;
        }

        public static double CalculateWardDistance(IEnumerable<List<double>> xs, IEnumerable<List<double>> ys, Func<IEnumerable<double>, IEnumerable<double>, double> distanceFunc) {
            // var zs = xs.Concat(ys);
            var zs = xs.Union(ys);
            var xCentroid = CalculateCentroid(xs);
            var yCentroid = CalculateCentroid(ys);
            var zCentroid = CalculateCentroid(zs);

            var ex = xs.Select(x => distanceFunc(xCentroid, x)).Sum();
            var ey = ys.Select(y => distanceFunc(yCentroid, y)).Sum();
            var ez = zs.Select(z => distanceFunc(zCentroid, z)).Sum();

            return ez - ex - ey;
        }

        public static double CalculateEuclideanDistance(IEnumerable<double> xs, IEnumerable<double> ys)
        {
            return Math.Sqrt(xs.Zip(ys, (x, y) => Math.Pow(x - y, 2)).Sum());
        }

        public static double UnsafeCalculatePearsonCorrelationDistance(IEnumerable<double> xs, IEnumerable<double> ys)
        {
            var n = xs.Count();
            double xm = 0, ym = 0, xx = 0, yy = 0, xy = 0;
            foreach((double x, double y) in xs.Zip(ys, Tuple.Create))
            {
                xm += x;
                ym += y;
                xx += x * x;
                yy += y * y;
                xy += x * y;
            }
            if (xx == xm * xm / n || yy == ym * ym / n)
                return double.PositiveInfinity;
            return 1 - (xy - xm * ym / n) / Math.Sqrt((xx - xm * xm / n) * (yy - ym * ym / n));
        }

        public static double CalculatePearsonCorrelationDistance(IEnumerable<double> xs, IEnumerable<double> ys)
        {
            var result = UnsafeCalculatePearsonCorrelationDistance(xs, ys);
            if (double.IsPositiveInfinity(result))
                return 1;
            return result;
        }

        public static double CalculateSpearmanCorrelationDistance(IEnumerable<double> xs, IEnumerable<double> ys)
        {
            var zs = CalculateRank(xs);
            var ws = CalculateRank(ys);

            return CalculatePearsonCorrelationDistance(zs, ws);
        }
        
        public static double[] CalculateRank(IEnumerable<double> xs)
        {
            var ys = xs.ToList();
            var grouped = ys.GroupBy(e => e).OrderByDescending(g => g.Key);
            var rank = 0;
            var v2r = new Dictionary<double, double>();
            foreach(var group in grouped)
            {
                var n = group.Count();
                v2r[group.Key] = Enumerable.Range(rank + 1, n).Average();
                rank += n;
            }
            return ys.Select(e => v2r[e]).ToArray();
        }

        public static List<double> CalculateCentroid(IEnumerable<List<double>> xss) {
            List<double> first;
            try
            {
                first = xss.First();
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException("This cluster is empty.");
            }
            var result = Enumerable.Repeat(0.0, first.Count).ToList();
            var cnt = 0;
            foreach(var xs in xss)
            {
                if(result.Count != xs.Count)
                {
                    throw new ArgumentException("Datas lengthes are different.");
                }
                ++cnt;
                for(var i = 0; i < result.Count; ++i)
                {
                    result[i] += xs[i];
                }
            }
            for(var i = 0; i < result.Count; ++i)
            {
                result[i] /= cnt;
            }
            return result;
        }
    }
}
