using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MatrixCalculate
    {
        private MatrixCalculate() { }

        public static double[,] MatrixTranspose(double[,] rawMatirx)
        {
            int rowSize = rawMatirx.GetLength(0);
            int columnSize = rawMatirx.GetLength(1);

            double[,] transposedMatrix = new double[columnSize, rowSize];

            for (int i = 0; i < rowSize; i++)
                for (int j = 0; j < columnSize; j++)
                    transposedMatrix[j, i] = rawMatirx[i, j];
            return transposedMatrix;
        }

        public static double[] MatrixProduct(double[] leftVector, double[,] rightMatrix)
        {
            var leftColumnSize = leftVector.Length;
            var rightRowSize = rightMatrix.GetLength(0);
            var rightColumnSize = rightMatrix.GetLength(1);
            if (leftColumnSize != rightRowSize) return null;

            var vector = new double[rightColumnSize];
            for (int i = 0; i < rightColumnSize; i++)
            {
                var sum = 0.0;
                for (int j = 0; j < leftColumnSize; j++)
                {
                    sum += leftVector[j] * rightMatrix[j, i];
                }
                vector[i] = sum;
            }
            return vector;
        }

        public static double[] MatrixProduct(double[, ] leftMatrix, double[] rightVector)
        {
            int leftRowSize = leftMatrix.GetLength(0);
            int leftColumnSize = leftMatrix.GetLength(1);
            var rightRowSize = rightVector.Length;
            if (leftColumnSize != rightRowSize) return null;

            var vector = new double[leftRowSize];
            for (int i = 0; i < leftRowSize; i++)
            {
                var sum = 0.0;
                for (int j = 0; j < leftColumnSize; j++)
                {
                    sum += leftMatrix[i, j] * rightVector[j];
                }
                vector[i] = sum;
            }
            return vector;
        }

        public static double[,] MatrixProduct(double[,] leftMatrix, double[,] rightMatrix)
        {
            int leftRowSize = leftMatrix.GetLength(0);
            int leftColumnSize = leftMatrix.GetLength(1);
            int rightRowSize = rightMatrix.GetLength(0);
            int rightColumnSize = rightMatrix.GetLength(1);

            double[,] multipliedMatix = new double[leftRowSize, rightColumnSize];
            double sum;

            for (int i = 0; i < leftRowSize; i++)
            {
                for (int j = 0; j < rightColumnSize; j++)
                {
                    sum = 0;
                    for (int k = 0; k < leftColumnSize; k++)
                    {
                        sum += leftMatrix[i, k] * rightMatrix[k, j];
                    }
                    multipliedMatix[i, j] = sum;
                }
            }
            return multipliedMatix;
        }

        public static bool MatrixAreEqual(double[,] matrixA, double[,] matrixB, double epsilon)
        {
            var aRows = matrixA.GetLength(0);
            var bRows = matrixB.GetLength(0);
            var aCols = matrixA.GetLength(1);
            var bCols = matrixB.GetLength(1);

            if (aRows != bRows || aCols != bCols) return false;
            for (int i = 0; i < aRows; i++)
                for (int j = 0; j < aCols; j++)
                    if (Math.Abs(matrixA[i, j] - matrixB[i, j]) > epsilon) return false;
            return true;
        }

        public static LUmatrix MatrixDecompose(double[,] rawMatrix)
        {
            int elementSize = rawMatrix.GetLength(0), imax, tmp;
            var scalingVector = new double[elementSize];
            var indexVector = new int[elementSize];
            double big, dum, sum, temp, d = 1.0;

            for (int i = 0; i < elementSize; i++)
            {
                big = 0.0;
                for (int j = 0; j < elementSize; j++) { temp = Math.Abs(rawMatrix[i, j]); if (temp > big) big = temp; }
                if (big == 0.0) { Debug.Print("Singular matrix in touine ludcmp"); return null; }
                scalingVector[i] = 1.0 / big;
                indexVector[i] = i;
            }

            for (int j = 0; j < elementSize; j++)
            {
                imax = j;
                for (int i = 0; i < j; i++)
                {
                    sum = rawMatrix[i, j];
                    for (int k = 0; k < i; k++) sum -= rawMatrix[i, k] * rawMatrix[k, j];
                    rawMatrix[i, j] = sum;
                }

                big = 0.0;
                for (int i = j; i < elementSize; i++)
                {
                    sum = rawMatrix[i, j];
                    for (int k = 0; k < j; k++) sum -= rawMatrix[i, k] * rawMatrix[k, j];
                    rawMatrix[i, j] = sum;

                    dum = scalingVector[i] * Math.Abs(sum);
                    if (dum >= big) { big = dum; imax = i; }
                }

                if (j != imax)
                {
                    for (int k = 0; k < elementSize; k++)
                    {
                        dum = rawMatrix[imax, k];
                        rawMatrix[imax, k] = rawMatrix[j, k];
                        rawMatrix[j, k] = dum;
                    }
                    d = -1 * d;
                    temp = scalingVector[imax];
                    scalingVector[imax] = scalingVector[j];
                    scalingVector[j] = temp;

                    tmp = indexVector[imax];
                    indexVector[imax] = indexVector[j];
                    indexVector[j] = tmp;
                }

                if (rawMatrix[j, j] == 0.0) rawMatrix[j, j] = Math.Pow(10, -10);
                if (j != elementSize)
                {
                    dum = 1.0 / rawMatrix[j, j];
                    for (int i = j + 1; i < elementSize; i++) rawMatrix[i, j] *= dum;
                }
            }
            return new LUmatrix(rawMatrix, indexVector, d);
        }

        // solve luMatrix * x = b
        public static double[] HelperSolve(LUmatrix luMatrix, double[] b)
        {
            double sum;
            int n = luMatrix.Matrix.GetLength(0);

            double[] x = new double[n];
            b.CopyTo(x, 0);
            for (int i = 1; i < n; ++i)
            {
                sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix.Matrix[i, j] * x[j];
                x[i] = sum;
            }
            x[n - 1] /= luMatrix.Matrix[n - 1, n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix.Matrix[i, j] * x[j];
                x[i] = sum / luMatrix.Matrix[i, i];
            }
            return x;
        }

        public static double DeterminantA(LUmatrix luMatrix)
        {
            double detA = luMatrix.Reverse;
            int elementSize = luMatrix.Matrix.GetLength(0);
            for (int i = 0; i < elementSize; i++) { detA *= luMatrix.Matrix[i, i]; }
            return detA;
        }

        public static double[,] MatrixInverse(LUmatrix luMatrix)
        {
            int elementSize = luMatrix.Matrix.GetLength(0);
            double[,] inverseMatrix = new double[elementSize, elementSize];
            double[] colVector, inverseVector;
            for (int j = 0; j < elementSize; j++)
            {
                colVector = new double[elementSize];
                for (int i = 0; i < elementSize; i++)
                {
                    if (j == luMatrix.IndexVector[i])
                        colVector[i] = 1.0;
                    else
                        colVector[i] = 0.0;
                }
                inverseVector = HelperSolve(luMatrix, colVector);
                for (int i = 0; i < elementSize; i++) inverseMatrix[i, j] = inverseVector[i];
            }
            return inverseMatrix;
        }
    }
}
