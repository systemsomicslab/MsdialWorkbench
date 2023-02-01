/* Copyright (C) 2005-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, version 2.1.
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

using NCDK.Config;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.AtomTypes
{
    /// <summary>
    /// AtomTypeMatcher that finds an AtomType by matching the Atom's element symbol.
    /// This atom type matcher takes into account formal charge and number of
    /// implicit hydrogens, and requires bond orders to be given.
    /// <para>
    /// This class uses the <b>cdk/config/data/structgen_atomtypes.xml</b>
    /// list. If there is not an atom type defined for the tested atom, then null
    /// is returned.</para>
    /// </summary>
    // @author         egonw
    // @cdk.created    2006-09-22
    // @cdk.module     structgen
    public class StructGenMatcher : IAtomTypeMatcher
    {
        private readonly AtomTypeFactory factory = CDK.StructgenAtomTypeFactory;
        
        /// <summary>
        /// Constructor for the StructGenMatcher object.
        /// </summary>
        public StructGenMatcher() { }

        public IEnumerable<IAtomType> FindMatchingAtomTypes(IAtomContainer atomContainer)
        {
            int typeCounter = 0;
            foreach (var atom in atomContainer.Atoms)
            {
                yield return FindMatchingAtomType(atomContainer, atom);
                typeCounter++;
            }
            yield break;
        }

        /// <summary>
        /// Finds the AtomType matching the Atom's element symbol, formal charge and
        /// hybridization state.
        /// </summary>
        /// <param name="atomContainer">AtomContainer</param>
        /// <param name="atom">the target atom</param>
        /// <exception cref="CDKException">Exception thrown if something goes wrong</exception>
        /// <returns>the matching AtomType</returns>
        public IAtomType FindMatchingAtomType(IAtomContainer atomContainer, IAtom atom)
        {
            var bondOrderSum = atomContainer.GetBondOrderSum(atom);
            var maxBondOrder = atomContainer.GetMaximumBondOrder(atom);
            var charge = atom.FormalCharge.Value;
            var hcount = atom.ImplicitHydrogenCount == null ? 0 : atom.ImplicitHydrogenCount.Value;

            var types = factory.GetAtomTypes(atom.Symbol);
            foreach (var type in types)
            {
                Debug.WriteLine("   ... matching atom ", atom, " vs ", type);
                if (bondOrderSum - charge + hcount == type.BondOrderSum
                        && !BondManipulator.IsHigherOrder(maxBondOrder, type.MaxBondOrder))
                {
                    return type;
                }
            }
            Debug.WriteLine("    No Match");

            return null;
        }
    }
}
