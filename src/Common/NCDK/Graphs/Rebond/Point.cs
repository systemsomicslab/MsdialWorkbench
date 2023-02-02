/* Copyright (C) 2003-2007  Miguel Howard <miguel@jmol.org>
 *
 * Contact: cdk-devel@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Graphs.Rebond
{
    // @author      Miguel Howard
    // @cdk.created 2003-05
    // @cdk.module  standard
    internal class Point : ITuple
    {
        double X { get; set; }
        double Y { get; set; }
        double Z { get; set; }

        public double Distance2 { get; set; }

        public Point(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public double GetDimValue(int dim)
        {
            if (dim == 0) return X;
            if (dim == 1) return Y;
            return Z;
        }

        public override string ToString()
        {
            return "<" + X + "," + Y + "," + Z + ">";
        }
    }
}
