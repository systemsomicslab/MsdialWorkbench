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
    /// Class that returns the number of aromatic atoms in an atom container.
    /// </summary>
    /// <remarks>
    /// <para>This descriptor uses these parameters:
    /// <list type="table">
    ///   <item>
    ///     <term>Name</term>
    ///     <term>Default</term>
    ///     <term>Description</term>
    ///   </item>
    ///   <item>
    ///     <term>checkAromaticity</term>
    ///     <term>false</term>
    ///     <term>True is the aromaticity has to be checked</term>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// Returns a single value with name <i>nAromAtom</i>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:aromaticAtomsCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#aromaticAtomsCount")]
    public class AromaticAtomsCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public AromaticAtomsCountDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfAromaticAtoms = value;
            }

            [DescriptorResultProperty("naAromAtom")]
            public int NumberOfAromaticAtoms { get; private set; }

            public int Value => NumberOfAromaticAtoms;
        }

        /// <summary>
        /// Calculate the count of aromatic atoms in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>the number of aromatic atoms of this AtomContainer</returns>
        public Result Calculate(IAtomContainer container)
        {
            if (checkAromaticity)
            {
                container = (IAtomContainer)container.Clone();
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            var count = container.Atoms.Count(n => n.IsAromatic);
            return new Result(count);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
