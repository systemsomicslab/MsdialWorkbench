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

using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the occurrence of the <see cref="IElement"/> in the <see cref="IMolecularFormula"/>
    /// are into a limits. As default defines all elements of the periodic table with
    /// a occurrence of zero to 100.
    /// </summary>
    /// <remarks>
    /// Table 1: Parameters set by this rule.
    /// <list type="table">
    /// <listheader>
    /// <term>Name</term>
    /// <term>Default</term>
    /// <term>Default</term>
    /// </listheader>
    /// <item>
    /// <term>elements</term>
    /// <term>C,H,N,O</term>
    /// <term>The <see cref="IElement"/> to be analyzed</term>
    /// </item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public class ElementRule : IRule
    {
        private MolecularFormulaRange mfRange;

        /// <summary>
        /// Constructor for the ElementRule object.
        /// </summary>
        public ElementRule() { }

        /// <summary>
        /// The parameters attribute of the ElementRule object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get
            {
                // return the parameters as used for the rule validation
                object[] parameters = new object[1];
                parameters[0] = mfRange;
                return parameters;
            }

            set
            {
                if (value.Count != 1)
                    throw new CDKException("ElementRule expects one parameters");

                if (!(value[0] is null || value[0] is MolecularFormulaRange))
                    throw new CDKException("The parameter must be of type MolecularFormulaExpand");

                mfRange = (MolecularFormulaRange)value[0];
            }
        }

        /// <summary>
        /// Validate the occurrence of this <see cref="IMolecularFormula"/>.
        /// </summary>
        /// <param name="formula">Parameter is the <see cref="IMolecularFormula"/></param>
        /// <returns>An ArrayList containing 9 elements in the order described above</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);
            EnsureDefaultOccurElements(formula.Builder);

            double isValid = 1.0;
            foreach (var element in MolecularFormulaManipulator.Elements(formula))
            {
                int occur = MolecularFormulaManipulator.GetElementCount(formula, element);
                var elemIsotope = formula.Builder.NewIsotope(element.Symbol);
                if ((occur < mfRange.GetIsotopeCountMin(elemIsotope)) || (occur > mfRange.GetIsotopeCountMax(elemIsotope)))
                {
                    isValid = 0.0;
                    break;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Initiate the MolecularFormulaExpand with the maximum and minimum occurrence of the Elements.
        /// In this case all elements of the periodic table are loaded.
        /// </summary>
        /// <param name="builder"></param>
        private void EnsureDefaultOccurElements(IChemObjectBuilder builder)
        {
            if (mfRange == null)
            {
                string[] elements = new string[]{"C", "H", "O", "N", "Si", "P", "S", "F", "Cl", "Br", "I", "Sn", "B", "Pb",
                    "Tl", "Ba", "In", "Pd", "Pt", "Os", "Ag", "Zr", "Se", "Zn", "Cu", "Ni", "Co", "Fe", "Cr", "Ti",
                    "Ca", "K", "Al", "Mg", "Na", "Ce", "Hg", "Au", "Ir", "Re", "W", "Ta", "Hf", "Lu", "Yb", "Tm", "Er",
                    "Ho", "Dy", "Tb", "Gd", "Eu", "Sm", "Pm", "Nd", "Pr", "La", "Cs", "Xe", "Te", "Sb", "Cd", "Rh",
                    "Ru", "Tc", "Mo", "Nb", "Y", "Sr", "Rb", "Kr", "As", "Ge", "Ga", "Mn", "V", "Sc", "Ar", "Ne", "Be",
                    "Li", "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th", "Pa", "U", "Np", "Pu"};

                mfRange = new MolecularFormulaRange();
                foreach (var element in elements)
                    mfRange.AddIsotope(builder.NewIsotope(element), 0, 50);
            }
        }
    }
}
