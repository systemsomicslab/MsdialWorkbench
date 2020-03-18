/* Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using System.Collections.Generic;
using System.Linq;

namespace NCDK.Fragments
{
    /// <summary>
    /// Helper methods for fragmentation algorithms.
    /// </summary>
    /// <remarks>
    /// Most of these methods are specific to the fragmentation algorithms
    /// in this package and so are protected. In general, these methods will
    /// not be used by the rest of the API or by other users of the library.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.module fragment
    internal static class FragmentUtils
    {
        /// <summary>
        /// Non destructively split a molecule into two parts at the specified bond.
        /// </summary>
        /// <remarks>
        /// Note that if a ring bond is specified, the resultant list will contain
        /// teh opened ring twice.
        /// </remarks>
        /// <param name="atomContainer">The molecule to split</param>
        /// <param name="bond">The bond to split at</param>
        /// <returns>A list containing the two parts of the molecule</returns>
        internal static List<IAtomContainer> SplitMolecule(IAtomContainer atomContainer, IBond bond)
        {
            List<IAtomContainer> ret = new List<IAtomContainer>();

            foreach (var atom in bond.Atoms)
            {
                // later on we'll want to make sure that the fragment doesn't contain
                // the bond joining the current atom and the atom that is on the other side
                IAtom excludedAtom;
                if (atom.Equals(bond.Begin))
                    excludedAtom = bond.End;
                else
                    excludedAtom = bond.Begin;

                List<IBond> part = new List<IBond>
                {
                    bond
                };
                part = Traverse(atomContainer, atom, part);

                // at this point we have a partion which contains the bond we
                // split. This partition should actually 2 partitions:
                // - one with the splitting bond
                // - one without the splitting bond
                // note that this will lead to repeated fragments when we  do this
                // with adjacent bonds, so when we gather all the fragments we need
                // to check for repeats
                IAtomContainer partContainer;
                partContainer = MakeAtomContainer(atom, part, excludedAtom);

                // by checking for more than 2 atoms, we exclude single bond fragments
                // also if a fragment has the same number of atoms as the parent molecule,
                // it is the parent molecule, so we exclude it.
                if (partContainer.Atoms.Count > 2 && partContainer.Atoms.Count != atomContainer.Atoms.Count)
                    ret.Add(partContainer);

                part.RemoveAt(0);
                partContainer = MakeAtomContainer(atom, part, excludedAtom);
                if (partContainer.Atoms.Count > 2 && partContainer.Atoms.Count != atomContainer.Atoms.Count)
                    ret.Add(partContainer);
            }
            return ret;
        }

        /// Given a list of bonds representing a fragment obtained by splitting the molecule
        /// at a bond, we need to create an IAtomContainer from it, containing *one* of the atoms
        /// of the splitting bond. In addition, the new IAtomContainer should not contain the
        /// splitting bond itself
        internal static IAtomContainer MakeAtomContainer(IAtom atom, IEnumerable<IBond> parts, IAtom excludedAtom)
        {
            IAtomContainer partContainer = atom.Builder.NewAtomContainer();
            partContainer.Atoms.Add(atom);
            foreach (var aBond in parts)
            {
                foreach (var bondedAtom in aBond.Atoms)
                {
                    if (!bondedAtom.Equals(excludedAtom) && !partContainer.Contains(bondedAtom))
                        partContainer.Atoms.Add(bondedAtom);
                }
                if (!aBond.Contains(excludedAtom)) partContainer.Bonds.Add(aBond);
            }
            return partContainer;
        }

        internal static List<IBond> Traverse(IAtomContainer atomContainer, IAtom atom, List<IBond> bondList)
        {
            var connectedBonds = atomContainer.GetConnectedBonds(atom);
            foreach (var aBond in connectedBonds)
            {
                if (bondList.Contains(aBond)) continue;
                bondList.Add(aBond);
                IAtom nextAtom = aBond.GetOther(atom);
                if (atomContainer.GetConnectedBonds(nextAtom).Count() == 1) continue;
                Traverse(atomContainer, nextAtom, bondList);
            }
            return bondList;
        }
    }
}
