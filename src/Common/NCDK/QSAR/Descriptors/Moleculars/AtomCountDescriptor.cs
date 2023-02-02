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

using System;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Descriptor based on the number of atoms of a certain element type.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:atomCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomCount")]
    public class AtomCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public AtomCountDescriptor()
        {            
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorSingleResult<int>
        {
            public Result(int count, string symbol)
                : base(symbol == null ? throw new ArgumentNullException(nameof(symbol)) : "n" + symbol, count)
            {
            }
        }

        /// <summary>
        /// This method calculate the number of atoms of a given type in an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>Number of atoms of a certain type is returned.</returns>
        /// <param name="elementName">Symbol of the element we want to count. "*" get the count of all atoms.</param>
        public Result Calculate(IAtomContainer container, string elementName = "*")
        {
            // it could be interesting to accept as elementName a SMARTS atom, to get the frequency of this atom
            // this could be useful for other descriptors like polar surface area...

            int atomCount = 0;

            switch (elementName)
            {
                case "*":
                    atomCount += container.Atoms.Select(n => n.ImplicitHydrogenCount ?? 0).Sum();
                    atomCount += container.Atoms.Count;
                    break;
                case "H":
                    atomCount += container.Atoms.Select(n => n.ImplicitHydrogenCount ?? 0).Sum();
                    goto default;
                default:
                    atomCount += container.Atoms.Count(n => n.Symbol.Equals(elementName, StringComparison.Ordinal));
                    break;
            }

            return new Result(atomCount, elementName);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
