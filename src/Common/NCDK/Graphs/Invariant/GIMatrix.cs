/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using System;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// This class is intended to provide the user an efficient way of implementing matrix of double number and
    /// using normal operations (linear operations, addition, subtraction, multiplication, inversion, concatenation)
    /// on them. The internal representation of a matrix is an array of array of double objects. For the moment,
    /// double class is the best way I have developed to perform exact operation on numbers; however, for
    /// irdoubles, normal operations on float and doubles have to be performed, with the well-known risks of error
    /// this implies. This class also provides a way of representing matrix as arrays of string for output use.
    /// <para>
    /// Please note that although in most books matrix elements' indexes take values between [1..n] I chose not
    /// to disturb Java language way of calling indexes; so the indexes used here take values between [0..n-1] instead.
    /// </para>
    /// </summary>
    // @author Jean-Sebastien Senecal
    // @version 1.0
    // @cdk.created 1999-05-20
    public class GIMatrix
    {
        private double[][] array; // the matrix itself as an array of doubles

        /// <summary>
        /// Class constructor. Uses an array of integers to create a new Matrix object. Note that integers
        /// will be converted to double objects so mathematical operations may be properly performed and
        /// provide exact solutions. The given array should be properly instantiated as a matrix i.e. it
        /// must contain a fixed number of lines and columns, otherwise an exception will be thrown.
        /// Array must be at leat 1x1.
        /// </summary>
        /// <param name="array">an array of integer (first index is the line, second is the column)</param>
        public GIMatrix(int[][] array)
        {
            double[][] temp = new double[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                temp[i] = new double[array[i].Length]; // line by line ...
                for (int j = 0; j < array[i].Length; j++)
                    temp[i][j] = array[i][j]; // converts ints to doubles
            }
            //    VerifyMatrixFormat(temp);
            this.array = temp;
            Height = temp.Length;
            Width = temp[0].Length;
        } // constructor Matrix(int[][])

        /// <summary>
        /// Class constructor. Uses an array of doubles to create a new Matrix object. The given array should
        /// be properly instantiated as a matrix i.e. it must contain a fixed number of lines and columns,
        /// otherwise an exception will be thrown. Array must be at leat 1x1.
        /// </summary>
        /// <param name="array">an array of double objects (first index is the line, second is the column)</param>
        /// <exception cref="BadMatrixFormatException">in case the given array is unproper to construct a matrix</exception>
        public GIMatrix(double[][] array)
        {
            VerifyMatrixFormat(array);
            double[][] temp = new double[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                temp[i] = new double[array[i].Length]; // line by line ...
                for (int j = 0; j < array[i].Length; j++)
                    temp[i][j] = array[i][j];
            }
            this.array = temp;
            Height = array.Length;
            Width = array[0].Length;
        } // constructor Matrix(double[][])

        /// <summary>
        /// Class constructor. Creates a new Matrix object with fixed dimensions. The matrix is
        /// initialised to the "zero" matrix.
        /// </summary>
        /// <param name="line">number of lines</param>
        /// <param name="col">number of columns</param>
        public GIMatrix(int line, int col)
        {
            array = Arrays.CreateJagged<double>(line, col);
            for (int i = 0; i < line; i++)
                for (int j = 0; j < col; j++)
                    array[i][j] = 0.0;
            Height = line;
            Width = col;
        } // constructor Matrix(int,int)

        /// <summary>
        /// Class constructor. Copies an already existing Matrix object in a new Matrix object.
        /// </summary>
        /// <param name="matrix">a Matrix object</param>
        public GIMatrix(GIMatrix matrix)
        {
            double[][] temp = new double[matrix.Height][];
            for (int i = 0; i < matrix.Height; i++)
            {
                temp[i] = new double[matrix.Width]; // line by line ...
                for (int j = 0; j < matrix.Width; j++)
                {
                    try
                    {
                        temp[i][j] = matrix.GetValueAt(i, j);
                    }
                    catch (IndexOutOfRangeException)
                    {
                    } // never happens
                }
            }
            this.array = temp;
            Height = array.Length;
            Width = array[0].Length;
        } // constructor Matrix(Matrix)

        /// <summary>
        /// Class constructor. Creates a new Matrix object using a table of matrices (an array of Matrix objects).
        /// The given array should be properly instantiated i.e. it must contain a fixed number of lines and columns,
        /// otherwise an exception will be thrown.
        /// </summary>
        /// <param name="table">an array of matrices</param>
        /// <exception cref="BadMatrixFormatException">if the table is not properly instantiated</exception>
        public GIMatrix(GIMatrix[][] table)
        {
            VerifyTableFormat(table);
            Height = Width = 0;
            for (int i = 0; i < table.Length; i++)
                Height += table[i][0].Height;
            for (int j = 0; j < table[0].Length; j++)
                Width += table[0][j].Width;
            double[][] temp = Arrays.CreateJagged<double>(Height, Width);
            int k = 0; // counters for matrices
            for (int i = 0; i < Height; i++)
            {
                temp[i] = new double[Width]; // line by line ...
                if (i == table[k][0].Height) k++; // last line of matrix reached
                int h = 0;
                for (int j = 0; j < Width; j++)
                {
                    if (j == table[k][h].Width) h++; // last column of matrix reached
                    try
                    {
                        GIMatrix tempMatrix = table[k][h];
                        temp[i][j] = tempMatrix.GetValueAt(i - k * tempMatrix.Height, j - h * tempMatrix.Width);
                    }
                    catch (IndexOutOfRangeException)
                    {
                    } // never happens
                }
            }
            this.array = temp;
        } // constructor Matrix(Matrix)

        /// <summary>
        /// The number of lines of the matrix.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The number of columns of the matrix.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The internal representation of the matrix, that is an array of double objects.
        ///  (first index is the line, second is the column)
        /// </summary>
        /// <exception cref="BadMatrixFormatException">in case the given array is unproper to construct a matrix</exception>
        internal double[][] ArrayValue
        {
            get { return array; }
            set
            {
                VerifyMatrixFormat(array);
                this.array = value;
            }
        }

        /// <summary>
        /// Returns the value of the given element.
        /// </summary>
        /// <param name="i">the line number</param>
        /// <param name="j">the column number</param>
        /// <returns>the double at the given index in the Matrix</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public double GetValueAt(int i, int j)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            return array[i][j];
        } // method GetValueAt(int,int)

        /// <summary>
        /// Sets the value of the element at the given index.
        /// </summary>
        /// <param name="i">the line number</param>
        /// <param name="j">the column number</param>
        /// <param name="element">the double to place at the given index in the Matrix</param>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public void SetValueAt(int i, int j, double element)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            array[i][j] = element;
        } // method SetValueAt(int,int,double)

        /// <summary>
        /// Returns the line-matrix at the given line index.
        /// </summary>
        /// <param name="i">the line number</param>
        /// <returns>the specified line as a Matrix object</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public GIMatrix GetLine(int i)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            double[][] line = Arrays.CreateJagged<double>(1, Width);
            for (int k = 0; k < Width; k++)
                line[0][k] = array[i][k];
            try
            {
                return new GIMatrix(line);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method GetLine(int)

        /// <summary>
        /// Returns the column-matrix at the given line index.
        /// </summary>
        /// <param name="j">the column number</param>
        /// <returns>the specified column as a Matrix object</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public GIMatrix GetColumn(int j)
        {
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            double[][] column = Arrays.CreateJagged<double>(Height, 1);
            for (int k = 0; k < Height; k++)
                column[k][0] = array[k][j];
            try
            {
                return new GIMatrix(column);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method GetColumn(int)

        /// <summary>
        /// Sets the line of the matrix at the specified index to a new value.
        /// </summary>
        /// <param name="i">the line number</param>
        /// <param name="line">the line to be placed at the specified index</param>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        /// <exception cref="BadMatrixFormatException">in case the given Matrix is unproper to replace a line of this Matrix</exception>
        public void SetLine(int i, GIMatrix line)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            if ((line.Height != 1) || (line.Width != Width)) throw new BadMatrixFormatException();
            for (int k = 0; k < Width; k++)
                array[i][k] = line.GetValueAt(0, k);
        } // method SetLine(int,Matrix)

        /// <summary>
        /// Sets the column of the matrix at the specified index to a new value.
        /// </summary>
        /// <param name="j">the column number</param>
        /// <param name="column">the column to be placed at the specified index</param>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        /// <exception cref="BadMatrixFormatException">in case the given Matrix is unproper to replace a column of this Matrix</exception>
        public void SetColumn(int j, GIMatrix column)
        {
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            if ((column.Height != Height) || (column.Width != 1))
                throw new BadMatrixFormatException();
            for (int k = 0; k < Height; k++)
                array[k][j] = column.GetValueAt(k, 0);
        } // method SetColumn(int,Matrix)

        /// <summary>
        /// Returns the identity matrix.
        /// </summary>
        /// <param name="n">the matrix's dimension (identity matrix is a square matrix)</param>
        /// <returns>the identity matrix of format nxn</returns>
        public static GIMatrix CreateIdentity(int n)
        {
            double[][] identity = Arrays.CreateJagged<double>(n, n);
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < i; j++)
                    identity[i][j] = 0.0;
                identity[i][i] = 1.0;
                for (int j = i + 1; j < n; j++)
                    identity[i][j] = 0.0;
            }
            try
            {
                return new GIMatrix(identity);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method Identity(int)

        /// <summary>
        /// Returns a null matrix (with zeros everywhere) of given dimensions.
        /// </summary>
        /// <param name="m">number of lines</param>
        /// <param name="n">number of columns</param>
        /// <returns>the zero (null) matrix of format mxn</returns>
        public static GIMatrix CreateZero(int m, int n)
        {
            double[][] zero = Arrays.CreateJagged<double>(m, n);
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    zero[i][j] = 0.0;
            try
            {
                return new GIMatrix(zero);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method Zero(int,int)

        /// <summary>
        /// Verifies if two given matrix are equal or not. The matrix must be of the same size and dimensions,
        /// otherwise an exception will be thrown.
        /// </summary>
        /// <param name="matrix">the Matrix object to be compared to</param>
        /// <returns>true if both matrix are equal element to element</returns>
        /// <exception cref="BadMatrixFormatException">if the given matrix doesn't have the same dimensions as this one</exception>
        public bool Equals(GIMatrix matrix)
        {
            if ((Height != matrix.Height) || (Width != matrix.Width))
                throw new BadMatrixFormatException();
            double[][] temp = matrix.ArrayValue;
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    if (!(array[i][j] == temp[i][j])) return false;
            return true;
        } // method Equals(Matrix)

        /// <summary>
        /// Verifies if the matrix is square, that is if it has an equal number of lines and columns.
        /// </summary>
        /// <returns>true if this matrix is square</returns>
        public bool IsSquare()
        {
            return (Height == Width);
        } // method IsSquare()

        /// <summary>
        /// Verifies if the matrix is symmetric, that is if the matrix is equal to it's transpose.
        /// </summary>
        /// <returns>true if the matrix is symmetric</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsSymmetric()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            // the loop looks in the lower half of the matrix to find non-symetric elements
            for (int i = 1; i < Height; i++)
                // starts at index 1 because index (0,0) always symmetric
                for (int j = 0; j < i; j++)
                    if (!(array[i][j] == array[j][i])) return false;
            return true; // the matrix has passed the test
        } //method IsSymmetric()

        // NOT OVER, LOOK MORE CAREFULLY FOR DEFINITION
        /// <summary>
        /// Verifies if the matrix is antisymmetric, that is if the matrix is equal to the opposite of
        /// it's transpose.
        /// </summary>
        /// <returns>true if the matrix is antisymmetric</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsAntisymmetric()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            // the loop looks in the lower half of the matrix to find non-antisymetric elements
            for (int i = 0; i < Height; i++)
                // not as IsSymmetric() loop
                for (int j = 0; j <= i; j++)
                    if (!(array[i][j] == -array[j][i])) return false;
            return true; // the matrix has passed the test
        } // method IsAntisymmetric()

        /// <summary>
        /// Verifies if the matrix is triangular superior or not. A triangular superior matrix has
        /// zero (0) values everywhere under it's diagonal.
        /// </summary>
        /// <returns>true if the matrix is triangular superior</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsTriangularSuperior()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            // the loop looks in the lower half of the matrix to find non-null elements
            for (int i = 1; i < Height; i++)
                // starts at index 1 because index (0,0) is on the diagonal
                for (int j = 0; j < i; j++)
                    if (!(array[i][j] == 0.0)) return false;
            return true; // the matrix has passed the test
        } // method isTriangularSuperior

        /// <summary>
        /// Verifies if the matrix is triangular inferior or not. A triangular inferior matrix has
        /// zero (0) values everywhere upper it's diagonal.
        /// </summary>
        /// <returns>true if the matrix is triangular inferior</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsTriangularInferior()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            // the loop looks in the upper half of the matrix to find non-null elements
            for (int i = 1; i < Height; i++)
                // starts at index 1 because index (0,0) is on the diagonal
                for (int j = i; j < Width; j++)
                    if (!(array[i][j] == 0.0)) return false;
            return true; // the matrix has passed the test
        } // method IsTriangularInferior()

        /// <summary>
        /// Verifies whether or not the matrix is diagonal. A diagonal matrix only has elements on its diagonal
        /// and zeros (0) at every other index. The matrix must be square.
        /// </summary>
        /// <returns>true if the matrix is diagonal</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsDiagonal()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            // the loop looks both halves of the matrix to find non-null elements
            for (int i = 1; i < Height; i++)
                // starts at index 1 because index (0,0) must not be checked
                for (int j = 0; j < i; j++)
                    if ((!(array[i][j] == 0.0)) || (!(array[j][i] == 0.0))) return false; // not null
            return true;
        } // method isDiagonal

        /// <summary>
        /// Verifies if the matrix is invertible or not by asking for its determinant.
        /// </summary>
        /// <returns>true if the matrix is invertible</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public bool IsInvertible()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            return (!(Determinant() == 0)); // det != 0
        } // method IsInvertible()

        /// <summary>
        /// Returns the transpose of this matrix. The transpose of a matrix A = {a(i,j)} is the matrix B = {b(i,j)}
        /// such that b(i,j) = a(j,i) for every i,j i.e. it is the symmetrical reflection of the matrix along its
        /// diagonal. The matrix must be square to use this method, otherwise an exception will be thrown.
        /// </summary>
        /// <returns>the matrix's transpose as a Matrix object</returns>
        public GIMatrix Inverse()
        {
            try
            {
                if (!IsInvertible()) throw new MatrixNotInvertibleException();
            }
            catch (BadMatrixFormatException)
            {
                throw new MatrixNotInvertibleException();
            }
            GIMatrix I = CreateIdentity(Width); // Creates an identity matrix of same dimensions
            GIMatrix table;
            try
            {
                GIMatrix[][] temp = new[] { new[] { this, I } };
                table = new GIMatrix(temp);
            }
            catch (BadMatrixFormatException)
            {
                return null;
            } // never happens
            table = table.GaussJordan(); // linear reduction method applied
            double[][] inv = Arrays.CreateJagged<double>(Height, Width);
            for (int i = 0; i < Height; i++)
                // extracts inverse matrix
                for (int j = Width; j < 2 * Width; j++)
                {
                    try
                    {
                        inv[i][j - Width] = table.GetValueAt(i, j);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return null;
                    } // never happens
                }
            try
            {
                return new GIMatrix(inv);
            }
            catch (BadMatrixFormatException)
            {
                return null;
            } // never happens...
        } // method Inverse()

        /// <summary>
        /// Gauss-Jordan algorithm. Returns the reduced-echeloned matrix of this matrix. The
        /// algorithm has not yet been optimised but since it is quite simple, it should not be
        /// a serious problem.
        /// </summary>
        /// <returns>the reduced matrix</returns>
        public GIMatrix GaussJordan()
        {
            GIMatrix tempMatrix = new GIMatrix(this);
            try
            {
                int i = 0;
                int j = 0;
                int k = 0;
                bool end = false;
                while ((i < Height) && (!end))
                {
                    bool allZero = true; // true if all elements under line i are null (zero)
                    while (j < Width)
                    { // determination of the pivot
                        for (k = i; k < Height; k++)
                        {
                            if (!(tempMatrix.GetValueAt(k, j) == 0.0))
                            { // if an element != 0
                                allZero = false;
                                break;
                            }
                        }
                        if (allZero)
                            j++;
                        else
                            break;
                    }
                    if (j == Width)
                        end = true;
                    else
                    {
                        if (k != i) tempMatrix = tempMatrix.InvertLine(i, k);
                        if (!(tempMatrix.GetValueAt(i, j) == 1.0)) // if element != 1
                            tempMatrix = // A = L(i)(1/a(i,j))(A)
                            tempMatrix.MultiplyLine(i, 1 / tempMatrix.GetValueAt(i, j));
                        for (int q = 0; q < Height; q++)
                            if (q != i) // A = L(q,i)(-a(q,j))(A)
                                tempMatrix = tempMatrix.AddLine(q, i, -tempMatrix.GetValueAt(q, j));
                    }
                    i++;
                }
                // normally here, r = i-1
                return tempMatrix;
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            } // never happens... well I hope ;)
               // From: LEROUX, P. Algebre lineaire: une approche matricielle. Modulo Editeur, 1983. p. 75. (In French)
        } // method GaussJordan()

        /// <summary>
        /// Returns the transpose of this matrix. The transpose of a matrix A = {a(i,j)} is the matrix B = {b(i,j)}
        /// such that b(i,j) = a(j,i) for every i,j i.e. it is the symmetrical reflection of the matrix along its
        /// diagonal. The matrix must be square to use this method, otherwise an exception will be thrown.
        /// </summary>
        /// <returns>the matrix's transpose as a Matrix object</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public GIMatrix Transpose()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            double[][] transpose = Arrays.CreateJagged<double>(array.Length, array[0].Length);
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    transpose[i][j] = array[j][i];
            return new GIMatrix(transpose);
        } // method Transpose()

        /// <summary>
        /// Returns a matrix containing all of the diagonal elements of this matrix and zero (0) everywhere
        /// else. This matrix is called the diagonal of the matrix.
        /// </summary>
        /// <returns>the diagonal of the matrix</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public GIMatrix Diagonal()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            double[][] diagonal = Arrays.CreateJagged<double>(array.Length, array[0].Length);
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    if (i == j)
                        diagonal[i][j] = array[i][j];
                    else
                        diagonal[i][j] = 0;
                }
            return new GIMatrix(diagonal);
        } // method Diagonal()

        /// <summary>
        /// Returns the resulting matrix of an elementary linear operation that consists of multiplying a
        /// single line of the matrix by a constant.
        /// </summary>
        /// <param name="i">the line number</param>
        /// <param name="c">the double constant that multiplies the line</param>
        /// <returns>the resulting Matrix object of the linear operation</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public GIMatrix MultiplyLine(int i, double c)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            double[][] temp = array;
            for (int k = 0; k < Width; k++)
                temp[i][k] = c * temp[i][k]; // mutliply every member of the line by c
            try
            {
                return new GIMatrix(temp);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method MultiplyLine(int,int)

        /// <summary>
        /// Returns the resulting matrix of an elementary linear operation that consists of inverting two lines.
        /// </summary>
        /// <param name="i">the first line number</param>
        /// <param name="j">the second line number</param>
        /// <returns>the resulting Matrix object of the linear operation</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public GIMatrix InvertLine(int i, int j)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            double[][] temp = array;
            double[] tempLine = temp[j]; // temporary line
            temp[j] = temp[i];
            temp[i] = tempLine;
            try
            {
                return new GIMatrix(temp);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method InvertLine(int,int)

        /// <summary>
        /// Returns the resulting matrix of an elementary linear operation that consists of adding one line,
        /// multiplied by some constant factor, to another line.
        /// </summary>
        /// <param name="i">the first line number</param>
        /// <param name="j">the second line number (to be added to the first)</param>
        /// <param name="c">the double constant that multiplies the first line</param>
        /// <returns>the resulting Matrix object of the linear operation</returns>
        /// <exception cref="ArgumentOutOfRangeException">if the given index is out of the matrix's range</exception>
        public GIMatrix AddLine(int i, int j, double c)
        {
            if ((i < 0) || (i >= Height))
                throw new ArgumentOutOfRangeException(nameof(i));
            if ((j < 0) || (j >= Width))
                throw new ArgumentOutOfRangeException(nameof(j));
            double[][] temp = array;
            for (int k = 0; k < Width; k++)
                temp[i][k] = temp[i][k] + c * temp[j][k]; // add multiplied element of i to element of j
            try
            {
                return new GIMatrix(temp);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method AddLine(int,int,double)

        /// <summary>
        ///  Addition from two matrices.
        /// </summary>
        public GIMatrix Add(GIMatrix b)
        {
            if ((b == null) || (Height != b.Height) || (Width != b.Width))
                return null;

            int i, j;
            GIMatrix result = new GIMatrix(Height, Width);
            for (i = 0; i < Height; i++)
                for (j = 0; j < Width; j++)
                    result.array[i][j] = array[i][j] + b.array[i][j];
            return result;
        }

        /// <summary>
        /// Returns the result of the scalar multiplication of the matrix, that is the multiplication of every
        /// of its elements by a given number.
        /// </summary>
        /// <param name="c">the constant by which the matrix is multiplied</param>
        /// <returns>the resulting matrix of the scalar multiplication</returns>
        public GIMatrix Multiply(double c)
        {
            double[][] temp = array;
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    temp[i][j] = c * temp[i][j];
            try
            {
                return new GIMatrix(temp);
            } // format is always OK anyway ...
            catch (BadMatrixFormatException)
            {
                return null;
            }
        } // method Multiply(double)

        /// <summary>
        /// Returns the result of the matrix multiplication of this matrix by another one. The matrix passed
        /// as parameter <i>follows</i> this matrix in the multiplication, so for an example if the dimension of
        /// the actual matrix is mxn, the dimension of the second one should be nxp in order for the multiplication
        /// to be performed (otherwise an exception will be thrown) and the resulting matrix will have dimension mxp.
        /// </summary>
        /// <param name="matrix">the matrix following this one in the matrix multiplication</param>
        /// <returns>the resulting matrix of the matrix multiplication</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix passed in arguments has wrong dimensions</exception>
        public GIMatrix Multiply(GIMatrix matrix)
        {
            if (Width != matrix.Height)
                throw new BadMatrixFormatException(); // unsuitable dimensions
            int p = matrix.Width;
            double[][] temp = Arrays.CreateJagged<double>(Height, p);
            double[][] multiplied = matrix.ArrayValue;
            for (int i = 0; i < Height; i++)
                // line index of the first matrix
                for (int k = 0; k < p; k++)
                { // column index of the second matrix
                    temp[i][k] = array[i][0] * multiplied[0][k]; // first multiplication
                    for (int j = 1; j < Width; j++)
                        // sum of multiplications
                        temp[i][k] = temp[i][k] + array[i][j] * multiplied[j][k];
                }
            return new GIMatrix(temp);
        } // method Multiply(Matrix)

        /// <summary>
        /// Returns the determinant of this matrix. The matrix must be
        /// square in order to use this method, otherwise an exception will be thrown.
        /// <i>Warning: this algorithm is very inefficient and takes too much time to compute
        /// with large matrices.</i>
        /// </summary>
        /// <returns>the determinant of the matrix</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public double Determinant()
        {
            if (Height != Width) throw new BadMatrixFormatException();
            return Det(array); // use of recursive method
        } // method Determinant()

        // Method used for recursive determinant algorithm. Supposes the given array is square.
        private double Det(double[][] mat)
        {
            if (mat.Length == 1) return mat[0][0];
            double temp = mat[0][0] * Det(M(mat, 0, 0)); // (-1)^(0+0)*m[0][0]*Det(M(i,j)) ... first assignation
            for (int k = 1; k < mat.Length; k++)
                temp = temp + (Det(M(mat, 0, k)) * ((k % 2 == 0) ? mat[0][k] : -mat[0][k]));
            // Note: ((0+k)%2 == 0)?1:-1 is equivalent to (-1)^(0+k)
            return temp;
        } // method Det(double[][])

        // Returns the minor of the array (supposed square) i.e. the array least its i-th line
        // and j-th column
        private static double[][] M(double[][] mat, int i, int j)
        {
            double[][] temp = Arrays.CreateJagged<double>(mat.Length - 1, mat[0].Length - 1); // "void minor"
            for (int k = 0; k < i; k++)
            {
                for (int h = 0; h < j; h++)
                    temp[k][h] = mat[k][h];
                for (int h = j + 1; h < mat[0].Length; h++)
                    temp[k][h - 1] = mat[k][h];
            }
            for (int k = i + 1; k < mat.Length; k++)
            {
                for (int h = 0; h < j; h++)
                    temp[k - 1][h] = mat[k][h];
                for (int h = j + 1; h < mat[0].Length; h++)
                    temp[k - 1][h - 1] = mat[k][h];
            }
            return temp;
        } // method M(double[][],int,int)

        /// <summary>
        /// Returns the trace of this matrix, that is the sum of the elements of its diagonal. The matrix must be
        /// square in order to use this method, otherwise an exception will be thrown.
        /// </summary>
        /// <returns>the trace of the matrix</returns>
        /// <exception cref="BadMatrixFormatException">if the matrix is not square</exception>
        public double Trace()
        {
            if (Height != Width)
                throw new BadMatrixFormatException();
            double trace = array[0][0];
            for (int i = 1; i < Height; i++)
                trace = trace + array[i][i];
            return trace;
        } // method Trace()

        // Verifies if the matrix is of good format when calling a constructor or setArrayValue
        private static void VerifyMatrixFormat(double[][] testedMatrix)
        {
            if ((testedMatrix.Length == 0) || (testedMatrix[0].Length == 0))
                throw new BadMatrixFormatException();
            int noOfColumns = testedMatrix[0].Length;
            for (int i = 1; i < testedMatrix.Length; i++)
                if (testedMatrix[i].Length != noOfColumns)
                    throw new BadMatrixFormatException();
        } // method VerifyMatrixFormat(double[][])

        // In the case of the implementation of a table i.e. an array of matrices, verifies if the table is proper.
        private static void VerifyTableFormat(GIMatrix[][] testedTable)
        {
            if ((testedTable.Length == 0) || (testedTable[0].Length == 0))
                throw new BadMatrixFormatException();
            int noOfColumns = testedTable[0].Length;
            int currentHeigth, currentWidth;
            for (int i = 0; i < testedTable.Length; i++)
            { // verifies correspondence of m's (heigth)
                if (testedTable[i].Length != noOfColumns)
                    throw new BadMatrixFormatException();
                currentHeigth = testedTable[i][0].Height;
                for (int j = 1; j < testedTable[0].Length; j++)
                    if (testedTable[i][j].Height != currentHeigth)
                        throw new BadMatrixFormatException();
            }
            for (int j = 0; j < testedTable[0].Length; j++)
            { // verifies correspondence of n's (width)
                currentWidth = testedTable[0][j].Width;
                for (int i = 1; i < testedTable.Length; i++)
                    if (testedTable[i][j].Width != currentWidth)
                        throw new BadMatrixFormatException();
            }
        } // method VerifyTableFormat(Matrix[][])

    } // class Matrix
}
