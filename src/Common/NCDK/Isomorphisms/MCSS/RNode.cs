/* Copyright (C) 2002-2007  Stephane Werner <mail@ixelis.net>
 *
 * This code has been kindly provided by Stephane Werner
 * and Thierry Hanser from IXELIS mail@ixelis.net.
 *
 * IXELIS sarl - Semantic Information Systems
 *               17 rue des C?dres 67200 Strasbourg, France
 *               Tel/Fax : +33(0)3 88 27 81 39 Email: mail@ixelis.net
 *
 * CDK Contact: cdk-devel@lists.sf.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using System.Collections;

namespace NCDK.Isomorphisms.MCSS
{
    /// <summary>
    ///  Node of the resolution graph (RGraph) An RNode represents an association
    ///  between two edges of the source graphs G1 and G2 that are compared. Two
    ///  edges may be associated if they have at least one common feature. The
    ///  association is defined outside this class. The node keeps tracks of the ID
    ///  of the mapped edges (in an RMap), of its neighbours in the RGraph it belongs
    ///  to and of the set of incompatible nodes (nodes that may not be along with
    ///  this node in the same solution)
    /// </summary>
    // @author      Stephane Werner from IXELIS mail@ixelis.net
    // @cdk.created 2002-07-17
    // @cdk.module  standard
    public class RNode
    {
        /// <summary>
        /// The rMap attribute of the RNode object.
        /// </summary>
        public RMap RMap { get; set; } = null;

        /// <summary>
        /// The extension attribute of the RNode object.
        /// </summary>
        public BitArray Extension { get; private set; } = null;

        /// <summary>
        /// The extension attribute of the RNode object.
        /// </summary>
        public BitArray Forbidden { get; private set; } = null;

        /// <summary>
        ///  Constructor for the RNode object.
        /// </summary>
        /// <param name="id1">number of the bond in the graph 1</param>
        /// <param name="id2">number of the bond in the graph 2</param>
        public RNode(int id1, int id2)
        {
            RMap = new RMap(id1, id2);
            Extension = new BitArray(0);
            Forbidden = new BitArray(0);
        }

        /// <summary>
        ///  Returns a string representation of the RNode.
        /// </summary>
        /// <returns>the string representation of the RNode</returns>
        public override string ToString()
        {
            return ("id1 : " + RMap.Id1 + ", id2 : " + RMap.Id2 + "\n" 
                + "extension : " + BitArrays.ToString(Extension) + "\n" 
                + "forbiden : " + BitArrays.ToString(Forbidden));
        }

        internal void EnsureNodeCount(int count)
        {
            Extension.Length = count;
            Forbidden.Length = count;
        }
    }
}
