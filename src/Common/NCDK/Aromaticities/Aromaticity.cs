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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Graphs;
using NCDK.RingSearches;
using System;
using System.Collections.Generic;

namespace NCDK.Aromaticities
{
    /// <summary>
    /// A configurable model to perceive aromatic systems. 
    /// </summary>
    /// <remarks>
    /// Aromaticity is useful as
    /// both a chemical property indicating stronger stabilisation and as a way to
    /// treat different resonance forms as equivalent. Each has its own implications
    /// the first in physicochemical attributes and the second in similarity,
    /// depiction and storage.
    /// <para>
    /// To address the resonance forms, several simplified (sometimes conflicting)
    /// models have arisen. Generally the models <b>loosely</b> follow
    /// <see href="https://en.wikipedia.org/wiki/H%C3%BCckel's_rule">Hückel's rule</see>
    /// for determining aromaticity. A common omission being that planarity is not
    /// tested and chemical compounds which are non-planar can be perceived
    /// as aromatic. An example of one such compound is, cyclodeca-1,3,5,7,9-pentaene.
    /// </para>
    /// <para>
    /// Although there is not a single universally accepted model there are models
    /// which may better suited for a specific use (<see href="http://www.slideshare.net/NextMoveSoftware/cheminformatics-toolkits-a-personal-perspective">Cheminformatics Toolkits: A Personal Perspective, Roger Sayle</see>).
    /// The different models are often ill-defined or unpublished but it is important
    /// to acknowledge that there are differences.
    /// </para>
    /// <para>
    /// Although models may get more complicated (e.g. considering tautomers)
    /// normally the reasons for differences are:
    /// <list type="bullet">
    ///     <item>the atoms allowed and how many electrons each contributes</item>
    ///     <item>the rings/cycles are tested</item>
    /// </list>
    /// </para>
    /// <para>
    /// This implementation allows configuration of these via an <see cref="ElectronDonation"/> model and <see cref="ICycleFinder"/>. To obtain an instance
    /// of the electron donation model use one of the factory methods,
    /// <see cref="ElectronDonation.CDKModel"/>, <see cref="ElectronDonation.CDKAllowingExocyclicModel"/>,
    /// <see cref="ElectronDonation.DaylightModel"/> or <see cref="ElectronDonation.PiBondsModel"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Recommended Usage:</para>
    /// <para>Which model/cycles to use depends on the situation but a good general
    /// purpose configuration is shown below:</para>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs"]/*' />
    /// </example>
    /// <seealso href="http://en.wikipedia.org/wiki/H%C3%BCckel's_rule">Hückel's rule</seealso>
    /// <seealso href="http://www.slideshare.net/NextMoveSoftware/cheminformatics-toolkits-a-personal-perspective">Cheminformatics Toolkits: A Personal Perspective, Roger Sayle</seealso>
    /// <seealso href="http://blueobelisk.shapado.com/questions/aromaticity-perception-differences">Aromaticity Perception Differences, Blue Obelisk</seealso>
    // @author John May
    // @cdk.module standard
    public sealed class Aromaticity
    {
        /// <summary>Find how many electrons each atom contributes.</summary>
        private readonly ElectronDonation model;

        /// <summary>The method to find cycles which will be tested for aromaticity.</summary>
        private readonly ICycleFinder cycles;

