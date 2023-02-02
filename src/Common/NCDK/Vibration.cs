/* Copyright (C) 2002-2007  Bradley A. Smith <yeldar@home.com>
 *
 * Contact: jmol-developers@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Numerics;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// A molecular vibration composed of a set of atom vectors.
    /// The atom vectors represent forces acting on the atoms. 
    /// They are specified by <see cref="Vector3"/> containing the components of the vector.
    /// </summary>
    // @author Bradley A. Smith <yeldar@home.com>
    public class Vibration 
        : List<Vector3>
    {
        /// <summary>
        /// Label identifying this vibration. For example, the frequency in reciprocal centimetres could be used.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Create a vibration identified by the label.
        /// </summary>
        /// <param name="label">identification for this vibration</param>
        public Vibration(string label)
        {
            this.Label = label;
        }
    }
}
