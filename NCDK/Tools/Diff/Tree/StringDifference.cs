/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
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

using System;

namespace NCDK.Tools.Diff.Tree
{
    /// <summary>
    /// <see cref="IDifference"/> between two <see cref="string"/>.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public class StringDifference : IDifference
    {
        private readonly string name;
        private readonly string first;
        private readonly string second;

        private StringDifference(string name, string first, string second)
        {
            this.name = name;
            this.first = first;
            this.second = second;
        }

        /// <summary>
        /// Constructs a new <see cref="IDifference"/> object.
        /// </summary>
        /// <param name="name">a name reflecting the nature of the created <see cref="IDifference"/></param>
        /// <param name="first">the first object to compare</param>
        /// <param name="second">the second object to compare</param>
        /// <returns>an <see cref="IDifference"/> reflecting the differences between the first and second object</returns>
        public static IDifference Construct(string name, string first, string second)
        {
            if (first == null && second == null)
            {
                return null; // no difference
            }
            if (first == null || second == null)
            {
                return new StringDifference(name, first, second);
            }
            if (first.Equals(second, StringComparison.Ordinal))
            {
                return null; // no difference
            }
            return new StringDifference(name, first, second);
        }

        /// <summary>
        /// Returns a <see cref="string"/> representation for this <see cref="IDifference"/>.
        /// </summary>
        public override string ToString()
        {
            return $"{name}:{first ?? "NA"}/{second ?? "NA"}";
        }
    }
}
