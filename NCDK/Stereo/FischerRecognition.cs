/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
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
using NCDK.RingSearches;
using System;
using System.Collections.Generic;

namespace NCDK.Stereo
{
    /// <summary>
    /// Recognize the configuration of tetrahedral stereocenters depicted as
    /// Fischer projection. Fischer projection is a convenient means of depicting
    /// 3D geometry commonly used in depicting carbohydrates. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fischer projection depicts tetrahedral stereocenters as though they were 
    /// coplanar with the four substituents at cardinal directions (up,right,down, 
    /// and left). The horizontal bonds (right and left) are interpreted as pointing
    /// out of the plane towards the viewer; They are not depicted with non-planar
    /// wedge bonds.</para>
    /// <para>
    /// This class provides the recognition of Fischer projections. Each asymmetric
    /// carbon is checked as to whether it's 2D depiction is coplanar with cardinal
    /// directions. All of these bonds must be planar (i.e. not wedge or hatch) and
    /// sigma bonds. In a hydrogen suppressed representation, one of the left or 
    /// right bonds (to the implied hydrogen) may be omitted but can be correctly
    /// interpreted.</para>
    /// </remarks>
    /// <seealso href="http://en.wikipedia.org/wiki/Fischer_projection">Fischer projection (Wikipedia)</seealso>
    // @author John May
    internal sealed class FischerRecognition
    {
        /// <summary>
        /// The threshold at which to snap bonds to the cardinal direction. The
        /// threshold allows bonds slightly of absolute directions to be interpreted.
        /// The tested vector is of unit length and so the threshold is simply the
        /// angle (in radians).
        /// </summary>
        public const double CARDINALITY_THRESHOLD = 5.0 / 180 * Math.PI; // 5 degrees in radians

        /// <summary>Cardinal direction, North index.</summary>
        public const int NORTH = 0;

        /// <summary>Cardinal direction, East index.</summary>
        public const int EAST = 1;

        /// <summary>Cardinal direction, South index.</summary>
        public const int SOUTH = 2;

        /// <summary>Cardinal direction, West index.</summary>
        public const int WEST = 3;

        private readonly IAtomContainer container;
        private readonly int[][] graph;
        private readonly EdgeToBondMap bonds;
        private readonly Stereocenters stereocenters;

        /// <summary>
        /// Required information to recognise stereochemistry.
        /// </summary>
        /// <param name="container">input structure</param>
        /// <param name="graph">adjacency list representation</param>
        /// <param name="bonds">edge to bond index</param>
        /// <param name="stereocenters">location and type of asymmetries</param>
        public FischerRecognition(IAtomContainer container,
                           int[][] graph,
                           EdgeToBondMap bonds,
                           Stereocenters stereocenters)
        {
            this.container = container;
            this.graph = graph;
            this.bonds = bonds;
            this.stereocenters = stereocenters;
        }

        /// <summary>
        /// Recognise the tetrahedral stereochemistry in the provided structure.
        /// </summary>
        /// <param name="projections">allowed projection types</param>
        /// <returns>zero of more stereo elements</returns>
        public IEnumerable<IStereoElement<IChemObject, IChemObject>> Recognise(ICollection<Projection> projections)
        {
            if (!projections.Contains(Projection.Fischer))
                return Array.Empty<IStereoElement<IChemObject, IChemObject>>();

            // build atom index and only recognize 2D depictions
            var atomToIndex = new Dictionary<IAtom, int>();
            foreach (var atom in container.Atoms)
            {
                if (atom.Point2D == null)
                    return Array.Empty<IStereoElement<IChemObject, IChemObject>>();
                atomToIndex.Add(atom, atomToIndex.Count);
            }

            var ringSearch = new RingSearch(container, graph);

            var elements = new List<IStereoElement<IChemObject, IChemObject>>(5);

            for (int v = 0; v < container.Atoms.Count; v++)
            {
                var focus = container.Atoms[v];
                if (!focus.AtomicNumber.Equals(AtomicNumbers.Carbon))
                    continue;
                if (ringSearch.Cyclic(v))
                    continue;
                if (stereocenters.ElementType(v) != CoordinateType.Tetracoordinate)
                    continue;
                if (!stereocenters.IsStereocenter(v))
                    continue;

                var element = NewTetrahedralCenter(focus, Neighbors(v, graph, bonds));

                if (element == null)
                    continue;

                // east/west bonds must be to terminal atoms
                var east = element.Ligands[EAST];
                var west = element.Ligands[WEST];

                if (east != focus && !IsTerminal(east, atomToIndex))
                    continue;
                if (west != focus && !IsTerminal(west, atomToIndex))
                    continue;

                elements.Add(element);
            }

            return elements;
        }


