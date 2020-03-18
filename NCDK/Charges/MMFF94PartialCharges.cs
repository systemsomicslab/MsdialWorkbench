/* Copyright (C) 2004-2008  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.ForceFields;
using System;

namespace NCDK.Charges
{
    /// <summary>
    /// The calculation of the MMFF94 partial charges. 
    /// </summary>
    /// <example>
    /// Charges are stored as atom properties ("MMFF94charge") for an AtomContainer ac, values are calculated with:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Charges.MMFF94PartialCharges_Example.cs"]/*' />
    /// and for each atom, the value is given by:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Charges.MMFF94PartialCharges_Example.cs+result"]/*' />
    ///  </example>
    /// <remarks>
    /// <note type="note">
    /// This class delegates to <see cref="Mmff"/> and charges are also assigned
    /// directly to the atom attribute <see cref="IAtom.Charge"/>.
    /// </note>
    /// </remarks>
    /// <seealso cref="Mmff.PartialCharges(IAtomContainer)"/> 
    // @author mfe4
    // @author chhoppe
    // @cdk.created 2004-11-03
    // @cdk.module forcefield
    public class MMFF94PartialCharges : IChargeCalculator
    {
        public const string Key = "MMFF94charge";
        private readonly Mmff mmff = new Mmff();

        /// <summary>
        /// Constructor for the MMFF94PartialCharges object
        /// </summary>
        public MMFF94PartialCharges()
        {
        }

        /// <summary>
        /// Main method which assigns MMFF94 partial charges
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>AtomContainer with MMFF94 partial charges as atom properties</returns>
        public IAtomContainer AssignMMFF94PartialCharges(IAtomContainer ac)
        {
            if (!mmff.AssignAtomTypes(ac))
                throw new CDKException("Molecule had an atom of unknown MMFF type");
            mmff.PartialCharges(ac);
            mmff.ClearProps(ac);
            foreach (var atom in ac.Atoms)
                atom.SetProperty(Key, atom.Charge);
            return ac;
        }

        public void CalculateCharges(IAtomContainer container)
        {
            try
            {
                AssignMMFF94PartialCharges(container);
            }
            catch (Exception exception)
            {
                throw new CDKException("Could not calculate MMFF94 partial charges: " + exception.Message, exception);
            }
        }
    }
}
