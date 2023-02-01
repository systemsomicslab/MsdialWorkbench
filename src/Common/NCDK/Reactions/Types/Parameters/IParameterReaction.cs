/* Copyright (C) 2008  Miquel Rojas Cherto <miguelrojasch@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Reactions.Types.Parameters
{
    /// <summary>
    /// Interface for classes that generate parameters used in reactions.
    /// </summary>
    // @author      miguelrojasch
    // @cdk.module  reaction
    public interface IParameterReaction
    {
        /// <summary>
        /// This parameter needs to take account or not.
        /// </summary>
        /// <value><see langword="true"/>, if the parameter needs to take account</value>
        bool IsSetParameter { get; set; }

        /// <summary>
        /// The value of the parameter.
        /// </summary>
        object Value { get; set; }
    }
}
