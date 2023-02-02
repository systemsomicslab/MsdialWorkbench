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

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the charge in the <see cref="IMolecularFormula"/> correspond with
    /// a specific value. As default it is defined as neutral == 0.0.
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
    ///   <term>charge</term>
    ///   <term>0.0</term>
    ///   <term>The Charge of <see cref="MolecularFormula"/></term>
    /// </item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public class ChargeRule : IRule
    {
        private double charge = 0.0;

        /// <summary>
        ///  Constructor for the ChargeRule object.
        /// </summary>
        public ChargeRule() { }

        /// <summary>
        /// The parameters attribute of the <see cref="ChargeRule"/> object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get
            {
                // return the parameters as used for the rule validation
                object[] params_ = new object[1];
                params_[0] = charge;
                return params_;
            }
            set
            {
                if (value.Count != 1)
                    throw new CDKException("ChargeRule expects only one parameter");

                if (!(value[0] is double))
                    throw new CDKException("The parameter must be of type double");

                charge = (double)value[0];
            }
        }

        /// <summary>
        /// Validate the charge of this IMolecularFormula.
        /// </summary>
        /// <param name="formula">Parameter is the IMolecularFormula</param>
        /// <returns>A double value meaning 1.0 True, 0.0 False</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);

            if (formula.Charge == null)
            {
                return 0.0;
            }
            else if (formula.Charge == charge)
            {
                return 1.0;
            }
            else
            {
                return 0.0;
            }
        }
    }
}
