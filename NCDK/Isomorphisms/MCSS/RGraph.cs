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
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms.MCSS
{
    /// <summary>
    /// This class implements the Resolution Graph (RGraph).
    /// The RGraph is a graph based representation of the search problem.
    /// An RGraph is constructed from the two compared graphs (G1 and G2).
    /// Each vertex (node) in the RGraph represents a possible association
    /// from an edge in G1 with an edge in G2. Thus two compatible bonds
    /// in two molecular graphs are represented by a vertex in the RGraph.
    /// Each edge in the RGraph corresponds to a common adjacency relationship
    /// between the 2 couple of compatible edges associated to the 2 RGraph nodes
    /// forming this edge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Example:
    /// <pre>
    ///    G1 : C-C=O  and G2 : C-C-C=0
    ///         1 2 3           1 2 3 4
    /// </pre>
    /// </para>
    /// <para>
    /// The resulting RGraph(G1,G2) will contain 3 nodes:
    /// <list type="bullet">
    ///    <item>Node A : association between bond C-C :  1-2 in G1 and 1-2 in G2</item>
    ///    <item>Node B : association between bond C-C :  1-2 in G1 and 2-3 in G2</item>
    ///    <item>Node C : association between bond C=0 :  2-3 in G1 and 3-4 in G2</item>
    /// </list> 
    /// </para>
    /// <para>
    /// The RGraph will also contain one edge representing the
    /// adjacency between node B and C that is : bonds 1-2 and 2-3 in G1
    /// and bonds 2-3 and 3-4 in G2.
    /// </para>
    /// <para>
    /// Once the RGraph has been built from the two compared graphs
    /// it becomes a very interesting tool to perform all kinds of
    /// structural search (isomorphism, substructure search, maximal common
    /// substructure,....).
    /// </para>
    /// <para>
    /// The search may be constrained by mandatory elements (e.g. bonds that
    /// have to be present in the mapped common substructures).
    /// </para>
    /// <para>
    /// Performing a query on an RGraph requires simply to set the constrains
    /// (if any) and to invoke the parsing method (Parse())
    /// </para>
    /// <para>
    ///  The RGraph has been designed to be a generic tool. It may be constructed
    ///  from any kind of source graphs, thus it is not restricted to a chemical
    ///  context.
    /// </para>
    /// <para>
    ///  The RGraph model is independent from the CDK model and the link between
    ///  both model is performed by the RTools class. In this way the RGraph
    ///  class may be reused in other graph context (conceptual graphs,....)
    /// </para>
    /// <note type="important">
    /// This implementation of the algorithm has not been
    ///                      optimized for speed at this stage. It has been
    ///                      written with the goal to clearly retrace the
    ///                      principle of the underlined search method. There is
    ///                      room for optimization in many ways including the
    ///                      the algorithm itself.
    /// </note>
    /// <para>
    /// This algorithm derives from the algorithm described in
    ///  <token>cdk-cite-HAN90</token> and modified in the thesis of T. Hanser <token>cdk-cite-HAN93</token>.
    /// </para>
    /// </remarks>
    // @author      Stephane Werner from IXELIS mail@ixelis.net
    // @cdk.created 2002-07-17
    // @cdk.module  standard
    public class RGraph
    {
        // an RGraph is a list of RGraph nodes
        // each node keeping track of its
        // neighbors.
        private List<RNode> graph = null;

        // maximal number of iterations before
        // search break
        int maxIteration = -1;

        // dimensions of the compared graphs

        /// <summary>
        /// The size of the first of the two compared graphs
        /// </summary>
        public int FirstGraphSize { get; set; } = 0;

        /// <summary>
        /// The size of the second of the two compared graphs
        /// </summary>
        public int SecondGraphSize { get; set; } = 0;

        // constrains
        BitArray c1 = null;
        BitArray c2 = null;

        // current solution list
        List<BitArray> solutionList = null;

        // flag to define if we want to get all possible 'mappings'
        bool findAllMap = false;

        // flag to define if we want to get all possible 'structures'
        bool findAllStructure = true;

        // working variables
        bool stop = false;
        int nbIteration = 0;
        BitArray graphBitSet = null;

        /// <summary>
        /// The time in milliseconds until the substructure search will be breaked. 
        /// Time in milliseconds. -1 to ignore the timeout.
        /// </summary>
        public long Timeout { get; set; } = -1;

        /// <summary>
        /// The start time in milliseconds.
        /// </summary>
        public long Start { get; set; }

        /// <summary>
        /// Constructor for the RGraph object and creates an empty RGraph.
        /// </summary>
        public RGraph()
        {
            graph = new List<RNode>();
            solutionList = new List<BitArray>();
            graphBitSet = new BitArray(0);  // realloc in AddNode(RNode newNode)
        }

        /// <summary>
        ///  Reinitialisation of the TGraph.
        /// </summary>
        public void Clear()
        {
            graph.Clear();
            graphBitSet.SetAll(false);
        }

        /// <summary>
        ///  Returns the graph object of this RGraph.
        /// </summary>
        /// <returns>The graph object, a list</returns>
        public IReadOnlyList<RNode> Graph => this.graph;

        /// <summary>
        ///  Adds a new node to the RGraph.
        /// </summary>
        /// <param name="newNode">The node to add to the graph</param>
        public void AddNode(RNode newNode)
        {
            graph.Add(newNode);
            BitArrays.EnsureCapacity(graphBitSet, graph.Count);
            graphBitSet.Set(graph.Count - 1, true);
        }

        /// <summary>
        /// Parsing of the RGraph. This is the main method
        /// to perform a query. Given the constrains c1 and c2
        /// defining mandatory elements in G1 and G2 and given
        /// the search options, this method builds an initial set
        /// of starting nodes (B) and parses recursively the
        /// RGraph to find a list of solution according to
        /// these parameters.
        /// </summary>
        /// <param name="c1">constrain on the graph G1</param>
        /// <param name="c2">constrain on the graph G2</param>
        /// <param name="findAllStructure">true if we want all results to be generated</param>
        /// <param name="findAllMap">true is we want all possible 'mappings'</param>
        public void Parse(BitArray c1, BitArray c2, bool findAllStructure, bool findAllMap)
        {
            // initialize the list of solution
            solutionList.Clear();

            // builds the set of starting nodes
            // according to the constrains
            BitArray b = BuildB(c1, c2);

            // setup options
            SetAllStructure(findAllStructure);
            SetAllMap(findAllMap);

            // parse recursively the RGraph
            ParseRec(new BitArray(b.Count), b, new BitArray(b.Count));
        }

        /// <summary>
        ///  Parsing of the RGraph. This is the recursive method
        ///  to perform a query. The method will recursively
        ///  parse the RGraph thru connected nodes and visiting the
        ///  RGraph using allowed adjacency relationship.
        /// </summary>
        /// <param name="traversed">node already parsed</param>
        /// <param name="extension">possible extension node (allowed neighbors)</param>
        /// <param name="forbidden">node forbidden (set of node incompatible with the current solution)</param>
        private void ParseRec(BitArray traversed, BitArray extension, BitArray forbidden)
        {
            BitArray newTraversed = null;
            BitArray newExtension = null;
            BitArray newForbidden = null;
            BitArray potentialNode = null;

            // Test whether the timeout is reached. Stop searching.
            if (this.Timeout > -1 &&  (DateTime.Now.Ticks / 10000 - this.Start) > this.Timeout)
            {
                stop = true;
            }

            // if there is no more extension possible we
            // have reached a potential new solution
            if (BitArrays.IsEmpty(extension))
            {
                Solution(traversed);
            }
            // carry on with each possible extension
            else
            {
                // calculates the set of nodes that may still
                // be reached at this stage (not forbidden)
                potentialNode = ((BitArray)graphBitSet.Clone());
                BitArrays.AndNot(potentialNode, forbidden);
                potentialNode.Or(traversed);

                // checks if we must continue the search
                // according to the potential node set
                if (MustContinue(potentialNode))
                {
                    // carry on research and update iteration count
                    nbIteration++;

                    // for each node in the set of possible extension (neighbors of
                    // the current partial solution, include the node to the solution
                    // and parse recursively the RGraph with the new context.
                    for (int x = BitArrays.NextSetBit(extension, 0); x >= 0 && !stop; x = BitArrays.NextSetBit(extension, x + 1))
                    {
                        // evaluates the new set of forbidden nodes
                        // by including the nodes not compatible with the
                        // newly accepted node.
                        newForbidden = (BitArray)forbidden.Clone();
                        newForbidden.Or(((RNode)graph[x]).Forbidden);

                        // if it is the first time we are here then
                        // traversed is empty and we initialize the set of
                        // possible extensions to the extension of the first
                        // accepted node in the solution.
                        if (BitArrays.IsEmpty(traversed))
                        {
                            newExtension = (BitArray)(((RNode)graph[x]).Extension.Clone());
                        }
                        // else we simply update the set of solution by
                        // including the neighbors of the newly accepted node
                        else
                        {
                            newExtension = (BitArray)extension.Clone();
                            newExtension.Or(((RNode)graph[x]).Extension);
                        }

                        // extension my not contain forbidden nodes
                        BitArrays.AndNot(newExtension, newForbidden);

                        // create the new set of traversed node
                        // (update current partial solution)
                        // and add x to the set of forbidden node
                        // (a node may only appear once in a solution)
                        newTraversed = (BitArray)traversed.Clone();
                        newTraversed.Set(x, true);
                        forbidden.Set(x, true);

                        // parse recursively the RGraph
                        ParseRec(newTraversed, newExtension, newForbidden);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a potential solution is a real one
        /// (not included in a previous solution)
        ///  and add this solution to the solution list
        /// in case of success.
        /// </summary>
        /// <param name="traversed">new potential solution</param>
        private void Solution(BitArray traversed)
        {
            bool included = false;
            BitArray projG1 = ProjectG1(traversed);
            BitArray projG2 = ProjectG2(traversed);

            // the solution must follows the search constrains
            // (must contain the mandatory elements in G1 an G2)
            if (IsContainedIn(c1, projG1) && IsContainedIn(c2, projG2))
            {
                // the solution should not be included in a previous solution
                // at the RGraph level. So we check against all previous solution
                // On the other hand if a previous solution is included in the
                // new one, the previous solution is removed.
                var removeList = new List<int>();
                for (var pp = 0; pp < solutionList.Count; pp++)
                {
                    if (included)
                        break;
                    BitArray sol = solutionList[pp];

                    if (!BitArrays.Equals(sol, traversed))
                    {
                        // if we asked to save all 'mappings' then keep this mapping
                        if (findAllMap 
                            && (BitArrays.Equals(projG1, ProjectG1(sol)) || BitArrays.Equals(projG2, ProjectG2(sol))))
                        {
                            // do nothing
                        }
                        // if the new solution is included mark it as included
                        else if (IsContainedIn(projG1, ProjectG1(sol)) || IsContainedIn(projG2, ProjectG2(sol)))
                        {
                            included = true;
                        }
                        // if the previous solution is contained in the new one, remove the previous solution
                        else if (IsContainedIn(ProjectG1(sol), projG1) || IsContainedIn(ProjectG2(sol), projG2))
                        {
                            removeList.Add(pp);
                        }
                    }
                    else
                    {
                        // solution already exists
                        included = true;
                    }
                }
                removeList.Reverse();
                foreach (var i in removeList)
                    solutionList.RemoveAt(i);

                if (included == false)
                {
                    // if it is really a new solution add it to the
                    // list of current solution
                    solutionList.Add(traversed);
                }

                if (!findAllStructure)
                {
                    // if we need only one solution
                    // stop the search process
                    // (e.g. substructure search)
                    stop = true;
                }
            }
        }

        /// <summary>
        ///  Determine if there are potential solution remaining.
        /// </summary>
        /// <param name="potentialNode">set of remaining potential nodes</param>
        /// <returns>true if it is worse to continue the search</returns>
        private bool MustContinue(BitArray potentialNode)
        {
            bool result = true;
            bool cancel = false;
            BitArray projG1 = ProjectG1(potentialNode);
            BitArray projG2 = ProjectG2(potentialNode);

            // if we reached the maximum number of
            // search iterations than do not continue
            if (maxIteration != -1 && nbIteration >= maxIteration)
            {
                return false;
            }

            // if constrains may no more be fulfilled then stop.
            if (!IsContainedIn(c1, projG1) || !IsContainedIn(c2, projG2))
            {
                return false;
            }

            // check if the solution potential is not included in an already
            // existing solution
            foreach (var sol in solutionList)
            {
                if (cancel)
                    break;

                // if we want every 'mappings' do not stop
                if (findAllMap && (BitArrays.Equals(projG1, ProjectG1(sol)) || BitArrays.Equals(projG2, ProjectG2(sol))))
                {
                    // do nothing
                }
                // if it is not possible to do better than an already existing solution than stop.
                else if (IsContainedIn(projG1, ProjectG1(sol)) || IsContainedIn(projG2, ProjectG2(sol)))
                {
                    result = false;
                    cancel = true;
                }
            }

            return result;
        }

        /// <summary>
        ///  Builds the initial extension set. This is the
        ///  set of node that may be used as seed for the
        ///  RGraph parsing. This set depends on the constrains
        ///  defined by the user.
        /// </summary>
        /// <param name="c1">constraint in the graph G1</param>
        /// <param name="c2">constraint in the graph G2</param>
        /// <returns>the new extension set</returns>
        private BitArray BuildB(BitArray c1, BitArray c2)
        {
            this.c1 = c1;
            this.c2 = c2;

            BitArray bs = new BitArray(graph.Count);

            // only nodes that fulfill the initial constrains
            // are allowed in the initial extension set : B
            foreach (var rn in graph)
            {
                if ((c1[rn.RMap.Id1] || BitArrays.IsEmpty(c1)) && (c2[rn.RMap.Id2] || BitArrays.IsEmpty(c2)))
                {
                    bs.Set(graph.IndexOf(rn), true);
                }
            }
            return bs;
        }

        /// <summary>
        ///  Returns the list of solutions.
        /// </summary>
        /// <returns>The solution list</returns>
        public IReadOnlyList<BitArray> Solutions => solutionList;

        /// <summary>
        ///  Converts a RGraph bitset (set of RNode)
        /// to a list of RMap that represents the
        /// mapping between to substructures in G1 and G2
        /// (the projection of the RGraph bitset on G1
        /// and G2).
        /// </summary>
        /// <param name="set">the BitArray</param>
        /// <returns>the RMap list</returns>
        public IReadOnlyList<RMap> BitSetToRMap(BitArray set)
        {
            var rMapList = new List<RMap>();

            for (int x = BitArrays.NextSetBit(set, 0); x >= 0; x = BitArrays.NextSetBit(set, x + 1))
            {
                RNode xNode = graph[x];
                rMapList.Add(xNode.RMap);
            }
            return rMapList;
        }

        /// <summary>
        ///  Sets the 'AllStructres' option. If true
        /// all possible solutions will be generated. If false
        /// the search will stop as soon as a solution is found.
        /// (e.g. when we just want to know if a G2 is
        ///  a substructure of G1 or not).
        /// </summary>
        /// <param name="findAllStructure"></param>
        public void SetAllStructure(bool findAllStructure)
        {
            this.findAllStructure = findAllStructure;
        }

        /// <summary>
        ///  Sets the 'finAllMap' option. If true
        /// all possible 'mappings' will be generated. If false
        /// the search will keep only one 'mapping' per structure
        /// association.
        /// </summary>
        /// <param name="findAllMap"></param>
        public void SetAllMap(bool findAllMap)
        {
            this.findAllMap = findAllMap;
        }

        /// <summary>
        /// Sets the maxIteration for the RGraph parsing. If set to -1,
        /// then no iteration maximum is taken into account.
        /// </summary>
        /// <param name="it">The new maxIteration value</param>
        public void SetMaxIteration(int it)
        {
            this.maxIteration = it;
        }

        /// <summary>
        ///  Returns a string representation of the RGraph.
        /// </summary>
        /// <returns>the string representation of the RGraph</returns>
        public override string ToString()
        {
            string message = "";
            int j = 0;

            foreach (var rn in graph)
            {
                message += "-------------\n" + "RNode " + j + "\n" + rn.ToString() + "\n";
                j++;
            }
            return message;
        }

        /////////////////////////////////
        // BitArray tools
        /// <summary>
        ///  Projects a RGraph bitset on the source graph G1.
        /// </summary>
        /// <param name="set">RGraph BitArray to project</param>
        /// <returns>The associate BitArray in G1</returns>
        public BitArray ProjectG1(BitArray set)
        {
            BitArray projection = new BitArray(FirstGraphSize);
            RNode xNode = null;

            for (int x = BitArrays.NextSetBit(set, 0); x >= 0; x = BitArrays.NextSetBit(set, x + 1))
            {
                xNode = (RNode)graph[x];
                projection.Set(xNode.RMap.Id1, true);
            }
            return projection;
        }

        /// <summary>
        ///  Projects a RGraph bitset on the source graph G2.
        /// </summary>
        /// <param name="set">RGraph BitArray to project</param>
        /// <returns>The associate BitArray in G2</returns>
        public BitArray ProjectG2(BitArray set)
        {
            BitArray projection = new BitArray(SecondGraphSize);
            RNode xNode = null;

            for (int x = BitArrays.NextSetBit(set, 0); x >= 0; x = BitArrays.NextSetBit(set, x + 1))
            {
                xNode = graph[x];
                projection.Set(xNode.RMap.Id2, true);
            }
            return projection;
        }

        /// <summary>
        /// Test if set A is contained in set B.
        /// </summary>
        /// <param name="A">a bitSet</param>
        /// <param name="B">a bitSet</param>
        /// <returns>true if A is contained in B</returns>
        private static bool IsContainedIn(BitArray A, BitArray B)
        {
            bool result = false;

            if (BitArrays.IsEmpty(A))
            {
                return true;
            }

            BitArray setA = (BitArray)A.Clone();
            setA.And(B);

            if (BitArrays.Equals(setA, A))
            {
                result = true;
            }

            return result;
        }
    }
}
