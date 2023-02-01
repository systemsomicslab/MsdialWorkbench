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

namespace NCDK.Formula.Rules
{
    /// <summary>
    /// Interface which groups all method that validate a <see cref="IMolecularFormula"/>.
    /// </summary>
    // @cdk.module  formula
    // @author      miguelrojasch
    // @cdk.created 2007-11-20
    public interface IRule
    {
        IReadOnlyList<object> Parameters { get; set; }

        /// <summary>
        /// Analyze the validity for the given IMolecularFormula.
        /// </summary>
        /// <param name="formula">An <see cref="IMolecularFormula"/> for which this rule should be analyzed</param>
        /// <returns>A double value between 0 and 1. 1 meaning 100% valid and 0 not valid</returns>
        /// <exception cref="CDKException">if an error occurs during the validation. See documentation for individual rules</exception>
        double Validate(IMolecularFormula formula);
    }
}
