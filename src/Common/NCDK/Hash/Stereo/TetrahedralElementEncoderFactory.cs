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
    /// generation of stereo hash codes for molecules with predefined
    /// <see cref="ITetrahedralChirality"/> stereo elements.
    /// </summary>
    // @author John May
    // @cdk.module hash
    public sealed class TetrahedralElementEncoderFactory : IStereoEncoderFactory
    {
        public IStereoEncoder Create(IAtomContainer container, int[][] graph)
        {
            // index atoms for quick lookup - wish we didn't have to do this
            // but the it's better than calling getAtomNumber every time - we use
            // a lazy creation so it's only created if there was a need for it
            Dictionary<IAtom, int> atomToIndex = null;

            var encoders = new List<IStereoEncoder>();

            // for each tetrahedral element - create a new encoder
            foreach (var se in container.StereoElements)
            {
                if (se is ITetrahedralChirality)
                {
                    encoders.Add(Encoder((ITetrahedralChirality)se, atomToIndex = IndexMap(atomToIndex, container)));
                }
            }

            return encoders.Count == 0 ? StereoEncoder.Empty : new MultiStereoEncoder(encoders);
        }

        /// <summary>
        /// Create an encoder for the <see cref="ITetrahedralChirality"/> element.
        /// </summary>
        /// <param name="tc">stereo element from an atom container</param>
        /// <param name="atomToIndex">map of atoms to indices</param>
        /// <returns>a new geometry encoder</returns>
        private static GeometryEncoder Encoder(ITetrahedralChirality tc, IDictionary<IAtom, int> atomToIndex)
        {
            var ligands = tc.Ligands;

            var centre = atomToIndex[tc.ChiralAtom];
            var indices = new int[4];

            int offset = -1;
            {
                int i = 0;
                foreach (var ligand in ligands)
                {
                    indices[i] = atomToIndex[ligands[i]];
                    if (indices[i] == centre)
                        offset = i;
                    i++;
                }
            }

            // convert clockwise/anticlockwise to -1/+1
            var parity = tc.Stereo == TetrahedralStereo.Clockwise ? -1 : 1;

            // now if any atom is the centre (indicating an implicit
            // hydrogen) we need to adjust the indicies and the parity
            if (offset >= 0)
            {
                // remove the 'implicit' central from the first 3 vertices
                for (int i = offset; i < indices.Length - 1; i++)
                {
                    indices[i] = indices[i + 1];
                }

                // we now take how many vertices we moved which is
                // 3 (last index) minus the index where we started. if the
                // value is odd we invert the parity (odd number of
                // inversions)
                if ((3 - offset) % 2 == 1)
                    parity *= -1;

                // trim the array to size we don't include the last (implicit)
                // vertex when checking the invariants
                var sindices = new int[indices.Length - 1];
                Array.Copy(indices, sindices, indices.Length - 1);
                indices = sindices;
            }

            return new GeometryEncoder(centre, new BasicPermutationParity(indices), GeometricParity.ValueOf(parity));
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
