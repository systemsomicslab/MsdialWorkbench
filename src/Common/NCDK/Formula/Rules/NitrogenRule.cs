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
using System.Linq;

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// This class validate if the rule of nitrogen is kept.
    /// <para>If a compound has an odd number of nitrogen atoms,
    /// then the molecular ion (the [M]<sup>+</sup>) will have an odd mass and the value for m/e will be odd.</para>
    /// <para>If a compound has no nitrogen atom or an even number of nitrogen atoms, then the m/e value of [M]<sup>+</sup> will be even.</para>
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
    ///   <term>charge</term>
    ///   <term>0.0</term>
    ///   <term>The Nitrogen rule of MolecularFormula</term>
    /// </item>
    /// </list>
    /// </remarks>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2008-06-11
    public class NitrogenRule : IRule
    {
        /// <summary>
        /// Constructor for the NitrogenRule object.
        /// </summary>
        public NitrogenRule() { }
        
        /// <summary>
        /// The parameters attribute of the NitrogenRule object.
        /// </summary>
        public IReadOnlyList<object> Parameters
        {
            get { return null; }
            set { if (value != null) throw new CDKException("NitrogenRule doesn't expect parameters"); }
        }

        /// <summary>
        /// Validate the nitrogen rule of this IMolecularFormula.
        /// </summary>
        /// <param name="formula">Parameter is the IMolecularFormula</param>
        /// <returns>A double value meaning 1.0 True, 0.0 False</returns>
        public double Validate(IMolecularFormula formula)
        {
            Trace.TraceInformation("Start validation of ", formula);

            double mass = MolecularFormulaManipulator.GetTotalMassNumber(formula);
            if (mass == 0) return 0.0;

            int numberN = MolecularFormulaManipulator.GetElementCount(formula, ChemicalElement.N);
            numberN += GetOthers(formula);

            if (formula.Charge == null || formula.Charge == 0 || !IsOdd(Math.Abs(formula.Charge.Value)))
            {
                if (IsOdd(mass) && IsOdd(numberN))
                {
                    return 1.0;
                }
                else if (!IsOdd(mass) && (numberN == 0 || !IsOdd(numberN)))
                {
                    return 1.0;
                }
                else
                    return 0.0;
            }
            else
            {
                if (!IsOdd(mass) && IsOdd(numberN))
                {
                    return 1.0;
                }
                else if (IsOdd(mass) && (numberN == 0 || !IsOdd(numberN)))
                {
                    return 1.0;
                }
                else
                    return 0.0;
            }
        }

        /// <summary>
        /// Get the number of other elements which affect to the calculation of the nominal mass.
        /// For example Fe, Co, Hg, Pt, As.
        /// </summary>
        /// <param name="formula">The <see cref="IMolecularFormula"/> to analyze</param>
        /// <returns>Number of elements</returns>
        private static int GetOthers(IMolecularFormula formula)
        {
            return NominalMassAffectables.Sum(n => MolecularFormulaManipulator.GetElementCount(formula, n));
        }

        private static readonly ChemicalElement[] NominalMassAffectables = new ChemicalElement[]
        {
            ChemicalElement.Co, 
            ChemicalElement.Hg,
            ChemicalElement.Pt,
            ChemicalElement.As,
        };

        /// <summary>
        /// Determine if a integer is odd.
        /// </summary>
        /// <param name="value">The value to analyze</param>
        /// <returns>True, if the integer is odd</returns>
        private static bool IsOdd(double value)
        {
            if (value % 2 == 0)
                return false;
            else
                return true;
        }
    }
}
