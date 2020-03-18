/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Graphs;
using System;
using System.Collections;
using System.Linq;

namespace NCDK.Aromaticities
{
    /// <summary>
    /// Assign a Kekulé representation to the aromatic systems of a compound. 
    /// </summary>
    /// <remarks>
    /// Input from some file-formats provides some bonds as aromatic / delocalised bond
    /// types. This method localises the electrons and assigns single and double
    /// bonds. Different atom and bond orderings may produce distinct but valid
    /// Kekulé forms. Only bond orders are adjusted and any aromatic flags will
    /// remain untouched.
    /// <para>
    /// The procedure requires that all atoms have defined implicit hydrogens counts
    /// and formal charges. If this information is not present it should be assigned
    /// first. 
    /// </para>
    /// <para>
    /// For some inputs it may not be possible to assign a Kekulé form. In general
    /// theses cases are rare but usually occur for one of two reasons.
    /// 1) Missing / ambiguous implicit hydrogens, this is fundamental to determining the
    /// Kekulé form and if guessed may be wrong. Some formats (e.g. molfile) can not
    /// include the exact number of implicit hydrogens attached to atom whilst others
    /// may omit it or optionally skip encoding. The typical example is found in the
    /// example for 1H-pyrrole, a correct SMILES encoding should include the hydrogen
    /// on the aromatic nitrogen '[nH]1cccc1' (not: 'n1cccc1').
    /// 2) The aromaticity perception algorithm has allowed atoms with abnormal
    /// valence. This usually happens when a non-convalent bond has be <i>upgraded</i>
    /// to a sigma bond during format conversion. 
    /// </para>
    /// </remarks>
    // @author John May
    // @cdk.keyword kekule
    // @cdk.keyword kekulize
    // @cdk.keyword dearomatize
    // @cdk.keyword aromatic
    // @cdk.keyword fix bond orders
    // @cdk.keyword deduce bond orders
    public sealed class Kekulization
    {
        /// <summary>
        /// Assign a Kekulé representation to the aromatic systems of a compound.
        /// </summary>
        /// <param name="ac">structural representation</param>
        /// <exception cref="CDKException">a Kekulé form could not be assigned</exception>
        public static void Kekulize(IAtomContainer ac)
        {
            // storage of pairs of atoms that have pi-bonded
            var matching = Matching.WithCapacity(ac.Atoms.Count);

            // exract data structures for efficient access
            var atoms = ac.Atoms.ToArray();
            var bonds = EdgeToBondMap.WithSpaceFor(ac);
            var graph = GraphUtil.ToAdjList(ac, bonds);

            // determine which atoms are available to have a pi bond placed
            var available = IsAvailable(graph, atoms, bonds);

            // attempt to find a perfect matching such that a pi bond is placed
            // next to each available atom. if not found the solution is ambiguous
            if (!matching.Perfect(graph, available))
                throw new CDKException("Cannot assign Kekulé structure without randomly creating radicals.");

            // propagate bond order information from the matching
            foreach (var bond in ac.Bonds)
            {
                if (bond.Order == BondOrder.Unset && bond.IsAromatic)
                    bond.Order = BondOrder.Single;
            }
            for (int v = BitArrays.NextSetBit(available, 0); v >= 0; v = BitArrays.NextSetBit(available, v + 1))
            {
                var w = matching.Other(v);
                var bond = bonds[v, w];

                // sanity check, something wrong if this happens
                if (bond.Order.Numeric() > 1)
                    throw new CDKException("Cannot assign Kekulé structure, non-sigma bond order has already been assigned?");

                bond.Order = BondOrder.Double;
                available.Set(w, false);
            }
        }

        /// <summary>
        /// Determine the set of atoms that are available to have a double-bond.
        /// </summary>
        /// <param name="graph">adjacent list representation</param>
        /// <param name="atoms">array of atoms</param>
        /// <param name="bonds">map of atom indices to bonds</param>
        /// <returns>atoms that can require a double-bond</returns>
        private static BitArray IsAvailable(int[][] graph, IAtom[] atoms, EdgeToBondMap bonds)
        {
            var available = new BitArray(atoms.Length);

            // for all atoms, select those that require a double-bond
            for (int i = 0; i < atoms.Length; i++)
            {
                var atom = atoms[i];

                // preconditions
                if (atom.FormalCharge == null)
                    throw new ArgumentException($"atom {i + 1} had unset formal charge");
                if (atom.ImplicitHydrogenCount == null)
                    throw new ArgumentException($"atom {i + 1} had unset implicit hydrogen count");

                if (!atom.IsAromatic)
                    continue;

                // count preexisting pi-bonds, a higher bond order causes a skip
                int nPiBonds = 0;
                foreach (var w in graph[i])
                {
                    var order = bonds[i, w].Order;
                    if (order == BondOrder.Double)
                    {
                        nPiBonds++;
                    }
                    else if (order.Numeric() > 2)
                    {
                        goto continue_ATOMS;
                    }
                }

                // check if a pi bond can be assigned
                var element = atom.AtomicNumber;
                var charge = atom.FormalCharge.Value;
                var valence = graph[i].Length + atom.ImplicitHydrogenCount.Value + nPiBonds;

                if (IsAvailable(element, charge, valence))
                {
                    available.Set(i, true);
                }

            continue_ATOMS:
                ;
            }

            return available;
        }

        /// <summary>
        /// Determine if the specified element with the provided charge and valance
        /// requires a pi bond?
        /// </summary>
        /// <param name="element">atomic number >= 0</param>
        /// <param name="charge">formal charge</param>
        /// <param name="valence">bonded electrons</param>
        /// <returns>a double-bond is required</returns>
        private static bool IsAvailable(int element, int charge, int valence)
        {
            // higher atomic number elements aren't likely to be found but
            // we have them for rare corner cases (tellurium).
            // Germanium, Silicon, Tin and Antimony are a bit bonkers...
            switch (ChemicalElement.Of(element).AtomicNumber)
            {
                case 5: // Boron
                    if (charge == 0 && valence <= 2)
                        return true;
                    if (charge == -1 && valence <= 3)
                        return true;
                    break;
                case 6: // Carbon
                case 14: //  Silicon:
                case 32: // Germanium:
                case 50: // Tin:
                    if (charge == 0 && valence <= 3)
                        return true;
                    break;
                case 7: // Nitrogen:
                case 15: // Phosphorus:
                case 33: // Arsenic:
                case 51: // Antimony:
                    if (charge == 0)
                        return valence <= 2 || valence == 4;
                    if (charge == 1)
                        return valence <= 3;
                    break;
                case 8: // Oxygen:
                case 16: // Sulfur:
                case 34: // Selenium:
                case 52: // Tellurium:
                         // valence of three or five are really only for sulphur but
                         // are applied generally to all of group eight for simplicity
                    if (charge == 0)
                        return valence <= 1 || valence == 3 || valence == 5;
                    if (charge == 1)
                        return valence <= 2 || valence == 4;
                    break;
                default:
                    break;
            }

            return false;
        }
    }
}
