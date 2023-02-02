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
using NCDK.Graphs.Invariant;
using NCDK.RingSearches;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Stereo
{
    /// <summary>
    /// Find atoms which can support stereo chemistry based on the connectivity.
    /// Stereocenters are classified as <i>True</i> when they have constitutionally
    /// different ligands and <i>Para</i> ("resemble") stereo centers with
    /// constitutionally identical ligands. Some examples of para-centers
    /// are listed below. Non and potential stereogenic atoms are also indicated. The
    /// method partially implements the rules described by <token>cdk-cite-Razinger93</token>.
    /// Para centers are identified in isolated rings (more common) but are not
    /// currently found in fused systems (e.g. decalin), spiro linked 'assemblages'
    /// or acyclic interdependent centers. 
    /// </summary>
    /// <remarks>
    /// <para><b>Accepted Stereo Atoms</b></para>
    /// <para>
    /// This atoms accepted as being potentially stereogenic are those defined
    /// in the InChI Technical Manual <token>cdk-cite-InChITechManual</token>. These are: 
    /// </para>
    /// <para>
    /// <b>Tetrahedral Stereochemistry:</b>
    /// <list type="bullet">
    ///     <item>Carbon - 4 valent, 4 sigma bonds</item>
    ///     <item>Silicon - 4 valent, 4 sigma bonds</item>
    ///     <item>Germanium - 4 valent, 4 sigma bonds</item>
    ///     <item>Tin - 4 valent, 4 sigma bonds</item>
    ///     <item>Nitrogen cation - 4 valent,4 sigma bonds</item>
    ///     <item>Phosphorus cation - 4 valent, 4 sigma bonds</item>
    ///     <item>Arsenic cation - 4 valent, 4 sigma bonds</item>
    ///     <item>Boron anion - 4 valent, 4 sigma bonds</item>
    ///     <item>Nitrogen - 5 valent, 3 sigma and 1 pi bond</item>
    ///     <item>Phosphorus - 5 valent, 3 sigma and 1 pi bond</item>
    ///     <item>Sulphur - 4 valent, 2 sigma and 1 pi bond</item>
    ///     <item>Sulphur - 6 valent, 2 sigma and 2 pi bonds</item>
    ///     <item>Sulphur Cation - 3 valent, 3 sigma bonds</item>
    ///     <item>Sulphur cation - 5 valent, 3 sigma and 1 pi bond</item>
    ///     <item>Selenium - 4 valent, 2 sigma and 1 pi bond</item>
    ///     <item>Selenium - 6 valent, 2 sigma and 2 pi bonds</item>
    ///     <item>Selenium Cation - 3 valent, 3 sigma bonds</item>
    ///     <item>Selenium cation - 5 valent, 3 sigma and 1 pi bond</item>
    ///     <item>Nitrogen - 3 valent, 3 sigma bonds and in a 3 member ring</item>
    /// </list>
    /// </para>
    /// <para>
    /// <i>N, P, As, S or Se are not stereogenic if they have a terminal H neighbor
    /// or if they have 2 neighbors of the same element (O, S, Se, Te, N) which
    /// have at least one hydrogen. Consider: "P(O)(=O)(OC)OCCC". Phosphines and
    /// arsines are always stereogenic regardless of H neighbors</i>
    /// </para>
    /// <para>
    /// <b>Double Bond Stereochemistry:</b>
    /// The following atoms can appear at either end of a double bond.
    /// <list type="bullet">
    ///     <item>Carbon - 4 valent, 2 sigma and 1 pi bond</item>
    ///     <item>Silicon - 4 valent, 2 sigma and 1 pi bond</item>
    ///     <item>Germanium - 4 valent, 2 sigma and 1 pi bond</item>
    ///     <item>Nitrogen - 3 valent, 1 sigma and 1 pi bond</item>
    ///     <item>Nitrogen cation - 4 valent, 2 sigma and 1 pi bond</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Examples of Para Stereocenters</b>
    /// <list type="bullet"> 
    /// <item>inositol - has 9 stereo isomers, "O[C@H]1[C@H](O)[C@@H](O)[C@H](O)[C@H](O)[C@@H]1O myo-inositol"</item> 
    /// <item>decalin - has 2 stereo isomers, "C1CC[C@H]2CCCC[C@H]2C1" (not currently identified)</item> 
    /// <item>spiro/double-bond linked ring - "InChI=1/C14H24/c1-11-3-7-13(8-4-11)14-9-5-12(2)6-10-14/h11-12H,3-10H2,1-2H3/b14-13-/t11-,12-" (not currently identified)</item> 
    /// <item>An example of a para-center not in a cycle "C[C@@H](O)[C@H](C)[C@H](C)O" (not currently identified)</item>
    /// </list>
    /// </para>
    /// <para>
    /// It should be noted that para-centers may not actually have a configuration. A
    /// simple example of this is seen that by changing the configuration of one
    /// center in "C[C@@H](O)[C@H:1](C)[C@H](C)O" removes the central
    /// configuration as the ligands are now equivalent "C[C@@H](O)[CH:1]](C)[C@@H](C)O"
    /// </para>
    /// </remarks>
    // @author John May
    // @cdk.module standard
    public sealed class Stereocenters
    {
        /// <summary>native CDK structure representation.</summary>
        private readonly IAtomContainer container;

        /// <summary>adjacency list representation for fast traversal.</summary>
        private readonly int[][] g;

        /// <summary>lookup bonds by the index of their atoms.</summary>
        private readonly EdgeToBondMap bondMap;

        /// <summary>the type of stereo center - indexed by atom.</summary>
        private readonly CenterType[] stereocenters;

        /// <summary>the stereo elements - indexed by atom.</summary>
        private readonly StereoElement[] elements;

        /// <summary>basic cycle information (i.e. is atom/bond cyclic) and cycle systems.</summary>
        private readonly RingSearch ringSearch;

        private int numStereoElements;

        private bool checkSymmetry = false;

        /// <summary>
        /// Determine the stereocenter atoms in the provided container based on connectivity.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.Stereocenters_Example.cs+Of"]/*' />
        /// </example>
        /// <param name="container">input container</param>
        /// <returns>the stereocenters</returns>
        public static Stereocenters Of(IAtomContainer container)
        {
            var bondMap = EdgeToBondMap.WithSpaceFor(container);
            var g = GraphUtil.ToAdjList(container, bondMap);
            var stereocenters = new Stereocenters(container, g, bondMap);
            stereocenters.CheckSymmetry();
            return stereocenters;
        }

        /// <summary>
        /// Create a perception method for the provided container, graph
        /// representation and bond map.
        /// </summary>
        /// <param name="container">native CDK structure representation</param>
        /// <param name="graph">graph representation (adjacency list)</param>
        /// <param name="bondMap">fast lookup bonds by atom index</param>
        internal Stereocenters(IAtomContainer container, int[][] graph, EdgeToBondMap bondMap)
        {
            this.container = container;
            this.bondMap = bondMap;
            this.g = graph;
            this.ringSearch = new RingSearch(container, graph);
            this.elements = new StereoElement[g.Length];
            this.stereocenters = new CenterType[g.Length];
            this.numStereoElements = CreateElements();
        }

        internal void CheckSymmetry()
        {
            if (!checkSymmetry)
            {
                checkSymmetry = true;
                numStereoElements = CreateElements();
                int[] symmetry = ToIntArray(Canon.Symmetry(container, g));

                LabelTrueCenters(symmetry);
                LabelIsolatedPara(symmetry);
            }
        }

        /// <summary>
        /// Obtain the type of stereo element support for atom at index <paramref name="v"/>.
        /// Supported elements types are:
        /// <list type="bullet"> 
        /// <item><see cref="CoordinateType.Bicoordinate"/> - an central atom involved in a cumulated system (not yet supported)</item> 
        /// <item><see cref="CoordinateType.Tricoordinate"/> - an atom at one end of a geometric (double-bond) stereo bond or cumulated system.</item>
        /// <item><see cref="CoordinateType.Tetracoordinate"/> - a tetrahedral atom (could also be square planar in future)</item>
        /// <item><see cref="CoordinateType.None"/> - the atom is not a (supported) stereo element type.</item>
        /// </list>
        /// </summary>
        /// <param name="v">atom index (vertex)</param>
        /// <returns>the type of element</returns>
        public CoordinateType ElementType(int v)
        {
            if (stereocenters[v] == CenterType.None || elements[v] == null)
                return CoordinateType.None;
            else
                return elements[v].type;
        }

        internal bool IsSymmetryChecked()
        {
            return checkSymmetry;
        }

        /// <summary>
        /// Is the atom be a stereocenter (i.e. <see cref="CenterType.True"/> or <see cref="CenterType.Para"/>).
        /// </summary>
        /// <param name="v">atom index (vertex)</param>
        /// <returns>the atom at index <paramref name="v"/> is a stereocenter</returns>
        public bool IsStereocenter(int v)
        {
            return stereocenters[v] == CenterType.True || stereocenters[v] == CenterType.Para;
        }

        /// <summary>
        /// Determine the type of stereocenter is the atom at index <paramref name="v"/>.
        /// <list type="bullet"> 
        /// <item><see cref="CenterType.True"/> - the atom has constitutionally different neighbors</item> 
        /// <item><see cref="CenterType.Para"/> - the atom resembles a stereo centre but has constitutionally equivalent neighbors
        /// (e.g. inositol, decalin). The stereocenter depends on the configuration of one or more stereocenters.</item> 
        /// <item><see cref="CenterType.Potential"/> - the atom can supported stereo chemistry but has not be shown ot be a true or para center.</item> 
        /// <item><see cref="CenterType.None"/> - the atom is not a stereocenter (e.g. methane).</item>
        /// </list>
        /// </summary>
        /// <param name="v">atom index.</param>
        /// <returns>the type of stereocenter</returns>
        public CenterType StereocenterType(int v)
        {
            return stereocenters[v];
        }

        /// <summary>
        /// Create <see cref="StereoElement"/>
        /// instance for atoms which support stereochemistry. Each new element is
        /// considered a potential stereo center - any atoms which have not been
        /// assigned an element are non stereo centers.
        /// </summary>
        /// <returns>the number of elements created</returns>
        private int CreateElements()
        {
            bool[] tricoordinate = new bool[g.Length];
            int nElements = 0;

            // all atoms we don't define as potential are considered
            // non-stereogenic
            Arrays.Fill(stereocenters, CenterType.None);

            for (int i = 0; i < g.Length; i++)
            {
                // determine hydrogen count, connectivity and valence
                int h = container.Atoms[i].ImplicitHydrogenCount.Value;
                int x = g[i].Length + h;
                int d = g[i].Length;
                int v = h;

                if (x < 2 || x > 4 || h > 1) continue;

                int piNeighbor = 0;
                foreach (var w in g[i])
                {
                    if (GetAtomicNumber(container.Atoms[w]) == 1 &&
                        container.Atoms[w].MassNumber == null)
                        h++;
                    switch (bondMap[i, w].Order)
                    {
                        case BondOrder.Single:
                            v++;
                            break;
                        case BondOrder.Double:
                            v += 2;
                            piNeighbor = w;
                            break;
                        default:
                            // triple, quadruple or unset? can't be a stereo centre
                            goto GoNext_VERTICES;
                    }
                }

                // check the type of stereo chemistry supported
                switch (SupportedType(i, v, d, h, x))
                {
                    case CoordinateType.Bicoordinate:
                        stereocenters[i] = CenterType.Potential;
                        elements[i] = new Bicoordinate(i, g[i]);
                        nElements++;
                        int u = g[i][0];
                        int w = g[i][1];
                        if (tricoordinate[u])
                        {
                            stereocenters[u] = CenterType.Potential;
                            elements[u] = new Tricoordinate(u, i, g[u]);
                        }
                        if (tricoordinate[w])
                        {
                            stereocenters[w] = CenterType.Potential;
                            elements[w] = new Tricoordinate(w, i, g[w]);
                        }
                        break;
                    case CoordinateType.Tricoordinate:
                        u = i;
                        w = piNeighbor;

                        tricoordinate[u] = true;

                        if (!tricoordinate[w])
                        {
                            if (elements[w] != null && elements[w].type == CoordinateType.Bicoordinate)
                            {
                                stereocenters[u] = CenterType.Potential;
                                elements[u] = new Tricoordinate(u, w, g[u]);
                            }
                            continue;
                        }

                        stereocenters[w] = CenterType.Potential;
                        stereocenters[u] = CenterType.Potential;
                        elements[u] = new Tricoordinate(u, w, g[u]);
                        elements[w] = new Tricoordinate(w, u, g[w]);
                        nElements++;
                        break;

                    case CoordinateType.Tetracoordinate:
                        elements[i] = new Tetracoordinate(i, g[i]);
                        stereocenters[i] = CenterType.Potential;
                        nElements++;
                        break;

                    default:
                        stereocenters[i] = CenterType.None;
                        break;
                }

            GoNext_VERTICES:
                ;
            }

            // link up tetracoordinate atoms accross cumulate systems
            for (int v = 0; v < g.Length; v++)
            {
                if (elements[v] != null && elements[v].type == CoordinateType.Bicoordinate)
                {
                    int u = elements[v].neighbors[0];
                    int w = elements[v].neighbors[1];
                    if (elements[u] != null && elements[w] != null && elements[u].type == CoordinateType.Tricoordinate
                            && elements[w].type == CoordinateType.Tricoordinate)
                    {
                        ((Tricoordinate)elements[u]).other = w;
                        ((Tricoordinate)elements[w]).other = u;
                    }
                }
            }

            return nElements;
        }

        /// <summary>
        /// Labels true stereocenters where all neighbors are constitutionally
        /// different. Potential stereocenters where all constitutionally equivalent
        /// neighbors are terminal (consider [C@H](C)(C)N) are also eliminated.
        /// </summary>
        /// <param name="symmetry">symmetry classes of the atoms</param>
        private void LabelTrueCenters(int[] symmetry)
        {
            // auxiliary array, has the symmetry class already been 'visited'
            bool[] visited = new bool[symmetry.Length + 1];

            for (int v = 0; v < g.Length; v++)
            {
                if (stereocenters[v] == CenterType.Potential)
                {
                    var ws = elements[v].neighbors;
                    int nUnique = 0;
                    bool terminal = true;

                    foreach (var w in ws)
                    {
                        if (!visited[symmetry[w]])
                        {
                            visited[symmetry[w]] = true;
                            nUnique++;
                        }
                        else
                        {
                            // is symmetric neighbor non-terminal
                            if (g[w].Length > 1) terminal = false;
                        }
                    }

                    // reset for testing next element
                    foreach (var w in ws)
                        visited[symmetry[w]] = false;

                    // neighbors are constitutionally different
                    if (nUnique == ws.Length)
                        stereocenters[v] = CenterType.True;

                    // all the symmetric neighbors are terminal then 'v' can not
                    // be a stereocenter. There is an automorphism which inverts
                    // only this stereocenter
                    else if (terminal) stereocenters[v] = CenterType.None;
                }
            }
        }

        /// <summary>
        /// Labels para stereocenter in isolated rings. Any elements which are now
        /// known to not be stereo centers are also eliminated.
        /// </summary>
        /// <param name="symmetry">the symmetry classes of each atom</param>
        private void LabelIsolatedPara(int[] symmetry)
        {
            // auxiliary array, has the symmetry class already been 'visited'
            var visited = new bool[symmetry.Length + 1];

            foreach (var isolated in ringSearch.Isolated())
            {
                var potential = new List<StereoElement>();
                var trueCentres = new List<StereoElement>();
                var cyclic = new BitArray(1000);

                foreach (var v in isolated)
                {
                    cyclic.Set(v, true);
                    if (stereocenters[v] == CenterType.Potential)
                        potential.Add(elements[v]);
                    else if (stereocenters[v] == CenterType.True) trueCentres.Add(elements[v]);
                }

                // there is only 1 potential and 0 true stereocenters in this cycle
                // the element is not a stereocenter
                if (potential.Count + trueCentres.Count < 2)
                {
                    foreach (var element in potential)
                        stereocenters[element.focus] = CenterType.None;
                    continue;
                }

                List<StereoElement> paraElements = new List<StereoElement>();
                foreach (var element in potential)
                {
                    switch (element.type)
                    {
                        case CoordinateType.Tetracoordinate:
                            {
                                int[] ws = element.neighbors;
                                int nUnique = 0;
                                bool terminal = true;

                                foreach (var w in ws)
                                {
                                    if (!cyclic[w])
                                    {
                                        if (!visited[symmetry[w]])
                                        {
                                            visited[symmetry[w]] = true;
                                            nUnique++;
                                        }
                                        else
                                        {
                                            if (g[w].Length > 1) terminal = false;
                                        }
                                    }
                                }

                                // reset for next element
                                foreach (var w in ws)
                                    visited[symmetry[w]] = false;

                                int deg = g[element.focus].Length;

                                if (deg == 3 || (deg == 4 && nUnique == 2)) paraElements.Add(element);

                                // remove those we know cannot possibly be stereocenters
                                if (deg == 4 && nUnique == 1 && terminal) stereocenters[element.focus] = CenterType.None;
                            }
                            break;
                        case CoordinateType.Tricoordinate:
                            {
                                Tricoordinate either = (Tricoordinate)element;
                                if (stereocenters[either.other] == CenterType.True) paraElements.Add(element);
                            }
                            break;
                    }
                }

                if (paraElements.Count + trueCentres.Count >= 2)
                    foreach (var para in paraElements)
                        stereocenters[para.focus] = CenterType.Para;
                else
                    foreach (var para in paraElements)
                        stereocenters[para.focus] = CenterType.None;
            }
        }

        /// <summary>
        /// Determine the type of stereo chemistry (if any) which could be supported
        /// by the atom at index 'i'. The rules used to define the types of
        /// stereochemistry are encoded from the InChI Technical Manual.
        /// </summary>
        /// <param name="i">atom index</param>
        /// <param name="v">valence</param>
        /// <param name="h">hydrogen</param>
        /// <param name="x">connectivity</param>
        /// <returns>type of stereo chemistry</returns>
        private CoordinateType SupportedType(int i, int v, int d, int h, int x)
        {
            var atom = container.Atoms[i];

            // the encoding a bit daunting and to be concise short variable names
            // are used. these parameters make no distinction between implicit/
            // explicit values and allow complete (and fast) characterisation of
            // the type of stereo atom
            //
            // i: atom index
            // v: valence (bond order sum)
            // h: total hydrogen count
            // x: connected atoms
            // q: formal charge

            var q = Charge(atom);

            // more than one hydrogen
            if (checkSymmetry && h > 1)
                return CoordinateType.None;

            switch (GetAtomicNumber(atom))
            {
                case 0: // stop the nulls on pseudo atoms messing up anything else
                    return CoordinateType.None;
                case 5: // boron
                    return q == -1 && v == 4 && x == 4 ? CoordinateType.Tetracoordinate : CoordinateType.None;
                case 6: // carbon
                    if (v != 4 || q != 0)
                        return CoordinateType.None;
                    if (x == 2)
                        return CoordinateType.Bicoordinate;
                    if (x == 3)
                        return CoordinateType.Tricoordinate;
                    if (x == 4)
                        return CoordinateType.Tetracoordinate;
                    return CoordinateType.None;
                case 7: // nitrogen
                    if (x == 2 && v == 3 && d == 2 && q == 0)
                        return CoordinateType.Tricoordinate;
                    if (x == 3 && v == 4 && q == 1)
                        return CoordinateType.Tricoordinate;
                    if (x == 4 && h == 0 && (q == 0 && v == 5 || q == 1 && v == 4))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    // note: bridgehead not allowed by InChI but makes sense
                    return x == 3 && h == 0 && (IsBridgeHead(i) || InThreeMemberRing(i)) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                case 14: // silicon
                    if (v != 4 || q != 0)
                        return CoordinateType.None;
                    if (x == 3)
                        return CoordinateType.Tricoordinate;
                    if (x == 4)
                        return CoordinateType.Tetracoordinate;
                    return CoordinateType.None;
                case 15: // phosphorus
                    if (x == 4 && (q == 0 && v == 5 && h == 0 || q == 1 && v == 4))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    // note 3 valent phosphorus not documented as accepted
                    // by InChI tech manual but tests show it is
                    if (x == 3 && q == 0 && v == 3 && h == 0)
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    goto case 16;
                case 16: // sulphur
                    if (h > 0)
                        return CoordinateType.None;
                    if (q == 0 && ((v == 4 && x == 3) || (v == 6 && x == 4)))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    if (q == 1 && ((v == 3 && x == 3) || (v == 5 && x == 4)))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    return CoordinateType.None;

                case 32: // germanium
                    if (v != 4 || q != 0)
                        return CoordinateType.None;
                    if (x == 3)
                        return CoordinateType.Tricoordinate;
                    if (x == 4)
                        return CoordinateType.Tetracoordinate;
                    return CoordinateType.None;
                case 33: // arsenic
                    if (x == 4 && q == 1 && v == 4)
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    return CoordinateType.None;
                case 34: // selenium
                    if (h > 0)
                        return CoordinateType.None;
                    if (q == 0 && ((v == 4 && x == 3) || (v == 6 && x == 4)))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    if (q == 1 && ((v == 3 && x == 3) || (v == 5 && x == 4)))
                        return VerifyTerminalHCount(i) ? CoordinateType.Tetracoordinate : CoordinateType.None;
                    return CoordinateType.None;

                case 50: // tin
                    return q == 0 && v == 4 && x == 4 ? CoordinateType.Tetracoordinate : CoordinateType.None;
            }

            return CoordinateType.None;
        }

        /// <summary>
        /// Verify that there are is not 2 terminal heavy atoms (of the same element)
        /// which have a hydrogen count > 0. This follows the InChI specification
        /// that - An atom or positive ion N, P, As, S, or Se is not treated as
        /// stereogenic if it has (a) A terminal H atom neighbor or (b) At least two
        /// terminal neighbors, XHm and XHn, (n+m>0) connected by any kind of bond,
        /// where X is O, S, Se, Te, or N. This avoids the issue that under
        /// Cahn-Ingold-Prelog (or canonicalisation) the oxygens in 'P(=O)(O)(*)*'
        /// would not be found to be equivalent and a parity/winding would be
        /// assigned.
        /// </summary>
        /// <param name="v">the vertex (atom index) to check</param>
        /// <returns>the atom does not have > 2 terminal neighbors with a combined hydrogen count of > 0</returns>
        private bool VerifyTerminalHCount(int v)
        {
            if (!checkSymmetry)
                return true;

            var counts = new int[6];
            var atoms = Arrays.CreateJagged<int>(6, g[v].Length);

            bool found = false;

            foreach (var w in g[v])
            {
                int idx = IndexNeighbor(container.Atoms[w]);
                atoms[idx][counts[idx]++] = w;
                found = found || (idx > 0 && counts[idx] > 1);
            }

            if (!found)
                return true;

            for (int i = 1; i < counts.Length; i++)
            {
                if (counts[i] < 2)
                    continue;

                int terminalCount = 0;
                int terminalHCount = 0;

                for (int j = 0; j < counts[i]; j++)
                {
                    int hCount = 0;
                    var ws = g[atoms[i][j]];
                    foreach (var w in g[atoms[i][j]])
                    {
                        var atom = container.Atoms[w];
                        if (GetAtomicNumber(atom) == 1 && atom.MassNumber == null)
                        {
                            hCount++;
                        }
                    }

                    // is terminal?
                    if (ws.Length - hCount == 1)
                    {
                        terminalCount++;
                        terminalHCount += hCount + container.Atoms[atoms[i][j]].ImplicitHydrogenCount.Value;
                    }
                }

                if (terminalCount > 1 && terminalHCount > 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Index the atom by element to a number between 0-5. This allows us to
        /// quickly count up neighbors we need to and the ignore those we don't
        /// (defaults to 0).
        /// </summary>
        /// <param name="atom">an atom to get the element index of</param>
        /// <returns>the element index</returns>
        private static int IndexNeighbor(IAtom atom)
        {
            switch (GetAtomicNumber(atom))
            {
                case 7: // N
                    return 1;
                case 8: // O
                    return 2;
                case 16: // S
                    return 3;
                case 34: // Se
                    return 4;
                case 52: // Te
                    return 5;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Check if the atom at index <paramref name="v"/> is a member of a small ring
        /// (n=3). This is the only time a 3 valent nitrogen is allowed by InChI to
        /// be potentially stereogenic.
        /// </summary>
        /// <param name="v">atom index</param>
        /// <returns>the atom is a member of a 3 member ring</returns>
        private bool InThreeMemberRing(int v)
        {
            var adj = new BitArray(0);
            foreach (var w in g[v])
                BitArrays.SetValue(adj, w, true);
            // is a neighbors neighbor adjacent?
            foreach (var w in g[v])
                foreach (var u in g[w])
                    if (BitArrays.GetValue(adj, u))
                        return true;
            return false;
        }

        private void VisitPart(bool[] visit, IAtom atom)
        {
            visit[container.Atoms.IndexOf(atom)] = true;
            foreach (var bond in container.GetConnectedBonds(atom))
            {
                var nbr = bond.GetOther(atom);
                if (!visit[container.Atoms.IndexOf(nbr)])
                    VisitPart(visit, nbr);
            }
        }

        /// <summary>
        /// Detects if a bond is a fused-ring bond (e.g. napthalene). A bond is a
        /// ring fusion if deleting it creates a new component. Or to put it another
        /// way if using a flood-fill we can't visit every atom without going through
        /// this bond, not we only check ring bonds.
        /// </summary>
        /// <param name="bond">the bond</param>
        /// <returns>the bond is a fused bond</returns>
        private bool IsFusedBond(IBond bond)
        {
            var beg = bond.Begin;
            var end = bond.End;
            if (GetRingDegree(container.Atoms.IndexOf(beg)) < 3
             && GetRingDegree(container.Atoms.IndexOf(end)) < 3)
                return false;
            var avisit = new bool[container.Bonds.Count];
            avisit[container.Atoms.IndexOf(beg)] = true;
            avisit[container.Atoms.IndexOf(end)] = true;
            int count = 0;
            foreach (var nbond in container.GetConnectedBonds(beg))
            {
                var nbr = nbond.GetOther(beg);
                if (nbr.Equals(end) || !ringSearch.Cyclic(nbond))
                    continue;
                if (count == 0)
                {
                    count++;
                    VisitPart(avisit, nbr);
                }
                else
                {
                    if (!avisit[container.Atoms.IndexOf(nbr)])
                        return true;
                }
            }
            return false;
        }

        private int GetRingDegree(int v)
        {
            int x = 0;
            foreach (int w in g[v])
                if (ringSearch.Cyclic(v, w))
                    x++;
            return x;
        }

        private bool IsBridgeHead(int v)
        {
            if (GetRingDegree(v) < 3)
                return false;
            var atom = container.Atoms[v];
            foreach (var bond in container.GetConnectedBonds(atom))
                if (IsFusedBond(bond))
                    return false;
            return true;
        }

        /// <summary>
        /// Safely obtain the atomic number of an atom. If the atom has undefined
        /// atomic number and is not a pseudo-atom it is considered an error. Pseudo
        /// atoms with undefined atomic number default to 0.
        /// </summary>
        /// <param name="a">an atom</param>
        /// <returns>the atomic number of the atom</returns>
        private static int GetAtomicNumber(IAtom a)
        {
            var elem = a.AtomicNumber;
            return elem;
        }

        /// <summary>
        /// Safely obtain the formal charge on an atom. If the atom had undefined
        /// formal charge it is considered as neutral (0).
        /// </summary>
        /// <param name="a">an atom</param>
        /// <returns>the formal charge</returns>
        private static int Charge(IAtom a)
        {
            int? chg = a.FormalCharge;
            return chg ?? 0;
        }

        /// <summary>
        /// Convert an array of long (64-bit) values to an array of (32-bit)
        /// integrals.
        /// </summary>
        /// <param name="org">the original array of values</param>
        /// <returns>the array cast to int values</returns>
        private static int[] ToIntArray(long[] org)
        {
            var cpy = new int[org.Length];
            for (int i = 0; i < cpy.Length; i++)
                cpy[i] = (int)org[i];
            return cpy;
        }

        /// <summary>
        /// Internal stereo element representation. We need to define the sides of a
        /// double bond separately and want to avoid reflection (is) by using
        /// a type parameter. We also store the neighbors we need to check for
        /// equivalence directly.
        /// </summary>
        private abstract class StereoElement
        {
            public int focus;
            public int[] neighbors;
            public CoordinateType type;
        }

        /// <summary>Represents a tetrahedral stereocenter with 2 neighbors.</summary>
        private sealed class Bicoordinate : StereoElement
        {
            public Bicoordinate(int v, int[] neighbors)
            {
                this.focus = v;
                this.type = CoordinateType.Bicoordinate;
                this.neighbors = Arrays.CopyOf(neighbors, neighbors.Length);
            }
        }

        /// <summary>Represents a tetrahedral stereocenter with 3 or 4 neighbors.</summary>
        private sealed class Tetracoordinate : StereoElement
        {
            public Tetracoordinate(int v, int[] neighbors)
            {
                this.focus = v;
                this.type = CoordinateType.Tetracoordinate;
                this.neighbors = Arrays.CopyOf(neighbors, neighbors.Length);
            }
        }

        /// <summary>
        /// Represents one end of a double bond. The element only stores non-double
        /// bonded neighbors and also indexes it's <see cref="other"/> end.
        /// </summary>
        private sealed class Tricoordinate : StereoElement
        {
            public int other;

            /// <summary>
            /// Create a tri-coordinate atom for one end of a double bond. Two
            /// elements need to be created which reference each other.
            /// </summary>
            /// <param name="v">the focus of this end</param>
            /// <param name="w">the double bonded other end of the element</param>
            /// <param name="neighbors">the neighbors of v</param>
            public Tricoordinate(int v, int w, int[] neighbors)
            {
                this.focus = v;
                this.other = w;
                this.type = CoordinateType.Tricoordinate;
                this.neighbors = new int[neighbors.Length - 1];
                int n = 0;

                // remove the other neighbor from neighbors when checking
                // equivalence
                for (int i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i] != other)
                        this.neighbors[n++] = neighbors[i];
                }
            }
        }
    }

    public enum CoordinateType
    {
        /// <summary>A non-stereogenic atom.</summary>
        None = 0,

        /// <summary>An atom within a cumulated system. (not yet supported)</summary>
        Bicoordinate,

        /// <summary>
        /// A potentially stereogenic atom with 3 neighbors - one atom in a
        /// geometric centres or cumulated system (allene, cumulene).
        /// </summary>
        Tricoordinate,

        /// <summary>
        /// A potentially stereogenic atom with 4 neighbors - tetrahedral
        /// centres.
        /// </summary>
        Tetracoordinate,
    }

    /// <summary>Defines the type of a stereocenter.</summary>
    public enum CenterType
    {
        /// <summary>Non stereo-centre.</summary>
        None = 0,

        /// <summary>Atom is a true stereo-centre.</summary>
        True,

        /// <summary>Atom resembles a stereo-centre (para).</summary>
        Para,

        /// <summary>Atom is a potential stereo-centre</summary>
        Potential,
    }
}
