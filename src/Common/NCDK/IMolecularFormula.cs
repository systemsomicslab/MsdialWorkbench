/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
 *               2012  John May <john.wilkinsonmay@gmail.com>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Class defining a molecular formula object. It maintains a list of <see cref="IIsotope"/>.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item>[C5H5]-</item>
    /// <item>C6H6</item>
    /// <item><sup>12</sup>C<sub>5</sub><sup>13</sup>CH<sub>6</sub></item>
    /// </list>
    /// </remarks>
    // @cdk.module  interfaces
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    // @cdk.keyword molecular formula
    public partial interface IMolecularFormula : ICDKObject
    {
        /// <summary>
        /// Adds an <see cref="IMolecularFormula"/> to this <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <param name="formula">The <see cref="IMolecularFormula"/> to be added to this <see cref="IChemObject"/></param>
        /// <returns>the new molecular formula</returns>
        IMolecularFormula Add(IMolecularFormula formula);

        /// <summary>
        /// Adds an <see cref="IIsotope"/> to this <see cref="IMolecularFormula"/> one time.
        /// </summary>
        /// <param name="isotope">The isotope to be added to this <see cref="IMolecularFormula"/></param>
        /// <returns>the new molecular formula</returns>
        /// <seealso cref="Add(IIsotope, int)"/>
        IMolecularFormula Add(IIsotope isotope);

        /// <summary>
        ///  Adds an <see cref="IIsotope"/> to this <see cref="IMolecularFormula"/> in a number of occurrences.
        /// </summary>
        /// <param name="isotope">The isotope to be added to this <see cref="IMolecularFormula"/></param>
        /// <param name="count">The number of occurrences to add</param>
        /// <returns>the new molecular formula</returns>
        /// <seealso cref="Add(IIsotope)"/>
        IMolecularFormula Add(IIsotope isotope, int count);

        /// <summary>
        /// Checks a set of Nodes for the occurrence of the isotope in the
        /// <see cref="IMolecularFormula"/> from a particular isotope. It returns 0 if the does not exist.
        /// </summary>
        /// <param name="isotope">The <see cref="IIsotope"/> to look for</param>
        /// <returns>The occurrence of this isotope in this IMolecularFormula</returns>
        int GetCount(IIsotope isotope);

        /// <summary>
        ///  Returns an <see cref="IEnumerable{T}"/> for looping over all isotopes in this <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> with the isotopes in this <see cref="IMolecularFormula"/></returns>
        IEnumerable<IIsotope> Isotopes { get; }

        /// <summary>
        /// Checks a set of Nodes for the number of different isotopes in the
        /// <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <value>The the number of different isotopes in this <see cref="IMolecularFormula"/></value>
        int IsotopesCount { get; }

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IMolecularFormula"/> contains the given <see cref="IIsotope"/> object. Not
        /// the instance. The method looks for other isotopes which has the same
        /// symbol, natural abundance and exact mass.
        /// </summary>
        /// <param name="isotope">The <see cref="IIsotope"/> this IMolecularFormula is searched for</param>
        /// <returns>True, if the <see cref="IMolecularFormula"/> contains the given isotope object</returns>
        bool Contains(IIsotope isotope);

        /// <summary>
        /// Removes the given isotope from the <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <param name="isotope">The IIsotope to be removed</param>
        void Remove(IIsotope isotope);

        /// <summary>
        /// Removes all isotopes of this molecular formula.
        /// </summary>
        void Clear();

        /// <summary>
        /// Sets the charge of this IMolecularFormula, since there is no atom
        /// associated with the charge the number of a given isotope is not modified.
        /// </summary>
        /// <example>
        /// <code>
        /// // Correct usage
        /// IMolecularFormula phenolate = MolecularFormulaManipulator.GetMolecularFormula("C6H5O", builder)
        /// mf.Charge = -1;
        /// // MF=C6H5O-
        /// 
        /// // Wrong! the H6 is not automatically adjust
        /// IMolecularFormula phenolate = MolecularFormulaManipulator.getMolecularFormula("C6H6O", builder)
        /// mf.Charge = -1;
        /// // MF=C6H6O- (wrong)
        /// </code>
        /// 
        /// If you wish to adjust the protonation of a formula try the convenience method of the <see cref="Tools.Manipulator.MolecularFormulaManipulator"/>:
        /// 
        /// <code>
        /// IMolecularFormula mf = MolecularFormulaManipulator.getMolecularFormula("[C6H5O]-", bldr);
        /// MolecularFormulaManipulator.AdjustProtonation(mf, +1);
        /// MolecularFormulaManipulator.GetString(mf); // "C6H6O"
        /// </code>
        /// </example>
        /// <value>
        /// If the charge has not been set the return value is <see langword="null"/>.</value>
        int? Charge { get; set; }
    }
}
