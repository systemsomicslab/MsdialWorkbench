/* Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using MathNet.Numerics.LinearAlgebra;
using NCDK.Common.Collections;
using NCDK.Geometries;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Holistic descriptors described by Todeschini et al <token>cdk-cite-TOD98</token>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Currently weighting schemes 1,2,3,4, and 5 are implemented. The weight values
    /// are taken from <token>cdk-cite-TOD98</token> and as a result 19 elements are considered.
    /// </para>
    /// <para>For each weighting scheme we can obtain
    /// <list type="bullet"> 
    /// <item>11 directional WHIM descriptors (É…<sub>1 .. 3</sub>, ÉÀ<sub>1 .. 2</sub>, É¡<sub>1 .. 3</sub>, É≈<sub>1 .. 3</sub>)</item>
    /// <item>6 non-directional WHIM descriptors (T, A, V, K, G, D)</item>
    /// </list>
    /// </para>
    /// <para>Though <token>cdk-cite-TOD98</token> mentions that for planar molecules only 8 directional WHIM
    /// descriptors are required the current code will return all 11.
    /// </para>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2004-12-1
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:WHIM
    // @cdk.keyword WHIM
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#WHIM", Requirements = DescriptorRequirements.Geometry3D)]
    public class WHIMDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private static readonly IReadOnlyDictionary<int, double> hashatwt = new Dictionary<int, double>
        {
            [AtomicNumbers.H] = 0.084,
            [AtomicNumbers.B] = 0.900,
            [AtomicNumbers.C] = 1.000,
            [AtomicNumbers.N] = 1.166,
            [AtomicNumbers.O] = 1.332,
            [AtomicNumbers.F] = 1.582,
            [AtomicNumbers.Al] = 2.246,
            [AtomicNumbers.Si] = 2.339,
            [AtomicNumbers.P] = 2.579,
            [AtomicNumbers.S] = 2.670,
            [AtomicNumbers.Cl] = 2.952,
            [AtomicNumbers.Fe] = 4.650,
            [AtomicNumbers.Co] = 4.907,
            [AtomicNumbers.Ni] = 4.887,
            [AtomicNumbers.Cu] = 5.291,
            [AtomicNumbers.Zn] = 5.445,
            [AtomicNumbers.Br] = 6.653,
            [AtomicNumbers.Sn] = 9.884,
            [AtomicNumbers.I] = 10.566,
        };

        private static readonly IReadOnlyDictionary<int, double> hashvdw = new Dictionary<int, double>
        {
            [AtomicNumbers.H] = 0.299,
            [AtomicNumbers.B] = 0.796,
            [AtomicNumbers.C] = 1.000,
            [AtomicNumbers.N] = 0.695,
            [AtomicNumbers.O] = 0.512,
            [AtomicNumbers.F] = 0.410,
            [AtomicNumbers.Al] = 1.626,
            [AtomicNumbers.Si] = 1.424,
            [AtomicNumbers.P] = 1.181,
            [AtomicNumbers.S] = 1.088,
            [AtomicNumbers.Cl] = 1.035,
            [AtomicNumbers.Fe] = 1.829,
            [AtomicNumbers.Co] = 1.561,
            [AtomicNumbers.Ni] = 0.764,
            [AtomicNumbers.Cu] = 0.512,
            [AtomicNumbers.Zn] = 1.708,
            [AtomicNumbers.Br] = 1.384,
            [AtomicNumbers.Sn] = 2.042,
            [AtomicNumbers.I] = 1.728,
        };

        private static readonly IReadOnlyDictionary<int, double> hasheneg = new Dictionary<int, double>
        {
            [AtomicNumbers.H] = 0.944,
            [AtomicNumbers.B] = 0.828,
            [AtomicNumbers.C] = 1.000,
            [AtomicNumbers.N] = 1.163,
            [AtomicNumbers.O] = 1.331,
            [AtomicNumbers.F] = 1.457,
            [AtomicNumbers.Al] = 0.624,
            [AtomicNumbers.Si] = 0.779,
            [AtomicNumbers.P] = 0.916,
            [AtomicNumbers.S] = 1.077,
            [AtomicNumbers.Cl] = 1.265,
            [AtomicNumbers.Fe] = 0.728,
            [AtomicNumbers.Co] = 0.728,
            [AtomicNumbers.Ni] = 0.728,
            [AtomicNumbers.Cu] = 0.740,
            [AtomicNumbers.Zn] = 0.810,
            [AtomicNumbers.Br] = 1.172,
            [AtomicNumbers.Sn] = 0.837,
            [AtomicNumbers.I] = 1.012,
        };

        private static readonly IReadOnlyDictionary<int, double> hashpol = new Dictionary<int, double>
        {
            [AtomicNumbers.H] = 0.379,
            [AtomicNumbers.B] = 1.722,
            [AtomicNumbers.C] = 1.000,
            [AtomicNumbers.N] = 0.625,
            [AtomicNumbers.O] = 0.456,
            [AtomicNumbers.F] = 0.316,
            [AtomicNumbers.Al] = 3.864,
            [AtomicNumbers.Si] = 3.057,
            [AtomicNumbers.P] = 2.063,
            [AtomicNumbers.S] = 1.648,
            [AtomicNumbers.Cl] = 1.239,
            [AtomicNumbers.Fe] = 4.773,
            [AtomicNumbers.Co] = 4.261,
            [AtomicNumbers.Ni] = 3.864,
            [AtomicNumbers.Cu] = 3.466,
            [AtomicNumbers.Zn] = 4.034,
            [AtomicNumbers.Br] = 1.733,
            [AtomicNumbers.Sn] = 4.375,
            [AtomicNumbers.I] = 3.040,
        };

        /// <summary>
        /// Atom weighting types
        /// </summary>
        public enum AtomWeightingType
        {
            /// <summary>
            /// unit weights
            /// </summary>
            Unity = 1,

            /// <summary>
            /// atomic masses
            /// </summary>
            Mass = 2,

            /// <summary>
            /// van der Waals volumes
            /// </summary>
            Volume = 3,

            /// <summary>
            /// Mulliken atomic electronegativites
            /// </summary>
            Electronegativity = 4,

            /// <summary>
            /// atomic polarizabilities
            /// </summary>
            Polarizability = 5,
        }

        public WHIMDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(
                double Wlambda1,
                double Wlambda2,
                double Wlambda3,
                double Wnu1,
                double Wnu2,
                double Wgamma1,
                double Wgamma2,
                double Wgamma3,
                double Weta1,
                double Weta2,
                double Weta3,
                double WT,
                double WA,
                double WV,
                double WK,
                double WG,
                double WD)
            {
                this.Wlambda1 = Wlambda1;
                this.Wlambda2 = Wlambda2;
                this.Wlambda3 = Wlambda3;
                this.Wnu1 = Wnu1;
                this.Wnu2 = Wnu2;
                this.Wgamma1 = Wgamma1;
                this.Wgamma2 = Wgamma2;
                this.Wgamma3 = Wgamma3;
                this.Weta1 = Weta1;
                this.Weta2 = Weta2;
                this.Weta3 = Weta3;
                this.WT = WT;
                this.WA = WA;
                this.WV = WV;
                this.WK = WK;
                this.WG = WG;
                this.WD = WD;
            }

            [DescriptorResultProperty] public double Wlambda1 { get; private set; }
            [DescriptorResultProperty] public double Wlambda2 { get; private set; }
            [DescriptorResultProperty] public double Wlambda3 { get; private set; }
            [DescriptorResultProperty] public double Wnu1 { get; private set; }
            [DescriptorResultProperty] public double Wnu2 { get; private set; }
            [DescriptorResultProperty] public double Wgamma1 { get; private set; }
            [DescriptorResultProperty] public double Wgamma2 { get; private set; }
            [DescriptorResultProperty] public double Wgamma3 { get; private set; }
            [DescriptorResultProperty] public double Weta1 { get; private set; }
            [DescriptorResultProperty] public double Weta2 { get; private set; }
            [DescriptorResultProperty] public double Weta3 { get; private set; }
            [DescriptorResultProperty] public double WT { get; private set; }
            [DescriptorResultProperty] public double WA { get; private set; }
            [DescriptorResultProperty] public double WV { get; private set; }
            [DescriptorResultProperty] public double WK { get; private set; }
            [DescriptorResultProperty] public double WG { get; private set; }
            [DescriptorResultProperty] public double WD { get; private set; }
        }

        /// <summary>
        /// Calculates 11 directional and 6 non-directional WHIM descriptors for.
        /// the specified weighting scheme
        /// </summary>
        /// <returns>An ArrayList containing the descriptors in the order described above.</returns>
        public Result Calculate(IAtomContainer container, AtomWeightingType type = AtomWeightingType.Unity)
        {
            if (!GeometryUtil.Has3DCoordinates(container)) {
                Console.WriteLine("Error: Molecule must have 3D coordinates");
                return null;
                //throw new ThreeDRequiredException("Molecule must have 3D coordinates");
            }
            double sum = 0.0;
            var ac = (IAtomContainer)container.Clone();

            // do aromaticity detecttion for calculating polarizability later on
            //HueckelAromaticityDetector had = new HueckelAromaticityDetector();
            //had.DetectAromaticity(ac);

            // get the coordinate matrix
            var cmat = Arrays.CreateJagged<double>(ac.Atoms.Count, 3);
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                var coords = ac.Atoms[i].Point3D.Value;
                cmat[i][0] = coords.X;
                cmat[i][1] = coords.Y;
                cmat[i][2] = coords.Z;
            }

            // set up the weight vector
            var wt = new double[ac.Atoms.Count];
            IReadOnlyDictionary<int, double> hash = null;
            switch (type)
            {
                case AtomWeightingType.Mass:
                    hash = hashatwt;
                    break;
                case AtomWeightingType.Volume:
                    hash = hashvdw;
                    break;
                case AtomWeightingType.Electronegativity:
                    hash = hasheneg;
                    break;
                case AtomWeightingType.Polarizability:
                    hash = hashpol;
                    break;
                default:
                    break;
            }
            switch (type)
            {
                case AtomWeightingType.Unity:
                    for (int i = 0; i < ac.Atoms.Count; i++)
                        wt[i] = 1.0;
                    break;
                default:
                    for (int i = 0; i < ac.Atoms.Count; i++)
                        wt[i] = hash[ac.Atoms[i].AtomicNumber];
                    break;
            }

            PCA pcaobject = null;
            try
            {
                pcaobject = new PCA(cmat, wt);
            }
            catch (CDKException cdke)
            {
                Debug.WriteLine(cdke);
            }

            // directional WHIM's
            var lambda = pcaobject.GetEigenvalues();
            var gamma = new double[3];
            var nu = new double[3];
            var eta = new double[3];

            for (int i = 0; i < 3; i++)
                sum += lambda[i];
            for (int i = 0; i < 3; i++)
                nu[i] = lambda[i] / sum;

            var scores = pcaobject.GetScores();
            for (int i = 0; i < 3; i++)
            {
                sum = 0.0;
                for (int j = 0; j < ac.Atoms.Count; j++)
                    sum += scores[j][i] * scores[j][i] * scores[j][i] * scores[j][i];
                sum = sum / (lambda[i] * lambda[i] * ac.Atoms.Count);
                eta[i] = 1.0 / sum;
            }

            // look for symmetric & asymmetric atoms for the gamma descriptor
            for (int i = 0; i < 3; i++)
            {
                double ns = 0.0;
                double na = 0.0;
                for (int j = 0; j < ac.Atoms.Count; j++)
                {
                    bool foundmatch = false;
                    for (int k = 0; k < ac.Atoms.Count; k++)
                    {
                        if (k == j)
                            continue;
                        if (scores[j][i] == -1 * scores[k][i])
                        {
                            ns++;
                            foundmatch = true;
                            break;
                        }
                    }
                    if (!foundmatch) na++;
                }
                var n = (double)ac.Atoms.Count;
                gamma[i] = -1.0 * ((ns / n) * Math.Log(ns / n) / Math.Log(2.0) + (na / n) * Math.Log(1.0 / n) / Math.Log(2.0));
                gamma[i] = 1.0 / (1.0 + gamma[i]);
            }
            {
                // non directional WHIMS's
                var t = lambda[0] + lambda[1] + lambda[2];
                var a = lambda[0] * lambda[1] + lambda[0] * lambda[2] + lambda[1] * lambda[2];
                var v = t + a + lambda[0] * lambda[1] * lambda[2];

                double k = 0.0;
                sum = 0.0;
                for (int i = 0; i < 3; i++)
                    sum += lambda[i];
                for (int i = 0; i < 3; i++)
                    k = (lambda[i] / sum) - (1.0 / 3.0);
                k = k / (4.0 / 3.0);

                var g = Math.Pow(gamma[0] * gamma[1] * gamma[2], 1.0 / 3.0);
                var d = eta[0] + eta[1] + eta[2];

                // return all the stuff we calculated
                return new Result(
                    lambda[0],
                    lambda[1],
                    lambda[2],

                    nu[0],
                    nu[1],

                    gamma[0],
                    gamma[1],
                    gamma[2],

                    eta[0],
                    eta[1],
                    eta[2],

                    t,
                    a,
                    v,
                    k,
                    g,
                    d);
            }
        }

        class PCA
        {
            private readonly Matrix<double> evec;
            private Matrix<double> t;
            private readonly double[] eval;

            public PCA(double[][] cmat, double[] wt)
            {
                int ncol = 3;
                int nrow = wt.Length;

                if (cmat.Length != wt.Length)
                    throw new CDKException($"{nameof(WHIMDescriptor)}: number of weights should be equal to number of atoms");

                // make a copy of the coordinate matrix
                var d = Arrays.CreateJagged<double>(nrow, ncol);
                for (int i = 0; i < nrow; i++)
                {
                    for (int j = 0; j < ncol; j++)
                        d[i][j] = cmat[i][j];
                }

                // do mean centering - though the first paper used
                // barymetric centering
                for (int i = 0; i < ncol; i++)
                {
                    double mean = 0.0;
                    for (int j = 0; j < nrow; j++)
                        mean += d[j][i];
                    mean = mean / (double)nrow;
                    for (int j = 0; j < nrow; j++)
                        d[j][i] = (d[j][i] - mean);
                }

                // get the covariance matrix
                var covmat = Arrays.CreateJagged<double>(ncol, ncol);
                double sumwt = 0;
                for (int i = 0; i < nrow; i++)
                    sumwt += wt[i];
                for (int i = 0; i < ncol; i++)
                {
                    double meanx = 0;
                    for (int k = 0; k < nrow; k++)
                        meanx += d[k][i];
                    meanx = meanx / (double)nrow;
                    for (int j = 0; j < ncol; j++)
                    {
                        double meany = 0.0;
                        for (int k = 0; k < nrow; k++)
                            meany += d[k][j];
                        meany = meany / (double)nrow;

                        double sum = 0;
                        for (int k = 0; k < nrow; k++)
                        {
                            sum += wt[k] * (d[k][i] - meanx) * (d[k][j] - meany);
                        }
                        covmat[i][j] = sum / sumwt;
                    }
                }

                // get the loadings (ie eigenvector matrix)
                var m = Matrix<double>.Build.DenseOfColumnArrays(covmat);
                var ed = m.Evd();
                this.eval = ed.EigenValues.Select(n => n.Real).ToArray();
                this.evec = ed.EigenVectors;
                var x = Matrix<double>.Build.DenseOfColumnArrays(d);
                this.t = this.evec * x;
            }

            public double[] GetEigenvalues()
            {
                return (this.eval);
            }

            public double[][] GetScores()
            {
                return t.ToColumnArrays();
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
