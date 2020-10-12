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

using MathNet.Numerics.LinearAlgebra;
using NCDK.Common.Collections;
using NCDK.Geometries;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// A descriptor that calculates the moment of inertia and radius of gyration.
    /// </summary>
    /// <remarks>
    /// Moment of inertia (MI) values characterize the mass distribution of a molecule.
    /// Related to the MI values, ratios of the MI values along the three principal axes
    /// are also well know modeling variables. This descriptor calculates the MI values
    /// along the X, Y and Z axes as well as the ratio's X/Y, X/Z and Y/Z. Finally it also
    /// calculates the radius of gyration of the molecule.
    /// <para>
    /// One important aspect of the algorithm is that if the eigenvalues of the MI tensor
    /// are below 1e-3, then the ratio's are set to a default of 1000.
    /// </para>
    /// </remarks>
    // @author           Rajarshi Guha
    // @cdk.created      2005-02-07
    // @cdk.module       qsarmolecular
    // @cdk.dictref      qsar-descriptors:momentOfInertia
    // @cdk.keyword      moment of inertia
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#momentOfInertia", Requirements = DescriptorRequirements.Geometry3D)]
    public class MomentOfInertiaDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public MomentOfInertiaDescriptor()
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
            /// MI along X axis
            /// </summary>
            [DescriptorResultProperty("MOMI-X")]
            public double MOMIX => Values[0];

            /// <summary>
            /// MI along Y axis
            /// </summary>
            [DescriptorResultProperty("MOMI-Y")]
            public double MOMIY => Values[1];

            /// <summary>
            /// MI along Z axis
            /// </summary>
            [DescriptorResultProperty("MOMI-Z")]
            public double MOMIZ => Values[2];

            /// <summary>
            /// X/Y
            /// </summary>
            [DescriptorResultProperty("MOMI-XY")]
            public double MOMIXY => Values[3];

            /// <summary>
            /// X/Z
            /// </summary>
            [DescriptorResultProperty("MOMI-XZ")]
            public double MOMIXZ => Values[4];

            /// <summary>
            /// Y/Z
            /// </summary>
            [DescriptorResultProperty("MOMI-YZ")]
            public double MOMIYZ => Values[5];

            /// <summary>
            /// Radius of gyration
            /// </summary>
            [DescriptorResultProperty("MOMI-R")]
            public double MOMIR => Values[6];
        }

        /// <summary>
        /// Calculates the 3 MI's, 3 ration and the R_gyr value.
        /// The molecule should have hydrogens.
        /// </summary>
        /// <returns>An <see cref="Result"/> containing 7 elements in the order described above</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            var factory = CDK.IsotopeFactory;
            factory.ConfigureAtoms(container);

            if (!GeometryUtil.Has3DCoordinates(container)) {
                Console.WriteLine("Error: Molecule must have 3D coordinates");
                return null;
                //throw new ThreeDRequiredException("Molecule must have 3D coordinates");
            }

            var retval = new List<double>(7);

            double ccf = 1.000138;
            double eps = 1e-5;

            var imat = Arrays.CreateJagged<double>(3, 3);
            var centerOfMass = GeometryUtil.Get3DCentreOfMass(container).Value;

            double xdif;
            double ydif;
            double zdif;
            double xsq;
            double ysq;
            double zsq;
            for (int i = 0; i < container.Atoms.Count; i++)
            {
                var currentAtom = container.Atoms[i];

                var _mass = factory.GetMajorIsotope(currentAtom.Symbol).ExactMass;
                var mass = _mass == null ? factory.GetNaturalMass(currentAtom.Element) : _mass.Value;

                var p = currentAtom.Point3D.Value;
                xdif = p.X - centerOfMass.X;
                ydif = p.Y - centerOfMass.Y;
                zdif = p.Z - centerOfMass.Z;
                xsq = xdif * xdif;
                ysq = ydif * ydif;
                zsq = zdif * zdif;

                imat[0][0] += mass * (ysq + zsq);
                imat[1][1] += mass * (xsq + zsq);
                imat[2][2] += mass * (xsq + ysq);

                imat[1][0] += -1 * mass * ydif * xdif;
                imat[0][1] = imat[1][0];

                imat[2][0] += -1 * mass * xdif * zdif;
                imat[0][2] = imat[2][0];

                imat[2][1] += -1 * mass * ydif * zdif;
                imat[1][2] = imat[2][1];
            }

            // diagonalize the MI tensor
            var tmp = Matrix<double>.Build.DenseOfColumnArrays(imat);
            var eigenDecomp = tmp.Evd();
            var eval = eigenDecomp.EigenValues.Select(n => n.Real).ToArray();

            retval.Add(eval[2]);
            retval.Add(eval[1]);
            retval.Add(eval[0]);

            var etmp = eval[0];
            eval[0] = eval[2];
            eval[2] = etmp;

            if (Math.Abs(eval[1]) > 1e-3)
                retval.Add(eval[0] / eval[1]);
            else
                retval.Add(1000);

            if (Math.Abs(eval[2]) > 1e-3)
            {
                retval.Add(eval[0] / eval[2]);
                retval.Add(eval[1] / eval[2]);
            }
            else
            {
                retval.Add(1000);
                retval.Add(1000);
            }

            // finally get the radius of gyration
            var formula = MolecularFormulaManipulator.GetMolecularFormula(container);
            var pri = Math.Abs(eval[2]) > eps
                    ? Math.Pow(eval[0] * eval[1] * eval[2], 1.0 / 3.0)
                    : Math.Sqrt(eval[0] * ccf / MolecularFormulaManipulator.GetTotalExactMass(formula));
            retval.Add(Math.Sqrt(Math.PI * 2 * pri * ccf / MolecularFormulaManipulator.GetTotalExactMass(formula)));

            return new Result(retval);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
