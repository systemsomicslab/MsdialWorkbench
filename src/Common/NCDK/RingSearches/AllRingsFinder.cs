/* Copyright (C) 2002-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                    2009  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *                    2013  European Bioinformatics Institute (EMBL-EBI)
 *                          John May <jwmay@users.sf.net>
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using System;

namespace NCDK.RingSearches
{
    /// <summary>
    /// Compute the set of all rings in a molecule. This set includes <i>every</i>
    /// cyclic path of atoms. As the set is exponential it can be very large and is
    /// often impractical (e.g. fullerenes).
    /// </summary>
    /// <remarks>
    /// To avoid combinatorial explosion there is a configurable threshold, at which
    /// the computation aborts. The <see cref="threshold"/> values have been precomputed on
    /// PubChem-Compound and can be used with the <see cref="UsingThreshold(Threshold)"/> .
    /// Alternatively, other ring sets which are a subset of this set offer a
    /// tractable alternative. </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.AllRingsFinder_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="AllCycles"/>
    // @author steinbeck
    // @author johnmay
    // @cdk.module standard
    // @cdk.keyword rings
    // @cdk.keyword all rings
    public sealed class AllRingsFinder
    {
        /// <summary>Precomputed threshold - stops the computation running forever.</summary>
        private readonly Threshold threshold;

        /// <summary>Default constructor using a threshold of <see cref="Threshold.PubChem99"/>.</summary>
        public AllRingsFinder()
            : this(Threshold.PubChem99)
        { }

        /// <summary>Internal constructor.</summary>
        private AllRingsFinder(Threshold threshold)
        {
            this.threshold = threshold;
        }

        /// <summary>
        /// Compute all rings in the given <see cref="IAtomContainer"/>. The container is
        /// first partitioned into ring systems which are then processed separately.
        /// If the molecule has already be partitioned, consider using <see cref="FindAllRingsInIsolatedRingSystem(IAtomContainer)"/>. 
        /// </summary>
        /// <param name="container">The AtomContainer to be searched for rings</param>
        /// <returns>A RingSet with all rings in the AtomContainer</returns>
        /// <exception cref="CDKException">An exception thrown if the threshold was exceeded</exception>
        /// <seealso cref="FindAllRings(IAtomContainer, int)"/>
        /// <seealso cref="FindAllRingsInIsolatedRingSystem(IAtomContainer)"/>
        public IRingSet FindAllRings(IAtomContainer container)
        {
            return FindAllRings(container, container.Atoms.Count);
        }

        /// <summary>
        /// Compute all rings up to and including the <paramref name="maxRingSize"/>. The
        /// container is first partitioned into ring systems which are then processed
        /// separately. If the molecule has already be partitioned, consider using <see cref="FindAllRingsInIsolatedRingSystem(IAtomContainer, int)"/>. 
        /// </summary>
        /// <param name="container">The AtomContainer to be searched for rings</param>
        /// <param name="maxRingSize">Maximum ring size to consider. Provides a possible
        ///                    breakout from recursion for complex compounds.</param>
        /// <returns>A RingSet with all rings in the AtomContainer</returns>
        /// <exception cref="CDKException">An exception thrown if the threshold was exceeded</exception>
        public IRingSet FindAllRings(IAtomContainer container, int maxRingSize)
        {
            var edges = EdgeToBondMap.WithSpaceFor(container);
            var graph = GraphUtil.ToAdjList(container, edges);

            var rs = new RingSearch(container, graph);
            var ringSet = container.Builder.NewRingSet();

            // don't need to run on isolated rings, just need to put vertices in
            // cyclic order
            foreach (var isolated in rs.Isolated())
            {
                if (isolated.Length <= maxRingSize)
                {
                    var ring = ToRing(container, edges, GraphUtil.Cycle(graph, isolated));
                    ringSet.Add(ring);
                }
            }

            // for each set of fused cyclic vertices run the separate search
            foreach (var fused in rs.Fused())
            {
                var ac = new AllCycles(GraphUtil.Subgraph(graph, fused), Math.Min(maxRingSize, fused.Length), threshold.Value);

                if (!ac.Completed) throw new CDKException("Threshold exceeded for AllRingsFinder");

                foreach (var path in ac.GetPaths())
                {
                    IRing ring = ToRing(container, edges, path, fused);
                    ringSet.Add(ring);
                }
            }

            return ringSet;
        }

        /// <summary>
        /// Compute all rings in the given <see cref="IAtomContainer"/>. No pre-processing
        /// is done on the container.
        /// </summary>
        /// <param name="container">The Atom Container to find the ring systems of</param>
        /// <returns>RingSet for the container</returns>
        /// <exception cref="CDKException">An exception thrown if the threshold was exceeded</exception>
        public IRingSet FindAllRingsInIsolatedRingSystem(IAtomContainer container)
        {
            return FindAllRingsInIsolatedRingSystem(container, container.Atoms.Count);
        }

        /// <summary>
        /// Compute all rings up to an including the <paramref name="maxRingSize"/>. No
        /// pre-processing is done on the container.
        /// </summary>
        /// <param name="atomContainer">the molecule to be searched for rings</param>
        /// <param name="maxRingSize">Maximum ring size to consider. Provides a possible
        ///                      breakout from recursion for complex compounds.</param>
        /// <returns>a RingSet containing the rings in molecule</returns>
        /// <exception cref="CDKException">An exception thrown if the threshold was exceeded</exception>
        public IRingSet FindAllRingsInIsolatedRingSystem(IAtomContainer atomContainer, int maxRingSize)
        {
            var edges = EdgeToBondMap.WithSpaceFor(atomContainer);
            var graph = GraphUtil.ToAdjList(atomContainer, edges);

            var ac = new AllCycles(graph, maxRingSize, threshold.Value);

            if (!ac.Completed)
                throw new CDKException("Threshold exceeded for AllRingsFinder");

            var ringSet = atomContainer.Builder.NewRingSet();

            foreach (var path in ac.GetPaths())
            {
                ringSet.Add(ToRing(atomContainer, edges, path));
            }

            return ringSet;
        }

        /// <summary>
        /// Convert a cycle in <see cref="int"/>[] representation to an <see cref="IRing"/>.
        /// </summary>
        /// <param name="container">atom container</param>
        /// <param name="edges">edge map</param>
        /// <param name="cycle">vertex walk forming the cycle, first and last vertex the same</param>
        /// <returns>a new ring</returns>
        private static IRing ToRing(IAtomContainer container, EdgeToBondMap edges, int[] cycle)
        {
            var len = cycle.Length - 1;
            var atoms = new IAtom[len];
            var bonds = new IBond[len];
            for (int i = 0; i < len; i++)
            {
                atoms[i] = container.Atoms[cycle[i]];
                bonds[i] = edges[cycle[i], cycle[i + 1]];
                atoms[i].IsInRing = true;
            }
            var ring = container.Builder.NewRing(atoms, bonds);
            return ring;
        }

        /// <summary>
        /// Convert a cycle in <see cref="int"/>[] representation to an <see cref="IRing"/>
        /// but first map back using the given <paramref name="mapping"/>.
        /// </summary>
        /// <param name="container">atom container</param>
        /// <param name="edges">edge map</param>
        /// <param name="cycle">vertex walk forming the cycle, first and last vertex the same</param>
        /// <returns>a new ring</returns>
        private static IRing ToRing(IAtomContainer container, EdgeToBondMap edges, int[] cycle, int[] mapping)
        {
            var len = cycle.Length - 1;
            var atoms = new IAtom[len];
            var bonds = new IBond[len];
            for (int i = 0; i < len; i++)
            {
                atoms[i] = container.Atoms[mapping[cycle[i]]];
                bonds[i] = edges[mapping[cycle[i]], mapping[cycle[i + 1]]];
                atoms[i].IsInRing = true;
            }
            var ring = container.Builder.NewRing(atoms, bonds);
            return ring;
        }

        /// <summary>
        /// Create an <see cref="AllRingsFinder"/> instance using the given threshold.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.AllRingsFinder_Example.cs+UsingThreshold"]/*' />
        /// </example>
        /// <param name="threshold">the threshold value</param>
        /// <returns>instance with the set threshold</returns>
        public static AllRingsFinder UsingThreshold(Threshold threshold)
        {
            return new AllRingsFinder(threshold);
        }
    }


    /// <summary>
    /// The threshold values provide a limit at which the computation stops.
    /// There will always be some ring systems in which we cannot compute every
    /// possible ring (e.g. fullerenes). This limit replaces the previous timeout
    /// and provides a more meaningful measure of what to expect based on
    /// precomputed percentiles. It is important to consider that, higher is not
    /// always better - generally the large values generate many more rings then
    /// can be reasonably be handled.
    /// </summary>
    /// <remarks>
    /// The latest results were calculated on PubChem Compound (Dec' 12) and
    /// summarised below.
    /// <list type="table">
    /// <item><term>Maximum Degree</term><term>Percent
    /// (%)</term><term>Completed<br /> (ring systems)</term><term>Uncompleted<br />
    /// (ring systems)</term></item> <item><term> </term></item>
    /// <item><term>72</term><term>99.95</term><term>17834013</term><term>8835</term></item>
    /// <item><term>84</term><term>99.96</term><term>17835876</term><term>6972</term></item>
    /// <item><term>126</term><term>99.97</term><term>17837692</term><term>5156</term></item>
    /// <item><term>216</term><term>99.98</term><term>17839293</term><term>3555</term></item>
    /// <item><term>684</term><term>99.99 (default)</term><term>17841065</term><term>1783</term></item>
    /// <item><term> </term></item> <item><term>882</term><term>99.991</term><term>17841342</term><term>1506</term></item>
    /// <item><term>1062</term><term>99.992</term><term>17841429</term><term>1419</term></item>
    /// <item><term>1440</term><term>99.993</term><term>17841602</term><term>1246</term></item>
    /// <item><term>3072</term><term>99.994</term><term>17841789</term><term>1059</term></item>
    /// </list>
    /// </remarks>
    /// <seealso href="http://efficientbits.blogspot.co.uk/2013/06/allringsfinder-sport-edition.html">AllRingsFinder, Sport Edition</seealso>
    public class Threshold
    {
        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.95% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem95 = new Threshold(72);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.96% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem96 = new Threshold(84);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.97% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem97 = new Threshold(126);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.98% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem98 = new Threshold(216);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.99% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem99 = new Threshold(684);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.991% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem991 = new Threshold(882);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.992% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem992 = new Threshold(1062);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.993% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem993 = new Threshold(1440);

        /// <summary>
        /// Based on PubChem Compound (Dec '12), perception will complete for
        /// 99.994% of ring systems.
        /// </summary>
        public static readonly Threshold PubChem994 = new Threshold(3072);

        /// <summary>
        /// Run without any threshold, possibly until the end of time itself.
        /// </summary>
        public static readonly Threshold None = new Threshold(int.MaxValue);

        internal int Value { get; private set; }

        private Threshold(int value)
        {
            Value = value;
        }
    }
}
