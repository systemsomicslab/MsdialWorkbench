using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class RegressionMathematics
    {
        private RegressionMathematics() { }
        
        public static double[] WeightedLeastSquaresRegression(double[] w, double[] x, double[] y, string method)
        {
            if (method == "linear")
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
            else if (method == "quand")
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

        public static double[] PolynomialRegression(double[] x, double[] y, int degree)
        {
            if (x.Length != y.Length)
            {
                Console.WriteLine("Array length is not the same.");
                return null;
            }
            var coeff = new double[degree + 1];
            var vanderMonde = new double[degree + 1, degree + 1];
            var responsVector = new double[degree + 1];

            for (int i = 0; i <= degree; i++)
            {
                for (int j = 0; j <= degree; j++)
                {
                    var sum = 0.0;
                    for (int k = 0; k < x.Length; k++)
                    {
                        sum += Math.Pow(x[k], 2 * degree - i - j);
                    }
                    vanderMonde[i, j] = sum;
                }
            }

            for (int i = 0; i <= degree; i++)
            {
                var sum = 0.0;
                for (int j = 0; j < x.Length; j++)
                {
                    sum += Math.Pow(x[j], degree - i) * y[j];
                }
                responsVector[i] = sum;
            }

            var luMatrix = MatrixCalculate.MatrixDecompose(vanderMonde);
            if (luMatrix == null) { Console.WriteLine("LU Matrix null"); return null; }

            var detA = MatrixCalculate.DeterminantA(luMatrix);
            if (detA == 0) { Console.WriteLine("Det A zero"); return null; }

            var inverseMatrix = MatrixCalculate.MatrixInverse(luMatrix);
            return MatrixCalculate.MatrixProduct(inverseMatrix, responsVector);
        }
    }
}
