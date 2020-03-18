/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
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

namespace NCDK
{
    /// <summary>
    /// Interface that is used to describe the specification of a certain
    /// implementation of an algorithm.
    /// </summary>
    // @cdk.module standard
    public interface IImplementationSpecification
    {
        /// <summary>
        /// Pointer to a dictionary or ontology describing a unique algorithm.
        /// </summary>
        /// <returns>the URN pointing to a (virtual) dictionary or ontology.</returns>
        string SpecificationReference { get; }

        /// <summary>
        /// Human-readable name for the implementation for the algorithm
        /// specified by the reference.
        ///
        /// <returns>the name of this implementation</returns>
        /// </summary>
        string ImplementationTitle { get; }

        /// <summary>
        /// Identifier for this implementation which must include
        /// version information. The format is free.
        /// </summary>
        /// <returns>a free format identifier for this implementation</returns>
        string ImplementationIdentifier { get; }

        /// <summary>
        /// Human-readable name for the vendor that holds copyright for this implementation.
        /// </summary>
        /// <returns>the copyright holder of the implementation</returns>
        string ImplementationVendor { get; }
    }
}
