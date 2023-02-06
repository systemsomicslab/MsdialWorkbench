/* Copyright (C) 2007  Rajarshi Guha <rajarshi@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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
using NCDK.Geometries;
using NCDK.Tools;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates length over breadth descriptors.
    /// </summary>
    /// <remarks>
    /// The current implementation reproduces the results obtained from the LOVERB descriptor
    /// routine in ADAPT. As a result ti does not perform any orientation and only considers the
    /// X &amp; Y extents for a series of rotations about the Z axis (in 10 degree increments).
    /// <note type="note">The descriptor assumes that the atoms have been configured.</note>
    /// </remarks>
    // @author      Rajarshi Guha
    // @cdk.created 2006-09-26
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:lengthOverBreadth
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#lengthOverBreadth", Requirements = DescriptorRequirements.Geometry3D)]
    public class LengthOverBreadthDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public LengthOverBreadthDescriptor()
        {            
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<double> values)
            {
                this.Values = values;
            }

            /// <summary>
            /// The maximum L/B ratio
            /// </summary>
            [DescriptorResultProperty("LOBMAX")]
            public double MaxLOB => Values[0];

            /// <summary>
            /// The L/B ratio for the rotation that results in the minimum area
            /// (defined by the product of the X &amp; Y extents for that orientation)
            /// </summary>
            [DescriptorResultProperty("LOBMIN")]
            public double MinLOB => Values[1];

            public new IReadOnlyList<double> Values { get; private set; }
        }

        /// <summary>
        /// Evaluate the descriptor for the molecule.
        /// </summary>
        /// <returns>LOBMAX and LOBMIN in that order</returns>
        public Result Calculate(IAtomContainer container)
        {
            if (!GeometryUtil.Has3DCoordinates(container)) {
                Console.WriteLine("Error: Molecule must have 3D coordinates");
                return null;
                //throw new ThreeDRequiredException("Molecule must have 3D coordinates");
            }

            double angle = 10.0;
            double maxLOB = 0;
            double minArea = 1e6;
            double mmLOB = 0;

            double lob, bol, area;
            double[] xyzRanges;

            var coords = Arrays.CreateJagged<double>(container.Atoms.Count, 3);
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var p = container.Atoms[i].Point3D.Value;
                coords[i][0] = p.X;
                coords[i][1] = p.Y;
                coords[i][2] = p.Z;
            }

            // get the com
            var acom = GeometryUtil.Get3DCentreOfMass(container);
            if (acom == null)
                throw new CDKException("Error in center of mass calculation, has exact mass been set on all atoms?");
            var com = acom.Value;

            // translate everything to COM
            for (int i = 0; i < coords.Length; i++)
            {
                coords[i][0] -= com.X;
                coords[i][1] -= com.Y;
                coords[i][2] -= com.Z;
            }

            int nangle = (int)(90 / angle);
            for (int i = 0; i < nangle; i++)
            {
                RotateZ(coords, Math.PI / 180.0 * angle);
                xyzRanges = Extents(container, coords, true);
                lob = xyzRanges[0] / xyzRanges[1];
                bol = 1.0 / lob;
                if (lob < bol)
                {
                    double tmp = lob;
                    lob = bol;
                    bol = tmp;
                }
                area = xyzRanges[0] * xyzRanges[1];
                if (lob > maxLOB) maxLOB = lob;
                if (area < minArea)
                {
                    minArea = area;
                    mmLOB = lob;
                }
            }

            return new Result(new[] { maxLOB, mmLOB });
        }

        private static void RotateZ(double[][] coords, double theta)
        {
            int natom = coords.Length;
            double[][] m;
            m = Arrays.CreateJagged<double>(4, 4);
            m[0][0] = Math.Cos(theta);
            m[0][1] = Math.Sin(theta);
            m[1][0] = -1 * Math.Sin(theta);
            m[1][1] = Math.Cos(theta);
            m[2][2] = 1;
            m[3][3] = 1;
            var rZ = Matrix<double>.Build.DenseOfColumnArrays(m);
            m = Arrays.CreateJagged<double>(4, natom);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < natom; j++)
                {
                    m[i][j] = coords[j][i];
                    m[3][j] = 1;
                }
            }
            var newCoord = Matrix<double>.Build.DenseOfColumnArrays(m);
            newCoord = newCoord * rZ;
            for (int i = 0; i < natom; i++)
            {
                for (int j = 0; j < 3; j++)
                    coords[i][j] = newCoord[i, j];
            }
        }

        private static double[] Extents(IAtomContainer container, double[][] coords, bool withRadii)
        {
            double xmax = -1e30;
            double ymax = -1e30;
            double zmax = -1e30;

            double xmin = 1e30;
            double ymin = 1e30;
            double zmin = 1e30;

            int natom = container.Atoms.Count;
            for (int i = 0; i < natom; i++)
            {
                var coord = new double[coords[0].Length];
                Array.Copy(coords[i], 0, coord, 0, coords[0].Length);
                if (withRadii)
                {
                    var atom = container.Atoms[i];
                    var radius = PeriodicTable.GetCovalentRadius(atom.Symbol).Value;
                    xmax = Math.Max(xmax, coord[0] + radius);
                    ymax = Math.Max(ymax, coord[1] + radius);
                    zmax = Math.Max(zmax, coord[2] + radius);

                    xmin = Math.Min(xmin, coord[0] - radius);
                    ymin = Math.Min(ymin, coord[1] - radius);
                    zmin = Math.Min(zmin, coord[2] - radius);
                }
                else
                {
                    xmax = Math.Max(xmax, coord[0]);
                    ymax = Math.Max(ymax, coord[1]);
                    zmax = Math.Max(zmax, coord[2]);

                    xmin = Math.Min(xmin, coord[0]);
                    ymin = Math.Min(ymin, coord[1]);
                    zmin = Math.Min(zmin, coord[2]);
                }
            }
            var ranges = new double[3];
            ranges[0] = xmax - xmin;
            ranges[1] = ymax - ymin;
            ranges[2] = zmax - zmin;
            return ranges;
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