        /// <summary>
        /// Create an aromaticity model using the specified electron donation
        /// <paramref name="model"/> which is tested on the <paramref name="cycles"/>. The <paramref name="model"/> defines
        /// how many π-electrons each atom may contribute to an aromatic system. The
        /// <paramref name="cycles"/> defines the <see cref="ICycleFinder"/> which is used to find
        /// cycles in a molecule. The total electron donation from each atom in each
        /// cycle is counted and checked. If the electron contribution is equal to
        /// "4n + 2" for a "n &gt;= 0" then the cycle is considered
        /// aromatic. 
        /// </summary>
        /// <remarks>
        /// Changing the electron contribution model or which cycles
        /// are tested affects which atoms/bonds are found to be aromatic. There are
        /// several <see cref="ElectronDonation"/> models and <see cref="Cycles"/>
        /// available. A good choice for the cycles
        /// is to use <see cref="Cycles.AllSimpleFinder()"/> falling back to
        /// <see cref="Cycles.RelevantFinder"/> on failure. Finding all cycles is very
        /// fast but may produce an exponential number of cycles. It is therefore not
        /// feasible for complex fused systems and an exception is thrown.
        /// In such cases the aromaticity can either be skipped or a simpler
        /// polynomial cycle set <see cref="Cycles.RelevantFinder"/> used.
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs+ctor"]/*' />
        /// </example>
        /// <param name="model"></param>
        /// <param name="cycles"></param>
        /// <seealso cref="ElectronDonation"/>
        /// <seealso cref="Cycles"/>
        public Aromaticity(ElectronDonation model, ICycleFinder cycles)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.cycles = cycles ?? throw new ArgumentNullException(nameof(cycles));
        }

        /// <summary>
        /// Find the bonds of a <paramref name="molecule"/> which this model determined were aromatic.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs+FindBonds"]/*' />
        /// </example>
        /// <param name="molecule">the molecule to apply the model to</param>
        /// <returns>the set of bonds which are aromatic</returns>
        /// <exception cref="CDKException">a problem occurred with the cycle perception - one can retry with a simpler cycle set</exception>
        public IEnumerable<IBond> FindBonds(IAtomContainer molecule)
        {
            // build graph data-structures for fast cycle perception
            var bondMap = EdgeToBondMap.WithSpaceFor(molecule);
            var graph = GraphUtil.ToAdjList(molecule, bondMap);

            // initial ring/cycle search and get the contribution from each atom
            RingSearch ringSearch = new RingSearch(molecule, graph);
            var electrons = model.Contribution(molecule, ringSearch);

            // obtain the subset of electron contributions which are >= 0 (i.e.
            // allowed to be aromatic) - we then find the cycles in this subgraph
            // and 'lift' the indices back to the original graph using the subset
            // as a lookup
            var subset = Subset(electrons);
            var subgraph = GraphUtil.Subgraph(graph, subset);

            // for each cycle if the electron sum is valid add the bonds of the
            // cycle to the set or aromatic bonds
            foreach (var cycle in cycles.Find(molecule, subgraph, subgraph.Length).GetPaths())
            {
                if (CheckElectronSum(cycle, electrons, subset))
                {
                    for (int i = 1; i < cycle.Length; i++)
                    {
                        yield return bondMap[subset[cycle[i]], subset[cycle[i - 1]]];
                    }
                }
            }
            yield break;
        }

        /// <summary>
        /// Apply this aromaticity model to a molecule. 
        /// </summary>
        /// <remarks>
        /// Any existing aromaticity
        /// flags are removed - even if no aromatic bonds were found. This follows
        /// the idea of <i>applying</i> an aromaticity model to a molecule such that
        /// the result is the same irrespective of existing aromatic flags. If you
        /// require aromatic flags to be preserved the <see cref="FindBonds(IAtomContainer)"/>
        /// can be used to find bonds without setting any flags.
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs+Apply"]/*' />
        /// </example>
        /// </remarks>
        /// <param name="molecule">the molecule to apply the model to</param>
        /// <returns>the model found the molecule was aromatic</returns>
        public bool Apply(IAtomContainer molecule)
        {
            // clear existing flags
            molecule.IsAromatic = false;
            foreach (var bond in molecule.Bonds)
                bond.IsAromatic = false;
            foreach (var atom in molecule.Atoms)
                atom.IsAromatic = false;

            var bonds = FindBonds(molecule);
            bool isAromatic = false;
            // set the new flags
            foreach (var bond in bonds)
            {
                isAromatic = true;
                bond.IsAromatic = true;
                bond.Begin.IsAromatic = true;
                bond.End.IsAromatic = true;
            }

            molecule.IsAromatic = isAromatic;
            return isAromatic;
        }

