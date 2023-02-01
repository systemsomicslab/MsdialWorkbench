/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System.Collections.Generic;
using NCDK.Numerics;

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// A stereo encoder factory for tetrahedral centres. This factory generates
    /// <see cref="StereoEncoder"/>s for centres with specified by 2D and 3D coordinates.
    /// The required preconditions are the central atom must have 3/4 neighboring
    /// atoms, Sp3 hybridization and no query bonds (e.g. wiggly). If there is at
    /// least one up/down bond and all required atoms have coordinates a new 2D
    /// encoder is created. If the there are no stereo bonds (up/down) and all
    /// required atoms have 3D coordinates then a new 3D encoder is created.
    /// </summary>
    // @author John May
    // @cdk.module hash
    public class GeometricTetrahedralEncoderFactory : IStereoEncoderFactory
    {
        /// <summary>
        /// Create a stereo encoder for all potential 2D and 3D tetrahedral
        /// elements.
        /// </summary>
        /// <param name="container">an atom container</param>
        /// <param name="graph">adjacency list representation of the container</param>
        /// <returns>a new encoder for tetrahedral elements</returns>
        public IStereoEncoder Create(IAtomContainer container, int[][] graph)
        {
            // XXX: this code isn't pretty, the current IAtomContainer
            // implementations are  slow for the queries (i.e. looking at connected
            // atoms/bonds) we need to ask to decide if something is a potential
            // tetrahedral centre. We can help out a little with the adjacency list
            // (int[][]) but this doesn't help with the bonds.

            var n = container.Atoms.Count;

            var encoders = new List<IStereoEncoder>();
            var elevation = new Dictionary<IAtom, int>(10);

            for (int i = 0; i < n; i++)
            {
                var degree = graph[i].Length;

                // ignore those which don't have 3 or 4 neighbors
                if (degree < 3 || degree > 4)
                    continue;

                var atom = container.Atoms[i];

                // only create encoders for SP3 hybridized atom. atom typing is
                // currently wrong for some atoms, in sulfoxide for example the atom
                // type sets SP2... but there we don't to fuss about with that here
                if (!Sp3(atom))
                    continue;

                // avoid nitrogen-inversion
                if (7.Equals(atom.AtomicNumber) && degree == 3)
                    continue;

                // TODO: we could be more strict with our selection, InChI uses C,
                // Si, Ge, P, As, B, Sn, N, P, S, Se but has preconditions for
                // certain cases. An atom or ion N, P, As, S or Se is not stereogenic
                // if it has a terminal H or two terminal neighbors -XHm, -XHn (n+m>0)
                // where X is O, S, Se, Te, or N

                // XXX: likely bottle neck
                var bonds = container.GetConnectedBonds(atom);

                // try to create geometric parity
                var geometric = Geometric(elevation, bonds, i, graph[i], container);

                if (geometric != null)
                {
                    // add a new encoder if a geometric parity
                    encoders.Add(new GeometryEncoder(i, new BasicPermutationParity(graph[i]), geometric));
                }
            }

            // no encoders, replace with the empty encoder
            return encoders.Count == 0 ? StereoEncoder.Empty : new MultiStereoEncoder(encoders);
        }

        /// <summary>
        /// Create the geometric part of an encoder
        /// </summary>
        /// <param name="elevationMap">temporary map to store the bond elevations (2D)</param>
        /// <param name="bonds">list of bonds connected to the atom at i</param>
        /// <param name="i">the central atom (index)</param>
        /// <param name="adjacent">adjacent atoms (indices)</param>
        /// <param name="container">container</param>
        /// <returns>geometric parity encoder (or null)</returns>
        private static GeometricParity Geometric(Dictionary<IAtom, int> elevationMap, IEnumerable<IBond> bonds, int i, int[] adjacent, IAtomContainer container)
        {
            var nStereoBonds = GetNumPfStereoBonds(bonds);
            if (nStereoBonds > 0)
                return Geometric2D(elevationMap, bonds, i, adjacent, container);
            else if (nStereoBonds == 0)
                return Geometric3D(i, adjacent, container);
            return null;
        }

        /// <summary>
        /// Create the geometric part of an encoder of 2D configurations
        /// </summary>
        /// <param name="elevationMap">temporary map to store the bond elevations (2D)</param>
        /// <param name="bonds">list of bonds connected to the atom at i</param>
        /// <param name="i">the central atom (index)</param>
        /// <param name="adjacent">adjacent atoms (indices)</param>
        /// <param name="container">container</param>
        /// <returns>geometric parity encoder (or null)</returns>
        private static GeometricParity Geometric2D(Dictionary<IAtom, int> elevationMap, IEnumerable<IBond> bonds, int i, int[] adjacent, IAtomContainer container)
        {
            var atom = container.Atoms[i];

            // create map of the atoms and their elevation from the center,
            MakeElevationMap(atom, bonds, elevationMap);

            var coordinates = new Vector2[4];
            var elevations = new int[4];

            // set the forth ligand to centre as default (overwritten if
            // we have 4 neighbors)
            if (atom.Point2D != null)
                coordinates[3] = atom.Point2D.Value;
            else
                return null;

            for (int j = 0; j < adjacent.Length; j++)
            {
                var neighbor = container.Atoms[adjacent[j]];
                elevations[j] = elevationMap[neighbor];

                if (neighbor.Point2D != null)
                    coordinates[j] = neighbor.Point2D.Value;
                else
                    return null; // skip to next atom

            }

            return new Tetrahedral2DParity(coordinates, elevations);
        }

        /// <summary>
        /// Create the geometric part of an encoder of 3D configurations
        /// </summary>
        /// <param name="i">the central atom (index)</param>
        /// <param name="adjacent">adjacent atoms (indices)</param>
        /// <param name="container">container</param>
        /// <returns>geometric parity encoder (or null)</returns>
        private static GeometricParity Geometric3D(int i, int[] adjacent, IAtomContainer container)
        {
            var atom = container.Atoms[i];
            var coordinates = new Vector3[4];

            // set the forth ligand to centre as default (overwritten if
            // we have 4 neighbors)
            if (atom.Point3D != null)
                coordinates[3] = atom.Point3D.Value;
            else
                return null;

            // for each neighboring atom check if we have 3D coordinates
            for (int j = 0; j < adjacent.Length; j++)
            {
                var neighbor = container.Atoms[adjacent[j]];

                if (neighbor.Point3D != null)
                    coordinates[j] = neighbor.Point3D.Value;
                else
                    return null; // skip to next atom
            }

            // add new 3D stereo encoder
            return new Tetrahedral3DParity(coordinates);
        }

        /// <summary>
        /// check whether the atom is Sp3 hybridization
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>whether the atom is Sp3</returns>
        private static bool Sp3(IAtom atom)
        {
            return Hybridization.SP3.Equals(atom.Hybridization);
        }

        /// <summary>
        /// access the number of stereo bonds in the provided bond list.
        /// </summary>
        /// <param name="bonds">input list</param>
        /// <returns>number of Up/Down bonds in the list, -1 if a query bond was found</returns>
        private static int GetNumPfStereoBonds(IEnumerable<IBond> bonds)
        {
            int count = 0;
            foreach (var bond in bonds)
            {
                var stereo = bond.Stereo;
                switch (stereo)
                {
                    // query bonds... no configuration possible
                    case BondStereo.EOrZ:
                    case BondStereo.UpOrDown:
                    case BondStereo.UpOrDownInverted:
                        return -1;
                    case BondStereo.Up:
                    case BondStereo.Down:
                    case BondStereo.UpInverted:
                    case BondStereo.DownInverted:
                        count++;
                        break;
                    default:
                        break;
                }
            }
            return count;
        }

        /// <summary>
        /// Maps the input bonds to a map of Atom->Elevation where the elevation is
        /// whether the bond is off the plane with respect to the central atom.
        /// </summary>
        /// <param name="atom">central atom</param>
        /// <param name="bonds">bonds connected to the central atom</param>
        /// <param name="map">map to load with elevation values (can be reused)</param>
        private static void MakeElevationMap(IAtom atom, IEnumerable<IBond> bonds, Dictionary<IAtom, int> map)
        {
            map.Clear();
            foreach (var bond in bonds)
            {
                int elevation = 0;
                switch (bond.Stereo)
                {
                    case BondStereo.Up:
                    case BondStereo.DownInverted:
                        elevation = +1;
                        break;
                    case BondStereo.Down:
                    case BondStereo.UpInverted:
                        elevation = -1;
                        break;
                    default:
                        break;
                }

                // change elevation depending on which end of the wedge/hatch
                // the atom is on
                if (bond.Begin.Equals(atom))
                {
                    map[bond.End] = elevation;
                }
                else
                {
                    map[bond.Begin] = -1 * elevation;
                }
            }
        }
    }
}
