using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Now I'm preparing six smoothing methods but do not use LowessFilter and LowessFilter since I do not test them yet.
    /// These methods will return the list of array, i.e. chromatogram information. Each array includes peak information as [0]scan number [1]retention time [2]m/z [3]intensity.
    /// The first argument of all smoothing methods should be raw chromatogram (list of array as described above.).
    /// The second argument of all smoothing methods is the number of data points which are used for the smoothing.
    /// </summary>
    public sealed class Smoothing
    {
        private Smoothing(){}

        #region for List<double[]>() peaklist
        public static List<double[]> LinearWeightedMovingAverage(List<double[]> peaklist, int smoothingLevel)
        {
            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;
            double smoothedPeakIntensity;
            double sum;
            int lwmaNormalizationValue = smoothingLevel + 1;

            for (int i = 1; i <= smoothingLevel; i++)
                lwmaNormalizationValue += i * 2;

            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i][3] * (smoothingLevel - Math.Abs(j) + 1);
                    else sum += peaklist[i + j][3] * (smoothingLevel - Math.Abs(j) + 1);
                }
                smoothedPeakIntensity = (double)(sum / lwmaNormalizationValue);
                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }
            return chromatogramDataPointCollection;
        }

        public static List<double[]> SimpleMovingAverage(List<double[]> peaklist, int smoothingLevel)
        {
            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;
            double smoothedPeakIntensity;
            double sum;
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i][3];
                    else sum += peaklist[i + j][3];
                }
                smoothedPeakIntensity = (double)(sum / normalizationValue);
                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }
            return chromatogramDataPointCollection;
        }

        public static List<double[]> SavitxkyGolayFilter(List<double[]> peaklist, int smoothingLevel)
        {
            double[,] hatMatrix;
            double[,] vandermondeMatrix = new double[2 * smoothingLevel + 1, 4];
            double[] xvector = new double[2 * smoothingLevel + 1];
            double[] coefficientVector = new double[2 * smoothingLevel + 1];

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                xvector[i] = (-1) * smoothingLevel + i;

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                for (int j = 0; j < 4; j++)
                    vandermondeMatrix[i, j] = Math.Pow(xvector[i], j);

            var luMatrix = MatrixCalculate.MatrixDecompose(MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixTranspose(vandermondeMatrix), vandermondeMatrix));

            hatMatrix = MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixProduct(vandermondeMatrix, MatrixCalculate.MatrixInverse(luMatrix)), MatrixCalculate.MatrixTranspose(vandermondeMatrix));

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = hatMatrix[smoothingLevel, i];

            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;
            double smoothedPeakIntensity;
            double sum;

            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i][3];
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j][3];
                }
                smoothedPeakIntensity = sum;
                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }
            return chromatogramDataPointCollection;
        }

        public static List<double[]> BinomialFilter(List<double[]> peaklist, int smoothingLevel)
        {
            double[] coefficientVector = new double[2 * smoothingLevel + 1];
            double sum = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                coefficientVector[i] = BasicMathematics.BinomialCoefficient(2 * smoothingLevel, i);
                sum += coefficientVector[i];
            }
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = coefficientVector[i] / sum;

            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;
            double smoothedPeakIntensity;

            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i][3];
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j][3];
                }
                smoothedPeakIntensity = sum;
                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }
            return chromatogramDataPointCollection;
        }

        public static List<double[]> LowessFilter(List<double[]> peaklist, int smoothingLevel)
        {
            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a = 0, b = 0, c = 0, d = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                b += (i - smoothingLevel) * coefficient[i];
                c = b;
                d += coefficient[i];
            }
            double[,] inverseMatrix = new double[2, 2];
            double detA = a * d - b * c;
            inverseMatrix[0, 0] = d / detA;
            inverseMatrix[0, 1] = (-1) * b / detA;
            inverseMatrix[1, 0] = (-1) * c / detA;
            inverseMatrix[1, 1] = a / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = 0;
                a = b = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i][3] * j * coefficient[smoothingLevel + j];
                        b += peaklist[i][3] * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j][3] * j * coefficient[smoothingLevel + j];
                        b += peaklist[i + j][3] * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                smoothedPeakIntensity = coefficientB;

                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }

            return chromatogramDataPointCollection;
        }

        public static List<double[]> LoessFilter(List<double[]> peaklist, int smoothingLevel)
        {
            List<double[]> chromatogramDataPointCollection = new List<double[]>();
            double[] smoothedPeakInformation;

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a11 = 0, a12 = 0, a13 = 0, a21 = 0, a22 = 0, a23 = 0, a31 = 0, a32 = 0, a33 = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a11 += Math.Pow(i - smoothingLevel, 4) * coefficient[i];
                a12 += Math.Pow(i - smoothingLevel, 3) * coefficient[i];
                a13 += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                a21 = a12;
                a22 = a13;
                a23 += (i - smoothingLevel) * coefficient[i];
                a31 = a13;
                a32 = a23;
                a33 += coefficient[i];
            }
            double[,] inverseMatrix = new double[3, 3];
            double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;
            inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
            inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
            inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
            inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
            inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
            inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
            inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
            inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
            inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB, coefficientC, a, b, c;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i][3] * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i][3] * j * coefficient[smoothingLevel + j];
                        c += peaklist[i][3] * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j][3] * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i + j][3] * j * coefficient[smoothingLevel + j];
                        c += peaklist[i + j][3] * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;

                smoothedPeakIntensity = coefficientC;
                smoothedPeakInformation = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                chromatogramDataPointCollection.Add(smoothedPeakInformation);
            }

            return chromatogramDataPointCollection;
        }
        #endregion

        #region for List<Peak>() peaklist
        public static List<Peak> LinearWeightedMovingAverage(List<Peak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<Peak>();
            double sum;
            int lwmaNormalizationValue = smoothingLevel + 1;

            for (int i = 1; i <= smoothingLevel; i++)
                lwmaNormalizationValue += i * 2;

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i].Intensity * (smoothingLevel - Math.Abs(j) + 1);
                    else sum += peaklist[i + j].Intensity * (smoothingLevel - Math.Abs(j) + 1);
                }
                smoothedPeakIntensity = (double)(sum / lwmaNormalizationValue);
                var smoothedPeak = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<Peak> SimpleMovingAverage(List<Peak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<Peak>();
            double sum;
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += peaklist[i].Intensity;
                    else sum += peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = (double)(sum / normalizationValue);
                var smoothedPeak = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<Peak> SavitxkyGolayFilter(List<Peak> peaklist, int smoothingLevel)
        {
            double[,] hatMatrix;
            double[,] vandermondeMatrix = new double[2 * smoothingLevel + 1, 4];
            double[] xvector = new double[2 * smoothingLevel + 1];
            double[] coefficientVector = new double[2 * smoothingLevel + 1];

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                xvector[i] = (-1) * smoothingLevel + i;

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                for (int j = 0; j < 4; j++)
                    vandermondeMatrix[i, j] = Math.Pow(xvector[i], j);

            var luMatrix = MatrixCalculate.MatrixDecompose(MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixTranspose(vandermondeMatrix), vandermondeMatrix));

            hatMatrix = MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixProduct(vandermondeMatrix, MatrixCalculate.MatrixInverse(luMatrix)), MatrixCalculate.MatrixTranspose(vandermondeMatrix));

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = hatMatrix[smoothingLevel, i];

            var smoothedPeaklist = new List<Peak>();
            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                var sum = 0.0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<Peak> BinomialFilter(List<Peak> peaklist, int smoothingLevel)
        {
            double[] coefficientVector = new double[2 * smoothingLevel + 1];
            double sum = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                coefficientVector[i] = BasicMathematics.BinomialCoefficient(2 * smoothingLevel, i);
                sum += coefficientVector[i];
            }
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = coefficientVector[i] / sum;

            var smoothedPeaklist = new List<Peak>();

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<Peak> LowessFilter(List<Peak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<Peak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a = 0, b = 0, c = 0, d = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                b += (i - smoothingLevel) * coefficient[i];
                c = b;
                d += coefficient[i];
            }
            double[,] inverseMatrix = new double[2, 2];
            double detA = a * d - b * c;
            inverseMatrix[0, 0] = d / detA;
            inverseMatrix[0, 1] = (-1) * b / detA;
            inverseMatrix[1, 0] = (-1) * c / detA;
            inverseMatrix[1, 1] = a / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = 0;
                a = b = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                smoothedPeakIntensity = coefficientB;

                var smoothedPeak = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<Peak> LoessFilter(List<Peak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<Peak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a11 = 0, a12 = 0, a13 = 0, a21 = 0, a22 = 0, a23 = 0, a31 = 0, a32 = 0, a33 = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a11 += Math.Pow(i - smoothingLevel, 4) * coefficient[i];
                a12 += Math.Pow(i - smoothingLevel, 3) * coefficient[i];
                a13 += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                a21 = a12;
                a22 = a13;
                a23 += (i - smoothingLevel) * coefficient[i];
                a31 = a13;
                a32 = a23;
                a33 += coefficient[i];
            }
            double[,] inverseMatrix = new double[3, 3];
            double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;
            inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
            inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
            inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
            inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
            inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
            inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
            inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
            inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
            inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB, coefficientC, a, b, c;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;

                smoothedPeakIntensity = coefficientC;
                var smoothedPeakInformation = new Peak() { ScanNumber = peaklist[i].ScanNumber, RetentionTime = peaklist[i].RetentionTime, Mz = peaklist[i].Mz, Intensity = smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeakInformation);
            }

            return smoothedPeaklist;
        }
        #endregion
    }
}
