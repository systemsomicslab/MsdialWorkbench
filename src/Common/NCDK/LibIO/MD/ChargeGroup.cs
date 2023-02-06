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
    /// A ChargeGroup (CG) is a numbered collection of atoms in an MDMolecule.
    /// 
    /// A CG is a small set of atoms with total zero or Integer charge.
    /// </summary>
    // @author ola
    // @cdk.module libiomd
    public class ChargeGroup : Silent.AtomContainer
    {
        private int number;
        private MDMolecule parentMolecule;
        private IAtom switchingAtom;

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public ChargeGroup() { }

        /// <summary>
        /// Constructor to create a ChargeGroup based on an AC, a number, and a MDMolecule.
        /// </summary>
        /// <param name="container"></param>
        /// <param name="number"></param>
        /// <param name="parentMolecule"></param>
        public ChargeGroup(IAtomContainer container, int number, MDMolecule parentMolecule)
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

        public IAtom GetSwitchingAtom()
        {
            return switchingAtom;
        }

        public void SetSwitchingAtom(IAtom switchingAtom)
        {
            this.switchingAtom = switchingAtom;
        }
    }
}
