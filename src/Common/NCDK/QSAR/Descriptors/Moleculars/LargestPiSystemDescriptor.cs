/*  Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Class that returns the number of atoms in the largest pi system.
    /// </summary>
    // @author chhoppe from EUROSCREEN
    // @cdk.created 2006-1-03
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:largestPiSystem
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#largestPiSystem")]
    public class LargestPiSystemDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public LargestPiSystemDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.LargestPiSystemAtomsCount = value;
            }

            /// <summary>
            /// The number of atoms in the largest chain.
            /// </summary>
            [DescriptorResultProperty("nAtomP")]
            public int LargestPiSystemAtomsCount { get; private set; }

            public int Value => LargestPiSystemAtomsCount;
        }

        /// <summary>
        /// Calculate the count of atoms of the largest pi system in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>the number of atoms in the largest pi system of this AtomContainer</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            int largestPiSystemAtomsCount = 0;

            //Set all VisitedFlags to False
            for (int i = 0; i < container.Atoms.Count; i++)
                container.Atoms[i].IsVisited = false;

            for (int i = 0; i < container.Atoms.Count; i++)
            {
                //Possible pi System double bond or triple bond, charge, N or O (free electron pair)
                if ((container.GetMaximumBondOrder(container.Atoms[i]) != BondOrder.Single
                  || Math.Abs(container.Atoms[i].FormalCharge.Value) >= 1
                  || container.Atoms[i].IsAromatic
                  || container.Atoms[i].AtomicNumber.Equals(AtomicNumbers.N)
                  || container.Atoms[i].AtomicNumber.Equals(AtomicNumbers.O))
                 && !container.Atoms[i].IsVisited)
                {
                    var startSphere = new List<IAtom>();
                    var path = new List<IAtom>();
                    startSphere.Add(container.Atoms[i]);
                    BreadthFirstSearch(container, startSphere, path);
                    if (path.Count > largestPiSystemAtomsCount)
                        largestPiSystemAtomsCount = path.Count;
                }
            }

            return new Result(largestPiSystemAtomsCount);
        }

        /// <summary>
        /// Performs a breadthFirstSearch in an AtomContainer starting with a
        /// particular sphere, which usually consists of one start atom, and searches
        /// for a pi system.
        /// </summary>
        /// <param name="container">The AtomContainer to be searched</param>
        /// <param name="sphere">A sphere of atoms to start the search with</param>
        /// <param name="path">An array list which stores the atoms belonging to the pi system</param>
        /// <exception cref="CDKException"></exception>
        private void BreadthFirstSearch(IAtomContainer container, List<IAtom> sphere, List<IAtom> path)
        {
            IAtom nextAtom;
            var newSphere = new List<IAtom>();
            foreach (var atom in sphere)
            {
                var bonds = container.GetConnectedBonds(atom);
                foreach (var bond in bonds)
                {
                    nextAtom = ((IBond)bond).GetConnectedAtom(atom);
                    if ((container.GetMaximumBondOrder(nextAtom) != BondOrder.Single
                      || Math.Abs(nextAtom.FormalCharge.Value) >= 1 || nextAtom.IsAromatic
                      || nextAtom.AtomicNumber.Equals(AtomicNumbers.N) 
                      || nextAtom.AtomicNumber.Equals(AtomicNumbers.O))
                     && !nextAtom.IsVisited)
                    {
                        //Debug.WriteLine("BDS> AtomNr:"+container.Atoms.IndexOf(nextAtom)+" maxBondOrder:"+container.GetMaximumBondOrder(nextAtom)+" Aromatic:"+nextAtom.IsAromatic+" FormalCharge:"+nextAtom.FormalCharge+" Charge:"+nextAtom.Charge+" Flag:"+nextAtom.IsVisited);
                        path.Add(nextAtom);
                        //Debug.WriteLine("BreadthFirstSearch is meeting new atom " + (nextAtomNr + 1));
                        nextAtom.IsVisited = true;
                        if (container.GetConnectedBonds(nextAtom).Count() > 1)
                        {
                            newSphere.Add(nextAtom);
                        }
                    }
                    else
                    {
                        nextAtom.IsVisited = true;
                    }
                }
            }
            if (newSphere.Count > 0)
            {
                BreadthFirstSearch(container, newSphere, path);
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
