using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Mathematics.Basic {
    public sealed class BasicMathematics
    {
        private BasicMathematics() { }

        public static double TanimotoIndex(double[] n, double[] k) {
            if (n.IsEmptyOrNull() || k.IsEmptyOrNull() || n.Length != k.Length) return 0;
            var x = 0;
            var y = 0;
            var x_y = 0;
            for (int i = 0; i < n.Length; i++) {
                if (n[i] > 0) x++;
                if (k[i] > 0) y++;
                if (n[i] > 0 && k[i] > 0) x_y++;
            }
            if (x + y - x_y <= 0) return 0;
            return (double)x_y / (double)(x + y - x_y);
        }

        public static ulong FactorialCalculation(int n)
        {
            ulong result = 1;
            for (int i = 1; i <= n; i++)
            {
                result *= (ulong)i;
            }
            return result;
        }

        public static ulong BinomialCoefficient(int n, int k)
        {
            ulong coefficient;
            if (k == 0)
                return 1;
            else if (n == k)
                return 1;
            else
            {
                var nkfactor = FactorialCalculation(n, k);
                var kfactor = FactorialCalculation(k);
                if (kfactor != 0) {
                    return nkfactor / kfactor;
                }
                else
                    return 1;
                //return coefficient;
            }
        }

        public static ulong FactorialCalculation(int n, int k)
        {
            ulong result = 1;
            for (int i = n - k + 1; i <= n; i++)
            {
                result *= (ulong)i;
            }
            return result;
        }

        public static double GaussianFunction(double normalizedValue, double mean, double standardDeviation, double variable)
        {
            double result = normalizedValue * Math.Exp((-1) * Math.Pow(variable - mean, 2) / (2 * Math.Pow(standardDeviation, 2)));
            return result;
        }

        public static double StandadizedGaussianFunction(double diff, double devi)
        {
            double result = Math.Exp(-0.5 * Math.Pow(diff / devi, 2));
            return result;
        }

        public static double Sum(double[] array)
        {
            double sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            return sum;
        }

        public static double Sum(List<double[]> list, int index)
        {
            double sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i][index];
            return sum;
        }

        public static double Mean(IReadOnlyList<double> array)
        {
            if (array == null || array.Count == 0) return 0;

            double sum = 0;
            int size = array.Count;
            for (int i = 0; i < size; i++)
                sum += array[i];
            return sum / size;
        }

        public static double Mean(double[] array)
        {
            if (array == null || array.Length == 0) return 0;

            double sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            return (double)(sum / array.Length);
        }

        public static double Mean(List<double[]> list, int index)
        {
            double sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i][index];
            return (double)(sum / list.Count);
        }

        public static double Median(IReadOnlyList<double> array)
        {
            if (array == null || array.Count == 0) return 0;

            int midArrayNumber = array.Count / 2;
            double[] sortArray = new double[array.Count];
            for (int i = 0; i < array.Count; i++)
                sortArray[i] = array[i];
            Array.Sort(sortArray);
            return sortArray[midArrayNumber];
        }

        public static float Median(float[] array) {
            int midArrayNumber = (int)(array.Length / 2);
            float[] sortArray = new float[array.Length];
            for (int i = 0; i < array.Length; i++)
                sortArray[i] = array[i];
            Array.Sort(sortArray);
            return sortArray[midArrayNumber];
        }

        public static double InplaceSortMedian(double[] array)
        {
            if (array == null || array.Length == 0) {
                return 0;
            }

            int midArrayNumber = array.Length / 2;
            Array.Sort(array);
            return array[midArrayNumber];
        }

        public static double InplaceSortMedian(double[] array, int size)
        {
            if (array is null || size == 0) {
                return 0;
            }
            Array.Sort(array, 0, size);
            return array[size / 2];
        }

        public static void BoxPlotProperties(double[] array, out double minvalue, out double twentyfive, 
            out double median, out double seventyfive, out double maxvalue) {
            if (array == null) {
                minvalue = 0;
                twentyfive = 0;
                median = 0;
                seventyfive = 0;
                maxvalue = 0;
                return;
            }
            if (array.Length == 1) {
                minvalue = array[0];
                twentyfive = array[0];
                median = array[0];
                seventyfive = array[0];
                maxvalue = array[0];
                return;
            }
            if (array.Length == 2) {

                var max = Math.Max(array[0], array[1]);
                var min = Math.Min(array[0], array[1]);
                minvalue = min;
                twentyfive = min;
                median = max;
                seventyfive = max;
                maxvalue = max;
                return;
            }
            if (array.Length == 3) {

                var max = array.Max();
                var min = array.Min();
                var med = Median(array);
                minvalue = min;
                twentyfive = min;
                median = med;
                seventyfive = max;
                maxvalue = max;
                return;
            }
            if (array.Length == 4) {

                var max = array.Max();
                var min = array.Min();
                var med = Median(array);
                minvalue = min;
                twentyfive = min;
                median = med;
                seventyfive = max;
                maxvalue = max;
                return;
            }

            int twentyfiveNumber = (int)(array.Length * 0.25);
            int midArrayNumber = (int)(array.Length * 0.5);
            int seventyfiveNumber = (int)(array.Length * 0.75);

            var sortArray = new double[array.Length];
            for (int i = 0; i < array.Length; i++)
                sortArray[i] = array[i];
            Array.Sort(sortArray);

            minvalue = sortArray[0];
            twentyfive = sortArray[twentyfiveNumber];
            median = sortArray[midArrayNumber];
            seventyfive = sortArray[seventyfiveNumber];
            maxvalue = sortArray[array.Length - 1];
        }


        public static double Median(List<double[]> list, int index)
        {
            int midArrayNumber = (int)(list.Count / 2);
            double[] sortArray = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
                sortArray[i] = list[i][index];
            Array.Sort(sortArray);
            return sortArray[midArrayNumber];
        }

        public static double Stdev(IReadOnlyList<double> array)
        {
            var mean = Mean(array);

            double sum = 0;
            int size = array.Count;
            for (int i = 0; i < size; i++)
                sum += Math.Pow(array[i] - mean, 2);
            return Math.Sqrt(sum / (size - 1));
        }

        public static double Stdev(double[] array)
        {
            double sum = 0, mean = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            mean = (double)(sum / array.Length);

            sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += Math.Pow(array[i] - mean, 2);
            return Math.Sqrt((double)(sum / (array.Length - 1)));
        }

        public static double Stdev(List<double[]> list, int index)
        {
            double sum = 0, mean = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i][index];
            mean = (double)(sum / list.Count);

            sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += Math.Pow(list[i][index] - mean, 2);
            return Math.Sqrt((double)(sum / (list.Count - 1)));
        }

        public static double Var(double[] array)
        {
            double sum = 0, mean = 0;
            for (int i = 0; i < array.Length; i++)
                sum += array[i];
            mean = (double)(sum / array.Length);

            sum = 0;
            for (int i = 0; i < array.Length; i++)
                sum += Math.Pow(array[i] - mean, 2);
            return (double)(sum / (array.Length - 1));
        }

        public static double Var(List<double[]> list, int index)
        {
            double sum = 0, mean = 0;
            for (int i = 0; i < list.Count; i++)
                sum += list[i][index];
            mean = (double)(sum / list.Count);

            sum = 0;
            for (int i = 0; i < list.Count; i++)
                sum += Math.Pow(list[i][index] - mean, 2);
            return (double)(sum / (list.Count - 1));
        }

        public static double Covariance(double[] array1, double[] array2)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                sum1 += array1[i];
                sum2 += array2[i];
            }
            mean1 = (double)(sum1 / array1.Length);
            mean2 = (double)(sum2 / array2.Length);

            for (int i = 0; i < array1.Length; i++)
                covariance += (array1[i] - mean1) * (array2[i] - mean2);
            return (double)(covariance / array1.Length);
        }

        public static double Covariance(List<double[]> list1, List<double[]> list2, int index)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0;
            for (int i = 0; i < list1.Count; i++)
            {
                sum1 += list1[i][index];
                sum2 += list2[i][index];
            }
            mean1 = (double)(sum1 / list1.Count);
            mean2 = (double)(sum2 / list2.Count);

            for (int i = 0; i < list1.Count; i++)
                covariance += (list1[i][index] - mean1) * (list2[i][index] - mean2);
            return (double)(covariance / list1.Count);
        }

        public static double SumOfSquare(double[] array)
        {
            double sum = 0;
            for (int i = 0; i < array.Length; i++) { sum += Math.Pow(array[i], 2); }
            return sum;
        }

        public static double InnerProduct(double[] array1, double[] array2)
        {
            double sum = 0;
            for (int i = 0; i < array1.Length; i++) { sum += array1[i] *array2[i]; }
            return sum;
        }

        public static double RootSumOfSquare(double[] array)
        {
            double sum = 0;
            for (int i = 0; i < array.Length; i++) { sum += Math.Pow(array[i], 2); }
            return Math.Sqrt(sum);
        }

        public static double RootSumOfSquare(double[] array1, double[] array2)
        {
            double sum = 0;
            for (int i = 0; i < array1.Length; i++) { sum += Math.Pow(array1[i] - array2[i], 2); }
            return Math.Sqrt(sum);
        }

        public static double Coefficient(double[] array1, double[] array2)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
            for (int i = 0; i < array1.Length; i++)
            {
                sum1 += array1[i];
                sum2 += array2[i];
            }
            mean1 = (double)(sum1 / array1.Length);
            mean2 = (double)(sum2 / array2.Length);

            for (int i = 0; i < array1.Length; i++)
            {
                covariance += (array1[i] - mean1) * (array2[i] - mean2);
                sqrt1 += Math.Pow(array1[i] - mean1, 2);
                sqrt2 += Math.Pow(array2[i] - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0)
                return 0;
            else
                return (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
        }

        public static double CalculateSpearmanCorrelation(double[] x, double[] y) {
            int[] rankX = x.Select((value, index) => new { Value = value, Index = index })
                           .OrderBy(item => item.Value)
                           .Select((item, index) => new { item.Index, Rank = index + 1 })
                           .OrderBy(item => item.Index)
                           .Select(item => item.Rank)
                           .ToArray();
            int[] rankY = y.Select((value, index) => new { Value = value, Index = index })
                           .OrderBy(item => item.Value)
                           .Select((item, index) => new { item.Index, Rank = index + 1 })
                           .OrderBy(item => item.Index)
                           .Select(item => item.Rank)
                           .ToArray();
            int n = x.Length;
            double d2 = 0;
            for (int i = 0; i < n; i++) {
                d2 += Math.Pow(rankX[i] - rankY[i], 2);
            }
            double spearmanCorrelation = 1 - (6 * d2) / (n * (n * n - 1));
            return spearmanCorrelation;
        }

        public static float Coefficient(float[] array1, float[] array2) {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
            for (int i = 0; i < array1.Length; i++) {
                sum1 += array1[i];
                sum2 += array2[i];
            }
            mean1 = (double)(sum1 / array1.Length);
            mean2 = (double)(sum2 / array2.Length);

            for (int i = 0; i < array1.Length; i++) {
                covariance += (array1[i] - mean1) * (array2[i] - mean2);
                sqrt1 += Math.Pow(array1[i] - mean1, 2);
                sqrt2 += Math.Pow(array2[i] - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0)
                return 0;
            else
                return (float)(covariance / Math.Sqrt(sqrt1 * sqrt2));
        }

        public static double Coefficient(List<double[]> list1, List<double[]> list2, int index)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
            for (int i = 0; i < list1.Count; i++)
            {
                sum1 += list1[i][index];
                sum2 += list2[i][index];
            }
            mean1 = (double)(sum1 / list1.Count);
            mean2 = (double)(sum2 / list2.Count);

            for (int i = 0; i < list1.Count; i++)
            {
                covariance += (list1[i][index] - mean1) * (list2[i][index] - mean2);
                sqrt1 += Math.Pow(list1[i][index] - mean1, 2);
                sqrt2 += Math.Pow(list2[i][index] - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0)
                return 0;
            else
                return (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
        }

        public static double Max(double[] x)
        {
            double maxValue = double.MinValue;
            for (int i = 0; i < x.Length; i++) { if (maxValue < x[i]) maxValue = x[i]; } return maxValue;
        }

        public static double Min(double[] x)
        {
            double minValue = double.MaxValue;
            for (int i = 0; i < x.Length; i++) { if (minValue > x[i]) minValue = x[i]; } return minValue;
        }

        public static double ErrorOfSquare(double[] x, double[] y)
        {
            int num = x.Length;
            double sum = 0;
            for (int i = 0; i < x.Length; i++) { sum += Math.Pow(x[i] - y[i], 2); }
            return sum / num;
        }

        public static double ErrorOfSquareVs2(double[] x, double[] y) {
            double sum = 0;
            for (int i = 0; i < x.Length; i++) { sum += Math.Pow(x[i] - y[i], 2); }
            return sum;
        }

        public static double[] ResidualArrayWithOriginal(double[] xActual, double[] xPredict)
        {
            double[] residuals = new double[xActual.Length];
            for (int i = 0; i < xActual.Length; i++)
                residuals[i] = xActual[i] - xPredict[i];
            return residuals;
        }

        public static double[] ResidualArrayWithAbs(double[] xActual, double[] xPredict)
        {
            double[] residuals = new double[xActual.Length];
            for (int i = 0; i < xActual.Length; i++)
                residuals[i] = Math.Abs(xActual[i] - xPredict[i]);
            return residuals;
        }

        public static int GetNearestIndex(double[] x, double value)
        {
            int index = -1;
            double diff = double.MaxValue;
            for (int i = 0; i < x.Length; i++) { if (Math.Abs(x[i] - value) < diff) { diff = Math.Abs(x[i] - value); index = i; } } return index;
        }

        public static double Gravity(double[] x, double[] y)
        {
            double sum = 0, totalIntensity = 0;
            for (int i = 0; i < x.Length; i++)
            {
                sum += x[i] * y[i];
                totalIntensity += y[i];
            }
            if (totalIntensity <= 0) return 0;
            else return sum / totalIntensity;
        }

        public static double Gravity(List<double[]> list, int xIndex, int yIndex)
        {
            double sum = 0, totalIntensity = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum += list[i][xIndex] * list[i][yIndex];
                totalIntensity += list[i][yIndex];
            }
            if (totalIntensity <= 0) return 0;
            else return sum / totalIntensity;
        }

        public static double[] getResidualArrayByOriginal(double[] xActual, double[] xPredict)
        {
            double[] residuals = new double[xActual.Length];
            for (int i = 0; i < xActual.Length; i++)
                residuals[i] = xActual[i] - xPredict[i];
            return residuals;
        }

        public static double[] getResidualArrayByAbs(double[] xActual, double[] xPredict)
        {
            double[] residuals = new double[xActual.Length];
            for (int i = 0; i < xActual.Length; i++)
                residuals[i] = Math.Abs(xActual[i] - xPredict[i]);
            return residuals;
        }
    }
}
