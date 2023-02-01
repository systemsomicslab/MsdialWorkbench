/*
 * Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Numerics;
using System;
using System.Collections.Generic;

namespace NCDK.Geometries.Surface
{
    /// <summary>
    /// Creates a list of atoms neighboring each atom in the molecule.
    /// </summary>
    /// <remarks>
    /// The routine is a simplified version of the neighbor list described
    /// in <token>cdk-cite-EIS95</token> and is based on the implementation by Peter McCluskey.
    /// Due to the fact that it divides the cube into a fixed number of sub cubes,
    /// some accuracy may be lost.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.created 2005-05-09
    // @cdk.module  qsarmolecular
    public class NeighborList
    {
        readonly Dictionary<Key, List<int>> boxes;
        readonly double boxSize;
        readonly IAtom[] atoms;

        /// <summary>
        /// Custom key class for looking up items in the map.
        /// </summary>
        private sealed class Key
        {
            readonly NeighborList parent;

            internal readonly int x, y, z;

            public Key(NeighborList parent, IAtom atom)
                : this(parent,
                      (int)(Math.Floor(atom.Point3D.Value.X / parent.boxSize)),
                      (int)(Math.Floor(atom.Point3D.Value.Y / parent.boxSize)),
                      (int)(Math.Floor(atom.Point3D.Value.Z / parent.boxSize)))
            {
            }

            public Key(NeighborList parent, int x, int y, int z)
            {
                this.parent = parent;

                this.x = x;
                this.y = y;
                this.z = z;
            }

            public override bool Equals(object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;
                Key key = (Key)o;
                return x == key.x &&
                       y == key.y &&
                       z == key.z;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (((x * 31) + y) * 31) + z;
                }
            }
        }

        public NeighborList(IAtom[] atoms, double radius)
        {
            this.atoms = atoms;
            this.boxes = new Dictionary<Key, List<int>>();
            this.boxSize = 2 * radius;
            for (int i = 0; i < atoms.Length; i++)
            {
                Key key = new Key(this, atoms[i]);
                if (!this.boxes.TryGetValue(key, out List<int> arl))
                {
                    this.boxes[key] = (arl = new List<int>());
                }
                arl.Add(i);
            }
        }

        public int GetNumberOfNeighbors(int i)
        {
            return GetNeighbors(i).Length;
        }

        /// <summary>
        /// Get the neighbors that are with the given radius of atom i.
        /// </summary>
        /// <param name="i">atom index</param>
        /// <returns>atom indexs within that radius</returns>
        public int[] GetNeighbors(int i)
        {
            var result = new List<int>();
            var maxDist2 = this.boxSize * this.boxSize;
            var atom = this.atoms[i];
            var key = new Key(this, atom);
            int[] bval = { -1, 0, 1 };
            foreach (int x in bval)
            {
                foreach (int y in bval)
                {
                    foreach (int z in bval)
                    {
                        var probe = new Key(this, key.x + x, key.y + y, key.z + z);
                        if (boxes.TryGetValue(probe, out List<int> nbrs))
                        {
                            foreach (var nbr in nbrs)
                            {
                                if (nbr != i)
                                {
                                    var anbr = atoms[nbr];
                                    var p1 = anbr.Point3D.Value;
                                    var p2 = atom.Point3D.Value;
                                    if (Vector3.DistanceSquared(p1, p2) < maxDist2)
                                        result.Add(nbr);
                                }
                            }
                        }
                    }
                }
            }

            // convert to primitive array
            return result.ToArray();
        }
    }
}
