/* Copyright (C) 2006-2007  Todd Martin (Environmental Protection Agency) <Martin.Todd@epamail.epa.gov>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;

namespace NCDK.AtomTypes
{
    /// <summary>
    /// Determines the EState atom types.
    /// </summary>
    // @author Todd Martin
    // @author nick
    // @cdk.module standard
    // @cdk.keyword atom type, E-state
    public class EStateAtomTypeMatcher : IAtomTypeMatcher
    {
        public IRingSet RingSet { get; private set; }

        public EStateAtomTypeMatcher()
            : this(null)
        {
        }

        public EStateAtomTypeMatcher(IRingSet ringSet)
        {
            this.RingSet = ringSet;
        }

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

        public IAtomType FindMatchingAtomType(IAtomContainer atomContainer, IAtom atom)
        {
            IAtomType atomType = null;
            try
            {
                string fragment = "";
                int NumHAtoms = atom.ImplicitHydrogenCount ?? 0;
                int NumSingleBonds2 = NumHAtoms;
                int NumDoubleBonds2 = 0;
                int NumTripleBonds2 = 0;
                int NumAromaticBonds2 = 0;
                int NumAromaticBondsTotal2 = 0;

                string element = atom.Symbol;
                var n = atom.AtomicNumber;

                var attachedAtoms = atomContainer.GetConnectedAtoms(atom);
                foreach (var attached in attachedAtoms)
                {
                    var b = atomContainer.GetBond(atom, attached);
                    if (attached.AtomicNumber.Equals(AtomicNumbers.H))
                        NumHAtoms++;

                    if (atom.IsAromatic && attached.IsAromatic)
                    {
                        bool SameRing = InSameAromaticRing(atomContainer, atom, attached);

                        if (SameRing)
                        {
                            NumAromaticBonds2++;
                            if (n.Equals(AtomicNumbers.N))
                            {
                                if (b.Order == BondOrder.Single) NumAromaticBondsTotal2++;
                                if (b.Order == BondOrder.Double)
                                    NumAromaticBondsTotal2 = NumAromaticBondsTotal2 + 2;
                            }
                        }
                        else
                        {
                            if (b.Order == BondOrder.Single) NumSingleBonds2++;
                            if (b.Order == BondOrder.Double) NumDoubleBonds2++;
                            if (b.Order == BondOrder.Triple) NumTripleBonds2++;
                        }
                    }
                    else
                    {
                        if (b.Order == BondOrder.Single) NumSingleBonds2++;
                        if (b.Order == BondOrder.Double) NumDoubleBonds2++;
                        if (b.Order == BondOrder.Triple) NumTripleBonds2++;
                    }
                }
                NumSingleBonds2 = NumSingleBonds2 - NumHAtoms;

                // assign frag here
                fragment = "S";

                for (int j = 0; j <= NumTripleBonds2 - 1; j++)
                {
                    fragment += "t";
                }

                for (int j = 0; j <= NumDoubleBonds2 - 1; j++)
                {
                    fragment += "d";
                }

                for (int j = 0; j <= NumSingleBonds2 - 1; j++)
                {
                    fragment += "s";
                }

                for (int j = 0; j <= NumAromaticBonds2 - 1; j++)
                {
                    fragment += "a";
                }

                fragment += element;

                if (atom.FormalCharge == 1)
                {
                    fragment += "p";
                }
                else if (atom.FormalCharge == -1)
                {
                    fragment += "m";
                }

                if (NumHAtoms == 1)
                    fragment += "H";
                else if (NumHAtoms > 1)
                    fragment += ("H" + NumHAtoms);

                atomType = atom.Builder.NewAtomType(fragment, atom.Symbol);
                atomType.FormalCharge = atom.FormalCharge;
                if (atom.IsAromatic)
                    atomType.IsAromatic = true;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }

            return atomType;
        }

        private bool InSameAromaticRing(IAtomContainer m, IAtom atom1, IAtom atom2)
        {
            if (RingSet == null)
                return false;
            foreach (var r in RingSet)
            {
                if (r.Contains(atom1) && r.Contains(atom2))
                {
                    if (IsAromaticRing(r))
                        return true;
                }
            }
            return false;
        }

        private static bool IsAromaticRing(IRing ring)
        {
            for (int i = 0; i < ring.Atoms.Count; i++)
                if (!ring.Atoms[i].IsAromatic)
                    return false;

            return true;
        }
    }
}
