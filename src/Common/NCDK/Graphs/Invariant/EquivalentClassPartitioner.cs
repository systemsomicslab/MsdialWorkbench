/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Graphs.Matrix;
using System;
using System.Diagnostics;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// An algorithm for topological symmetry. This algorithm derived from the
    /// algorithm <token>cdk-cite-Hu94</token>.
    /// </summary>
    // @author Junfeng Hao
    // @author Luis F. de Figueiredo
    // @cdk.created 2003-09-24
    // @cdk.dictref blue-obelisk:perceiveGraphSymmetry
    // @cdk.module extra
    public class EquivalentClassPartitioner
    {
        private double[][] nodeMatrix;
        private double[][] bondMatrix;
        private double[] weight;
        private readonly double[][] adjaMatrix;
        private readonly int[][] apspMatrix;
        private readonly int layerNumber;
        private readonly int nodeNumber;
        private const double LOST = 0.000000000001;

        /// <summary>
        /// Constructor for the TopologicalEquivalentClass object.
        /// </summary>
        public EquivalentClassPartitioner() { }

        /// <summary>
        /// Constructor for the TopologicalEquivalentClass object.
        /// </summary>
        public EquivalentClassPartitioner(IAtomContainer atomContainer)
        {
            adjaMatrix = ConnectionMatrix.GetMatrix(atomContainer);
            apspMatrix = PathTools.ComputeFloydAPSP(adjaMatrix);
            layerNumber = 1;
            nodeNumber = atomContainer.Atoms.Count;

            for (int i = 1; i < atomContainer.Atoms.Count; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    // define the number of layer equal to the longest path obtained
                    // by calculating the all-pair-shortest path
                    if (apspMatrix[i][j] > layerNumber)
                    {
                        layerNumber = apspMatrix[i][j];
                    }
                    // correct adjacency matrix to consider aromatic bonds as such
                    if (adjaMatrix[i][j] > 0)
                    {
                        IBond bond = atomContainer.GetBond(atomContainer.Atoms[i], atomContainer.Atoms[j]);
                        bool isArom = bond.IsAromatic;
                        adjaMatrix[i][j] = (isArom) ? 1.5 : adjaMatrix[i][j];
                        adjaMatrix[j][i] = adjaMatrix[i][j];
                    }
                }

            }
            nodeMatrix = Arrays.CreateJagged<double>(nodeNumber, layerNumber + 1);
            bondMatrix = Arrays.CreateJagged<double>(nodeNumber, layerNumber);
            weight = new double[nodeNumber + 1];
        }

        /// <summary>
        /// Get the topological equivalent class of the molecule.
        /// </summary>
        /// <param name="atomContainer">atoms and bonds of the molecule</param>
        /// <returns>an array contains the automorphism partition of the molecule</returns>
        public int[] GetTopoEquivClassbyHuXu(IAtomContainer atomContainer)
        {
            double[] nodeSequence = PrepareNode(atomContainer);
            nodeMatrix = BuildNodeMatrix(nodeSequence);
            bondMatrix = BuildBondMatrix();
            weight = BuildWeightMatrix(nodeMatrix, bondMatrix);
            return FindTopoEquivClass(weight);
        }

        /// <summary>
        /// Prepare the node identifier. The purpose of this is to increase the
        /// differentiation of the nodes. Detailed information please see the
        /// corresponding literature.
        /// </summary>
        /// <param name="atomContainer">atoms and bonds of the molecule</param>
        /// <returns>an array of node identifier</returns>
        public static double[] PrepareNode(IAtomContainer atomContainer)
        {
            double[] nodeSequence = new double[atomContainer.Atoms.Count];
            int i = 0;
            foreach (var atom in atomContainer.Atoms)
            {
                var bonds = atomContainer.GetConnectedBonds(atom).ToReadOnlyList();
                if (bonds.Count == 1)
                {
                    var bond0 = bonds[0];
                    var order = bond0.Order;
                    switch (atom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            if (order == BondOrder.Single)
                                nodeSequence[i] = 1;// CH3-
                            else if (order == BondOrder.Double)
                                nodeSequence[i] = 3;// CH2=
                            else if (order == BondOrder.Triple)
                                nodeSequence[i] = 6;// CH#
                            break;
                        case AtomicNumbers.O:
                            if (order == BondOrder.Single)
                                nodeSequence[i] = 14;// HO-
                            else if (order == BondOrder.Double)
                                nodeSequence[i] = 16;// O=
                                                     // missing the case of an aromatic double bond
                            break;
                        case AtomicNumbers.N:
                            if (order == BondOrder.Single)
                                nodeSequence[i] = 18;// NH2-
                            else if (order == BondOrder.Double)
                            {
                                if (atom.Charge == -1.0)
                                    nodeSequence[i] = 27;// N= contains -1 charge
                                else
                                    nodeSequence[i] = 20;// NH=
                            }
                            else if (order == BondOrder.Triple)
                                nodeSequence[i] = 23;// N#
                            break;
                        case AtomicNumbers.S:
                            if (order == BondOrder.Single)
                                nodeSequence[i] = 31;// HS-
                            else if (order == BondOrder.Double)
                                nodeSequence[i] = 33;// S=
                            break;
                        case AtomicNumbers.P:
                            nodeSequence[i] = 38;// PH2-
                            break;
                        case AtomicNumbers.F:
                            nodeSequence[i] = 42;// F-
                            break;
                        case AtomicNumbers.Cl:
                            nodeSequence[i] = 43;// Cl-
                            break;
                        case AtomicNumbers.Br:
                            nodeSequence[i] = 44;// Br-
                            break;
                        case AtomicNumbers.I:
                            nodeSequence[i] = 45;// I-
                            break;
                        default:
                            Debug.WriteLine("in case of a new node, please " + "report this bug to cdk-devel@lists.sf.net.");
                            break;
                    }
                }
                else if (bonds.Count == 2)
                {
                    IBond bond0 = (IBond)bonds[0];
                    IBond bond1 = (IBond)bonds[1];
                    BondOrder order0 = bond0.Order;
                    BondOrder order1 = bond1.Order;
                    switch (atom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single)
                                nodeSequence[i] = 2;// -CH2-
                            else if (order0 == BondOrder.Double && order1 == BondOrder.Double)
                                nodeSequence[i] = 10;// =C=
                            else if ((order0 == BondOrder.Single || bond1.Order == BondOrder.Single)
                                    && (order0 == BondOrder.Double || bond1.Order == BondOrder.Double))
                                nodeSequence[i] = 5;// -CH=
                            else if ((order0 == BondOrder.Single || bond1.Order == BondOrder.Triple)
                                    && (order0 == BondOrder.Triple || bond1.Order == BondOrder.Triple))
                                nodeSequence[i] = 9;// -C#
                                                    // case 3 would not allow to reach this statement as there
                                                    // is no aromatic bond order
                            if (bond0.IsAromatic && bond1.IsAromatic)
                                nodeSequence[i] = 11;// ArCH
                            break;
                        case AtomicNumbers.N:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single)
                                nodeSequence[i] = 19;// -NH-
                            else if (order0 == BondOrder.Double && order1 == BondOrder.Double)
                                nodeSequence[i] = 28;// =N= with charge=-1
                            else if ((order0 == BondOrder.Single || bond1.Order == BondOrder.Single)
                                    && (order0 == BondOrder.Double || bond1.Order == BondOrder.Double))
                                nodeSequence[i] = 22;// -N=
                            else if ((order0 == BondOrder.Double || bond1.Order == BondOrder.Double)
                                    && (order0 == BondOrder.Triple || bond1.Order == BondOrder.Triple))
                                nodeSequence[i] = 26;// =N#
                            else if ((order0 == BondOrder.Single || bond1.Order == BondOrder.Single)
                                    && (order0 == BondOrder.Triple || bond1.Order == BondOrder.Triple))
                                nodeSequence[i] = 29;// -N# with charge=+1
                                                     // case 3 would not allow to reach this statement as there
                                                     // is no aromatic bond order
                            if (bond0.IsAromatic && bond1.IsAromatic)
                                nodeSequence[i] = 30;// ArN
                                                     // there is no way to distinguish between ArNH and ArN as
                                                     // bonds to protons are not considered
                            break;
                        case AtomicNumbers.O:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single)
                                nodeSequence[i] = 15;// -O-
                            else if (bond0.IsAromatic && bond1.IsAromatic)
                                nodeSequence[i] = 17;// ArO
                            break;
                        case AtomicNumbers.S:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single)
                                nodeSequence[i] = 32;// -S-
                            else if (order0 == BondOrder.Double && order1 == BondOrder.Double)
                                nodeSequence[i] = 35;// =S=
                            else if (bond0.IsAromatic && bond1.IsAromatic)
                                nodeSequence[i] = 37;// ArS
                            break;
                        case AtomicNumbers.P:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single)
                                nodeSequence[i] = 39;// -PH-
                            break;
                        default:
                            Debug.WriteLine("in case of a new node, " + "please report this bug to cdk-devel@lists.sf.net.");
                            break;
                    }
                }
                else if (bonds.Count == 3)
                {
                    var bond0 = (IBond)bonds[0];
                    var bond1 = (IBond)bonds[1];
                    var bond2 = (IBond)bonds[2];
                    var order0 = bond0.Order;
                    var order1 = bond1.Order;
                    var order2 = bond2.Order;
                    switch (atom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single && order2 == BondOrder.Single)
                                nodeSequence[i] = 4;// >C-
                            else if (order0 == BondOrder.Double || order1 == BondOrder.Double
                                    || order2 == BondOrder.Double) nodeSequence[i] = 8;// >C=
                                                                                       // case 2 would not allow to reach this statement because
                                                                                       // there is always a double bond (pi system) around an
                                                                                       // aromatic atom
                            if ((bond0.IsAromatic || bond1.IsAromatic || bond2
                                    .IsAromatic)
                                    && (order0 == BondOrder.Single || order1 == BondOrder.Single || bond2.Order == BondOrder.Single))
                                nodeSequence[i] = 12;// ArC-
                                                     // case 3 would not allow to reach this statement
                            if (bond0.IsAromatic && bond1.IsAromatic
                                    && bond2.IsAromatic) nodeSequence[i] = 13;// ArC
                            break;
                        case AtomicNumbers.N:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single && order2 == BondOrder.Single)
                                nodeSequence[i] = 21;// >N-
                            else if (order0 == BondOrder.Single || order1 == BondOrder.Single
                                    || order2 == BondOrder.Single) nodeSequence[i] = 25;// -N(=)=
                            break;
                        case AtomicNumbers.S:
                            if (order0 == BondOrder.Double || order1 == BondOrder.Double || order2 == BondOrder.Double)
                                nodeSequence[i] = 34;// >S=
                            break;
                        case AtomicNumbers.P:
                            if (order0 == BondOrder.Single && order1 == BondOrder.Single && order2 == BondOrder.Single)
                                nodeSequence[i] = 40;// >P-
                            break;
                        default:
                            Debug.WriteLine("in case of a new node, " + "please report this bug to cdk-devel@lists.sf.net.");
                            break;
                    }
                }
                else if (bonds.Count == 4)
                {
                    switch (atom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            nodeSequence[i] = 7;// >C<
                            break;
                        case AtomicNumbers.N:
                            nodeSequence[i] = 24;// >N(=)-
                            break;
                        case AtomicNumbers.S:
                            nodeSequence[i] = 36;// >S(=)=
                            break;
                        case AtomicNumbers.P:
                            nodeSequence[i] = 41;// =P<-
                            break;
                        default:
                            Debug.WriteLine("in case of a new node, " + "please report this bug to cdk-devel@lists.sf.net.");
                            break;
                    }
                }
                i++;
            }
            return nodeSequence;
        }

        /// <summary>
        /// Build node Matrix.
        /// </summary>
        /// <param name="nodeSequence">an array contains node number for each atom</param>
        /// <returns>node Matrix</returns>
        public double[][] BuildNodeMatrix(double[] nodeSequence)
        {
            int i, j, k;
            for (i = 0; i < nodeNumber; i++)
            {
                nodeMatrix[i][0] = nodeSequence[i];
                for (j = 1; j <= layerNumber; j++)
                {
                    nodeMatrix[i][j] = 0.0;
                    for (k = 0; k < nodeNumber; k++)
                    {
                        if (apspMatrix[i][k] == j)
                        {
                            nodeMatrix[i][j] += nodeSequence[k];
                        }
                    }
                }
            }
            return nodeMatrix;
        }

        /// <summary>
        /// Build trial node Matrix.
        /// </summary>
        /// <param name="weight">an array contains the weight of atom</param>
        /// <returns>trial node matrix.</returns>
        public double[][] BuildTrialNodeMatrix(double[] weight)
        {
            int i, j, k;
            for (i = 0; i < nodeNumber; i++)
            {
                nodeMatrix[i][0] = weight[i + 1];
                for (j = 1; j <= layerNumber; j++)
                {
                    nodeMatrix[i][j] = 0.0;
                    for (k = 0; k < nodeNumber; k++)
                    {
                        if (apspMatrix[i][k] == j)
                        {
                            nodeMatrix[i][j] += weight[k + 1];
                        }
                    }
                }
            }
            return nodeMatrix;
        }

        /// <summary>
        /// Build bond matrix.
        /// </summary>
        /// <returns>bond matrix.</returns>
        public double[][] BuildBondMatrix()
        {
            int i, j, k, m;
            for (i = 0; i < nodeNumber; i++)
            {
                for (j = 1; j <= layerNumber; j++)
                {
                    bondMatrix[i][j - 1] = 0.0;
                    for (k = 0; k < nodeNumber; k++)
                    {
                        if (j == 1)
                        {
                            if (apspMatrix[i][k] == j)
                            {
                                bondMatrix[i][j - 1] += adjaMatrix[i][k];
                            }
                        }
                        else
                        {
                            if (apspMatrix[i][k] == j)
                            {
                                for (m = 0; m < nodeNumber; m++)
                                {
                                    if (apspMatrix[i][m] == (j - 1))
                                    {
                                        bondMatrix[i][j - 1] += adjaMatrix[k][m];
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return bondMatrix;
        }

        /// <summary>
        /// Build weight array for the given node matrix and bond matrix.
        /// </summary>
        /// <param name="nodeMatrix">array contains node information</param>
        /// <param name="bondMatrix">array contains bond information</param>
        /// <returns>weight array for the node</returns>
        public double[] BuildWeightMatrix(double[][] nodeMatrix, double[][] bondMatrix)
        {
            for (int i = 0; i < nodeNumber; i++)
            {
                weight[i + 1] = nodeMatrix[i][0];
                for (int j = 0; j < layerNumber; j++)
                {
                    weight[i + 1] += nodeMatrix[i][j + 1] * bondMatrix[i][j] * Math.Pow(10.0, (double)-(j + 1));
                }
            }
            weight[0] = 0.0;
            return weight;
        }

        /// <summary>
        /// Get different number of the given number.
        /// </summary>
        /// <param name="weight">array contains weight of the nodes</param>
        /// <returns>number of different weight</returns>
        private static int CheckDiffNumber(double[] weight)
        {
            // Count the number of different weight
            var category = new double[weight.Length];
            int i, j;
            int count = 1;
            double t;
            category[1] = weight[1];
            for (i = 2; i < weight.Length; i++)
            {
                for (j = 1; j <= count; j++)
                {
                    t = weight[i] - category[j];
                    if (t < 0.0) t = -t;
                    if (t < LOST) break;
                }
                if (j > count)
                {
                    count += 1;
                    category[count] = weight[i];
                }
            }
            return count;
        }

        /// <summary>
        /// Get the final equivalent class.
        /// </summary>
        /// <param name="weight">array contains weight of the nodes</param>
        /// <returns>an array contains the automorphism partition</returns>
        public static int[] GetEquivalentClass(double[] weight)
        {
            var category = new double[weight.Length];
            var equivalentClass = new int[weight.Length];
            int i, j;
            int count = 1;
            double t;
            category[1] = weight[1];
            for (i = 2; i < weight.Length; i++)
            {
                for (j = 1; j <= count; j++)
                {
                    t = weight[i] - category[j];
                    if (t < 0.0)
                    {
                        t = -t;
                    }
                    if (t < LOST)
                    {
                        break;
                    }
                }
                if (j > count)
                {
                    count += 1;
                    category[count] = weight[i];
                }
            }

            for (i = 1; i < weight.Length; i++)
            {
                for (j = 1; j <= count; j++)
                {
                    t = weight[i] - category[j];
                    if (t < 0.0)
                    {
                        t = -t;
                    }
                    if (t < LOST)
                    {
                        equivalentClass[i] = j;
                    }
                }
            }
            equivalentClass[0] = count;
            return equivalentClass;
        }

        /// <summary>
        /// Find the topological equivalent class for the given weight.
        /// </summary>
        /// <param name="weight">array contains weight of the nodes</param>
        /// <returns>an array contains the automorphism partition</returns>
        public int[] FindTopoEquivClass(double[] weight)
        {
            int trialCount, i;
            var equivalentClass = new int[weight.Length];
            int count = CheckDiffNumber(weight);
            trialCount = count;
            if (count == nodeNumber)
            {
                for (i = 1; i <= nodeNumber; i++)
                {
                    equivalentClass[i] = i;
                }
                equivalentClass[0] = count;
                return equivalentClass;
            }
            do
            {
                count = trialCount;
                double[][] trialNodeMatrix = BuildTrialNodeMatrix(weight);
                double[] trialWeight = BuildWeightMatrix(trialNodeMatrix, bondMatrix);
                trialCount = CheckDiffNumber(trialWeight);
                if (trialCount == nodeNumber)
                {
                    for (i = 1; i <= nodeNumber; i++)
                    {
                        equivalentClass[i] = i;
                    }
                    equivalentClass[0] = count;
                    return equivalentClass;
                }
                if (trialCount <= count)
                {
                    equivalentClass = GetEquivalentClass(weight);
                    return equivalentClass;
                }
            } while (trialCount > count);
            return equivalentClass;
        }
    }
}
