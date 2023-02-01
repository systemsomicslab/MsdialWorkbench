/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Numerics;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the 3D distance between two atoms. Only works with 3D coordinates, which must be calculated beforehand.
    /// </summary>
    // @author         mfe4
    // @cdk.created    2004-11-13
    // @cdk.module     qsaratomic
    // @cdk.dictref qsar-descriptors:distanceToAtom
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#distanceToAtom")]
    public partial class DistanceToAtomDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;

        public DistanceToAtomDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.DistanceToAtom = value;
            }

            [DescriptorResultProperty("distanceToAtom")]
            public double DistanceToAtom { get; private set; }

            public double Value => DistanceToAtom;
        }

        /// <summary>
        /// This method calculate the 3D distance between two atoms.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <param name="focusPosition">The position of the focus atom</param>
        /// <returns>The number of bonds on the shortest path between two atoms</returns>
        public Result Calculate(IAtom atom, int focusPosition = 0)
        {
            var focus = container.Atoms[focusPosition];

            if (atom.Point3D == null || focus.Point3D == null)
                throw new CDKException("Target or focus atom must have 3D coordinates.");

            var distanceToAtom = CalculateDistanceBetweenTwoAtoms(atom, focus);

            return new Result(distanceToAtom);
        }

        /// <summary>
        /// generic method for calculation of distance between 2 atoms
        /// </summary>
        /// <param name="atom1">The IAtom 1</param>
        /// <param name="atom2">The IAtom 2</param>
        /// <returns>distance between atom1 and atom2</returns>
        private static double CalculateDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            double distance;
            Vector3 firstPoint = atom1.Point3D.Value;
            Vector3 secondPoint = atom2.Point3D.Value;
            distance = Vector3.Distance(firstPoint, secondPoint);
            return distance;
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
