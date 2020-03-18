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

using System;
using System.Collections.Generic;

namespace NCDK.LibIO.MD
{
    // @cdk.module libiomd
    public class MDMolecule : Silent.AtomContainer
    {
        //List of Residues
        private List<Residue> residues;

        //List of ChargeGroups
        private List<ChargeGroup> chargeGroups;

        public MDMolecule()
            : base()
        { }

        public MDMolecule(IAtomContainer container)
            : base(container)
        { }

        public List<Residue> GetResidues()
        {
            return residues;
        }

        public void SetResidues(List<Residue> residues)
        {
            this.residues = residues;
        }

        /// <summary>
        /// Add a Residue to the MDMolecule if not already present.
        /// </summary>
        /// <param name="residue">Residue to add</param>
        public void AddResidue(Residue residue)
        {
            if (residues == null) residues = new List<Residue>();

            //Check if exists
            if (residues.Contains(residue))
            {
                Console.Out.WriteLine($"Residue: {residue.Name} already present in molecule: {Id}");
                return;
            }

            residues.Add(residue);
        }

        public List<ChargeGroup> GetChargeGroups()
        {
            return chargeGroups;
        }

        public void SetChargeGroups(List<ChargeGroup> chargeGroups)
        {
            this.chargeGroups = chargeGroups;
        }

        /// <summary>
        /// Add a ChargeGroup to the MDMolecule if not already present.
        /// </summary>
        /// <param name="chargeGroup"><see cref="ChargeGroup"/> to add</param>
        public void AddChargeGroup(ChargeGroup chargeGroup)
        {
            if (chargeGroups == null) chargeGroups = new List<ChargeGroup>();

            //Check if exists
            if (chargeGroups.Contains(chargeGroup))
            {
                Console.Out.WriteLine($"Charge group: {chargeGroup.GetNumber()} already present in molecule: {Id}");
                return;
            }

            chargeGroups.Add(chargeGroup);
        }
    }
}
