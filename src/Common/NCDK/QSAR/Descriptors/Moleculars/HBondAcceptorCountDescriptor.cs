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

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This descriptor calculates the number of hydrogen bond acceptors using a slightly simplified version of the
    /// <see href="http://www.chemie.uni-erlangen.de/model2001/abstracts/rester.html">PHACIR atom types</see>.
    /// </summary>
    /// <remarks>
    /// The following groups are counted as hydrogen bond acceptors:
    /// <list type="bullet"> 
    /// <item>any oxygen where the formal charge of the oxygen is non-positive (i.e. formal charge &lt;= 0) <b>except</b></item>
    /// <item>an aromatic ether oxygen (i.e. an ether oxygen that is adjacent to at least one aromatic carbon)</item>
    /// <item>an oxygen that is adjacent to a nitrogen</item>
    /// <item>any nitrogen where the formal charge of the nitrogen is non-positive (i.e. formal charge &lt;= 0) <b>except</b></item>
    /// </list>
    /// <para>
    /// This descriptor works properly with AtomContainers whose atoms contain <b>implicit hydrogens</b> or <b>explicit
    /// hydrogens</b>.
    /// </para>
    /// </remarks>
    // @author      ulif
    // @cdk.created 2005-22-07
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:hBondacceptors
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#hBondacceptors")]
    public class HBondAcceptorCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public HBondAcceptorCountDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfHBondAcceptor = value;
            }

            [DescriptorResultProperty("nHBAcc")]
            public int NumberOfHBondAcceptor { get; private set; }

            public int Value => NumberOfHBondAcceptor;
        }

        /// <summary>
        /// Calculates the number of H bond acceptors.
        /// </summary>
        /// <returns>number of H bond acceptors</returns>
        public Result Calculate(IAtomContainer container)
        {
            // do aromaticity detection
            if (checkAromaticity)
            {
                container = (IAtomContainer)container.Clone(); // don't mod original
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            int hBondAcceptors = 0;

            // labelled for loop to allow for labelled continue statements within the loop
            foreach (var atom in container.Atoms)
            {
                // looking for suitable nitrogen atoms
                if (atom.AtomicNumber.Equals(AtomicNumbers.N) && atom.FormalCharge <= 0)
                {
                    // excluding nitrogens that are adjacent to an oxygen
                    var bonds = container.GetConnectedBonds(atom);
                    int nPiBonds = 0;
                    foreach (var bond in bonds)
                    {
                        if (bond.GetConnectedAtom(atom).AtomicNumber.Equals(AtomicNumbers.O))
                            goto continue_atomloop;
                        if (BondOrder.Double.Equals(bond.Order))
                            nPiBonds++;
                    }

                    // if the nitrogen is aromatic and there are no pi bonds then it's
                    // lone pair cannot accept any hydrogen bonds
                    if (atom.IsAromatic && nPiBonds == 0)
                        continue;

                    hBondAcceptors++;
                }
                // looking for suitable oxygen atoms
                else if (atom.AtomicNumber.Equals(AtomicNumbers.O) && atom.FormalCharge <= 0)
                {
                    //excluding oxygens that are adjacent to a nitrogen or to an aromatic carbon
                    var neighbours = container.GetConnectedBonds(atom);
                    foreach (var bond in neighbours)
                    {
                        var neighbor = bond.GetOther(atom);
                        switch (neighbor.AtomicNumber)
                        {
                            case AtomicNumbers.N:
                                goto continue_atomloop;
                            case AtomicNumbers.C:
                                if (neighbor.IsAromatic && bond.Order != BondOrder.Double)
                                    goto continue_atomloop;
                                break;
                        }
                    }
                    hBondAcceptors++;
                }
            continue_atomloop:
                ;
            }

            return new Result(hBondAcceptors);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
