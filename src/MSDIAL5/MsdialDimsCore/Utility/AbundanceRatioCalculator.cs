using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CompMs.Common.Mathematics.Matrix;

namespace CompMs.MsdialDimsCore.Utility
{
    public class AbundanceRatioCalculator
    {
        private static readonly double eps = 1e-10;

        private double[,] q; // matrix (m x m)
        private double[] c; // vector (m)
        private double[] x; // vector (m)

        public AbundanceRatioCalculator(double[] measure, double[,] reference)
        {
            var refT = MatrixCalculate.MatrixTranspose(reference);
            q = MatrixCalculate.MatrixProduct(refT, reference);
            c = MatrixCalculate.MatrixProduct(refT, measure);
            x = new double[c.Length];
        }

        public (double[], bool) Calculate(int iteration = 1000)
        {
            List<int> active = GetActiveIndices(x);
            for (int i = 0; i < iteration; i++)
            {
                (double[] xhat, double[] lambdahat) = CalculateHat(active);

                if (!Feasible(xhat))
                {
                    double[] xdash = UpdateX(x, xhat);
                    x = xdash;
                    active = GetActiveIndices(xdash);
                }
                else
                {
                    x = xhat;
                    if (AllActive(lambdahat)) return (xhat, true);
                    RemoveActiveLambda(lambdahat, active);
                }
            }
            return (x, false);
        }

        (double[], double[]) CalculateHat(List<int> activeIndices)
        {
            var n = activeIndices.Count;
            var m = q.GetLength(0);
            double[] xhat, lambdahat;

            if (activeIndices.Count == 0)
            {
                xhat = MatrixCalculate.MatrixProduct(Inverse(q), c);
                lambdahat = new double[m];
                return (xhat, lambdahat);
            }

            double[,] a = new double[n + m, n + m];
            for (int i = 0; i < activeIndices.Count; i++)
            {
                a[i, activeIndices[i]] = 1;
                a[activeIndices[i] + n, i + m] = -1;
            }
            for (int i = 0; i < m; i++)
                for (int j = 0; j < m; j++)
                    a[i + n, j] = q[i, j];

            double[,] inv = Inverse(a);
            double[] b = new double[n + m];
            for (int i = 0; i < m; i++)
                b[n + i] = c[i]; 

            var tmp = MatrixCalculate.MatrixProduct(inv, b);
            xhat = new double[m];
            for (int i = 0; i < m; i++)
                xhat[i] = tmp[i];
            lambdahat = new double[m];
            for (int i = 0; i < n; i++)
                lambdahat[activeIndices[i]] = tmp[m + i];

            return (xhat, lambdahat);
        }

        static double[] UpdateX(double[] x, double[] xhat)
        {
            var ds = x.Zip(xhat, (p, q) => -p / (q - p)).Where(e => e > 0);
            var t = (ds.Count() == 0) ? 0d : ds.Min();
            return x.Zip(xhat, (p, q) => (q - p) * t + p).ToArray();
        }

        static List<int> GetActiveIndices(double[] x) =>
            x.Select((value, index) => (value, index)).Where(p => Math.Abs(p.value) < eps).Select(p => p.index).ToList();

        static void RemoveActiveLambda(double[] lambda, IList<int> activeIndices)
        {
            var idc = lambda.Select((value, index) => (value, index)).Aggregate((acc, p) => acc.value < p.value ? acc : p).index;
            activeIndices.Remove(idc);
        }

        static bool AllActive(double[] lambda) => lambda.All(l => l > -eps);
        static bool Feasible(double[] xs) => xs.All(x => x > -eps);

        static double[,] Inverse(double[,] matrix) => MatrixCalculate.MatrixInverse(MatrixCalculate.MatrixDecompose(matrix));
    }
}
