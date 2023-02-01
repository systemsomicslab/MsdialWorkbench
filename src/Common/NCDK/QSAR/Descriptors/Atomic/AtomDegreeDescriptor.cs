/*  Copyright (C)      2005  Matteo Floris <mfe4@users.sf.net>
 *                     2006  Kai Hartmann <kaihartmann@users.sf.net>
 *                     2006  Miguel Rojas-Cherto <miguelrojasch@users.sf.net>
 *                2005-2008  Egon Willighagen <egonw@users.sf.net>
 *                2008-2009  Rajarshi Guha <rajarshi@users.sf.net>
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

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the number of not-Hs substituents of an atom, also defined as "atom degree".
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:atomDegree
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomDegree")]
    public partial class AtomDegreeDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        private IAtomContainer container;

        public AtomDegreeDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.AtomDegree = value;
            }

            [DescriptorResultProperty("aNeg")]
            public int AtomDegree { get; private set; }

            public int Value => AtomDegree;
        }

        /// <summary>
        /// This method calculates the number of not-H substituents of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="IDescriptorResult"/> is requested</param>
        /// <returns>The number of bonds on the shortest path between two atoms</returns>
        public Result Calculate(IAtom atom)
        {
            int atomDegree = 0;
            var neighboors = container.GetConnectedAtoms(atom);
            foreach (var neighboor in neighboors)
            {
                if (!neighboor.AtomicNumber.Equals(AtomicNumbers.H))
                    atomDegree += 1;
            }
            return new Result(atomDegree);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
