



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2003-2015  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK.Default
{
    /// <summary>
    /// Atom that represents part of an residue in an enzyme, like Arg255.
    /// </summary>
    // @see  PseudoAtom
    // @cdk.module pdb
    public class EnzymeResidueLocator 
        : PseudoAtom
    {
        /// <summary>
        /// Constructs an EnzymeResidueLocator from a string containing the locator.
        /// </summary>
        /// <param name="label">The string describing the residue and its location.</param>
        public EnzymeResidueLocator(string label)
            : base(label)
        {
        }

        /// <summary>
        /// Constructs an EnzymeResidueLocator from an existing Atom.
        /// </summary>
        /// <param name="atom">Atom that should be converted into a EnzymeResidueLocator.</param>
        public EnzymeResidueLocator(IAtom atom)
            : base(atom)
        {
            if (atom is IPseudoAtom)
            {
                this.Label = ((IPseudoAtom)atom).Label;
            }
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// Atom that represents part of an residue in an enzyme, like Arg255.
    /// </summary>
    // @see  PseudoAtom
    // @cdk.module pdb
    public class EnzymeResidueLocator 
        : PseudoAtom
    {
        /// <summary>
        /// Constructs an EnzymeResidueLocator from a string containing the locator.
        /// </summary>
        /// <param name="label">The string describing the residue and its location.</param>
        public EnzymeResidueLocator(string label)
            : base(label)
        {
        }

        /// <summary>
        /// Constructs an EnzymeResidueLocator from an existing Atom.
        /// </summary>
        /// <param name="atom">Atom that should be converted into a EnzymeResidueLocator.</param>
        public EnzymeResidueLocator(IAtom atom)
            : base(atom)
        {
            if (atom is IPseudoAtom)
            {
                this.Label = ((IPseudoAtom)atom).Label;
            }
        }
    }
}
