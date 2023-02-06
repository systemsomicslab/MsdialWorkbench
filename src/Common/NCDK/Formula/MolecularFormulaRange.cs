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

using System;
using System.Collections.Generic;

namespace NCDK.Formula
{
    /// <summary>
    /// Class defining a expanded molecular formula object. The Isotopes don't have
    /// a fix occurrence in the <see cref="MolecularFormula"/> but they have a range.
    /// <para>
    /// With this class man can define a MolecularFormula which contains certain IIsotope
    /// with a maximum and minimum occurrence.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item>"[C(1-5)H(4-10)]-"</item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    // @cdk.keyword molecular formula
    public class MolecularFormulaRange
    {
        private Dictionary<IIsotope, int> isotopesMax;
        private Dictionary<IIsotope, int> isotopesMin;

        /// <summary>
        /// Constructs an empty MolecularFormulaExpand.
        /// </summary>
        public MolecularFormulaRange()
        {
            isotopesMax = new Dictionary<IIsotope, int>();
            isotopesMin = new Dictionary<IIsotope, int>();
        }

        /// <summary>
        /// Adds an Isotope to this MolecularFormulaExpand in a number of
        /// maximum and minimum occurrences allowed.
        /// </summary>
        /// <param name="isotope">The isotope to be added to this MolecularFormulaExpand</param>
        /// <param name="countMax">The maximal number of occurrences to add</param>
        /// <param name="countMin">The minimal number of occurrences to add</param>
        public void AddIsotope(IIsotope isotope, int countMin, int countMax)
        {
            if (isotope == null)
                throw new ArgumentNullException(nameof(isotope), "Isotope must not be null");

            bool flag = false;
            foreach (var thisIsotope in GetIsotopes())
            {
                if (IsTheSame(thisIsotope, isotope))
                {
                    isotopesMax[thisIsotope] = countMax;
                    isotopesMin[thisIsotope] = countMin;
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                isotopesMax[isotope] = countMax;
                isotopesMin[isotope] = countMin;
            }
        }

        /// <summary>
        /// True, if the MolecularFormulaExpand contains the given IIsotope.
        /// The method looks for other isotopes which has the same
        /// symbol, natural abundance and exact mass.
        /// </summary>
        /// <param name="isotope">The IIsotope this MolecularFormula is searched for</param>
        /// <returns>True, if the MolecularFormula contains the given isotope object</returns>
        public bool Contains(IIsotope isotope)
        {
            foreach (var thisIsotope in GetIsotopes())
            {
                if (IsTheSame(thisIsotope, isotope))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks a set of Nodes for the maximal occurrence of the isotope in the
        /// MolecularFormulaExpand from a particular isotope. It returns -1 if the Isotope
        /// does not exist.
        /// </summary>
        /// <param name="isotope">The IIsotope to look for</param>
        /// <returns>The occurrence of this isotope in this IMolecularFormula</returns>
        public int GetIsotopeCountMax(IIsotope isotope)
        {
            return !Contains(isotope) ? -1 : isotopesMax[GetIsotope(isotope)];
        }

        /// <summary>
        /// Checks a set of Nodes for the minimal occurrence of the isotope in the
        /// MolecularFormulaExpand from a particular isotope. It returns -1 if the Isotope
        /// does not exist.
        /// </summary>
        /// <param name="isotope">The IIsotope to look for</param>
        /// <returns>The occurrence of this isotope in this IMolecularFormula</returns>
        public int GetIsotopeCountMin(IIsotope isotope)
        {
            return !Contains(isotope) ? -1 : isotopesMin[GetIsotope(isotope)];
        }

        /// <summary>
        /// Checks a set of Nodes for the number of different isotopes in the
        /// MolecularFormulaExpand.
        /// </summary>
        /// <returns>The the number of different isotopes in this MolecularFormulaExpand</returns>
        public int Count => isotopesMax.Count;

        /// <summary>
        /// Get the isotope instance given an IIsotope. The instance is those
        /// that has the isotope with the same symbol, natural abundance and
        /// exact mass.
        /// </summary>
        /// <param name="isotope">The IIsotope for looking for</param>
        /// <returns>The IIsotope instance</returns>
        /// <seealso cref="GetIsotopes"/>
        private IIsotope GetIsotope(IIsotope isotope)
        {
            foreach (var thisIsotope in GetIsotopes())
            {
                if (IsTheSame(isotope, thisIsotope)) return thisIsotope;
            }
            return null;
        }

        /// <summary>
        /// Get all isotopes in this MolecularFormulaExpand.
        /// </summary>
        /// <returns>The isotopes in this MolecularFormulaExpand</returns>
        public IEnumerable<IIsotope> GetIsotopes() => isotopesMax.Keys;

        /// <summary>
        /// Removes all isotopes of this molecular formula.
        /// </summary>
        public void Clear()
        {
            isotopesMax.Clear();
            isotopesMin.Clear();
        }

        /// <summary>
        /// Removes the given isotope from the MolecularFormulaExpand.
        /// </summary>
        /// <param name="isotope">The IIsotope to be removed</param>
        public void Remove(IIsotope isotope)
        {
            {
                var k = GetIsotope(isotope);
                if (k != null) isotopesMax.Remove(k);
            }
            {
                var k = GetIsotope(isotope);
                if (k != null) isotopesMin.Remove(k);
            }
        }

        /// <summary>
        /// Clones this MolecularFormulaExpand object and its content. I should
        /// integrate into ChemObject.
        /// </summary>
        /// <returns>The cloned object</returns>
        public virtual object Clone()
        {
            MolecularFormulaRange clone = new MolecularFormulaRange();
            foreach (var isotope in GetIsotopes())
            {
                clone.AddIsotope((IIsotope)isotope.Clone(), GetIsotopeCountMin(isotope), GetIsotopeCountMax(isotope));
            }
            return clone;
        }

        /// <summary>
        /// Compare to IIsotope. The method doesn't compare instance but if they
        /// have the same symbol, natural abundance and exact mass.
        /// </summary>
        /// <param name="isotopeOne">The first Isotope to compare</param>
        /// <param name="isotopeTwo">The second Isotope to compare</param>
        /// <returns>True, if both isotope are the same</returns>
        private static bool IsTheSame(IIsotope isotopeOne, IIsotope isotopeTwo)
        {
            if (!isotopeOne.Symbol.Equals(isotopeTwo.Symbol, StringComparison.Ordinal))
                return false;
            // XXX: floating point comparision!
            if (isotopeOne.Abundance != isotopeTwo.Abundance)
                return false;
            if (isotopeOne.ExactMass != isotopeTwo.ExactMass)
                return false;

            return true;
        }
    }
}
