/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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

using System.Collections.Generic;
using System.IO;

namespace NCDK.Config
{
    /// <summary>
    /// Interface that allows reading atom type configuration data from some source.
    /// </summary>
    // @cdk.module core
    public interface IAtomTypeConfigurator
    {
        /// <summary>
        /// the file containing the configuration data.
        /// </summary>
        /// <param name="value">from which the atom type definitions are to be read</param>
        void SetStream(Stream value);

        /// <summary>
        /// Reads a set of configured <see cref="IAtomType"/>'s into a <see cref="IEnumerable{IAtomType}"/>.
        /// </summary>
        /// <returns>A List containing the AtomTypes extracted from the InputStream</returns>
        /// <exception cref="IOException">when something went wrong with reading the data</exception>
        IEnumerable<IAtomType> ReadAtomTypes();
    }
}
