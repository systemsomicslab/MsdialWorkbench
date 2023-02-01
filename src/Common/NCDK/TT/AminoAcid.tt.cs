



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2005-2007  Egon Willighagen <e.willighagen@science.ru.nl>
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

namespace NCDK.Default
{
    /// <summary>
    /// A AminoAcid is Monomer which stores additional amino acid specific
    /// informations, like the N-terminus atom.
    /// </summary>
    // @author      Egon Willighagen <e.willighagen@science.ru.nl>
    // @cdk.created 2005-08-11
    // @cdk.keyword amino acid
    public class AminoAcid
        : Monomer, IAminoAcid
    {
        internal IAtom nTerminus;
        internal IAtom cTerminus;

        public AminoAcid()
        {
        }

        /// <summary>N-terminus atom.</summary>
        public IAtom NTerminus => nTerminus;

        /// <summary>C-terminus atom.</summary>
        public IAtom CTerminus => cTerminus;
        
        /// <summary>
        /// Add an <see cref="IAtom"/> and makes it the N-terminus atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> that is the N-terminus</param>
        public void AddNTerminus(IAtom atom)
        {
            base.Atoms.Add(atom);    //  OnStateChanged is called here
            nTerminus = atom;
        }

        /// <summary>
        /// Add an <see cref="IAtom"/> and makes it the C-terminus atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> that is the C-terminus</param>
        public void AddCTerminus(IAtom atom)
        {
            base.Atoms.Add(atom);    //  OnStateChanged is called here
            cTerminus = atom;
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (AminoAcid)base.Clone(map);
            if (nTerminus != null)
                clone.nTerminus = clone.atoms[this.atoms.IndexOf(nTerminus)];
            if (cTerminus != null)
                clone.cTerminus = clone.atoms[this.atoms.IndexOf(cTerminus)];
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A AminoAcid is Monomer which stores additional amino acid specific
    /// informations, like the N-terminus atom.
    /// </summary>
    // @author      Egon Willighagen <e.willighagen@science.ru.nl>
    // @cdk.created 2005-08-11
    // @cdk.keyword amino acid
    public class AminoAcid
        : Monomer, IAminoAcid
    {
        internal IAtom nTerminus;
        internal IAtom cTerminus;

        public AminoAcid()
        {
        }

        /// <summary>N-terminus atom.</summary>
        public IAtom NTerminus => nTerminus;

        /// <summary>C-terminus atom.</summary>
        public IAtom CTerminus => cTerminus;
        
        /// <summary>
        /// Add an <see cref="IAtom"/> and makes it the N-terminus atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> that is the N-terminus</param>
        public void AddNTerminus(IAtom atom)
        {
            base.Atoms.Add(atom);    //  OnStateChanged is called here
            nTerminus = atom;
        }

        /// <summary>
        /// Add an <see cref="IAtom"/> and makes it the C-terminus atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> that is the C-terminus</param>
        public void AddCTerminus(IAtom atom)
        {
            base.Atoms.Add(atom);    //  OnStateChanged is called here
            cTerminus = atom;
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (AminoAcid)base.Clone(map);
            if (nTerminus != null)
                clone.nTerminus = clone.atoms[this.atoms.IndexOf(nTerminus)];
            if (cTerminus != null)
                clone.cTerminus = clone.atoms[this.atoms.IndexOf(cTerminus)];
            return clone;
        }
    }
}
