/* Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
 *                    2014  Egon Willighagen <egonw@users.sf.net>
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

using MathNet.Numerics.LinearAlgebra;
using NCDK.Common.Collections;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Geometries.Alignments
{
    /// <summary>
    /// Aligns two structures to minimize the RMSD using the Kabsch algorithm.
    /// </summary>
    /// <remarks>
    /// This class is an implementation of the Kabsch algorithm (<token>cdk-cite-KAB76</token>, <token>cdk-cite-KAB78</token>)
    /// and evaluates the optimal rotation matrix (U) to minimize the RMSD between the two structures.
    /// Since the algorithm assumes that the number of points are the same in the two structures
    /// it is the job of the caller to pass the proper number of atoms from the two structures. Constructors
    /// which take whole <see cref="IAtomContainer"/>'s are provided but they should have the same number
    /// of atoms.
    /// The algorithm allows for the use of atom weightings and by default all points are given a weight of 1.0,
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Geometries.Alignments.KabschAlignment_Example.cs"]/*' />
    /// In many cases, molecules will be aligned based on some common substructure.
    /// In this case the center of masses calculated during alignment refer to these
    /// substructures rather than the whole molecules. To superimpose the molecules
    /// for display, the second molecule must be rotated and translated by calling
    /// <see cref="RotateAtomContainer(IAtomContainer)"/>. However, since this will also translate the
    /// second molecule, the first molecule should also be translated to the center of mass
    /// of the substructure specified for this molecule. This center of mass can be obtained
    /// by a call to <see cref="CenterOfMass"/> and then manually translating the coordinates.
    /// Thus an example would be
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Geometries.Alignments.KabschAlignment_Example.cs+substructure"]/*' />
    ///</example>
    // @author           Rajarshi Guha
    // @cdk.created      2004-12-11
    // @cdk.dictref      blue-obelisk:alignmentKabsch
    public class KabschAlignment
    {
        /// <summary>
        /// The rotation matrix (u).
        /// </summary>
        /// <see cref="Align"/>
        public IReadOnlyList<IReadOnlyList<double>> RotationMatrix => rotationMatrix;

        private double[][] rotationMatrix;

        /// <summary>
        /// The RMSD from the alignment.
        /// If Align() has not been called the return value is -1.0
        /// </summary>
        /// <see cref="Align"/>
        public double RMSD { get; private set; } = -1;

        private readonly Vector3[] p1, p2;
        private Vector3[] rp;  // rp are the rotated coordinates
        private readonly double[] wts;
        private readonly int npoint;
        private Vector3 cm1;
        private Vector3 cm2;
        private readonly double[] atwt1, atwt2;

        private static Vector3[] GetPoint3DArray(IReadOnlyList<IAtom> a)
        {
            var p = new Vector3[a.Count];
            for (int i = 0; i < a.Count; i++)
            {
                p[i] = a[i].Point3D.Value;
            }
            return p;
        }

        private static Vector3[] GetPoint3DArray(IAtomContainer ac)
        {
            var p = new Vector3[ac.Atoms.Count];
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                p[i] = ac.Atoms[i].Point3D.Value;
            }
            return p;
        }

        private static double[] GetAtomicMasses(IReadOnlyList<IAtom> a)
        {
            var am = new double[a.Count];
            var factory = CDK.IsotopeFactory;
            for (int i = 0; i < a.Count; i++)
                am[i] = factory.GetMajorIsotope(a[i].Symbol).ExactMass.Value;
            return am;
        }

        private static double[] GetAtomicMasses(IAtomContainer ac)
        {
            var am = new double[ac.Atoms.Count];
            var factory = CDK.IsotopeFactory;
            for (int i = 0; i < ac.Atoms.Count; i++)
                am[i] = factory.GetMajorIsotope(ac.Atoms[i].Symbol).ExactMass.Value;
            return am;
        }

        private static Vector3 GetCenterOfMass(Vector3[] p, double[] atwt)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            double totalmass = 0;
            for (int i = 0; i < p.Length; i++)
            {
                x += atwt[i] * p[i].X;
                y += atwt[i] * p[i].Y;
                z += atwt[i] * p[i].Z;
                totalmass += atwt[i];
            }
            return new Vector3(x / totalmass, y / totalmass, z / totalmass);
        }

        /// <summary>
        /// Sets up variables for the alignment algorithm.
        /// The algorithm allows for atom weighting and the default is 1.0 for all atoms.
        /// </summary>
        /// <param name="al1">An array of <see cref="IAtom"/> objects</param>
        /// <param name="al2">An array of <see cref="IAtom"/> objects. This array will have its coordinates rotated so that the RMDS is minimized to the coordinates of the first array</param>
        /// <exception cref="CDKException">if the number of Atom's are not the same in the two arrays</exception>
        public KabschAlignment(IEnumerable<IAtom> al1, IEnumerable<IAtom> al2)
        {
            var _al1 = al1.ToReadOnlyList();
            var _al2 = al2.ToReadOnlyList();

            if (_al1.Count != _al2.Count)
            {
                throw new CDKException("The Atom[]'s being aligned must have the same numebr of atoms");
            }
            this.npoint = _al1.Count;
            this.p1 = GetPoint3DArray(_al1);
            this.p2 = GetPoint3DArray(_al2);
            this.wts = new double[this.npoint];

            this.atwt1 = GetAtomicMasses(_al1);
            this.atwt2 = GetAtomicMasses(_al2);

            for (int i = 0; i < this.npoint; i++)
                this.wts[i] = 1.0;
        }

        /// <summary>
        /// Sets up variables for the alignment algorithm.
        /// </summary>
        /// <param name="al1">An array of <see cref="IAtom"/> objects</param>
        /// <param name="al2">An array of <see cref="IAtom"/> objects. This array will have its coordinates rotated
        ///            so that the RMDS is minimized to the coordinates of the first array</param>
        /// <param name="wts">A vector atom weights.</param>
        /// <exception cref="CDKException">if the number of Atom's are not the same in the two arrays or
        ///                         length of the weight vector is not the same as the Atom arrays</exception>
        public KabschAlignment(IEnumerable<IAtom> al1, IEnumerable<IAtom> al2, IEnumerable<double> wts)
        {
            var _al1 = al1.ToReadOnlyList();
            var _al2 = al2.ToReadOnlyList();
            var _wts = wts.ToReadOnlyList();

            if (_al1.Count != _al2.Count)
            {
                throw new CDKException("The Atom[]'s being aligned must have the same number of atoms");
            }
            if (_al1.Count != _wts.Count)
            {
                throw new CDKException("Number of weights must equal number of atoms");
            }
            this.npoint = _al1.Count;
            this.p1 = GetPoint3DArray(_al1);
            this.p2 = GetPoint3DArray(_al2);
            this.wts = _wts.Take(this.npoint).ToArray();

            this.atwt1 = GetAtomicMasses(_al1);
            this.atwt2 = GetAtomicMasses(_al2);
        }

        /// <summary>
        /// Sets up variables for the alignment algorithm.
        /// The algorithm allows for atom weighting and the default is 1.0 for all
        /// atoms.
        /// </summary>
        /// <param name="ac1">An <see cref="IAtomContainer"/></param>
        /// <param name="ac2">An <see cref="IAtomContainer"/>. This AtomContainer will have its coordinates rotated
        ///            so that the RMDS is minimized to the coordinates of the first one</param>
        /// <exception cref="CDKException">if the number of atom's are not the same in the two AtomContainer's</exception>
        public KabschAlignment(IAtomContainer ac1, IAtomContainer ac2)
        {
            if (ac1.Atoms.Count != ac2.Atoms.Count)
            {
                throw new CDKException("The AtomContainer's being aligned must have the same number of atoms");
            }
            this.npoint = ac1.Atoms.Count;
            this.p1 = GetPoint3DArray(ac1);
            this.p2 = GetPoint3DArray(ac2);
            this.wts = new double[npoint];
            for (int i = 0; i < npoint; i++)
                this.wts[i] = 1.0;

            this.atwt1 = GetAtomicMasses(ac1);
            this.atwt2 = GetAtomicMasses(ac2);
        }

        /// <summary>
        /// Sets up variables for the alignment algorithm.
        /// </summary>
        /// <param name="ac1">An <see cref="IAtomContainer"/></param>
        /// <param name="ac2">An <see cref="IAtomContainer"/>. This AtomContainer will have its coordinates rotated
        ///            so that the RMDS is minimized to the coordinates of the first one</param>
        /// <param name="wts">A vector atom weights.</param>
        /// <exception cref="CDKException">if the number of atom's are not the same in the two AtomContainer's or
        ///                         length of the weight vector is not the same as number of atoms.</exception>
        public KabschAlignment(IAtomContainer ac1, IAtomContainer ac2, double[] wts)
        {
            if (ac1.Atoms.Count != ac2.Atoms.Count)
            {
                throw new CDKException("The AtomContainer's being aligned must have the same number of atoms");
            }
            if (ac1.Atoms.Count != wts.Length)
            {
                throw new CDKException("Number of weights must equal number of atoms");
            }
            this.npoint = ac1.Atoms.Count;
            this.p1 = GetPoint3DArray(ac1);
            this.p2 = GetPoint3DArray(ac2);
            this.wts = new double[npoint];
            Array.Copy(wts, 0, this.wts, 0, npoint);

            this.atwt1 = GetAtomicMasses(ac1);
            this.atwt2 = GetAtomicMasses(ac2);
        }

        /// <summary>
        /// Perform an alignment.
        /// </summary>
        /// <remarks>
        /// This method aligns to set of atoms which should have been specified
        /// prior to this call
        /// </remarks>
        public void Align()
        {
            Matrix<double> tmp;

            // get center of gravity and translate both to 0,0,0
            this.cm1 = new Vector3();
            this.cm2 = new Vector3();

            this.cm1 = GetCenterOfMass(p1, atwt1);
            this.cm2 = GetCenterOfMass(p2, atwt2);

            // move the points
            for (int i = 0; i < this.npoint; i++)
            {
                p1[i].X = p1[i].X - this.cm1.X;
                p1[i].Y = p1[i].Y - this.cm1.Y;
                p1[i].Z = p1[i].Z - this.cm1.Z;

                p2[i].X = p2[i].X - this.cm2.X;
                p2[i].Y = p2[i].Y - this.cm2.Y;
                p2[i].Z = p2[i].Z - this.cm2.Z;
            }

            // get the R matrix
            var tR = Arrays.CreateJagged<double>(3, 3);
            for (int i = 0; i < this.npoint; i++)
            {
                wts[i] = 1.0;
            }
            for (int i = 0; i < this.npoint; i++)
            {
                tR[0][0] += p1[i].X * p2[i].X * wts[i];
                tR[0][1] += p1[i].X * p2[i].Y * wts[i];
                tR[0][2] += p1[i].X * p2[i].Z * wts[i];

                tR[1][0] += p1[i].Y * p2[i].X * wts[i];
                tR[1][1] += p1[i].Y * p2[i].Y * wts[i];
                tR[1][2] += p1[i].Y * p2[i].Z * wts[i];

                tR[2][0] += p1[i].Z * p2[i].X * wts[i];
                tR[2][1] += p1[i].Z * p2[i].Y * wts[i];
                tR[2][2] += p1[i].Z * p2[i].Z * wts[i];
            }
            double[][] R = Arrays.CreateJagged<double>(3, 3);
            tmp = Matrix<double>.Build.DenseOfColumnArrays(tR);
            R = tmp.Transpose().ToColumnArrays();

            // now get the RtR (=R'R) matrix
            var RtR = Arrays.CreateJagged<double>(3, 3);
            var jamaR = Matrix<double>.Build.DenseOfColumnArrays(R);
            tmp = jamaR * tmp;
            RtR = tmp.ToColumnArrays();

            // get eigenvalues of RRt (a's)
            var jamaRtR = Matrix<double>.Build.DenseOfColumnArrays(RtR);
            var ed = jamaRtR.Evd();
            var mu = ed.EigenValues.Select(n => n.Real).ToArray();
            var a = ed.EigenVectors.ToRowArrays();

            // Jama returns the eigenvalues in increasing order so
            // swap the eigenvalues and vectors
            var tmp2 = mu[2];
            mu[2] = mu[0];
            mu[0] = tmp2;
            for (int i = 0; i < 3; i++)
            {
                tmp2 = a[i][2];
                a[i][2] = a[i][0];
                a[i][0] = tmp2;
            }

            // make sure that the a3 = a1 x a2
            a[0][2] = (a[1][0] * a[2][1]) - (a[1][1] * a[2][0]);
            a[1][2] = (a[0][1] * a[2][0]) - (a[0][0] * a[2][1]);
            a[2][2] = (a[0][0] * a[1][1]) - (a[0][1] * a[1][0]);

            // lets work out the b vectors
            var b = Arrays.CreateJagged<double>(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        b[i][j] += R[i][k] * a[k][j];
                    }
                    b[i][j] = b[i][j] / Math.Sqrt(mu[j]);
                }
            }

            // normalize and set b3 = b1 x b2
            double norm1 = 0;
            double norm2 = 0;
            for (int i = 0; i < 3; i++)
            {
                norm1 += b[i][0] * b[i][0];
                norm2 += b[i][1] * b[i][1];
            }
            norm1 = Math.Sqrt(norm1);
            norm2 = Math.Sqrt(norm2);
            for (int i = 0; i < 3; i++)
            {
                b[i][0] = b[i][0] / norm1;
                b[i][1] = b[i][1] / norm2;
            }
            b[0][2] = (b[1][0] * b[2][1]) - (b[1][1] * b[2][0]);
            b[1][2] = (b[0][1] * b[2][0]) - (b[0][0] * b[2][1]);
            b[2][2] = (b[0][0] * b[1][1]) - (b[0][1] * b[1][0]);

            // get the rotation matrix
            var tU = Arrays.CreateJagged<double>(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        tU[i][j] += b[i][k] * a[j][k];
                    }
                }
            }

            // take the transpose
            rotationMatrix = Arrays.CreateJagged<double>(3, 3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rotationMatrix[i][j] = tU[j][i];
                }
            }

            // now eval the RMS error
            // first, rotate the second set of points and ...
            this.rp = new Vector3[this.npoint];
            for (int i = 0; i < this.npoint; i++)
            {
                this.rp[i] = new Vector3(
                    RotationMatrix[0][0] * p2[i].X + RotationMatrix[0][1] * p2[i].Y + RotationMatrix[0][2] * p2[i].Z,
                    RotationMatrix[1][0] * p2[i].X + RotationMatrix[1][1] * p2[i].Y + RotationMatrix[1][2] * p2[i].Z,
                    RotationMatrix[2][0] * p2[i].X + RotationMatrix[2][1] * p2[i].Y + RotationMatrix[2][2] * p2[i].Z);
            }

            // ... then eval rms
            double rms = 0;
            for (int i = 0; i < this.npoint; i++)
            {
                rms += (p1[i].X - this.rp[i].X) * (p1[i].X - this.rp[i].X) + (p1[i].Y - this.rp[i].Y) * (p1[i].Y - this.rp[i].Y) + (p1[i].Z - this.rp[i].Z) * (p1[i].Z - this.rp[i].Z);
            }
            this.RMSD = Math.Sqrt(rms / this.npoint);
        }

        /// <summary>
        /// The center of mass for the first molecule or fragment used in the calculation.
        /// </summary>
        /// <remarks>
        /// This method is useful when using this class to align the coordinates
        /// of two molecules and them displaying them superimposed. Since the center of
        /// mass used during the alignment may not be based on the whole molecule (in
        /// general common substructures are aligned), when preparing molecules for display
        /// the first molecule should be translated to the center of mass. Then displaying the
        /// first molecule and the rotated version of the second one will result in superimposed
        /// structures.
        /// </remarks>
        public Vector3 CenterOfMass => this.cm1;

        /// <summary>
        /// Rotates the <see cref="IAtomContainer"/> coordinates by the rotation matrix.
        ///
        /// In general if you align a subset of atoms in a AtomContainer
        /// this function can be applied to the whole AtomContainer to rotate all
        /// atoms. This should be called with the second AtomContainer (or Atom[])
        /// that was passed to the constructor.
        /// </summary>
        /// <remarks>
        /// Note that the AtomContainer coordinates also get translated such that the
        /// center of mass of the original fragment used to calculate the alignment is at the origin.
        /// </remarks>
        /// <param name="ac">The <see cref="IAtomContainer"/> whose coordinates are to be rotated</param>
        public void RotateAtomContainer(IAtomContainer ac)
        {
            var p = GetPoint3DArray(ac);
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                // translate the the origin we have calculated
                p[i].X = p[i].X - this.cm2.X;
                p[i].Y = p[i].Y - this.cm2.Y;
                p[i].Z = p[i].Z - this.cm2.Z;

                // do the actual rotation
                ac.Atoms[i].Point3D =
                        new Vector3(
                            RotationMatrix[0][0] * p[i].X + RotationMatrix[0][1] * p[i].Y + RotationMatrix[0][2] * p[i].Z,
                            RotationMatrix[1][0] * p[i].X + RotationMatrix[1][1] * p[i].Y + RotationMatrix[1][2] * p[i].Z,
                            RotationMatrix[2][0] * p[i].X + RotationMatrix[2][1] * p[i].Y + RotationMatrix[2][2] * p[i].Z);
            }
        }
    }
}
