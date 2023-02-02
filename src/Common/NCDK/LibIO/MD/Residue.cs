/* Copyright (C) 2007  Ola Spjuth <ospjuth@users.sf.net>
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

namespace NCDK.LibIO.MD
{
    /// <summary>
    /// A residue is a named, numbered collection of atoms in an MDMolecule.
    /// 
    /// Residues are used to partition molecules in distinct pieces.
    /// </summary>
    // @author ola
    // @cdk.module libiomd
    public class Residue : Silent.AtomContainer
    {
        private int number;
        public string Name { get; set; }
        private MDMolecule parentMolecule;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public Residue() { }

        /// <summary>
        /// Constructor to create a Residue based on an AC, a number, and a MDMolecule.
        /// </summary>
        public Residue(IAtomContainer container, int number, MDMolecule parentMolecule)
            : base(container)
        {
            this.number = number;
            this.parentMolecule = parentMolecule;
        }

        public int GetNumber()
        {
            return number;
        }

        public void SetNumber(int number)
        {
            this.number = number;
        }

        public MDMolecule GetParentMolecule()
        {
            return parentMolecule;
        }

        public void SetParentMolecule(MDMolecule parentMolecule)
        {
            this.parentMolecule = parentMolecule;
        }
    }
}
