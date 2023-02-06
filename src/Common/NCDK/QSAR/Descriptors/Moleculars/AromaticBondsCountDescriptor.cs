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

using NCDK.Aromaticities;
using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This Class contains a method that returns the number of aromatic atoms in an AtomContainer.
    /// </summary>
    /// <remarks>
    /// Returns a single value with name <i>nAromBond</i>
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:aromaticBondsCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#aromaticBondsCount")]
    public class AromaticBondsCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public AromaticBondsCountDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfAromaticBonds = value;
            }

            [DescriptorResultProperty("nAromBond")]
            public int NumberOfAromaticBonds { get; private set; }

            public int Value => NumberOfAromaticBonds;
        }

        /// <summary>
        /// Calculate the count of aromatic atoms in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <remarks>
        /// The method take a boolean checkAromaticity: if the boolean is <see langword="true"/>, it means that
        /// aromaticity has to be checked.
        /// </remarks>
        /// <returns>the number of aromatic bonds</returns>
        public Result Calculate(IAtomContainer container)
        {
            if (checkAromaticity)
            {
                container = (IAtomContainer)container.Clone();
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            var count = container.Bonds.Count(bond => bond.IsAromatic);

            return new Result(count);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
