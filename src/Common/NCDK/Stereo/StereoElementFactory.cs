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

using NCDK.Graphs;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Stereo
{
    /// <summary>
    /// Create stereo elements for a structure with 2D and 3D coordinates. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The factory does not verify whether atoms can or cannot support stereochemistry -
    /// for this functionality use <see cref="Stereocenters"/>. The factory will not create
    /// stereo elements if there is missing information (wedge/hatch bonds, undefined
    /// coordinates) or the layout indicates unspecified configuration.
    /// </para>
    /// <para>
    /// Stereocenters specified with inverse down (hatch) bond style are created if
    /// the configuration is unambiguous and the bond does not connect to another
    /// stereocenter.
    /// </para>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="Stereocenters"/>
    // @author John May
    // @cdk.module standard
    public abstract class StereoElementFactory
    {
        /// <summary>Native CDK structure representation.</summary>
        private readonly IAtomContainer container;

        /// <summary>Adjacency list graph representation.</summary>
        private readonly int[][] graph;

        /// <summary>A bond map for fast access to bond labels between two atom indices.</summary>
        private readonly EdgeToBondMap bondMap;

        private readonly List<Projection> projections = new List<Projection>();

        /// <summary>
        /// Verify if created stereochemistry are actually stereo-centres.
        /// </summary>
        private bool check = false;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="container">an atom container</param>
        /// <param name="graph">adjacency list representation</param>
        /// <param name="bondMap">lookup bonds by atom index</param>
        internal StereoElementFactory(IAtomContainer container, int[][] graph, EdgeToBondMap bondMap)
        {
            this.container = container;
            this.graph = graph;
            this.bondMap = bondMap;
        }

        private bool VisitSmallRing(int[] mark, int aidx, int prev, int depth, int max)
        {
            if (mark[aidx] == 2)
                return true;
            if (depth == max)
                return false;
            if (mark[aidx] == 1)
                return false;
            mark[aidx] = 1;
            foreach (var nbr in graph[aidx])
            {
                if (nbr != prev && VisitSmallRing(mark, nbr, aidx, depth + 1, max))
                    return true;
            }
            mark[aidx] = 0;
            return false;
        }

        private bool IsInSmallRing(IBond bond, int max)
        {
            if (!bond.IsInRing)
                return false;
            var beg = bond.Begin;
            var end = bond.End;
            var mark = new int[container.Atoms.Count];
            var bidx = container.Atoms.IndexOf(beg);
            var eidx = container.Atoms.IndexOf(end);
            mark[bidx] = 2;
            return VisitSmallRing(mark,
                                  eidx,
                                  bidx,
                                  1,
                                  max);
        }

        private bool IsInSmallRing(IAtom atom, int max)
        {
            if (!atom.IsInRing)
                return false;
            foreach (var bond in container.GetConnectedBonds(atom))
            {
                if (IsInSmallRing(bond, max))
                    return true;
            }
            return false;
        }

        private IBond GetOtherDb(IAtom a, IBond other)
        {
            IBond result = null;
            foreach (var bond in container.GetConnectedBonds(a))
            {
                if (bond.Equals(other))
                    continue;
                if (bond.Order != BondOrder.Double)
                    continue;
                if (result != null)
                    return null;
                result = bond;
            }
            return result;
        }

        private static IAtom GetShared(IBond a, IBond b)
        {
            if (b.Contains(a.Begin))
                return a.Begin;
            if (b.Contains(a.End))
                return a.End;
            return null;
        }

        private List<IBond> GetCumulatedDbs(IBond endBond)
        {
            var dbs = new List<IBond> { endBond };
            var other = GetOtherDb(endBond.Begin, endBond);
            if (other == null)
                other = GetOtherDb(endBond.End, endBond);
            if (other == null)
                return null;
            while (other != null)
            {
                dbs.Add(other);
                IAtom a = GetShared(dbs[dbs.Count - 1], dbs[dbs.Count - 2]);
                other = GetOtherDb(other.GetOther(a), other);
            }
            return dbs;
        }

        /// <summary>
        /// Creates all stereo elements found by <see cref="Stereocenters"/> using the or
        /// 2D/3D coordinates to specify the configuration (clockwise/anticlockwise).
        /// Currently only <see cref="ITetrahedralChirality"/> and <see cref="IDoubleBondStereochemistry"/> 
        /// elements are created..
        /// </summary>
        /// <returns>stereo elements</returns>
        public IEnumerable<IStereoElement<IChemObject, IChemObject>> CreateAll()
        {
            Cycles.MarkRingAtomsAndBonds(container);
            var centers = new Stereocenters(container, graph, bondMap);
            if (check)
                centers.CheckSymmetry();

            // projection recognition (note no action in constructors)
            var fischerRecon = new FischerRecognition(container, graph, bondMap, centers);
            var cycleRecon = new CyclicCarbohydrateRecognition(container, graph, bondMap, centers);

            foreach (var stereo in fischerRecon.Recognise(projections))
                yield return stereo;
            foreach (var stereo in cycleRecon.Recognise(projections))
                yield return stereo;

            for (int v = 0; v < graph.Length; v++)
            {
                switch (centers.ElementType(v))
                {
                    // elongated tetrahedrals
                    case CoordinateType.Bicoordinate:
                        foreach (var w in graph[v])
                        {
                            // end of an extended tetrahedral or cis/trans
                            if (centers.ElementType(w) == CoordinateType.Tricoordinate)
                            {
                                List<IBond> dbs = GetCumulatedDbs(container.GetBond(container.Atoms[w], container.Atoms[v]));
                                if (dbs == null)
                                    continue;
                                if (container.Bonds.IndexOf(dbs[0]) > container.Bonds.IndexOf(dbs[dbs.Count - 1]))
                                    continue;
                                if ((dbs.Count & 0x1) == 0)
                                {
                                    IAtom focus = GetShared(dbs[dbs.Count / 2],
                                                            dbs[(dbs.Count / 2) - 1]);
                                    // extended tetrahedral
                                    var element = CreateExtendedTetrahedral(container.Atoms.IndexOf(focus), centers);
                                    if (element != null)
                                        yield return element;
                                }
                                else
                                {
                                    // extended cis-trans
                                    var element = CreateExtendedCisTrans(dbs, centers);
                                    if (element != null)
                                        yield return element;
                                }
                                break;
                            }
                        }
                        break;
                    // tetrahedrals
                    case CoordinateType.Tetracoordinate:
                        {
                            var element = CreateTetrahedral(v, centers);
                            if (element != null)
                                yield return element;
                        }
                        break;
                    // aryl-aryl atropisomers
                    case CoordinateType.Tricoordinate:
                        foreach (int w in graph[v])
                        {
                            var bond = bondMap[v, w];
                            if (w > v &&
                                centers.ElementType(w) == CoordinateType.Tricoordinate &&
                                bond.Order == BondOrder.Single &&
                                !IsInSmallRing(bond, 6) &&
                                IsInSmallRing(bond.Begin, 6) &&
                                IsInSmallRing(bond.End, 6))
                            {
                                var element = CreateAtropisomer(v, w, centers);
                                if (element != null)
                                    yield return element;
                                break;
                            }
                        }
                        break;
                }
            }

            // always need to verify for db...
            centers.CheckSymmetry();
            for (int v = 0; v < graph.Length; v++)
            {
                switch (centers.ElementType(v))
                {
                    // cis/trans double bonds
                    case CoordinateType.Tricoordinate:
                        if (!centers.IsStereocenter(v))
                            continue;
                        foreach (int w in graph[v])
                        {
                            var bond = bondMap[v, w];
                            if (w > v && bond.Order == BondOrder.Double)
                            {
                                if (centers.ElementType(w) == CoordinateType.Tricoordinate
                                 && centers.IsStereocenter(w)
                                 && !IsInSmallRing(bond, 7))
                                {
                                    var element = CreateGeometric(v, w, centers);
                                    if (element != null)
                                        yield return element;
                                }
                                break;
                            }
                        }
                        break;
                }
            }

            yield break;
        }

        /// <summary>
        /// Create a tetrahedral element for the atom at index <paramref name="v"/>. If a
        /// tetrahedral element could not be created then null is returned. An
        /// element can not be created if, one or more atoms was missing coordinates,
        /// the atom has an unspecified (wavy) bond, the atom is no non-planar bonds
        /// (i.e. up/down, wedge/hatch). The method does not check if tetrahedral
        /// chirality is supported - for this functionality use <see cref="Stereocenters"/>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs+CreateTetrahedral_int"]/*' />
        /// </example>
        /// <param name="v">atom index (vertex)</param>
        /// <returns>a new stereo element</returns>
        public abstract ITetrahedralChirality CreateTetrahedral(int v, Stereocenters stereocenters);

        /// <summary>
        /// Create axial atropisomers.
        /// </summary>
        /// <param name="v">first atom of single bond</param>
        /// <param name="w">other atom of single bond</param>
        /// <param name="stereocenters">stereo centres</param>
        /// <returns>new stereo element</returns>
        public abstract IStereoElement<IChemObject, IChemObject> CreateAtropisomer(int v, int w, Stereocenters stereocenters);

        /// <summary>
        /// Create a tetrahedral element for the atom. If a tetrahedral element could
        /// not be created then null is returned. An element can not be created if,
        /// one or more atoms was missing coordinates, the atom has an unspecified
        /// (wavy) bond, the atom is no non-planar bonds (i.e. up/down, wedge/hatch).
        /// The method does not check if tetrahedral chirality is supported - for
        /// this functionality use <see cref="Stereocenters"/>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs+CreateTetrahedral_IAtom"]/*' />
        /// </example>
        /// <param name="atom">atom</param>
        /// <returns>a new stereo element</returns>
        public abstract ITetrahedralChirality CreateTetrahedral(IAtom atom, Stereocenters stereocenters);

        /// <summary>
        /// Create a geometric element (double-bond stereochemistry) for the provided
        /// atom indices. If the configuration could not be created a null element is
        /// returned. There is no configuration is the coordinates do not indicate a
        /// configuration, there were undefined coordinates or an unspecified bond
        /// label. The method does not check if double bond stereo is supported - for
        /// this functionality use <see cref="Stereocenters"/>.
        /// </summary>
        /// <param name="u">an atom index</param>
        /// <param name="v">an atom pi bonded 'v'</param>
        /// <returns>a new stereo element</returns>
        public abstract IDoubleBondStereochemistry CreateGeometric(int u, int v, Stereocenters stereocenters);

        /// <summary>
        /// Create a geometric element (double-bond stereochemistry) for the provided
        /// double bond. If the configuration could not be created a null element is
        /// returned. There is no configuration is the coordinates do not indicate a
        /// configuration, there were undefined coordinates or an unspecified bond
        /// label. The method does not check if double bond stereo is supported - for
        /// this functionality use <see cref="Stereocenters"/>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs+CreateGeometric_IBond"]/*' />
        /// </example>
        /// <param name="bond">the bond to create a configuration for</param>
        /// <returns>a new stereo element</returns>
        public abstract IDoubleBondStereochemistry CreateGeometric(IBond bond, Stereocenters stereocenters);

        /// <summary>
        /// Create an extended tetrahedral element for the atom at index <paramref name="v"/>.
        /// If an extended tetrahedral element could not be created then null is
        /// returned. An element can not be created if, one or more atoms was
        /// missing coordinates, the atom has an unspecified (wavy) bond, the atom
        /// is no non-planar bonds (i.e. up/down, wedge/hatch). The method does not
        /// check if tetrahedral chirality is supported - for this functionality
        /// use <see cref="Stereocenters"/>.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs+CreateExtendedTetrahedral"]/*' />
        /// </example>
        /// <param name="v">atom index (vertex)</param>
        /// <returns>a new stereo element</returns>
        public abstract ExtendedTetrahedral CreateExtendedTetrahedral(int v, Stereocenters stereocenters);

        /// <summary>
        /// Create an extended cis/trans bond (cumulated) given one end (see diagram below). 
        /// </summary>
        /// <remarks>
        /// The stereo element geometry will only be created if there is an
        /// odd number of cumulated double bonds. The double bond list ('bonds')
        /// should be ordered consecutively from one end to the other.
        /// <pre>
        ///  C               C
        ///   \             /
        ///    C = C = C = C
        ///      ^   ^   ^
        ///      ^---^---^----- bonds
        /// </pre>
        /// </remarks>
        /// <param name="bonds">cumulated double bonds</param>
        /// <param name="centers">discovered stereocentres</param>
        /// <returns>the extended cis/trans geometry if one could be created</returns>
        public abstract ExtendedCisTrans CreateExtendedCisTrans(IReadOnlyList<IBond> bonds, Stereocenters centers);

        /// <summary>
        /// Indicate that stereochemistry drawn as a certain projection should be
        /// interpreted. 
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Stereo.StereoElementFactory_Example.cs+InterpretProjections"]/*' />
        /// </example>
        /// <param name="projections">types of projection</param>
        /// <returns>self</returns>
        /// <seealso cref="Projection"/>
        public StereoElementFactory InterpretProjections(params Projection[] projections)
        {
            this.projections.AddRange(projections);
            this.check = true;
            return this;
        }

        public StereoElementFactory CheckSymmetry(bool check)
        {
            this.check = check;
            return this;
        }

        /// <summary>
        /// Create a stereo element factory for creating stereo elements using 2D
        /// coordinates and depiction labels (up/down, wedge/hatch).
        /// </summary>
        /// <param name="container">the structure to create the factory for</param>
        /// <returns>the factory instance</returns>
        public static StereoElementFactory Using2DCoordinates(IAtomContainer container)
        {
            var bondMap = EdgeToBondMap.WithSpaceFor(container);
            var graph = GraphUtil.ToAdjList(container, bondMap);
            return new StereoElementFactory2D(container, graph, bondMap);
        }

        /// <summary>
        /// Create a stereo element factory for creating stereo elements using 3D
        /// coordinates and depiction labels (up/down, wedge/hatch).
        /// </summary>
        /// <param name="container">the structure to create the factory for</param>
        /// <returns>the factory instance</returns>
        public static StereoElementFactory Using3DCoordinates(IAtomContainer container)
        {
            var bondMap = EdgeToBondMap.WithSpaceFor(container);
            var graph = GraphUtil.ToAdjList(container, bondMap);
            return new StereoElementFactory3D(container, graph, bondMap).CheckSymmetry(true);
        }

        private static bool HasUnspecifiedParity(IAtom atom)
        {
            return (int)atom.StereoParity == 3;
        }

        /// <summary>Create stereo-elements from 2D coordinates.</summary>
        sealed class StereoElementFactory2D : StereoElementFactory
        {
            /// <summary>
            /// Threshold at which the determinant is considered too small (unspecified
            /// by coordinates).
            /// </summary>
            private const double Threshold = 0.1;

            /// <summary>
            /// Create a new stereo-element factory for the specified structure.
            /// </summary>
            /// <param name="container">native CDK structure representation</param>
            /// <param name="graph">adjacency list representation</param>
            /// <param name="bondMap">fast bond lookup from atom indices</param>
            public StereoElementFactory2D(IAtomContainer container, int[][] graph, EdgeToBondMap bondMap)
                : base(container, graph, bondMap)
            {
            }

            /// <inheritdoc/>
            public override ITetrahedralChirality CreateTetrahedral(IAtom atom, Stereocenters stereocenters)
            {
                return CreateTetrahedral(container.Atoms.IndexOf(atom), stereocenters);
            }

            /// <inheritdoc/>
            public override IDoubleBondStereochemistry CreateGeometric(IBond bond, Stereocenters stereocenters)
            {
                return CreateGeometric(container.Atoms.IndexOf(bond.Begin), container.Atoms.IndexOf(bond.End), stereocenters);
            }

            /// <inheritdoc/>
            public override ITetrahedralChirality CreateTetrahedral(int v, Stereocenters stereocenters)
            {
                var focus = container.Atoms[v];

                if (HasUnspecifiedParity(focus))
                    return null;

                var neighbors = new IAtom[4];
                var elevation = new int[4];

                neighbors[3] = focus;

                bool nonplanar = false;
                int n = 0;

                foreach (var w in graph[v])
                {
                    var bond = bondMap[v, w];

                    // wavy bond
                    if (IsUnspecified(bond))
                        return null;

                    neighbors[n] = container.Atoms[w];
                    elevation[n] = ElevationOf(focus, bond);

                    if (elevation[n] != 0)
                        nonplanar = true;

                    n++;
                }

                // too few/many neighbors
                if (n < 3 || n > 4)
                    return null;

                // TODO: verify valid wedge/hatch configurations using similar procedure
                // to NonPlanarBonds in the cdk-sdg package.

                // no up/down bonds present - check for inverted down/hatch
                if (!nonplanar)
                {
                    var ws = graph[v];
                    for (int i = 0; i < ws.Length; i++)
                    {
                        var w = ws[i];
                        var bond = bondMap[v, w];

                        // we have already previously checked whether 'v' is at the
                        // 'point' and so these must be inverse (fat-end @
                        // stereocenter) ala Daylight
                        if (bond.Stereo == BondStereo.Down || bond.Stereo == BondStereo.DownInverted)
                        {
                            // we stick to the 'point' end convention but can
                            // interpret if the bond isn't connected to another
                            // stereocenter - otherwise it's ambiguous!
                            if (stereocenters.IsStereocenter(w))
                                continue;

                            elevation[i] = -1;
                            nonplanar = true;
                        }
                    }

                    // still no bonds to use
                    if (!nonplanar)
                        return null;
                }

                var parity = Parity(focus, neighbors, elevation);

                if (parity == 0)
                    return null;

                var winding = parity > 0 ? TetrahedralStereo.AntiClockwise : TetrahedralStereo.Clockwise;

                return new TetrahedralChirality(focus, neighbors, winding);
            }

            private static bool IsWedged(IBond bond)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.Up:
                    case BondStereo.Down:
                    case BondStereo.UpInverted:
                    case BondStereo.DownInverted:
                        return true;
                    default:
                        return false;
                }
            }

            public override IStereoElement<IChemObject, IChemObject> CreateAtropisomer(int u, int v, Stereocenters stereocenters)
            {
                IAtom end1 = container.Atoms[u];
                IAtom end2 = container.Atoms[v];

                if (HasUnspecifiedParity(end1) || HasUnspecifiedParity(end2))
                    return null;

                if (graph[u].Length != 3 || graph[v].Length != 3)
                    return null;

                // check degrees of connected atoms, we only create the
                // atropisomer if the rings are 3x ortho substituted
                // CC1=CC=CC(C)=C1-C1=C(C)C=CC=C1C yes (sum1=9,sum2=9)
                // CC1=CC=CC=C1-C1=C(C)C=CC=C1C yes    (sum1=8,sum2=9)
                // CC1=CC=CC(C)=C1-C1=CC=CC=C1 no      (sum1=7,sum2=9)
                // CC1=CC=CC=C1-C1=C(C)C=CC=C1 no      (sum1=8,sum2=8)
                int sum1 = graph[graph[u][0]].Length +
                        graph[graph[u][1]].Length +
                        graph[graph[u][2]].Length;
                int sum2 = graph[graph[v][0]].Length +
                           graph[graph[v][1]].Length +
                           graph[graph[v][2]].Length;
                if (sum1 > 9 || sum1 < 8)
                    return null;
                if (sum2 > 9 || sum2 < 8)
                    return null;
                if (sum1 + sum2 < 17)
                    return null;

                var carriers = new IAtom[4];
                var elevation = new int[4];

                int n = 0;
                foreach (int w in graph[u])
                {
                    IBond bond = bondMap[u, w];
                    if (w == v)
                        continue;
                    if (IsUnspecified(bond))
                        return null;

                    carriers[n] = container.Atoms[w];
                    elevation[n] = ElevationOf(end1, bond);

                    foreach (int w2 in graph[w])
                    {
                        if (IsHydrogen(container.Atoms[w2]))
                            sum1--;
                        else if (elevation[n] == 0 &&
                                 IsWedged(bondMap[w, w2]))
                        {
                            elevation[n] = ElevationOf(container.Atoms[w], bondMap[w, w2]);
                        }
                    }

                    n++;
                }
                n = 2;
                foreach (int w in graph[v])
                {
                    IBond bond = bondMap[v, w];
                    if (w == u) continue;
                    if (IsUnspecified(bond))
                        return null;

                    carriers[n] = container.Atoms[w];
                    elevation[n] = ElevationOf(end2, bond);

                    foreach (int w2 in graph[w])
                    {
                        if (IsHydrogen(container.Atoms[w2]))
                            sum2--;
                        else if (elevation[n] == 0 &&
                                 IsWedged(bondMap[w, w2]))
                        {
                            elevation[n] = ElevationOf(container.Atoms[w], bondMap[w, w2]);
                        }
                    }

                    n++;
                }

                if (n != 4)
                    return null;

                // recheck now we have accounted for explicit hydrogens
                if (sum1 > 9 || sum1 < 8)
                    return null;
                if (sum2 > 9 || sum2 < 8)
                    return null;
                if (sum1 + sum2 < 17)
                    return null;

                if (elevation[0] != 0 || elevation[1] != 0)
                {
                    if (elevation[2] != 0 || elevation[3] != 0)
                        return null;
                }
                else
                {
                    if (elevation[2] == 0 && elevation[3] == 0)
                        return null; // undefined configuration
                }

                IAtom tmp = end1.Builder.NewAtom();
                tmp.Point2D = new Vector2((end1.Point2D.Value.X + end2.Point2D.Value.X) / 2,
                                           (end2.Point2D.Value.Y + end2.Point2D.Value.Y) / 2);
                int parity = Parity(tmp, carriers, elevation);
                var cfg = parity > 0 ? StereoConfigurations.Left : StereoConfigurations.Right;

                return new Atropisomeric(container.GetBond(end1, end2), carriers, cfg);
            }

            /// <inheritdoc/>
            public override IDoubleBondStereochemistry CreateGeometric(int u, int v, Stereocenters stereocenters)
            {
                if (HasUnspecifiedParity(container.Atoms[u]) ||
                 HasUnspecifiedParity(container.Atoms[v]))
                    return null;

                int[] us = graph[u];
                int[] vs = graph[v];

                if (us.Length < 2 || us.Length > 3 || vs.Length < 2 || vs.Length > 3)
                    return null;

                // move pi bonded neighbors to back
                MoveToBack(us, v);
                MoveToBack(vs, u);

                IAtom[] vAtoms = new IAtom[]{container.Atoms[us[0]],
                                         container.Atoms[us.Length > 2 ? us[1] : u],
                                         container.Atoms[v]};
                IAtom[] wAtoms = new IAtom[]{container.Atoms[vs[0]],
                                         container.Atoms[vs.Length > 2 ? vs[1] : v],
                                         container.Atoms[u]};

                // are any substituents a wavy unspecified bond
                if (IsUnspecified(bondMap[u, us[0]]) || IsUnspecified(bondMap[u, us[1]])
                        || IsUnspecified(bondMap[v, vs[0]]) || IsUnspecified(bondMap[v, vs[1]]))
                    return null;

                int parity = Parity(vAtoms) * Parity(wAtoms);
                DoubleBondConformation conformation = parity > 0 ? DoubleBondConformation.Opposite : DoubleBondConformation.Together;

                if (parity == 0)
                    return null;

                IBond bond = bondMap[u, v];

                // crossed bond
                if (IsUnspecified(bond))
                    return null;

                // put the bond in to v is the first neighbor
                bond.SetAtoms(new[] { container.Atoms[u], container.Atoms[v] });

                return new DoubleBondStereochemistry(bond, new IBond[] { bondMap[u, us[0]], bondMap[v, vs[0]] }, conformation);
            }

            /// <inheritdoc/>
            public override ExtendedTetrahedral CreateExtendedTetrahedral(int v, Stereocenters stereocenters)
            {
                var focus = container.Atoms[v];

                if (HasUnspecifiedParity(focus))
                    return null;

                var terminals = ExtendedTetrahedral.FindTerminalAtoms(container, focus);

                int t0 = container.Atoms.IndexOf(terminals[0]);
                int t1 = container.Atoms.IndexOf(terminals[1]);

                if (stereocenters.IsSymmetryChecked() 
                 && (!stereocenters.IsStereocenter(t0) 
                  || !stereocenters.IsStereocenter(t1)))
                    return null;

                var neighbors = new IAtom[4];
                var elevation = new int[4];

                neighbors[1] = terminals[0];
                neighbors[3] = terminals[1];

                int n = 0;
                foreach (var w in graph[t0])
                {
                    var bond = bondMap[t0, w];
                    if (w == v)
                        continue;
                    if (bond.Order != BondOrder.Single)
                        continue;
                    if (IsUnspecified(bond))
                        return null;
                    neighbors[n] = container.Atoms[w];
                    elevation[n] = ElevationOf(terminals[0], bond);
                    n++;
                }
                if (n == 0)
                    return null;
                n = 2;
                foreach (var w in graph[t1])
                {
                    var bond = bondMap[t1, w];
                    if (bond.Order != BondOrder.Single)
                        continue;
                    if (IsUnspecified(bond))
                        return null;
                    neighbors[n] = container.Atoms[w];
                    elevation[n] = ElevationOf(terminals[1], bond);
                    n++;
                }
                if (n == 2)
                    return null;
                if (elevation[0] != 0 || elevation[1] != 0)
                {
                    if (elevation[2] != 0 || elevation[3] != 0)
                        return null;
                }
                else
                {
                    if (elevation[2] == 0 && elevation[3] == 0)
                        return null; // undefined configuration
                }

                var parity = Parity(focus, neighbors, elevation);

                var winding = parity > 0 ? TetrahedralStereo.AntiClockwise : TetrahedralStereo.Clockwise;

                return new ExtendedTetrahedral(focus, neighbors, winding);
            }

            public override ExtendedCisTrans CreateExtendedCisTrans(IReadOnlyList<IBond> dbs, Stereocenters stereocenters)
            {
                // only applies to odd-counts
                if ((dbs.Count & 0x1) == 0)
                    return null;
                var focus = dbs[dbs.Count / 2];
                var carriers = new IBond[2];
                var config = StereoConfigurations.Unset;
                var begAtom = dbs[0].GetOther(GetShared(dbs[0], dbs[1]));
                var endAtom = dbs[dbs.Count - 1].GetOther(GetShared(dbs[dbs.Count - 1], dbs[dbs.Count - 2]));
                var begBonds = container.GetConnectedBonds(begAtom).ToList();
                var endBonds = container.GetConnectedBonds(endAtom).ToList();
                if (stereocenters.IsSymmetryChecked() &&
                   (!stereocenters.IsStereocenter(container.Atoms.IndexOf(begAtom)) ||
                    !stereocenters.IsStereocenter(container.Atoms.IndexOf(endAtom))))
                    return null;
                if (begBonds.Count < 2 || endBonds.Count < 2)
                    return null;
                begBonds.Remove(dbs[0]);
                endBonds.Remove(dbs[dbs.Count - 1]);
                var ends = ExtendedCisTrans.FindTerminalAtoms(container, focus);
                Trace.Assert(ends != null);
                if (ends[0].Equals(begAtom))
                {
                    carriers[0] = begBonds[0];
                    carriers[1] = endBonds[0];
                }
                else
                {
                    carriers[1] = begBonds[0];
                    carriers[0] = endBonds[0];
                }
                var begNbr = begBonds[0].GetOther(begAtom);
                var endNbr = endBonds[0].GetOther(endAtom);
                var begVec = new Vector2(begNbr.Point2D.Value.X - begAtom.Point2D.Value.X,
                                         begNbr.Point2D.Value.Y - begAtom.Point2D.Value.Y);
                var endVec = new Vector2(endNbr.Point2D.Value.X - endAtom.Point2D.Value.X,
                                         endNbr.Point2D.Value.Y - endAtom.Point2D.Value.Y);
                begVec = Vector2.Normalize(begVec);
                endVec = Vector2.Normalize(endVec);
                var dot = Vector2.Dot(begVec, endVec);
                if (dot < 0)
                    config = StereoConfigurations.Opposite;
                else
                    config = StereoConfigurations.Together;
                return new ExtendedCisTrans(focus, carriers, config);
            }

            /// <summary>
            /// Is the provided bond have an unspecified stereo label.
            /// </summary>
            /// <param name="bond">a bond</param>
            /// <returns>the bond has unspecified stereochemistry</returns>
            private static bool IsUnspecified(IBond bond)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.UpOrDown:
                    case BondStereo.UpOrDownInverted:
                    case BondStereo.EOrZ:
                        return true;
                    default:
                        return false;
                }
            }

            /// <summary>
            /// Parity computation for one side of a double bond in a geometric center.
            /// </summary>
            /// <param name="atoms">atoms around the double bonded atom, 0: substituent, 1:
            ///              other substituent (or focus), 2: double bonded atom</param>
            /// <returns>the parity of the atoms</returns>
            private static int Parity(IAtom[] atoms)
            {
                if (atoms.Length != 3)
                    throw new ArgumentException("incorrect number of atoms");

                Vector2? a = atoms[0].Point2D;
                Vector2? b = atoms[1].Point2D;
                Vector2? c = atoms[2].Point2D;

                if (a == null || b == null || c == null)
                    return 0;

                double det = Det(a.Value.X, a.Value.Y, b.Value.X, b.Value.Y, c.Value.X, c.Value.Y);

                // unspecified by coordinates
                if (Math.Abs(det) < Threshold)
                    return 0;

                return Math.Sign(det);
            }

            /// <summary>
            /// Parity computation for 2D tetrahedral stereocenters.
            /// </summary>
            /// <param name="atoms">the atoms surrounding the central focus atom</param>
            /// <param name="elevations">the elevations of each atom</param>
            /// <returns>the parity (winding)</returns>
            private static int Parity(IAtom focus, IAtom[] atoms, int[] elevations)
            {
                if (atoms.Length != 4)
                    throw new ArgumentException("incorrect number of atoms");

                var coordinates = new Vector2[atoms.Length];
                for (int i = 0; i < atoms.Length; i++)
                {
                    var atoms_i_Point2D = atoms[i].Point2D;
                    if (atoms_i_Point2D == null)
                        return 0;
                    coordinates[i] = ToUnitVector(focus.Point2D.Value, atoms_i_Point2D.Value);
                }

                var det = Parity(coordinates, elevations);

                return Math.Sign(det);
            }

            /// <summary>
            /// Obtain the unit vector between two points.
            /// </summary>
            /// <param name="from">the base of the vector</param>
            /// <param name="to">the point of the vector</param>
            /// <returns>the unit vector</returns>
            private static Vector2 ToUnitVector(Vector2 from, Vector2 to)
            {
                if (from == to)
                    return Vector2.Zero;
                var v2d = new Vector2(to.X - from.X, to.Y - from.Y);
                return Vector2.Normalize(v2d);
            }

            /// <summary>
            /// Compute the signed volume of the tetrahedron from the planar points
            /// and elevations.
            /// </summary>
            /// <param name="coordinates">locations in the plane</param>
            /// <param name="elevations">elevations above/below the plane</param>
            /// <returns>the determinant (signed volume of tetrahedron)</returns>
            private static double Parity(Vector2[] coordinates, int[] elevations)
            {
                double x1 = coordinates[0].X;
                double x2 = coordinates[1].X;
                double x3 = coordinates[2].X;
                double x4 = coordinates[3].X;

                double y1 = coordinates[0].Y;
                double y2 = coordinates[1].Y;
                double y3 = coordinates[2].Y;
                double y4 = coordinates[3].Y;

                return (elevations[0] * Det(x2, y2, x3, y3, x4, y4)) - (elevations[1] * Det(x1, y1, x3, y3, x4, y4))
                     + (elevations[2] * Det(x1, y1, x2, y2, x4, y4)) - (elevations[3] * Det(x1, y1, x2, y2, x3, y3));
            }

            /// <summary>3x3 determinant helper for a constant third column</summary>
            private static double Det(double xa, double ya, double xb, double yb, double xc, double yc)
            {
                return (xa - xc) * (yb - yc) - (ya - yc) * (xb - xc);
            }

            /// <summary>
            /// Utility find the specified value, <paramref name="v"/>, in the array of values,
            /// <paramref name="vs"/> and moves it to the back.
            /// </summary>
            /// <param name="vs">an array of values (containing v)</param>
            /// <param name="v">a value</param>
            private static void MoveToBack(int[] vs, int v)
            {
                for (int i = 0; i < vs.Length; i++)
                {
                    if (vs[i] == v)
                    {
                        Array.Copy(vs, i + 1, vs, i + 1 - 1, vs.Length - (i + 1));
                        vs[vs.Length - 1] = v;
                        return;
                    }
                }
            }

            /// <summary>
            /// Obtain the elevation of an atom connected to the <paramref name="focus"/> by the
            /// specified <paramref name="bond"/>.
            /// </summary>
            /// <param name="focus">a focus of stereochemistry</param>
            /// <param name="bond">a bond connecting the focus to a substituent</param>
            /// <returns>the elevation of the connected atom, +1 above, -1 below, 0 planar</returns>
            private static int ElevationOf(IAtom focus, IBond bond)
            {
                switch (bond.Stereo)
                {
                    case BondStereo.Up:
                        return bond.Begin == focus ? +1 : 0;
                    case BondStereo.UpInverted:
                        return bond.End == focus ? +1 : 0;
                    case BondStereo.Down:
                        return bond.Begin == focus ? -1 : 0;
                    case BondStereo.DownInverted:
                        return bond.End == focus ? -1 : 0;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>Create stereo-elements from 3D coordinates.</summary>
        private sealed class StereoElementFactory3D : StereoElementFactory
        {
            /// <summary>
            /// Create a new stereo-element factory for the specified structure.
            /// </summary>
            /// <param name="container">native CDK structure representation</param>
            /// <param name="graph">adjacency list representation</param>
            /// <param name="bondMap">fast bond lookup from atom indices</param>
            public StereoElementFactory3D(IAtomContainer container, int[][] graph, EdgeToBondMap bondMap)
                : base(container, graph, bondMap)
            {
            }

            /// <inheritdoc/>
            public override ITetrahedralChirality CreateTetrahedral(IAtom atom, Stereocenters stereocenters)
            {
                return CreateTetrahedral(container.Atoms.IndexOf(atom), stereocenters);
            }

            /// <inheritdoc/>
            public override IDoubleBondStereochemistry CreateGeometric(IBond bond, Stereocenters stereocenters)
            {
                return CreateGeometric(container.Atoms.IndexOf(bond.Begin), container.Atoms.IndexOf(bond.End), stereocenters);
            }

            /// <inheritdoc/>
            public override ITetrahedralChirality CreateTetrahedral(int v, Stereocenters stereocenters)
            {
                if (!stereocenters.IsStereocenter(v)) return null;

                IAtom focus = container.Atoms[v];

                if (HasUnspecifiedParity(focus)) return null;

                IAtom[] neighbors = new IAtom[4];

                neighbors[3] = focus;

                int n = 0;

                foreach (var w in graph[v])
                    neighbors[n++] = container.Atoms[w];

                // too few/many neighbors
                if (n < 3 || n > 4) return null;

                // TODO: verify valid wedge/hatch configurations using similar procedure
                // to NonPlanarBonds in the cdk-sdg package

                int parity = Parity(neighbors);

                TetrahedralStereo winding = parity > 0 ? TetrahedralStereo.AntiClockwise : TetrahedralStereo.Clockwise;

                return new TetrahedralChirality(focus, neighbors, winding);
            }

            public override IStereoElement<IChemObject, IChemObject> CreateAtropisomer(int u, int v, Stereocenters stereocenters)
            {
                IAtom end1 = container.Atoms[u];
                IAtom end2 = container.Atoms[v];

                if (HasUnspecifiedParity(end1) || HasUnspecifiedParity(end2))
                    return null;

                if (graph[u].Length != 3 || graph[v].Length != 3)
                    return null;

                // check degrees of connected atoms, we only create the
                // atropisomer if the rings are 3x ortho substituted
                // CC1=CC=CC(C)=C1-C1=C(C)C=CC=C1C yes (sum1=9,sum2=9)
                // CC1=CC=CC=C1-C1=C(C)C=CC=C1C yes    (sum1=8,sum2=9)
                // CC1=CC=CC(C)=C1-C1=CC=CC=C1 no      (sum1=7,sum2=9)
                // CC1=CC=CC=C1-C1=C(C)C=CC=C1 no      (sum1=8,sum2=8)
                int sum1 = graph[graph[u][0]].Length +
                           graph[graph[u][1]].Length +
                           graph[graph[u][2]].Length;
                int sum2 = graph[graph[v][0]].Length +
                           graph[graph[v][1]].Length +
                           graph[graph[v][2]].Length;

                if (sum1 > 9 || sum1 < 8)
                    return null;
                if (sum2 > 9 || sum2 < 8)
                    return null;
                if (sum1 + sum2 < 17)
                    return null;

                var carriers = new IAtom[4];

                int n = 0;
                foreach (int w in graph[u])
                {
                    if (w == v)
                        continue;

                    carriers[n] = container.Atoms[w];

                    foreach (int w2 in graph[w])
                    {
                        if (IsHydrogen(container.Atoms[w2]))
                            sum1--;
                    }

                    n++;
                }
                n = 2;
                foreach (int w in graph[v])
                {
                    if (w == u)
                        continue;

                    carriers[n] = container.Atoms[w];

                    foreach (int w2 in graph[w])
                    {
                        if (IsHydrogen(container.Atoms[w2]))
                            sum2--;
                    }

                    n++;
                }

                if (n != 4)
                    return null;

                // recheck now we have account for explicit hydrogens
                if (sum1 > 9 || sum1 < 8)
                    return null;
                if (sum2 > 9 || sum2 < 8)
                    return null;
                if (sum1 + sum2 < 17)
                    return null;

                int parity = Parity(carriers);
                var cfg = parity > 0 ? StereoConfigurations.Left : StereoConfigurations.Right;

                return new Atropisomeric(container.GetBond(end1, end2), carriers, cfg);
            }

            /// <inheritdoc/>
            public override IDoubleBondStereochemistry CreateGeometric(int u, int v, Stereocenters stereocenters)
            {
                if (HasUnspecifiedParity(container.Atoms[u]) || HasUnspecifiedParity(container.Atoms[v]))
                    return null;

                int[] us = graph[u];
                int[] vs = graph[v];

                int x = us[0] == v ? us[1] : us[0];
                int w = vs[0] == u ? vs[1] : vs[0];

                var uAtom = container.Atoms[u];
                var vAtom = container.Atoms[v];
                var uSubstituentAtom = container.Atoms[x];
                var vSubstituentAtom = container.Atoms[w];

                if (uAtom.Point3D == null || vAtom.Point3D == null || uSubstituentAtom.Point3D == null || vSubstituentAtom.Point3D == null)
                    return null;

                int parity = Parity(uAtom.Point3D.Value, vAtom.Point3D.Value, uSubstituentAtom.Point3D.Value, vSubstituentAtom.Point3D.Value);

                var conformation = parity > 0 ? DoubleBondConformation.Opposite : DoubleBondConformation.Together;

                var bond = bondMap[u, v];
                bond.SetAtoms(new[] { uAtom, vAtom });

                return new DoubleBondStereochemistry(bond, new IBond[] { bondMap[u, x], bondMap[v, w], }, conformation);
            }

            /// <inheritdoc/>
            public override ExtendedTetrahedral CreateExtendedTetrahedral(int v, Stereocenters stereocenters)
            {
                var focus = container.Atoms[v];

                if (HasUnspecifiedParity(focus))
                    return null;

                var terminals = ExtendedTetrahedral.FindTerminalAtoms(container, focus);
                var neighbors = new IAtom[4];

                var t0 = container.Atoms.IndexOf(terminals[0]);
                var t1 = container.Atoms.IndexOf(terminals[1]);

                // check for kinked cumulated bond
                if (!IsColinear(focus, terminals))
                    return null;

                neighbors[1] = terminals[0];
                neighbors[3] = terminals[1];

                int n = 0;
                foreach (var w in graph[t0])
                {
                    if (bondMap[t0, w].Order != BondOrder.Single)
                        continue;
                    neighbors[n++] = container.Atoms[w];
                }
                if (n == 0)
                    return null;
                n = 2;
                foreach (var w in graph[t1])
                {
                    if (bondMap[t1, w].Order != BondOrder.Single)
                        continue;
                    neighbors[n++] = container.Atoms[w];
                }
                if (n == 2)
                    return null;

                var parity = Parity(neighbors);

                var winding = parity > 0 ? TetrahedralStereo.AntiClockwise : TetrahedralStereo.Clockwise;

                return new ExtendedTetrahedral(focus, neighbors, winding);
            }

            public override ExtendedCisTrans CreateExtendedCisTrans(IReadOnlyList<IBond> dbs, Stereocenters centers)
            {
                // only applies to odd-counts
                if ((dbs.Count & 0x1) == 0)
                    return null;
                var focus = dbs[dbs.Count / 2];
                var carriers = new IBond[2];
                var config = StereoConfigurations.Unset;
                var begAtom = dbs[0].GetOther(GetShared(dbs[0], dbs[1]));
                var endAtom = dbs[dbs.Count - 1].GetOther(GetShared(dbs[dbs.Count - 1], dbs[dbs.Count - 2]));
                var begBonds = container.GetConnectedBonds(begAtom).ToList();
                var endBonds = container.GetConnectedBonds(endAtom).ToList();
                if (begBonds.Count < 2 || endBonds.Count < 2)
                    return null;
                begBonds.Remove(dbs[0]);
                endBonds.Remove(dbs[dbs.Count - 1]);
                var ends = ExtendedCisTrans.FindTerminalAtoms(container, focus);
                Trace.Assert(ends != null);
                if (ends[0].Equals(begAtom))
                {
                    carriers[0] = begBonds[0];
                    carriers[1] = endBonds[0];
                }
                else
                {
                    carriers[1] = begBonds[0];
                    carriers[0] = endBonds[0];
                }
                var begNbr = begBonds[0].GetOther(begAtom);
                var endNbr = endBonds[0].GetOther(endAtom);
                var begVec = new Vector3(begNbr.Point3D.Value.X - begAtom.Point3D.Value.X,
                                         begNbr.Point3D.Value.Y - begAtom.Point3D.Value.Y,
                                         begNbr.Point3D.Value.Z - begAtom.Point3D.Value.Z);
                var endVec = new Vector3(endNbr.Point3D.Value.X - endAtom.Point3D.Value.X,
                                         endNbr.Point3D.Value.Y - endAtom.Point3D.Value.Y,
                                         endNbr.Point3D.Value.Z - endAtom.Point3D.Value.Z);
                begVec = Vector3.Normalize(begVec);
                endVec = Vector3.Normalize(endVec);
                var dot = Vector3.Dot(begVec, endVec);
                if (dot < 0)
                    config = StereoConfigurations.Opposite;
                else
                    config = StereoConfigurations.Together;
                return new ExtendedCisTrans(focus, carriers, config);
            }

            private static bool IsColinear(IAtom focus, IAtom[] terminals)
            {
                var vec0 = new Vector3(terminals[0].Point3D.Value.X - focus.Point3D.Value.X,
                                       terminals[0].Point3D.Value.Y - focus.Point3D.Value.Y,
                                       terminals[0].Point3D.Value.Z - focus.Point3D.Value.Z);
                var vec1 = new Vector3(terminals[1].Point3D.Value.X - focus.Point3D.Value.X,
                                       terminals[1].Point3D.Value.Y - focus.Point3D.Value.Y,
                                       terminals[1].Point3D.Value.Z - focus.Point3D.Value.Z);
                vec0 = Vector3.Normalize(vec0);
                vec1 = Vector3.Normalize(vec1);
                return Math.Abs(Vector3.Dot(vec0, vec1) + 1) < 0.05;
            }

            /// <summary>3x3 determinant helper for a constant third column</summary>
            private static double Det(double xa, double ya, double xb, double yb, double xc, double yc)
            {
                return (xa - xc) * (yb - yc) - (ya - yc) * (xb - xc);
            }

            /// <summary>
            /// Parity computation for one side of a double bond in a geometric center.
            /// The method needs the 3D coordinates of the double bond atoms (first 2
            /// arguments) and the coordinates of two substituents (one at each end).
            /// </summary>
            /// <param name="u">an atom double bonded to v</param>
            /// <param name="v">an atom double bonded to u</param>
            /// <param name="x">an atom sigma bonded to u</param>
            /// <param name="w">an atom sigma bonded to v</param>
            /// <returns>the parity of the atoms</returns>
            private static int Parity(Vector3 u, Vector3 v, Vector3 x, Vector3 w)
            {
                // create three vectors, v->u, v->w and u->x
                double[] vu = ToVector(v, u);
                double[] vw = ToVector(v, w);
                double[] ux = ToVector(u, x);

                // normal vector (to compare against), the normal vector (n) looks like:
                // x     n w
                //  \    |/
                //   u = v
                double[] normal = CrossProduct(vu, CrossProduct(vu, vw));

                // compare the dot products of v->w and u->x, if the signs are the same
                // they are both pointing the same direction. if a value is close to 0
                // then it is at pi/2 radians (i.e. unspecified) however 3D coordinates
                // are generally discrete and do not normally represent on unspecified
                // stereo configurations so we don't check this
                int parity = (int)Math.Sign(Dot(normal, vw)) * (int)Math.Sign(Dot(normal, ux));

                // invert sign, this then matches with Sp2 double bond parity
                return parity * -1;
            }

            /// <summary>
            /// Parity computation for 3D tetrahedral stereocenters.
            /// </summary>
            /// <param name="atoms">the atoms surrounding the central focus atom</param>
            /// <returns>the parity (winding)</returns>
            private static int Parity(IAtom[] atoms)
            {
                if (atoms.Length != 4) throw new ArgumentException("incorrect number of atoms");

                Vector3[] coordinates = new Vector3[atoms.Length];
                for (int i = 0; i < atoms.Length; i++)
                {
                    var c = atoms[i].Point3D;
                    if (c == null) return 0;
                    coordinates[i] = c.Value;
                }

                double x1 = coordinates[0].X;
                double x2 = coordinates[1].X;
                double x3 = coordinates[2].X;
                double x4 = coordinates[3].X;

                double y1 = coordinates[0].Y;
                double y2 = coordinates[1].Y;
                double y3 = coordinates[2].Y;
                double y4 = coordinates[3].Y;

                double z1 = coordinates[0].Z;
                double z2 = coordinates[1].Z;
                double z3 = coordinates[2].Z;
                double z4 = coordinates[3].Z;

                double det = (z1 * Det(x2, y2, x3, y3, x4, y4)) - (z2 * Det(x1, y1, x3, y3, x4, y4))
                        + (z3 * Det(x1, y1, x2, y2, x4, y4)) - (z4 * Det(x1, y1, x2, y2, x3, y3));

                return (int)Math.Sign(det);
            }

            /// <summary>
            /// Create a vector by specifying the source and destination coordinates.
            /// </summary>
            /// <param name="src">start point of the vector</param>
            /// <param name="dest">end point of the vector</param>
            /// <returns>a new vector</returns>
            private static double[] ToVector(Vector3 src, Vector3 dest)
            {
                return new double[] { dest.X - src.X, dest.Y - src.Y, dest.Z - src.Z };
            }

            /// <summary>
            /// Dot product of two 3D coordinates
            /// </summary>
            /// <param name="u">either 3D coordinates</param>
            /// <param name="v">other 3D coordinates</param>
            /// <returns>the dot-product</returns>
            private static double Dot(double[] u, double[] v)
            {
                return (u[0] * v[0]) + (u[1] * v[1]) + (u[2] * v[2]);
            }

            /// <summary>
            /// Cross product of two 3D coordinates
            /// </summary>
            /// <param name="u">either 3D coordinates</param>
            /// <param name="v">other 3D coordinates</param>
            /// <returns>the cross-product</returns>
            private static double[] CrossProduct(double[] u, double[] v)
            {
                return new double[] { (u[1] * v[2]) - (v[1] * u[2]), (u[2] * v[0]) - (v[2] * u[0]), (u[0] * v[1]) - (v[0] * u[1]) };
            }
        }

        private static bool IsHydrogen(IAtom atom)
        {
            return atom.AtomicNumber == 1;
        }
    }
}
