/* Copyright (C) 2017  Gilleain Torrance <gilleain.torrance@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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
using NCDK.Common.Collections;
using NCDK.Common.Primitives;

namespace NCDK.Groups
{
    // @author maclean
    // @cdk.module group 
    class IntegerListInvariant : IInvariant
    {
        private int[] values;

        public IntegerListInvariant(int[] values)
        {
            this.values = values;
        }

        public int CompareTo(IInvariant o)
        {
            int[] other = ((IntegerListInvariant)o).values;
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] > other[index])
                {
                    return -1;
                }
                else if (values[index] < other[index])
                {
                    return 1;
                }
                else
                {
                    continue;
                }
            }
            return 0;
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(values);
        }

        public override bool Equals(object other)
        {
            return other is IntegerListInvariant
                    && Arrays.AreEqual(values, ((IntegerListInvariant)other).values);
        }

        public override string ToString()
        {
            return Arrays.ToJavaString(values);
        }
    }
}
