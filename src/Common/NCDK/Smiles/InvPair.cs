/* Copyright (C) 2001-2002  Oliver Horlacher <oliver.horlacher@therastrat.com>
 *                    2002  Christoph Steinbeck <steinbeck@users.sf.net>
 *          2003-2008,2011  Egon Willighagen <egonw@users.sf.net>
 *                    2004  Stefan Kuhn <shk3@users.sf.net>
 *                    2006  Kai Hartmann <kaihartmann@users.sf.net>
 *               2008-2009  Rajarshi Guha <rajarshi@users.sf.net>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Maths;
using System;
using System.Text;

namespace NCDK.Smiles
{
    /// <summary>
    /// This is used to hold the invariance numbers for the canonical labeling of
    /// <see cref="IAtomContainer"/>s.
    /// </summary>
    // @cdk.module standard
    public class InvPair
    {
        /// <summary>
        /// The description used to set the invariance numbers in the atom's property
        /// </summary>
        public const string InvariancePairPropertyKey = "InvariancePair";

        /// <summary>
        /// The description used to set the canonical numbers in the atom's property
        /// </summary>
        public const string CanonicalLabelPropertyKey = "CanonicalLabel";

        public long Last { get; set; } = 0;

        /// <summary>
        /// The value of the seed.
        /// </summary>
        /// <remarks>
        /// Note that use of this method implies that a new prime number is desired.
        /// If so, make sure to set <see cref="Prime"/> to ensure that a new prime
        /// number is obtained using the new seed.
        /// Todo make the following robust!       
        /// </remarks>
        public long Curr { get; set; } = 0;

        public IAtom Atom { get; set; }

        public InvPair() { }

        public InvPair(long current, IAtom atom)
        {
            Curr = current;
            Atom = atom;
            atom.SetProperty(InvariancePairPropertyKey, this);
        }

        public override bool Equals(object obj)
        {
            if (obj is InvPair o)
            {
                return (Last == o.Last && Curr == o.Curr);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Last.GetHashCode() * 31 + Curr.GetHashCode();
        }

        public void Commit()
        {
            Atom.SetProperty(CanonicalLabelPropertyKey, Curr);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var buff = new StringBuilder();
            buff.Append(Curr);
            buff.Append('\t');
            return buff.ToString();
        }

        /// <summary>
        /// The prime number based on the current seed.
        /// </summary>
        public int Prime { get; private set; }

        /// <summary>
        /// Sets the prime number based on the current seed.
        /// </summary>
        /// <remarks>
        /// Note that if you change the seed via <see cref="Curr"/>, you should make
        /// sure to call this method so that a new prime number is available via
        /// <see cref="Prime"/>. 
        /// </remarks>
        /// <seealso cref="Curr"/>
        /// <seealso cref="Prime"/>
        public void SetPrime()
        {
            Prime = Primes.GetPrimeAt((int)Curr - 1);
        }
    }
}
