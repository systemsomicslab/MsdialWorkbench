/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

namespace NCDK.Validate
{
    /// <summary>
    /// Error found during sematical validation of a IChemObject.
    /// </summary>
    /// <seealso cref="IChemObject"/>
    // @author   Egon Willighagen
    // @cdk.created  2003-03-28
    // @cdk.keyword atom, chemical validation
    public class ValidationTest
    {
        /// <summary>
        /// IChemObject which has the error.
        /// </summary>
        public IChemObject ChemObject { get; private set; }

        /// <summary>
        /// string representation of the found error.
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// Multiline string with details on the error.
        /// </summary>
        public string Details { get; set; }

        public ValidationTest(IChemObject o, string error) :
                this(o, error, "")
        { }

        public ValidationTest(IChemObject o, string error, string details)
        {
            ChemObject = o;
            Error = error;
            Details = details;
        }
    }
}