        /// <summary>
        /// Create a new tetrahedral stereocenter of the given focus and neighboring
        /// bonds. This is an internal method and is presumed the atom can support
        /// tetrahedral stereochemistry and it has three or four explicit neighbors. 
        /// </summary>
        /// <remarks>
        /// The stereo element is only created if the local arrangement looks like
        /// a Fischer projection. 
        /// </remarks>
        /// <param name="focus">central atom</param>
        /// <param name="bonds">adjacent bonds</param>
        /// <returns>a stereo element, or null if one could not be created</returns>
        public static ITetrahedralChirality NewTetrahedralCenter(IAtom focus, IBond[] bonds)
        {
            // obtain the bonds of a centre arranged by cardinal direction 
            var cardinalBonds = CardinalBonds(focus, bonds);

            if (cardinalBonds == null)
                return null;

            // vertical bonds must be present and be sigma and planar (no wedge/hatch)
            if (!IsPlanarSigmaBond(cardinalBonds[NORTH]) || !IsPlanarSigmaBond(cardinalBonds[SOUTH]))
                return null;

            // one of the horizontal bonds can be missing but not both
            if (cardinalBonds[EAST] == null && cardinalBonds[WEST] == null)
                return null;

            // the neighbors of our tetrahedral centre, the EAST or WEST may
            // be missing so we initialise these with the implicit (focus)
            var neighbors = new IAtom[]{cardinalBonds[NORTH].GetOther(focus),
                                            focus,
                                            cardinalBonds[SOUTH].GetOther(focus),
                                            focus};

            // fill in the EAST/WEST bonds, if they are define, single and planar we add the
            // connected atom. else if bond is defined (but not single or planar) or we
            // have 4 neighbours something is wrong and we skip this atom                
            if (IsPlanarSigmaBond(cardinalBonds[EAST]))
            {
                neighbors[EAST] = cardinalBonds[EAST].GetOther(focus);
            }
            else if (cardinalBonds[EAST] != null || bonds.Length == 4)
            {
                return null;
            }

            if (IsPlanarSigmaBond(cardinalBonds[WEST]))
            {
                neighbors[WEST] = cardinalBonds[WEST].GetOther(focus);
            }
            else if (cardinalBonds[WEST] != null || bonds.Length == 4)
            {
                return null;
            }

            return new TetrahedralChirality(focus, neighbors, TetrahedralStereo.AntiClockwise);
        }

        /// <summary>
        /// Arrange the bonds adjacent to an atom (focus) in cardinal direction. The
        /// cardinal directions are that of a compass. Bonds are checked as to
        /// whether they are horizontal or vertical within a predefined threshold.
        /// </summary>
        /// <param name="focus">an atom</param>
        /// <param name="bonds">bonds adjacent to the atom</param>
        /// <returns>array of bonds organised (N,E,S,W), or null if a bond was found
        /// that exceeded the threshold</returns>
        public static IBond[] CardinalBonds(IAtom focus, IBond[] bonds)
        {
            var centerXy = focus.Point2D.Value;
            var cardinal = new IBond[4];

            foreach (var bond in bonds)
            {
                var other = bond.GetOther(focus);
                var otherXy = other.Point2D.Value;

                var deltaX = otherXy.X - centerXy.X;
                var deltaY = otherXy.Y - centerXy.Y;

                // normalise vector length so thresholds are independent 
                var mag = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                deltaX /= mag;
                deltaY /= mag;

                var absDeltaX = Math.Abs(deltaX);
                var absDeltaY = Math.Abs(deltaY);

                // assign the bond to the cardinal direction
                if (absDeltaX < CARDINALITY_THRESHOLD
                        && absDeltaY > CARDINALITY_THRESHOLD)
                {
                    cardinal[deltaY > 0 ? NORTH : SOUTH] = bond;
                }
                else if (absDeltaX > CARDINALITY_THRESHOLD
                        && absDeltaY < CARDINALITY_THRESHOLD)
                {
                    cardinal[deltaX > 0 ? EAST : WEST] = bond;
                }
                else
                {
                    return null;
                }
            }

            return cardinal;
        }

        /// <summary>
        /// Is the atom terminal having only one connection.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <param name="atomToIndex">a map of atoms to index</param>
        /// <returns>the atom is terminal</returns>
        private bool IsTerminal(IAtom atom, Dictionary<IAtom, int> atomToIndex)
        {
            return graph[atomToIndex[atom]].Length == 1;
        }

        /// <summary>
        /// Helper method determines if a bond is defined (not null) and whether
        /// it is a sigma (single) bond with no stereo attribute (wedge/hatch).
        /// </summary>
        /// <param name="bond">the bond to test</param>
        /// <returns>the bond is a planar sigma bond</returns>
        private static bool IsPlanarSigmaBond(IBond bond)
        {
            return bond != null 
                && BondOrder.Single.Equals(bond.Order)
                && BondStereo.None.Equals(bond.Stereo);
        }

        /// <summary>
        /// Helper method to obtain the neighbouring bonds from an adjacency list
        /// graph and edge->bond map.
        /// </summary>
        /// <param name="v">vertex</param>
        /// <param name="g">graph (adj list)</param>
        /// <param name="bondMap">map of edges to bonds</param>
        /// <returns>neighboring bonds</returns>
        private static IBond[] Neighbors(int v, int[][] g, EdgeToBondMap bondMap)
        {
            var ws = g[v];
            var bonds = new IBond[ws.Length];
            for (int i = 0; i < ws.Length; i++)
            {
                bonds[i] = bondMap[v, ws[i]];
            }
            return bonds;
        }
    }
}

