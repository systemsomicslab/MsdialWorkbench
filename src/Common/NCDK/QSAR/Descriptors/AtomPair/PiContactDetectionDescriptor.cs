/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *                    2011  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Aromaticities;
using NCDK.Graphs.Invariant;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.AtomPair
{
    /// <summary>
    /// This class checks if two atoms have pi-contact (this is true when there is
    /// one and the same conjugated pi-system which contains both atoms, or directly
    /// linked neighbours of the atoms).
    /// </summary>
    // @author         mfe4
    // @cdk.created    2004-11-03
    // @cdk.module     qsarmolecular
    // @cdk.dictref    qsar-descriptors:piContact
    [DescriptorSpecification(DescriptorTargets.AtomPair, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#piContact")]
    public class PiContactDetectionDescriptor : AbstractDescriptor, IAtomPairDescriptor
    {
        private IAtomContainer container;
        private IAtomContainer clonedContainer;
        private IAtomContainer mol;
        readonly IChemObjectSet<IAtomContainer> acSet = null;

        public PiContactDetectionDescriptor(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedContainer = (IAtomContainer)container.Clone();
            mol = clonedContainer.Builder.NewAtomContainer(clonedContainer);
            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(mol);
                Aromaticity.CDKLegacy.Apply(mol);
            }
            acSet = ConjugatedPiSystemsDetector.Detect(mol);

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(bool value)
            {
                this.PiContact = value;
            }

            [DescriptorResultProperty("piContact")]
            public bool PiContact { get; private set; }

            public bool Value => PiContact;
        }

        /// <summary>
        /// The method returns if two atoms have pi-contact.
        /// </summary>
        /// <returns><see langword="true"/> if the atoms have pi-contact</returns>
        /// <param name="first">The first atom</param>
        /// <param name="second">The second atom</param>
        public Result Calculate(IAtom first, IAtom second)
        {
            var clonedFirst = clonedContainer.Atoms[container.Atoms.IndexOf(first)];
            var clonedSecond = clonedContainer.Atoms[container.Atoms.IndexOf(second)];

            bool piContact = false;
            int counter = 0;
            var detected = acSet;

            var neighboorsFirst = mol.GetConnectedAtoms(clonedFirst);
            var neighboorsSecond = mol.GetConnectedAtoms(clonedSecond);

            foreach (var detectedAC in detected)
            {
                if (detectedAC.Contains(clonedFirst) && detectedAC.Contains(clonedSecond))
                {
                    counter += 1;
                    break;
                }
                if (IsANeighboorsInAnAtomContainer(neighboorsFirst, detectedAC)
                 && IsANeighboorsInAnAtomContainer(neighboorsSecond, detectedAC))
                {
                    counter += 1;
                    break;
                }
            }

            if (counter > 0)
            {
                piContact = true;
            }

            return new Result(piContact);
        }

        /// <summary>
        /// Gets if neighbours of an atom are in an atom container.
        /// </summary>
        /// <param name="neighs">atoms</param>
        /// <param name="ac">container</param>
        /// <returns>The boolean result</returns>
        private static bool IsANeighboorsInAnAtomContainer(IEnumerable<IAtom> neighs, IAtomContainer ac)
        {
            int count = neighs.Where(neigh => ac.Contains(neigh)).Count();
            return count > 0;
        }

        IDescriptorResult IAtomPairDescriptor.Calculate(IAtom atom, IAtom atom2) => Calculate(atom, atom2);
    }
}
