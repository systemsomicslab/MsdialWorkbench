/* Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Similarity
{
    /// <summary>
    /// A class to evaluate the similarity between two LINGO's as described in <token>cdk-cite-Vidal2005</token>.
    /// </summary>
    /// <remarks>
    /// The similarity calculation is a variant of the Tanimoto coefficient and hence its
    /// value ranges from 0 to 1
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.keyword lingo
    // @cdk.keyword similarity, tanimoto
    // @cdk.module fingerprint
    public static class LingoSimilarity
    {
        /// <summary>
        /// Evaluate the LINGO similarity between two key,value sty;e fingerprints.
        /// The value will range from 0.0 to 1.0.
        /// </summary>
        /// <returns>similarity</returns>
        public static double Calculate(IReadOnlyDictionary<string, int> features1, IReadOnlyDictionary<string, int> features2)
        {
            var keys = features1.Keys.Union(features2.Keys).ToReadOnlyList();

            double sum = 0;
            foreach (var key in keys)
            {
                int c1 = features1[key];
                int c2 = features2[key];

                sum += 1 - Math.Abs(c1 - c2) / (c1 + c2);
            }

            return sum / keys.Count;
        }
    }
}
