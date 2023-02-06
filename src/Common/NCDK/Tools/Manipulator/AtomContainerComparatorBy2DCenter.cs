/*
 *  Copyright (C) 2009  Mark Rijnbeek <markrynbeek@gmail.com>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Mathematics;
using NCDK.Geometries;
using NCDK.Numerics;
using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Compares two IAtomContainers based on their 2D position.
    /// Implemented specifically to be used in JChemPaint.
    /// </summary>
    // @author Mark Rijnbeek
    // @cdk.created  2009-10-14
    // @cdk.module   standard
    public class AtomContainerComparatorBy2DCenter<T> : IComparer<T> where T : IAtomContainer
    {
        /// <summary>
        /// Compare two AtomContainers based on their 2D position.
        /// </summary>
        public int Compare(T a, T b)
        {
            var p1 = Center(a);
            var p2 = Center(b);

            if (p1.X > p2.X) return +1;
            if (p1.X < p2.X) return -1;
            if (p1.Y > p2.Y) return +1;
            if (p1.Y < p2.Y) return -1;

            return 0;
        }

        /// <summary>
        /// maximum point to use when an null container is provided (sorts null to end)
        /// </summary>
        private static readonly Vector2 Maximum = Vectors.Vector2MaxValue;

        private static Vector2 Center(IAtomContainer container)
        {
            return container != null ? GeometryUtil.Get2DCenter(container) : Maximum;
        }
    }
}
