/* Copyright (C) 1997-2009  Christoph Steinbeck, Stefan Kuhn <shk3@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Linq;

namespace NCDK.StructGen.Stochastic
{
    /// <summary>
    /// Randomly generates a single, connected, correctly bonded structure from
    /// a number of fragments.
    /// <para>Assign hydrogen counts to each heavy atom. The hydrogens should not be
    /// in the atom pool but should be assigned implicitly to the heavy atoms in
    /// order to reduce computational cost.</para>
    /// </summary>
    // @author     steinbeck
    // @cdk.created    2001-09-04
    // @cdk.module     structgen
    public class PartialFilledStructureMerger
    {
        private readonly ISaturationChecker satCheck = CDK.SaturationChecker;

        public PartialFilledStructureMerger()
        {
        }

        /// <summary>
        /// Randomly generates a single, connected, correctly bonded structure from
        /// a number of fragments.
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// The <see cref="IAtomContainer"/> in the set must be
        /// connected. If an <see cref="IAtomContainer"/> is disconnected, no valid result will
        /// </note>
        /// </remarks>
        /// <param name="atomContainers">The fragments to generate for.</param>
        /// <returns>The newly formed structure.</returns>
        /// <exception cref="CDKException">No valid result could be formed.</exception>"
        public IAtomContainer Generate(IChemObjectSet<IAtomContainer> atomContainers)
        {
            var container = Generate2(atomContainers);
            if (container == null)
                throw new CDKException("Could not combine the fragments to combine a valid, saturated structure");
            return container;
        }

        internal IAtomContainer Generate2(IChemObjectSet<IAtomContainer> atomContainers)
        {
            int iteration = 0;
            bool structureFound = false;
            do
            {
                iteration++;
                bool bondFormed;
                do
                {
                    bondFormed = false;
                    var atomContainersArray = atomContainers.ToList();
                    for (var atomContainersArrayIndex = 0; atomContainersArrayIndex < atomContainersArray.Count; atomContainersArrayIndex++)
                    {
                        var ac = atomContainersArray[atomContainersArrayIndex];
                        if (ac == null)
                            continue;

                        var atoms = ac.Atoms.ToList(); // ToList is required because some atoms are added to ac.Atoms in the loop.
                        foreach (var atom in atoms)
                        {
                            if (!satCheck.IsSaturated(atom, ac))
                            {
                                var partner = GetAnotherUnsaturatedNode(atom, ac, atomContainers);
                                if (partner != null)
                                {
                                    var toadd = AtomContainerSetManipulator.GetRelevantAtomContainer( atomContainers, partner);
                                    var cmax1 = satCheck.GetCurrentMaxBondOrder(atom, ac);
                                    var cmax2 = satCheck.GetCurrentMaxBondOrder(partner, toadd);
                                    var max = Math.Min(cmax1, cmax2);
                                    var order = Math.Min(Math.Max(1, max), 3); //(double)Math.Round(Math.Random() * max)
                                    Debug.WriteLine($"cmax1, cmax2, max, order: {cmax1}, {cmax2}, {max}, {order}");
                                    if (toadd != ac)
                                    {
                                        var indexToRemove = atomContainersArray.IndexOf(toadd);
                                        if (indexToRemove != -1)
                                            atomContainersArray[indexToRemove] = null;
                                        atomContainers.Remove(toadd);
                                        ac.Add(toadd);
                                    }
                                    ac.Bonds.Add(ac.Builder.NewBond(atom, partner, BondManipulator.CreateBondOrder(order)));
                                    bondFormed = true;
                                }
                            }
                        }
                    }
                } while (bondFormed);
                if (atomContainers.Count == 1
                 && satCheck.IsSaturated(atomContainers[0]))
                {
                    structureFound = true;
                }
            } while (!structureFound && iteration < 5);
            if (atomContainers.Count == 1 && satCheck.IsSaturated(atomContainers[0]))
            {
                structureFound = true;
            }
            if (!structureFound)
                return null;
            return atomContainers[0];
        }

        /// <summary>
        /// Gets a randomly selected unsaturated atom from the set. If there are any, it will be from another
        /// container than exclusionAtom.
        /// </summary>
        /// <returns>The unsaturated atom.</returns>
        private IAtom GetAnotherUnsaturatedNode(IAtom exclusionAtom, IAtomContainer exclusionAtomContainer, IChemObjectSet<IAtomContainer> atomContainers)
        {
            foreach (var ac in atomContainers)
            {
                if (ac != exclusionAtomContainer)
                {
                    int next = 0;//(int) (Math.Random() * ac.Atoms.Count);
                    for (int f = next; f < ac.Atoms.Count; f++)
                    {
                        var atom = ac.Atoms[f];
                        if (!satCheck.IsSaturated(atom, ac) && exclusionAtom != atom)
                        {
                            return atom;
                        }
                    }
                }
            }
            {
                var next = exclusionAtomContainer.Atoms.Count;//(int) (Math.random() * ac.getAtomCount());
                for (int f = 0; f < next; f++)
                {
                    var atom = exclusionAtomContainer.Atoms[f];
                    if (!satCheck.IsSaturated(atom, exclusionAtomContainer) 
                     && exclusionAtom != atom
                     && !exclusionAtomContainer.GetConnectedAtoms(exclusionAtom).Contains(atom))
                    {
                        return atom;
                    }
                }
            }
            return null;
        }
    }
}