        /// <summary>
        /// Check if the number electrons in the <paramref name="cycle"/> could delocalise. The
        /// <paramref name="contributions"/> array indicates how many π-electrons each atom can contribute.
        /// </summary>
        /// <param name="cycle">closed walk (last and first vertex the same) of vertices which form a cycle</param>
        /// <param name="contributions">π-electron contribution from each atom</param>
        /// <param name="subset"></param>
        /// <returns>the number of electrons indicate they could delocalise</returns>
        private static bool CheckElectronSum(IReadOnlyList<int> cycle, IReadOnlyList<int> contributions, IReadOnlyList<int> subset)
        {
            return ValidSum(ElectronSum(cycle, contributions, subset));
        }

        /// <summary>
        /// Count the number electrons in the <paramref name="cycle"/>. The 
        /// <paramref name="contributions"/> array indicates how many π-electrons each atom can
        /// contribute. When the contribution of an atom is less than 0 the sum for
        /// the cycle is always 0.
        /// </summary>
        /// <param name="cycle">closed walk (last and first vertex the same) of vertices which form a cycle</param>
        /// <param name="contributions">π-electron contribution from each atom</param>
        /// <param name="subset"></param>
        /// <returns>the total sum of π-electrons contributed by the <paramref name="cycle"/></returns>
        internal static int ElectronSum(IReadOnlyList<int> cycle, IReadOnlyList<int> contributions, IReadOnlyList<int> subset)
        {
            int sum = 0;
            for (int i = 1; i < cycle.Count; i++)
                sum += contributions[subset[cycle[i]]];
            return sum;
        }

        /// <summary>
        /// Given the number of pi electrons verify that "sum = 4n + 2" for "n ≧ 0".
        /// </summary>
        /// <param name="sum">π-electron sum</param>
        /// <returns>there is an "n" such that "<paramref name="sum"/> = 4n + 2" is equal to the provided <paramref name="sum"/>.</returns>
        internal static bool ValidSum(int sum)
        {
            return (sum - 2) % 4 == 0;
        }

        /// <summary>
        /// Obtain a subset of the vertices which can contribute <paramref name="electrons"/>
        /// and are allowed to be involved in an aromatic system.
        /// </summary>
        /// <param name="electrons">electron contribution</param>
        /// <returns>vertices which can be involved in an aromatic system</returns>
        private static int[] Subset(IReadOnlyList<int> electrons)
        {
            int[] vs = new int[electrons.Count];
            int n = 0;

            for (int i = 0; i < electrons.Count; i++)
                if (electrons[i] >= 0)
                    vs[n++] = i;

            return Arrays.CopyOf(vs, n);
        }

        /// <summary>
        /// Access an aromaticity instance. It has the following configuration: 
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs+CDKLegacy_CDKAromaticSetFinder"]/*' />
        /// </summary>
        /// <remarks>
        /// <para>
        /// This model is not necessarily bad (or really considered legacy) but
        /// should <b>not</b> be considered a gold standard model that covers all
        /// possible cases. It was however the primary method used in previous
        /// versions of the CDK (1.4).
        /// </para>
        /// <para>
        /// This factory method is provided for convenience for
        /// those wishing to replicate aromaticity perception used in previous
        /// versions. The same electron donation model can be used to test
        /// aromaticity of more cycles. For instance, the following configuration
        /// will identify more bonds in a some structures as aromatic:
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.Aromaticity_Example.cs+CDKLegacy_AllFinder_RelevantFinder"]/*' />
        /// </para>
        /// </remarks>
        public static Aromaticity CDKLegacy { get; } = new Aromaticity(ElectronDonation.CDKModel, Cycles.CDKAromaticSetFinder);
    }
}
