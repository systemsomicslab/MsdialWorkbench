/* Copyright (C) 2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using NCDK.Tools.Manipulator;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Topological descriptor characterizing the carbon connectivity.
    /// </summary>
    /// <remarks>
    /// The class calculates 9 descriptors in the following order
    /// <list type="bullet">
    /// <item>C1SP1 triply hound carbon bound to one other carbon</item>
    /// <item>C2SP1 triply bound carbon bound to two other carbons</item>
    /// <item>C1SP2 doubly hound carbon bound to one other carbon</item>
    /// <item>C2SP2 doubly bound carbon bound to two other carbons</item>
    /// <item>C3SP2 doubly bound carbon bound to three other carbons</item>
    /// <item>C1SP3 singly bound carbon bound to one other carbon</item>
    /// <item>C2SP3 singly bound carbon bound to two other carbons</item>
    /// <item>C3SP3 singly bound carbon bound to three other carbons</item>
    /// <item>C4SP3 singly bound carbon bound to four other carbons</item>
    /// </list>
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2007-09-28
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:carbonTypes
    // @cdk.keyword topological bond order ctypes
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#carbonTypes")]
    public class CarbonTypesDescriptor : AbstractDescriptor, IMolecularDescriptor
    { 
        public CarbonTypesDescriptor()
        {            
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<int> values)
            {
                this.Values = values;
            }

            [DescriptorResultProperty]
            public int C1SP1 => Values[0];
            [DescriptorResultProperty]
            public int C2SP1 => Values[1];
            [DescriptorResultProperty]
            public int C1SP2 => Values[2];
            [DescriptorResultProperty]
            public int C2SP2 => Values[3];
            [DescriptorResultProperty]
            public int C3SP2 => Values[4];
            [DescriptorResultProperty]
            public int C1SP3 => Values[5];
            [DescriptorResultProperty]
            public int C2SP3 => Values[6];
            [DescriptorResultProperty]
            public int C3SP3 => Values[7];
            [DescriptorResultProperty]
            public int C4SP3 => Values[8];

            public new IReadOnlyList<int> Values { get; private set; }
        }

        /// <summary>
        /// Calculates the 9 carbon types descriptors
        /// </summary>
        /// <returns>An ArrayList containing 9 elements in the order described above</returns>
        public Result Calculate(IAtomContainer container)
        {
            int c1sp1 = 0;
            int c2sp1 = 0;
            int c1sp2 = 0;
            int c2sp2 = 0;
            int c3sp2 = 0;
            int c1sp3 = 0;
            int c2sp3 = 0;
            int c3sp3 = 0;
            int c4sp3 = 0;

            foreach (var atom in container.Atoms)
            {
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.C:
                        break;
                    default:
                        continue;
                }

                var connectedAtoms = container.GetConnectedAtoms(atom);
                int cc = 0;
                foreach (var connectedAtom in connectedAtoms)
                {
                    switch (connectedAtom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                            cc++;
                            break;
                    }
                }

                var maxBondOrder = GetHighestBondOrder(container, atom);

                switch (maxBondOrder)
                {
                    case BondOrder.Triple:
                        switch (cc)
                        {
                            case 1: c1sp1++; break;
                            case 2: c2sp1++; break;
                        }
                        break;
                    case BondOrder.Double:
                        switch (cc)
                        {
                            case 1: c1sp2++; break;
                            case 2: c2sp2++; break;
                            case 3: c3sp2++; break;
                        }
                        break;
                    case BondOrder.Single:
                        switch (cc)
                        {
                            case 1: c1sp3++; break;
                            case 2: c2sp3++; break;
                            case 3: c3sp3++; break;
                            case 4: c4sp3++; break;
                        }
                        break;
                }
            }

            return new Result(new int[]
                {
                    c1sp1,
                    c2sp1,
                    c1sp2,
                    c2sp2,
                    c3sp2,
                    c1sp3,
                    c2sp3,
                    c3sp3,
                    c4sp3
                });
        }

        private static BondOrder GetHighestBondOrder(IAtomContainer container, IAtom atom)
        {
            var bonds = container.GetConnectedBonds(atom);
            var maxOrder = BondOrder.Single;
            foreach (var bond in bonds)
            {
                if (BondManipulator.IsHigherOrder(bond.Order, maxOrder))
                    maxOrder = bond.Order;
            }
            return maxOrder;
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
