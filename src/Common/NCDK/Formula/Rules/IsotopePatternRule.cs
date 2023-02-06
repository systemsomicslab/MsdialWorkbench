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
using System.Diagnostics;
using System.Linq;

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the <see cref="IsotopePattern"/> from a given <see cref="IMolecularFormula"/> correspond with other to compare.
    /// </summary>
    /// <remarks>
    /// Table 1: Parameters set by this rule.
    /// <list type="table">
    /// <listheader>
    ///   <term>Name</term>
    ///   <term>Default</term>
    ///   <term>Description</term>
    /// </listheader>
    /// <item>
    ///   <term>isotopePattern</term>
    ///   <term>List &lt;Double[]&gt;</term>
    ///   <term>The <see cref="IsotopePattern"/> to compare</term>
    /// </item>
    /// </list>
    /// </remarks> 
    // @cdk.module  formula
    // @author      Miguel Rojas Cherto
    // @cdk.created 2007-11-20
    public class IsotopePatternRule : IRule
    {
        /// <summary>Accuracy on the mass measuring isotope pattern</summary>
        private const double toleranceMass = 0.001;
        private IsotopePattern pattern;
        private readonly IsotopePatternGenerator isotopeGe;
        private readonly IsotopePatternSimilarity isotopePatternSimilarity;

        /// <summary>
        /// Constructor for the <see cref="IsotopePatternRule"/> object.
        /// </summary>
        public IsotopePatternRule()
        {
            isotopeGe = new IsotopePatternGenerator(0.01);
            isotopePatternSimilarity = new IsotopePatternSimilarity { Tolerance = toleranceMass };
        }

        /// <summary>
        /// The parameters attribute of the <see cref="IsotopePatternRule"/> object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get
            {
                // return the parameters as used for the rule validation
                object[] parameters = new object[2];
                if (pattern == null)
                    parameters[0] = null;
                else
                {
                    List<double[]> params0 = new List<double[]>();
                    foreach (IsotopeContainer isotope in pattern.Isotopes)
                    {
                        params0.Add(new double[] { isotope.Mass, isotope.Intensity });
                    }
                    parameters[0] = params0;
                }
                parameters[1] = toleranceMass;
                return parameters;
            }

            set
            {
                if (value.Count != 2)
                    throw new CDKException("IsotopePatternRule expects two parameter");

                if (!(value[0] is IList<double[]>))
                    throw new CDKException("The parameter one must be of type List<Double[]>");

                if (!(value[1] is double))
                    throw new CDKException("The parameter two must be of type Double");

                pattern = new IsotopePattern(((IList<double[]>)value[0]).Select(n => new IsotopeContainer(n[0], n[1])));

                isotopePatternSimilarity.Tolerance = (double)value[1];
            }
        }

        /// <summary>
        /// Validate the isotope pattern of this <see cref="IMolecularFormula"/>. Important, first
        /// you have to add with the <see cref="Parameters"/> a <see cref="IMolecularFormulaSet"/>
        /// which represents the isotope pattern to compare.
        /// </summary>
        /// <param name="formula">Parameter is the <see cref="IMolecularFormula"/></param>
        /// <returns>A double value meaning 1.0 True, 0.0 False</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);

            IsotopePatternGenerator isotopeGe = new IsotopePatternGenerator(0.1);
            IsotopePattern patternIsoPredicted = isotopeGe.GetIsotopes(formula);
            IsotopePattern patternIsoNormalize = IsotopePatternManipulator.Normalize(patternIsoPredicted);

            return isotopePatternSimilarity.Compare(pattern, patternIsoNormalize);
        }
    }
}
