/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System;
using System.Collections.Generic;

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// Defines a stereo encoder factory for the hash code. The factory allows the
    /// generation of stereo hash codes for molecules with predefined <see cref="IDoubleBondStereochemistry"/> stereo elements. 
    /// </summary>
    // @author John May
    // @cdk.module hash
    public sealed class DoubleBondElementEncoderFactory : IStereoEncoderFactory
    {
        public IStereoEncoder Create(IAtomContainer container, int[][] graph)
        {
            // index atoms for quick lookup - wish we didn't have to do this
            // but the it's better than calling getAtomNumber every time - we use
            // a lazy creation so it's only created if there was a need for it
            Dictionary<IAtom, int> atomToIndex = null;

            var encoders = new List<IStereoEncoder>();

            // for each double-bond element - create a new encoder
            foreach (var se in container.StereoElements)
            {
                if (se is IDoubleBondStereochemistry)
                {
                    encoders.Add(GetEncoder((IDoubleBondStereochemistry)se, atomToIndex = IndexMap(atomToIndex, container), graph));
                }
            }

            return encoders.Count == 0 ? StereoEncoder.Empty : new MultiStereoEncoder(encoders);
        }

        /// <summary>
        /// Create an encoder for the <see cref="IDoubleBondStereochemistry"/> element.
        /// </summary>
        /// <param name="dbs">stereo element from an atom container</param>
        /// <param name="atomToIndex">map of atoms to indices</param>
        /// <param name="graph">adjacency list of connected vertices</param>
        /// <returns>a new geometry encoder</returns>
        private static GeometryEncoder GetEncoder(IDoubleBondStereochemistry dbs, IDictionary<IAtom, int> atomToIndex, int[][] graph)
        {
            var db = dbs.StereoBond;
            var u = atomToIndex[db.Begin];
            var v = atomToIndex[db.End];

            // we now need to expand our view of the environment - the vertex arrays
            // 'us' and <paramref name="vs"/> hold the neighbors of each end point of the double bond
            // (<paramref name="u"/> or 'v'). The first neighbor is always the one stored in the
            // stereo element. The second is the other non-double bonded vertex
            // which we must find from the neighbors list (findOther). If there is
            // no additional atom attached (or perhaps it is an implicit Hydrogen)
            // we use either double bond end point.
            var bs = dbs.Bonds;
            var us = new int[2];
            var vs = new int[2];

            us[0] = atomToIndex[bs[0].GetOther(db.Begin)];
            us[1] = graph[u].Length == 2 ? u : FindOther(graph[u], v, us[0]);

            vs[0] = atomToIndex[bs[1].GetOther(db.End)];
            vs[1] = graph[v].Length == 2 ? v : FindOther(graph[v], u, vs[0]);

            var parity = dbs.Stereo == DoubleBondConformation.Opposite ? +1 : -1;

            var geomParity = GeometricParity.ValueOf(parity);

            // the permutation parity is combined - but note we only use this if we
            // haven't used <paramref name="u"/> or 'v' as place holders (i.e. implicit hydrogens)
            // otherwise there is only '1' and the parity is just '1' (identity)
            PermutationParity permParity = new CombinedPermutationParity(us[1] == 
                u ? BasicPermutationParity.Identity
                  : new BasicPermutationParity(us), vs[1] == v 
                  ? BasicPermutationParity.Identity
                  : new BasicPermutationParity(vs));
            return new GeometryEncoder(new int[] { u, v }, permParity, geomParity);
        }

        /// <summary>
        /// Finds a vertex in <paramref name="vs"/> which is not <paramref name="u"/> or <paramref name="x"/>.
        /// </summary>
        /// <param name="vs">fixed size array of 3 elements</param>
        /// <param name="u">a vertex in <paramref name="vs"/></param>
        /// <param name="x">another vertex in <paramref name="vs"/></param>
        /// <returns>the other vertex</returns>
        private static int FindOther(int[] vs, int u, int x)
        {
            foreach (var v in vs)
            {
                if (v != u && v != x)
                    return v;
            }
            throw new ArgumentException("vs[] did not contain another vertex");
        }

        /// <summary>
        /// Lazy creation of an atom index map.
        /// </summary>
        /// <param name="map">existing map (possibly null)</param>
        /// <param name="container">the container we want the map for</param>
        /// <returns>a usable atom to index map for the given container</returns>
        private static Dictionary<IAtom, int> IndexMap(Dictionary<IAtom, int> map, IAtomContainer container)
        {
            if (map != null)
                return map;
            map = new Dictionary<IAtom, int>();
            foreach (var a in container.Atoms)
            {
                map[a] = map.Count;
            }
            return map;
        }
    }
}
