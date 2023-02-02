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

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Zagreb index: the sum of the squares of atom degree over all heavy atoms i.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:zagrebIndex
    // @cdk.keyword Zagreb index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#zagrebIndex")]
    public class ZagrebIndexDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public ZagrebIndexDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Zagreb = value;
            }

            /// <summary>
            /// Zagreb index
            /// </summary>
            [DescriptorResultProperty("Zagreb")]
            public double Zagreb { get; private set; }

            public double Value => Zagreb;
        }

        /// <summary>
        /// Evaluate the Zagreb Index for a molecule.
        /// </summary>
        /// <returns>Zagreb index</returns>
        public Result Calculate(IAtomContainer container)
        {
            double zagreb = 0;
            foreach (var atom in container.Atoms)
            {
                if (atom.AtomicNumber.Equals(AtomicNumbers.H))
                    continue;
                int atomDegree = 0;
                var neighbours = container.GetConnectedAtoms(atom);
                foreach (var neighbour in neighbours)
                {
                    if (!neighbour.AtomicNumber.Equals(AtomicNumbers.H))
                    {
                        atomDegree += 1;
                    }
                }
                zagreb += atomDegree * atomDegree;
            }

            return new Result(zagreb);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
