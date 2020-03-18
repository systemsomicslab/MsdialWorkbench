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

using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Evaluates rotatable bonds count.
    /// </summary>
    /// <remarks>
    /// The number of rotatable bonds is given by the SMARTS specified by Daylight on
    /// <see href="http://www.daylight.com/dayhtml_tutorials/languages/smarts/smarts_examples.html#EXMPL">SMARTS tutorial</see>
    /// Returns a single value named <i>nRotB</i>
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:rotatableBondsCount
    // @cdk.keyword bond count, rotatable
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#rotatableBondsCount")]
    public class RotatableBondsCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public RotatableBondsCountDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfRotatableBonds = value;
            }

            /// <summary>
            /// number of rotatable bonds
            /// </summary>
            [DescriptorResultProperty("nRotB")]
            public int NumberOfRotatableBonds { get; private set; }

            public int Value => NumberOfRotatableBonds;
        }

        /// <summary>
        /// The method calculates the number of rotatable bonds of an atom container.
        /// If the boolean parameter is set to <see langword="true"/>, terminal bonds are included.
        /// </summary>
        /// <returns>number of rotatable bonds</returns>
        /// <param name="includeTerminals"><see langword="true"/> if terminal bonds are included</param>
        /// <param name="excludeAmides"><see langword="true"/> if amide C-N bonds should be excluded</param>
        public Result Calculate(IAtomContainer container, bool includeTerminals = false, bool excludeAmides = false)
        {
            container = (IAtomContainer)container.Clone();

            IRingSet ringSet;
            try
            {
                ringSet = new SpanningTree(container).GetBasicRings();
            }
            catch (NoSuchAtomException)
            {
                return new Result(0);
            }

            foreach (var bond in container.Bonds)
                if (ringSet.GetRings(bond).Count() > 0)
                    bond.IsInRing = true;

            int rotatableBondsCount = 0;
            foreach (var bond in container.Bonds)
            {
                var atom0 = bond.Atoms[0];
                var atom1 = bond.Atoms[1];
                if (atom0.AtomicNumber.Equals(AtomicNumbers.H) || atom1.AtomicNumber.Equals(AtomicNumbers.H))
                    continue;
                if (bond.Order == BondOrder.Single)
                {
                    if (BondManipulator.IsLowerOrder(container.GetMaximumBondOrder(atom0), BondOrder.Triple)
                     && BondManipulator.IsLowerOrder(container.GetMaximumBondOrder(atom1), BondOrder.Triple))
                    {
                        if (!bond.IsInRing)
                        {
                            if (excludeAmides && (IsAmide(atom0, atom1, container) || IsAmide(atom1, atom0, container)))
                                continue;

                            // if there are explicit H's we should ignore those bonds
                            var degree0 = container.GetConnectedBonds(atom0).Count() - GetConnectedHCount(container, atom0);
                            var degree1 = container.GetConnectedBonds(atom1).Count() - GetConnectedHCount(container, atom1);
                            if ((degree0 == 1) || (degree1 == 1))
                            {
                                if (includeTerminals)
                                    rotatableBondsCount += 1;
                            }
                            else
                            {
                                rotatableBondsCount += 1;
                            }
                        }
                    }
                }
            }

            return new Result(rotatableBondsCount);
        }

        /// <summary>
        /// Checks whether both atoms are involved in an amide C-N bond: *N(*)C(*)=O.
        /// 
        /// Only the most common constitution is considered. Tautomeric, O\C(*)=N\*,
        /// and charged forms, [O-]\C(*)=N\*, are ignored.
        /// </summary>
        /// <param name="atom0">the first bonding partner</param>
        /// <param name="atom1">the second bonding partner</param>
        /// <param name="container">the parent container</param>
        /// <returns>if both partners are involved in an amide C-N bond</returns>
        private static bool IsAmide(IAtom atom0, IAtom atom1, IAtomContainer container)
        {
            if (atom0.AtomicNumber.Equals(AtomicNumbers.C) && atom1.AtomicNumber.Equals(AtomicNumbers.N))
                foreach (var neighbor in container.GetConnectedAtoms(atom0))
                    if (neighbor.AtomicNumber.Equals(AtomicNumbers.O)
                     && container.GetBond(atom0, neighbor).Order == BondOrder.Double)
                        return true;
            return false;
        }

        private static int GetConnectedHCount(IAtomContainer container, IAtom atom)
        {
            return container.GetConnectedAtoms(atom).Count(n => n.AtomicNumber.Equals(AtomicNumbers.H));
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
