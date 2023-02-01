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
using NCDK.Graphs.Invariant;
using NCDK.Tools.Manipulator;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class evaluates if a proton is joined to a conjugated system.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:isProtonInConjugatedPiSystem
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#isProtonInConjugatedPiSystem")]
    public partial class IsProtonInConjugatedPiSystemDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedAtomContainer;
        private IChemObjectSet<IAtomContainer> acSet;

        public IsProtonInConjugatedPiSystemDescriptor(IAtomContainer container, bool checkAromaticity = false)
        {
            clonedAtomContainer = (IAtomContainer)container.Clone();
            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedAtomContainer);
                Aromaticity.CDKLegacy.Apply(clonedAtomContainer);
            }
            this.acSet = ConjugatedPiSystemsDetector.Detect(clonedAtomContainer);

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(bool value)
            {
                this.ProtonInConjugatedSystem = value;
            }

            [DescriptorResultProperty("protonInConjSystem")]
            public bool ProtonInConjugatedSystem { get; private set; }

            public bool Value => ProtonInConjugatedSystem;
        }

        /// <summary>
        /// The method is a proton descriptor that evaluates if a proton is joined to a conjugated system.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns><see langword="true"/> if the proton is bonded to a conjugated system</returns>
        public Result Calculate(IAtom atom)
        {
            var clonedAtom = clonedAtomContainer.Atoms[container.Atoms.IndexOf(atom)];

            bool isProtonInPiSystem = false;
            if (atom.AtomicNumber.Equals(AtomicNumbers.H))
            {
                var detected = acSet.GetEnumerator();
                var neighboors = clonedAtomContainer.GetConnectedAtoms(clonedAtom);
                foreach (var neighboor in neighboors)
                {
                    while (detected.MoveNext())
                    {
                        var detectedAC = detected.Current;
                        if (detectedAC != null && detectedAC.Contains(neighboor))
                        {
                            isProtonInPiSystem = true;
                            break;
                        }
                    }
                }
            }
            return new Result(isProtonInPiSystem);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
