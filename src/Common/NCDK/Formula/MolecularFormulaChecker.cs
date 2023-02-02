/* Copyright (C) 2007  Miguel Rojasch <miguelrojasch@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Formula.Rules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Formula
{
    /// <summary>
    /// Validate a molecular formula given in IMolecularformula object. The
    /// validation is based according different rules that you have to introduce before
    /// see IRule.
    /// </summary>
    /// <seealso cref="IRule"/>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    // @cdk.keyword molecule, molecular formula
    public class MolecularFormulaChecker
    {
       /// <summary>List of IRules to be applied in the validation.</summary>
        private readonly IReadOnlyList<IRule> rules;

        /// <summary>
        /// Construct an instance of <see cref="MolecularFormulaChecker"/>. It must be initialized with the rules to applied.
        /// </summary>
        /// <param name="rules"><see cref="IRule"/>s to be applied</param>
        public MolecularFormulaChecker(IEnumerable<IRule> rules)
        {
            this.rules = rules.ToReadOnlyList();
        }

        /// <summary>
        /// The <see cref="IRule"/> to be applied to validate the <see cref="IMolecularFormula"/>.
        /// </summary>
        public IReadOnlyList<IRule> Rules => rules;

        /// <summary>
        /// Validate if a <see cref="IMolecularFormula"/> is valid. The result more close to 1 means
        /// maximal probability to be valid. Opposite more close to 0 means minimal
        /// probability to be valid. To know the result in each <see cref="IRule"/> use
        /// <see cref="IsValid(IMolecularFormula)"/>.
        /// </summary>
        /// <param name="formula">The <see cref="IMolecularFormula"/> value</param>
        /// <returns>The percent of the validity</returns>
        /// <seealso cref="IsValid(IMolecularFormula)"/>
        public double IsValidSum(IMolecularFormula formula)
        {
            double result = 1.0;

            var formulaWith = IsValid(formula);
            var properties = formulaWith.GetProperties();

            foreach (var rule in rules)
            {
                result *= (double)properties[rule.GetType().ToString()];
            }
            return result;
        }

        /// <summary>
        /// Validate if a <see cref="IMolecularFormula"/> is valid. The results of each <see cref="IRule"/> which
        /// has to be applied is put into <see cref="IMolecularFormula"/> as properties. To extract
        /// the result final as the product of rule's result use
        /// <see cref="IsValidSum(IMolecularFormula)"/>.
        /// </summary>
        /// <param name="formula">The IMolecularFormula value</param>
        /// <returns>The <see cref="IMolecularFormula"/> with the results for each <see cref="IRule"/> into properties</returns>
        /// <seealso cref="IsValidSum(IMolecularFormula)"/>
        public IMolecularFormula IsValid(IMolecularFormula formula)
        {
            Trace.TraceInformation("Generating the validity of the molecular formula");

            if (formula.IsotopesCount == 0)
            {
                Trace.TraceError("Proposed molecular formula has not elements");
                return formula;
            }

            try
            {
                foreach (var rule in rules)
                {
                    double result = rule.Validate(formula);
                    formula.SetProperty(rule.GetType().ToString(), result);
                }
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            return formula;
        }
    }
}
