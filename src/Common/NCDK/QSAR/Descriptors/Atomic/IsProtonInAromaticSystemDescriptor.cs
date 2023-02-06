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

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This descriptor returns 1 if the protons is directly bonded to an aromatic system,
    /// it returns 2 if the distance between aromatic system and proton is 2 bonds,
    /// and it return 0 for other positions. It is needed to use addExplicitHydrogensToSatisfyValency method.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:isProtonInAromaticSystem
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#isProtonInAromaticSystem")]
    public partial class IsProtonInAromaticSystemDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedAtomContainer;

        public IsProtonInAromaticSystemDescriptor(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedAtomContainer = (IAtomContainer)container.Clone();
            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedAtomContainer);
                Aromaticity.CDKLegacy.Apply(clonedAtomContainer);
            }

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.ProtonInArmaticSystem = value;
            }

            [DescriptorResultProperty("protonInArmaticSystem")]
            public int ProtonInArmaticSystem { get; private set; }

            public int Value => ProtonInArmaticSystem;
        }

        /// <summary>
        /// The method is a proton descriptor that evaluate if a proton is bonded to an aromatic system or if there is distance of 2 bonds.
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>true if the proton is bonded to an aromatic atom.</returns>
        public Result Calculate(IAtom atom)
        {
            var clonedAtom = clonedAtomContainer.Atoms[container.Atoms.IndexOf(atom)];
            var neighboor = clonedAtomContainer.GetConnectedAtoms(clonedAtom);
            var neighbour0 = neighboor.First();
            int isProtonInAromaticSystem = 0;
            if (atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                if (neighbour0.IsAromatic)
                    isProtonInAromaticSystem = 1;
                else
                {
                    var betaAtom = clonedAtomContainer.GetConnectedAtoms(neighbour0).LastOrDefault();
                    if (betaAtom != null)
                        if (betaAtom.IsAromatic)
                            isProtonInAromaticSystem = 2;
                        else
                            isProtonInAromaticSystem = 0;
                }
            }
            else
                isProtonInAromaticSystem = 0;
            return new Result(isProtonInAromaticSystem);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
