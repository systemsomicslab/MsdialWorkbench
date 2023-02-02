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

using NCDK.Geometries;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Descriptor characterizing the mass distribution of the molecule.
    /// </summary>
    /// <remarks>
    /// Described by Katritzky et al. <token>cdk-cite-KAT96</token>.
    /// For modelling purposes the value of the descriptor is calculated
    /// both with and without H atoms. Furthermore the square and cube roots
    /// of the descriptor are also generated as described by Wessel et al. <token>cdk-cite-WES98</token>.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2004-11-23
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:gravitationalIndex
    // @cdk.keyword gravitational index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#gravitationalIndex", Requirements = DescriptorRequirements.Geometry3D)]
    public class GravitationalIndexDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public GravitationalIndexDescriptor()
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
            /// gravitational index of heavy atoms
            /// </summary>
            [DescriptorResultProperty("GRAV-1")]
            public double GRAV1 => Values[0];

            /// <summary>
            /// square root of gravitational index of heavy atoms
            /// </summary>
            [DescriptorResultProperty("GRAV-2")]
            public double GRAV2 => Values[1];

            /// <summary>
            /// cube root of gravitational index of heavy atoms
            /// </summary>
            [DescriptorResultProperty("GRAV-3")]
            public double GRAV3 => Values[2];

            /// <summary>
            /// gravitational index - hydrogens included
            /// </summary>
            [DescriptorResultProperty("GRAVH-1")]
            public double GRAVH1 => Values[3];

            /// <summary>
            /// square root of hydrogen-included gravitational index
            /// </summary>
            [DescriptorResultProperty("GRAVH-2")]
            public double GRAVH2 => Values[4];

            /// <summary>
            /// cube root of hydrogen-included gravitational index
            /// </summary>
            [DescriptorResultProperty("GRAVH-3")]
            public double GRAVH3 => Values[5];

            /// <summary>
            /// grav1 for all pairs of atoms (not just bonded pairs)
            /// </summary>
            [DescriptorResultProperty("GRAV-4")]
            public double GRAV4 => Values[6];

            /// <summary>
            /// grav2 for all pairs of atoms (not just bonded pairs)
            /// </summary>
            [DescriptorResultProperty("GRAV-5")]
            public double GRAV5 => Values[7];

            /// <summary>
            /// grav3 for all pairs of atoms (not just bonded pairs)
            /// </summary>
            [DescriptorResultProperty("GRAV-6")]
            public double GRAV6 => Values[8];


            public new IReadOnlyList<double> Values { get; private set; }
        }

        private struct Pair
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Calculates the 9 gravitational indices.
        /// </summary>
        /// <returns>An ArrayList containing 9 elements in the order described above</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            if (!GeometryUtil.Has3DCoordinates(container)) {
                Console.WriteLine("Error: Molecule must have 3D coordinates");
                return null;
                //throw new ThreeDRequiredException("Molecule must have 3D coordinates");
            }

            var factory = CDK.IsotopeFactory;
            double sum = 0;
            foreach (var bond in container.Bonds)
            {
                if (bond.Atoms.Count != 2)
                    throw new CDKException("GravitationalIndex: Only handles 2 center bonds");

                var mass1 = factory.GetMajorIsotope(bond.Atoms[0].Symbol).MassNumber.Value;
                var mass2 = factory.GetMajorIsotope(bond.Atoms[1].Symbol).MassNumber.Value;

                var p1 = bond.Atoms[0].Point3D.Value;
                var p2 = bond.Atoms[1].Point3D.Value;

                var x1 = p1.X;
                var y1 = p1.Y;
                var z1 = p1.Z;
                var x2 = p2.X;
                var y2 = p2.Y;
                var z2 = p2.Z;

                var dist = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);
                sum += (mass1 * mass2) / dist;
            }

            // heavy atoms only
            double heavysum = 0;
            foreach (var b in container.Bonds)
            {
                if (b.Atoms.Count != 2)
                    throw new CDKException("GravitationalIndex: Only handles 2 center bonds");

                if (b.Atoms[0].AtomicNumber.Equals(AtomicNumbers.H) || b.Atoms[1].AtomicNumber.Equals(AtomicNumbers.H))
                    continue;

                var mass1 = factory.GetMajorIsotope(b.Atoms[0].Symbol).MassNumber.Value;
                var mass2 = factory.GetMajorIsotope(b.Atoms[1].Symbol).MassNumber.Value;

                var point0 = b.Atoms[0].Point3D.Value;
                var point1 = b.Atoms[1].Point3D.Value;

                var x1 = point0.X;
                var y1 = point0.Y;
                var z1 = point0.Z;
                var x2 = point1.X;
                var y2 = point1.Y;
                var z2 = point1.Z;

                var dist = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);
                heavysum += (mass1 * mass2) / dist;
            }

            // all pairs
            var x = new List<int>();
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                if (!container.Atoms[i].AtomicNumber.Equals(AtomicNumbers.H))
                    x.Add(i);
            }
            var npair = x.Count * (x.Count - 1) / 2;
            var p = new Pair[npair];
            for (int i = 0; i < npair; i++)
                p[i] = new Pair();
            int pcount = 0;
            for (int i = 0; i < x.Count - 1; i++)
            {
                for (int j = i + 1; j < x.Count; j++)
                {
                    int present = 0;
                    var a = x[i];
                    var b = x[j];
                    for (int k = 0; k < pcount; k++)
                    {
                        if ((p[k].X == a && p[k].Y == b) || (p[k].Y == a && p[k].X == b))
                            present = 1;
                    }
                    if (present == 1)
                        continue;
                    p[pcount].X = a;
                    p[pcount].Y = b;
                    pcount += 1;
                }
            }
            double allheavysum = 0;
            foreach (var aP in p)
            {
                var atomNumber1 = aP.X;
                var atomNumber2 = aP.Y;

                var mass1 = factory.GetMajorIsotope(container.Atoms[atomNumber1].Symbol).MassNumber.Value;
                var mass2 = factory.GetMajorIsotope(container.Atoms[atomNumber2].Symbol).MassNumber.Value;

                var x1 = container.Atoms[atomNumber1].Point3D.Value.X;
                var y1 = container.Atoms[atomNumber1].Point3D.Value.Y;
                var z1 = container.Atoms[atomNumber1].Point3D.Value.Z;
                var x2 = container.Atoms[atomNumber2].Point3D.Value.X;
                var y2 = container.Atoms[atomNumber2].Point3D.Value.Y;
                var z2 = container.Atoms[atomNumber2].Point3D.Value.Z;

                var dist = (x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2) + (z1 - z2) * (z1 - z2);
                allheavysum += (mass1 * mass2) / dist;
            }

            return new Result(new double[] 
                {
                    heavysum,
                    Math.Sqrt(heavysum),
                    Math.Pow(heavysum, 1.0 / 3.0),

                    sum,
                    Math.Sqrt(sum),
                    Math.Pow(sum, 1.0 / 3.0),

                    allheavysum,
                    Math.Sqrt(allheavysum),
                    Math.Pow(allheavysum, 1.0 / 3.0)
                });
        }
       
        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
