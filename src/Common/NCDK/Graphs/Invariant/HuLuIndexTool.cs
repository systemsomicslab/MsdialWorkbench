/* 
 * Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using NCDK.Graphs.Matrix;
using System;
using System.Diagnostics;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// Collection of methods for the calculation of topological indices of a
    /// molecular graph.
    /// </summary>
    public static class HuLuIndexTool
    {
        /// <summary>
        /// Calculates the extended adjacency matrix index.
        /// An implementation of the algorithm published in <token>cdk-cite-HU96</token>.
        /// </summary>
        // @cdk.keyword EAID number
        public static double GetEAIDNumber(IAtomContainer atomContainer)
        {
            GIMatrix matrix = new GIMatrix(GetExtendedAdjacenyMatrix(atomContainer));

            GIMatrix tempMatrix = matrix;
            GIMatrix fixedMatrix = matrix;
            for (int i = 2; i < atomContainer.Atoms.Count; i++)
            {
                tempMatrix = tempMatrix.Multiply(fixedMatrix);
                matrix = matrix.Add(tempMatrix);
            }

            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                matrix.SetValueAt(i, i, matrix.GetValueAt(i, i) + 1);
            }
            double eaid = matrix.Trace();

            Debug.WriteLine("final matrix - the sum of the powers of EA matrix: ");
            DisplayMatrix(matrix.ArrayValue);
            Debug.WriteLine($"eaid number: {eaid}");

            return eaid;
        }

        public static double[][] GetExtendedAdjacenyMatrix(IAtomContainer atomContainer)
        {
            double[][] adjaMatrix = ConnectionMatrix.GetMatrix(atomContainer);

            Debug.WriteLine("adjacency matrix: ");
            DisplayMatrix(adjaMatrix);

            double[] atomWeights = GetAtomWeights(atomContainer);

            for (int i = 0; i < adjaMatrix.Length; i++)
            {
                for (int j = 0; j < adjaMatrix.Length; j++)
                {
                    if (i == j)
                    {
                        if (string.Equals("O", atomContainer.Atoms[i].Symbol, StringComparison.Ordinal))
                        {
                            adjaMatrix[i][j] = Math.Sqrt(0.74) / 6;
                        }
                        else
                        {
                            adjaMatrix[i][j] = Math.Sqrt(0.74) / 6;
                        }
                    }
                    else
                    {
                        adjaMatrix[i][j] = (Math.Sqrt(atomWeights[i] / atomWeights[j]) + Math.Sqrt(atomWeights[j] / atomWeights[i])) * Math.Sqrt(adjaMatrix[i][j]) / 6;
                    }
                }
            }

            Debug.WriteLine("extended adjacency matrix: ");
            DisplayMatrix(adjaMatrix);

            return adjaMatrix;
        }

        public static double[] GetAtomWeights(IAtomContainer atomContainer)
        {
            IAtom atom, headAtom, endAtom;
            int headAtomPosition, endAtomPosition;

            //int k = 0;
            double[] weightArray = new double[atomContainer.Atoms.Count];
            double[][] adjaMatrix = ConnectionMatrix.GetMatrix(atomContainer);

            int[][] apspMatrix = PathTools.ComputeFloydAPSP(adjaMatrix);
            int[] atomLayers = GetAtomLayers(apspMatrix);

            int[] valenceSum;
            int[] interLayerBondSum;

            Debug.WriteLine("adjacency matrix: ");
            DisplayMatrix(adjaMatrix);
            Debug.WriteLine("all-pairs-shortest-path matrix: ");
            DisplayMatrix(apspMatrix);
            Debug.WriteLine("atom layers: ");
            DisplayArray(atomLayers);

            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                atom = atomContainer.Atoms[i];

                valenceSum = new int[atomLayers[i]];
                for (int v = 0; v < valenceSum.Length; v++)
                {
                    valenceSum[v] = 0;
                }

                interLayerBondSum = new int[atomLayers[i] - 1];
                for (int v = 0; v < interLayerBondSum.Length; v++)
                {
                    interLayerBondSum[v] = 0;
                }

                //weightArray[k] = atom.GetValenceElectronsCount() - atom.GetHydrogenCount(); // method unfinished
                if (AtomicNumbers.O.Equals(atom.AtomicNumber))
                    weightArray[i] = 6 - atom.ImplicitHydrogenCount.Value;
                else
                    weightArray[i] = 4 - atom.ImplicitHydrogenCount.Value;

                for (int j = 0; j < apspMatrix.Length; j++)
                {
                    if (string.Equals("O", atomContainer.Atoms[j].Symbol, StringComparison.Ordinal))
                        valenceSum[apspMatrix[j][i]] += 6 - atomContainer.Atoms[j].ImplicitHydrogenCount.Value;
                    else
                        valenceSum[apspMatrix[j][i]] += 4 - atomContainer.Atoms[j].ImplicitHydrogenCount.Value;
                }

                var bonds = atomContainer.Bonds;
                foreach (var bond in bonds)
                {
                    headAtom = bond.Begin;
                    endAtom = bond.End;

                    headAtomPosition = atomContainer.Atoms.IndexOf(headAtom);
                    endAtomPosition = atomContainer.Atoms.IndexOf(endAtom);

                    if (Math.Abs(apspMatrix[i][headAtomPosition] - apspMatrix[i][endAtomPosition]) == 1)
                    {
                        int min = Math.Min(apspMatrix[i][headAtomPosition], apspMatrix[i][endAtomPosition]);
                        BondOrder order = bond.Order;
                        interLayerBondSum[min] += order.IsUnset() ? 0 : order.Numeric();
                    }
                }

                for (int j = 0; j < interLayerBondSum.Length; j++)
                {
                    weightArray[i] += interLayerBondSum[j] * valenceSum[j + 1] * Math.Pow(10, -(j + 1));
                }

                Debug.WriteLine("valence sum: ");
                DisplayArray(valenceSum);
                Debug.WriteLine("inter-layer bond sum: ");
                DisplayArray(interLayerBondSum);
            }

            Debug.WriteLine("weight array: ");
            DisplayArray(weightArray);

            return weightArray;
        }

        public static int[] GetAtomLayers(int[][] apspMatrix)
        {
            int[] atomLayers = new int[apspMatrix.Length];
            for (int i = 0; i < apspMatrix.Length; i++)
            {
                atomLayers[i] = 0;
                for (int j = 0; j < apspMatrix.Length; j++)
                {
                    if (atomLayers[i] < 1 + apspMatrix[j][i]) atomLayers[i] = 1 + apspMatrix[j][i];
                }

            }
            return atomLayers;
        }

        /// <summary> Lists a 2D double matrix to the System console. </summary>
        public static void DisplayMatrix(double[][] matrix)
        {
            string line;
            for (int f = 0; f < matrix.Length; f++)
            {
                line = "";
                for (int g = 0; g < matrix.Length; g++)
                {
                    line += matrix[g][f] + " | ";
                }
                Debug.WriteLine(line);
            }
        }

        /// <summary> Lists a 2D int matrix to the System console. </summary>
        public static void DisplayMatrix(int[][] matrix)
        {
            string line;
            for (int f = 0; f < matrix.Length; f++)
            {
                line = "";
                for (int g = 0; g < matrix.Length; g++)
                {
                    line += matrix[g][f] + " | ";
                }
                Debug.WriteLine(line);
            }
        }

        /// <summary> Lists a 1D array to the System console. </summary>
        public static void DisplayArray(int[] array)
        {
            string line = "";
            for (int f = 0; f < array.Length; f++)
            {
                line += array[f] + " | ";
            }
            Debug.WriteLine(line);
        }

        /// <summary> Lists a 1D array to the System console. </summary>
        public static void DisplayArray(double[] array)
        {
            string line = "";
            for (int f = 0; f < array.Length; f++)
            {
                line += array[f] + " | ";
            }
            Debug.WriteLine(line);
        }
    }
}
