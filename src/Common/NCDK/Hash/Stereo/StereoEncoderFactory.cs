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

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// Describes a factory for stereo elements. The factory create encoders for
    /// specific stereo elements.
    /// </summary>
    // @author John May
    // @cdk.module hash
    public interface IStereoEncoderFactory
    {
        /// <summary>
        /// Create a stereo-encoder for possible stereo-chemical configurations.
        /// </summary>
        /// <param name="container">the container</param>
        /// <param name="graph">adjacency list representation of the container</param>
        /// <returns>a new stereo encoder</returns>
        IStereoEncoder Create(IAtomContainer container, int[][] graph);
    }

    public static class StereoEncoderFactory
    {
        /// <summary>
        /// Empty factory for when stereo encoding is not required
        /// </summary>
        public readonly static IStereoEncoderFactory Empty = new EmptyStereoEncoderFactory();

        class EmptyStereoEncoderFactory : IStereoEncoderFactory
        {
            public IStereoEncoder Create(IAtomContainer container, int[][] graph)
            {
                return StereoEncoder.Empty;
            }
        }
    }
}
