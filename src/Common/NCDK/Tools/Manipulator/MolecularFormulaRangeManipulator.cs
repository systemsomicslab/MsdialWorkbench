/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
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

using NCDK.Formula;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with convenience methods that provide methods to manipulate
    /// MolecularFormulaRange's.
    /// </summary>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public static class MolecularFormulaRangeManipulator
    {
        /// <summary>
        /// Extract from a set of MolecularFormula the range of each each element found and
        /// put the element and occurrence in a new MolecularFormulaRange.
        /// </summary>
        /// <param name="mfSet">The set of molecularFormules to inspect</param>
        /// <returns>A MolecularFormulaRange containing range occurrence of the elements</returns>
        public static MolecularFormulaRange GetRange(IMolecularFormulaSet mfSet)
        {
            var mfRange = new MolecularFormulaRange();

            foreach (var mf in mfSet)
            {
                foreach (var isotope in mf.Isotopes)
                {
                    int occur_new = mf.GetCount(isotope);
                    if (!mfRange.Contains(isotope))
                    {
                        mfRange.AddIsotope(isotope, occur_new, occur_new);
                    }
                    else
                    {
                        int occur_old_Max = mfRange.GetIsotopeCountMax(isotope);
                        int occur_old_Min = mfRange.GetIsotopeCountMin(isotope);
                        if (occur_new > occur_old_Max)
                        {
                            mfRange.Remove(isotope);
                            mfRange.AddIsotope(isotope, occur_old_Min, occur_new);
                        }
                        else if (occur_new < occur_old_Min)
                        {
                            mfRange.Remove(isotope);
                            mfRange.AddIsotope(isotope, occur_new, occur_old_Max);
                        }
                    }
                }
            }
            // looking for those Isotopes which are not contained which then should be 0.
            foreach (var mf in mfSet)
            {
                if (mf.IsotopesCount != mfRange.Count)
                {
                    foreach (var isotope in mfRange.GetIsotopes().ToReadOnlyList())
                    {
                        if (!mf.Contains(isotope))
                        {
                            int occurMax = mfRange.GetIsotopeCountMax(isotope);
                            mfRange.AddIsotope(isotope, 0, occurMax);
                        }
                    }
                }
            }
            return mfRange;
        }

        /// <summary>
        /// Returns the maximal occurrence of the IIsotope into IMolecularFormula
        /// from this MolelecularFormulaRange.
        /// </summary>
        /// <param name="mfRange">The MolecularFormulaRange to analyze</param>
        /// <returns>A IMolecularFormula containing the maximal occurrence of each isotope</returns>
        public static IMolecularFormula GetMaximalFormula(MolecularFormulaRange mfRange, IChemObjectBuilder builder)
        {
            var formula = builder.NewMolecularFormula();

            foreach (var isotope in mfRange.GetIsotopes())
            {
                formula.Add(isotope, mfRange.GetIsotopeCountMax(isotope));
            }

            return formula;
        }

        /// <summary>
        /// Returns the minimal occurrence of the IIsotope into IMolecularFormula
        /// from this MolelecularFormulaRange.
        /// </summary>
        /// <param name="mfRange">The MolecularFormulaRange to analyze</param>
        /// <returns>A IMolecularFormula containing the minimal occurrence of each isotope</returns>
        public static IMolecularFormula GetMinimalFormula(MolecularFormulaRange mfRange, IChemObjectBuilder builder)
        {
            var formula = builder.NewMolecularFormula();

            foreach (var isotope in mfRange.GetIsotopes())
            {
                formula.Add(isotope, mfRange.GetIsotopeCountMin(isotope));
            }

            return formula;
        }
    }
}
