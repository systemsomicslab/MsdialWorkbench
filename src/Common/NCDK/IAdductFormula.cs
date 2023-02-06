/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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

using System.Collections.Generic;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace NCDK
{
    /// <summary>
    /// Class defining an adduct object in a MolecularFormula. 
    /// It maintains a list of list IMolecularFormula.
    /// Examples: [C2H4O2+Na]+
    /// </summary>
    public interface IAdductFormula
        : IMolecularFormulaSet
    {
        /// <summary>
        ///  Checks a set of Nodes for the occurrence of the isotope in the
        ///  adduct formula from a particular isotope. It returns 0 if the does not exist.
        /// </summary>
        /// <param name="isotope">The IIsotope to look for</param>
        /// <returns>The occurrence of this isotope in this adduct</returns>
        /// <seealso cref="IsotopeCount"/>
        int GetCount(IIsotope isotope);

        /// <summary>
        ///  Checks a set of Nodes for the number of different isotopes in the
        ///  adduct formula.
        /// </summary>
        /// <returns>The the number of different isotopes in this adduct formula</returns>
        /// <seealso cref="GetCount(IIsotope)"/>
        int IsotopeCount { get; }

        /// <summary>
        ///  An IEnumerator for looping over all isotopes in this adduct formula.
        /// </summary>
        IEnumerable<IIsotope> GetIsotopes();

        /// <summary>
        ///  The partial charge of this Adduct. If the charge
        ///  has not been set the return value is double.NaN.
        /// </summary>
        int? Charge { get; }

        /// <summary>
        ///  True, if the AdductFormula contains the given IIsotope object.
        /// </summary>
        /// <param name="isotope">The IIsotope this AdductFormula is searched for</param>
        /// <returns>True, if the AdductFormula contains the given isotope object</returns>
        bool Contains(IIsotope isotope);
    }
}
