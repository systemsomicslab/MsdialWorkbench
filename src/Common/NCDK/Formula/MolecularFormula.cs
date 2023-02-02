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
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
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
    /// Class defining a molecular formula object. It maintains a list of list <see cref="IIsotope"/>.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    /// <item>[C<sub>5</sub>H<sub>5</sub>]-</item>
    /// <item>C<sub>6</sub>H<sub>6</sub></item>
    /// <item><sup>12</sup>C<sub>5</sub><sup>13</sup>CH<sub>6</sub></item>
    /// </list>
    /// </remarks>
    // @cdk.module  data
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    // @cdk.keyword molecular formula
    public partial class MolecularFormula : IMolecularFormula
    {
        private Dictionary<IIsotope, int?> isotopes;

        /// <inheritdoc/>
        public int? Charge { get; set; } = null;

        /// <summary>
        /// Constructs an empty MolecularFormula.
        /// </summary>
        public MolecularFormula()
        {
            isotopes = new Dictionary<IIsotope, int?>(new IsotopeComparer());
        }

        internal class IsotopeComparer : IEqualityComparer<IIsotope>
        {
            public bool Equals(IIsotope isotopeOne, IIsotope isotopeTwo)
            {
                return IsTheSame(isotopeOne, isotopeTwo);
            }

            public int GetHashCode(IIsotope obj)
            {
                return 0;   // hash is not implemented.
            }
        }

        /// <summary>
        /// Adds an molecularFormula to this MolecularFormula.
        /// </summary>
        /// <param name="formula">The molecularFormula to be added to this chemObject</param>
        /// <returns>The IMolecularFormula</returns>
        public IMolecularFormula Add(IMolecularFormula formula)
        {
            foreach (var newIsotope in formula.Isotopes)
            {
                Add(newIsotope, formula.GetCount(newIsotope));
            }
            if (formula.Charge != null)
            {
                if (Charge != null)
                    Charge += formula.Charge;
                else
                    Charge = formula.Charge;
            }
            return this;
        }

        /// <summary>
        /// Adds an Isotope to this MolecularFormula one time.
        /// </summary>
        /// <param name="isotope">The isotope to be added to this MolecularFormula</param>
        public IMolecularFormula Add(IIsotope isotope)
        {
            return Add(isotope, 1);
        }

        /// <summary>
        /// Adds an Isotope to this MolecularFormula in a number of occurrences.
        /// </summary>
        /// <param name="isotope">The isotope to be added to this MolecularFormula</param>
        /// <param name="count">The number of occurrences to add</param>
        public IMolecularFormula Add(IIsotope isotope, int count)
        {
            if (count == 0)
                return this;
            foreach (var thisIsotope in isotopes)
            {
                if (IsTheSame(thisIsotope.Key, isotope))
                {
                    isotopes[thisIsotope.Key] = (thisIsotope.Value ?? 0) + count;
                    return this;
                }
            }
            isotopes.Add(isotope, count);
            return this;
        }

        /// <summary>
        /// True, if the MolecularFormula contains the given IIsotope object and not
        /// the instance. The method looks for other isotopes which has the same
        /// symbol, natural abundance and exact mass.
        /// </summary>
        /// <param name="isotope">The IIsotope this MolecularFormula is searched for</param>
        /// <returns>True, if the MolecularFormula contains the given isotope object</returns>
        public virtual bool Contains(IIsotope isotope)
            => isotopes.ContainsKey(isotope);

        /// <summary>
        /// Checks a set of Nodes for the occurrence of the isotope in the
        /// IMolecularFormula from a particular isotope. It returns 0 if the does not exist.
        /// </summary>
        /// <param name="isotope">The IIsotope to look for</param>
        /// <returns>The occurrence of this isotope in this IMolecularFormula</returns>
        public int GetCount(IIsotope isotope)
            => !Contains(isotope) ? 0 : isotopes[isotope] ?? 0;

        /// <summary>
        /// The number of different isotopes in this IMolecularFormula.
        /// </summary>
        public int IsotopesCount => isotopes.Count;

        /// <summary>
        /// An IEnumerator with the isotopes in this IMolecularFormula.
        /// </summary>
        public IEnumerable<IIsotope> Isotopes => isotopes.Keys;

        /// <summary>
        /// Removes all isotopes of this molecular formula.
        /// </summary>
        public void Clear()
        {
            isotopes.Clear();
        }

        /// <summary>
        /// Removes the given isotope from the MolecularFormula.
        /// </summary>
        /// <param name="isotope">The IIsotope to be removed</param>
        public void Remove(IIsotope isotope)
        {
            isotopes.Remove(isotope);
        }

        /// <summary>
        /// Clones this <see cref="MolecularFormula"/> object and its content. I should integrate into ChemObject.
        /// </summary>
        /// <returns>The cloned object</returns>
        public object Clone()
        {
            MolecularFormula clone = new MolecularFormula();
            foreach (var isotope_count in isotopes)
            {
                clone.isotopes.Add(isotope_count.Key, isotope_count.Value);
            }
            clone.Charge = Charge;
            return clone;
        }

        public ICDKObject Clone(CDKObjectMap map) => (ICDKObject)Clone();

        /// <summary>
        /// Compare to IIsotope. The method doesn't compare instance but if they
        /// have the same symbol, natural abundance and exact mass.
        /// </summary>
        /// <param name="isotopeOne">The first Isotope to compare</param>
        /// <param name="isotopeTwo">The second Isotope to compare</param>
        /// <returns>True, if both isotope are the same</returns>
        internal static bool IsTheSame(IIsotope isotopeOne, IIsotope isotopeTwo)
        {
            if (!object.Equals(isotopeOne.MassNumber, isotopeTwo.MassNumber))
                return false;

            var natAbund1 = isotopeOne.Abundance;
            var natAbund2 = isotopeTwo.Abundance;

            var exactMass1 = isotopeOne.ExactMass;
            var exactMass2 = isotopeTwo.ExactMass;

            if (natAbund1 == null)
                natAbund1 = -1.0;
            if (natAbund2 == null)
                natAbund2 = -1.0;
            if (exactMass1 == null)
                exactMass1 = -1.0;
            if (exactMass2 == null)
                exactMass2 = -1.0;

            if (!isotopeOne.Symbol.Equals(isotopeTwo.Symbol, StringComparison.Ordinal))
                return false;
            if (natAbund1.Value != natAbund2)
                return false;
            return exactMass1.Value == exactMass2;
        }

        public virtual IChemObjectBuilder Builder => CDK.Builder;
    }
}
