
/*
 *  Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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
using NCDK.Graphs;
using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Calculates the Molecular Distance Edge descriptor described in <token>cdk-cite-LIU98</token>.
    /// </summary>
    /// <remarks>
    /// This class evaluates the 10 MDE descriptors described by Liu et al. and
    /// in addition it calculates variants where O and N are considered (as found in the DEDGE routine
    /// from ADAPT).
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2006-09-18
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:mde
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#mde")]
    public class MDEDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public MDEDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public new IReadOnlyList<double> Values { get; private set; }

            public Result(IReadOnlyList<double> values)
            {
                this.Values = values;
            }

            /// <summary>
            /// molecular distance edge between all primary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-11")]
            public double MDEC11 => Values[0];
            /// <summary>
            /// molecular distance edge between all primary and secondary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-12")]
            public double MDEC12 => Values[1];
            /// <summary>
            /// molecular distance edge between all primary and tertiary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-13")]
            public double MDEC13 => Values[2];
            /// <summary>
            /// molecular distance edge between all primary and quaternary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-14")]
            public double MDEC14 => Values[3];
            /// <summary>
            /// molecular distance edge between all secondary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-22")]
            public double MDEC22 => Values[4];
            /// <summary>
            /// molecular distance edge between all secondary and tertiary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-23")]
            public double MDEC23 => Values[5];
            /// <summary>
            /// molecular distance edge between all secondary and quaternary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-24")]
            public double MDEC24 => Values[6];
            /// <summary>
            /// molecular distance edge between all tertiary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-33")]
            public double MDEC33 => Values[7];
            /// <summary>
            /// molecular distance edge between all tertiary and quaternary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-34")]
            public double MDEC34 => Values[8];
            /// <summary>
            /// molecular distance edge between all quaternary carbons
            /// </summary>
            [DescriptorResultProperty("MDEC-44")]
            public double MDEC44 => Values[9];
            /// <summary>
            /// molecular distance edge between all primary oxygens 
            /// </summary>
            [DescriptorResultProperty("MDEO-11")]
            public double MDEO11 => Values[10];
            /// <summary>
            /// molecular distance edge between all primary and secondary oxygens
            /// </summary>
            [DescriptorResultProperty("MDEO-12")]
            public double MDEO12 => Values[11];
            /// <summary>
            /// molecular distance edge between all secondary oxygens
            /// </summary>
            [DescriptorResultProperty("MDEO-22")]
            public double MDEO22 => Values[12];
            /// <summary>
            /// molecular distance edge between all primary nitrogens
            /// </summary>
            [DescriptorResultProperty("MDEN-11")]
            public double MDEN11 => Values[13];
            /// <summary>
            /// molecular distance edge between all primary and secondary nitrogens
            /// </summary>
            [DescriptorResultProperty("MDEN-12")]
            public double MDEN12 => Values[14];
            /// <summary>
            /// molecular distance edge between all primary and tertiary niroqens
            /// </summary>
            [DescriptorResultProperty("MDEN-13")]
            public double MDEN13 => Values[15];
            /// <summary>
            /// molecular distance edge between all secondary nitroqens
            /// </summary>
            [DescriptorResultProperty("MDEN-22")]
            public double MDEN22 => Values[16];
            /// <summary>
            /// molecular distance edge between all secondary and tertiary nitrogens
            /// </summary>
            [DescriptorResultProperty("MDEN-23")]
            public double MDEN23 => Values[17];
            /// <summary>
            /// molecular distance edge between all tertiary nitrogens
            /// </summary>
            [DescriptorResultProperty("MDEN-33")]
            public double MDEN33 => Values[18];
        }

        private const int MDEC11 = 0;
        private const int MDEC12 = 1;
        private const int MDEC13 = 2;
        private const int MDEC14 = 3;
        private const int MDEC22 = 4;
        private const int MDEC23 = 5;
        private const int MDEC24 = 6;
        private const int MDEC33 = 7;
        private const int MDEC34 = 8;
        private const int MDEC44 = 9;
        private const int MDEO11 = 10;
        private const int MDEO12 = 11;
        private const int MDEO22 = 12;
        private const int MDEN11 = 13;
        private const int MDEN12 = 14;
        private const int MDEN13 = 15;
        private const int MDEN22 = 16;
        private const int MDEN23 = 17;
        private const int MDEN33 = 18;
        private const int C_1 = 1;
        private const int C_2 = 2;
        private const int C_3 = 3;
        private const int C_4 = 4;

        private const int O_1 = 1;
        private const int O_2 = 2;

        private const int N_1 = 1;
        private const int N_2 = 2;
        private const int N_3 = 3;

        /// <summary>
        /// Calculate the weight of specified element type in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">
        /// The AtomContainer for which this descriptor is to be calculated. If 'H
        /// is specified as the element symbol make sure that the AtomContainer has hydrogens.
        /// </param>
        /// <returns>The total weight of atoms of the specified element type</returns>
        public Result Calculate(IAtomContainer container)
        {
            return new Calculator(container).Calculate();
        }

        class Calculator
        {
            private readonly IAtomContainer container;
            private readonly int[][] adjMatrix;
            private readonly int[][] tdist;

            public Calculator(IAtomContainer container)
            {
                this.container = AtomContainerManipulator.RemoveHydrogens(container); // it returns clone
                adjMatrix = AdjacencyMatrix.GetMatrix(container);
                tdist = PathTools.ComputeFloydAPSP(adjMatrix);
            }

            public Result Calculate()
            { 
                var retval = new double[19];
                for (int i = 0; i < 19; i++)
                    retval[i] = Dedge(i);

                return new Result(retval);
            }

            private double Dedge(int which)
            {
                int[][] atypes = null;

                switch (which)
                {
                    case MDEC11:
                    case MDEC12:
                    case MDEC13:
                    case MDEC14:
                    case MDEC22:
                    case MDEC23:
                    case MDEC24:
                    case MDEC33:
                    case MDEC34:
                    case MDEC44:
                        atypes = EvalATable(6);
                        break;
                    case MDEO11:
                    case MDEO12:
                    case MDEO22:
                        atypes = EvalATable(8);
                        break;
                    case MDEN11:
                    case MDEN12:
                    case MDEN13:
                    case MDEN22:
                    case MDEN23:
                    case MDEN33:
                        atypes = EvalATable(7);
                        break;
                }
                double retval = 0;
                switch (which)
                {
                    case MDEC11:
                        retval = EvalCValue(atypes, C_1, C_1);
                        break;
                    case MDEC12:
                        retval = EvalCValue(atypes, C_1, C_2);
                        break;
                    case MDEC13:
                        retval = EvalCValue(atypes, C_1, C_3);
                        break;
                    case MDEC14:
                        retval = EvalCValue(atypes, C_1, C_4);
                        break;
                    case MDEC22:
                        retval = EvalCValue(atypes, C_2, C_2);
                        break;
                    case MDEC23:
                        retval = EvalCValue(atypes, C_2, C_3);
                        break;
                    case MDEC24:
                        retval = EvalCValue(atypes, C_2, C_4);
                        break;
                    case MDEC33:
                        retval = EvalCValue(atypes, C_3, C_3);
                        break;
                    case MDEC34:
                        retval = EvalCValue(atypes, C_3, C_4);
                        break;
                    case MDEC44:
                        retval = EvalCValue(atypes, C_4, C_4);
                        break;

                    case MDEO11:
                        retval = EvalCValue(atypes, O_1, O_1);
                        break;
                    case MDEO12:
                        retval = EvalCValue(atypes, O_1, O_2);
                        break;
                    case MDEO22:
                        retval = EvalCValue(atypes, O_2, O_2);
                        break;

                    case MDEN11:
                        retval = EvalCValue(atypes, N_1, N_1);
                        break;
                    case MDEN12:
                        retval = EvalCValue(atypes, N_1, N_2);
                        break;
                    case MDEN13:
                        retval = EvalCValue(atypes, N_1, N_3);
                        break;
                    case MDEN22:
                        retval = EvalCValue(atypes, N_2, N_2);
                        break;
                    case MDEN23:
                        retval = EvalCValue(atypes, N_2, N_3);
                        break;
                    case MDEN33:
                        retval = EvalCValue(atypes, N_3, N_3);
                        break;
                }

                return retval;
            }

            private int[][] EvalATable(int atomicNum)
            {
                var natom = container.Atoms.Count;
                var atypes = Arrays.CreateJagged<int>(natom, 2);
                for (int i = 0; i < natom; i++)
                {
                    var atom = container.Atoms[i];
                    var numConnectedBonds = container.GetConnectedBonds(atom).Count();
                    atypes[i][1] = i;
                    if (atomicNum == atom.AtomicNumber)
                        atypes[i][0] = numConnectedBonds;
                    else
                        atypes[i][0] = -1;
                }
                return atypes;
            }

            private double EvalCValue(int[][] codemat, int type1, int type2)
            {
                double lambda = 1;
                double n = 0;

                var v1 = new List<int>();
                var v2 = new List<int>();
                for (int i = 0; i < codemat.Length; i++)
                {
                    if (codemat[i][0] == type1) v1.Add(codemat[i][1]);
                    if (codemat[i][0] == type2) v2.Add(codemat[i][1]);
                }

                for (int i = 0; i < v1.Count; i++)
                {
                    for (int j = 0; j < v2.Count; j++)
                    {
                        var a = v1[i];
                        var b = v2[j];
                        if (a == b)
                            continue;
                        var distance = tdist[a][b];
                        lambda = lambda * distance;
                        n++;
                    }
                }

                if (type1 == type2)
                {
                    lambda = Math.Sqrt(lambda);
                    n = n / 2;
                }
                if (n == 0)
                    return 0.0;
                else
                    return n / Math.Pow(Math.Pow(lambda, 1.0 / (2.0 * n)), 2);
            }
        }       

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
