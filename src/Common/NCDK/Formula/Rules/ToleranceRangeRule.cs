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
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the mass from an IMolecularFormula is
    /// between the tolerance range give a experimental mass. As default
    /// the mass to range is 0.0.
    /// </summary>
    /// <remarks>
    /// Table 1: Parameters set by this rule.
    /// <list type="table">
    /// <item>
    ///   <term>Name</term>
    ///   <term>Default</term>
    ///   <term>Description</term>
    /// </item>
    /// <item>
    ///   <term>mass</term>
    ///   <term>0.0</term>
    ///   <term>The Mass which the MolecularFormula has to be compared</term>
    /// </item>
    /// <item>
    ///   <term>tolerance</term>
    ///   <term>0.05</term>
    ///   <term>The range tolerance</term>
    /// </item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public class ToleranceRangeRule : IRule
    {
        private double mass = 0.0;
        private double tolerance = 0.05;

        /// <summary>
        /// Constructor for the ToleranceRangeRule object.
        /// </summary>
        public ToleranceRangeRule() { }
        
        /// <summary>
        /// The parameters attribute of the ToleranceRangeRule object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get
            {
                // return the parameters as used for the rule validation
                object[] parameters = new object[2];
                parameters[0] = mass;
                parameters[1] = tolerance;
                return parameters;
            }
            set
            {
                if (value.Count > 2) throw new CDKException("ToleranceRangeRule expects only two parameter");
                if (!(value[0] is double)) throw new CDKException("The parameter 0 must be of type Double");
                if (!(value[1] is double)) throw new CDKException("The parameter 1 must be of type Double");

                mass = (double)value[0];
                tolerance = (double)value[1];
            }
        }

        /// <summary>
        /// Validate the Tolerance Range of this IMolecularFormula.
        /// </summary>
        /// <param name="formula">Parameter is the IMolecularFormula</param>
        /// <returns>A double value meaning 1.0 True, 0.0 False</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);

            double totalExactMass = MolecularFormulaManipulator.GetTotalExactMass(formula);

            if (Math.Abs(totalExactMass - mass) > tolerance)
                return 0.0;
            else
                return 1.0;
        }
    }
}
