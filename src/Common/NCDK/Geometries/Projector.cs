/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Numerics;
using System.Collections.Generic;

namespace NCDK.Geometries
{
    /// <summary>
    /// Tool to make projections from 3D to 2D.
    /// </summary>
    // @cdk.keyword projection in 2D
    public static class Projector
    {
        public static void Project2D(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
            {
                if (atom.Point3D != null)
                {
                    atom.Point2D = new Vector2(atom.Point3D.Value.X, atom.Point3D.Value.Y);
                }
                else
                {
                    // should throw an exception
                }
            }
        }

        public static void Project2D(IAtomContainer container, IDictionary<IAtom, Vector2> renderingCoordinates)
        {
            foreach (var atom in container.Atoms)
            {
                if (atom.Point3D != null)
                {
                    atom.Point2D = new Vector2(atom.Point3D.Value.X, atom.Point3D.Value.Y);
                    renderingCoordinates[atom] = new Vector2(atom.Point3D.Value.X, atom.Point3D.Value.Y);
                }
                else
                {
                    // should throw an exception
                }
            }
        }
    }
}
