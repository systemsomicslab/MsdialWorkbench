/* Copyright (C) 2008-2009  Arvid Berg <goglepox@users.sf.net>
 *               2008-2009  Gilleain Torrance <gilleain@users.sf.net>
 *                    2009  Mark Rijnbeek <markr@ebi.ac.uk>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Geometries;
using NCDK.Graphs;
using NCDK.Numerics;
using NCDK.Renderers.Elements;
using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;
using System.Windows.Media;
using static NCDK.Renderers.Elements.WedgeLineElement;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generator for elements from bonds. Only two-atom bonds are supported by this generator.
    /// </summary>
    // @cdk.module renderbasic
    public class BasicBondGenerator : IGenerator<IAtomContainer>
    {
        /// <summary>
        /// Necessary for calculating inner-ring bond elements.
        /// </summary>
        private IRingSet ringSet;

        /// <summary>
        /// A hack to allow the HighlightGenerator to override the standard colors.
        /// Set it to non-null to have all bond-lines in this color.
        /// </summary>
        private Color? overrideColor = null;

        /// <summary>
        /// A similar story to the override color
        /// </summary>
        private double overrideBondWidth = -1;

        /// <summary>
        /// The ideal ring size for the given center proportion.
        /// </summary>
        private readonly int IDEAL_RINGSIZE = 6;

        /// <summary>
        /// The minimum ring size factor to ensure a minimum gap.
        /// </summary>
        private readonly double MIN_RINGSIZE_FACTOR = 2.5;

        /// <summary>
        /// An empty constructor necessary for reflection.
        /// </summary>
        public BasicBondGenerator() { }

        /// <summary>
        /// Set the color to use for all bonds, overriding the standard bond colors.
        /// </summary>
        /// <param name="color">the override color</param>
        public virtual void SetOverrideColor(Color? color)
        {
            this.overrideColor = color;
        }

        /// <summary>
        /// Set the width to use for all bonds, overriding any standard bond widths.
        /// <param name="bondWidth"></param>
        /// </summary>
        public virtual void SetOverrideBondWidth(double bondWidth)
        {
            this.overrideBondWidth = bondWidth;
        }

        /// <summary>
        /// Determine the ring set for this atom container.
        /// </summary>
        /// <param name="atomContainer">the atom container to find rings in.</param>
        /// <returns>the rings of the molecule</returns>
        protected virtual IRingSet GetRingSet(IAtomContainer atomContainer)
        {
            var ringSet = atomContainer.Builder.NewRingSet();
            try
            {
                var molecules = ConnectivityChecker.PartitionIntoMolecules(atomContainer);
                foreach (var mol in molecules)
                {
                    ringSet.Add(Cycles.FindSSSR(mol).ToRingSet());
                }

                return ringSet;
            }
            catch (Exception exception)
            {
                Trace.TraceWarning("Could not partition molecule: " + exception.Message);
                Debug.WriteLine(exception);
                return ringSet;
            }
        }

        /// <summary>
        /// Determine the color of a bond, returning either the default color,
        /// the override color or whatever is in the color hash for that bond.
        /// </summary>
        /// <param name="bond">the bond we are generating an element for</param>
        /// <param name="model">the rendering model</param>
        /// <returns>the color to paint the bond</returns>
        public virtual Color GetColorForBond(IBond bond, RendererModel model)
        {
            if (this.overrideColor != null)
            {
                return overrideColor.Value;
            }

            if (!model.GetColorHash().TryGetValue(bond, out Color color))
            {
                color = model.GetDefaultBondColor();
            }
            return color;
        }

        /// <summary>
        /// Determine the width of a bond, returning either the width defined
        /// in the model, or the override width. Note that this will be scaled
        /// to the space of the model.
        /// </summary>
        /// <param name="bond">the bond to determine the width for</param>
        /// <param name="model">the renderer model</param>
        /// <returns>a double in chem-model space</returns>
        public virtual double GetWidthForBond(IBond bond, RendererModel model)
        {
            var scale = model.GetScale();
            if (this.overrideBondWidth != -1)
            {
                return this.overrideBondWidth / scale;
            }
            else
            {
                return model.GetBondWidth() / scale;
            }
        }

        /// <inheritdoc/>
        public virtual IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            var group = new ElementGroup();
            this.ringSet = this.GetRingSet(container);

            //Sort the ringSet consistently to ensure consistent rendering.
            //If this is omitted, the bonds may 'tremble'.
            ringSet.Sort(new AtomContainerComparatorBy2DCenter<IRing>());

            foreach (var bond in container.Bonds)
            {
                var bondElement = this.Generate(bond, model);
                if (bondElement != null)
                    group.Add(MarkedElement.MarkupBond(bondElement, bond));
            }
            return group;
        }

        /// <summary>
        /// Generate rendering Element(s) for the current bond, including ring
        /// elements if this bond is part of a ring.
        /// </summary>
        /// <param name="currentBond">the bond to use when generating elements</param>
        /// <param name="model">the renderer model</param>
        /// <returns>one or more rendering elements</returns>
        public virtual IRenderingElement Generate(IBond currentBond, RendererModel model)
        {
            var ring = RingSetManipulator.GetHeaviestRing(ringSet, currentBond);
            if (ring != null)
            {
                return GenerateRingElements(currentBond, ring, model);
            }
            else
            {
                return GenerateBond(currentBond, model);
            }
        }

        /// <summary>
        /// Generate rendering elements for a bond, without ring elements but
        /// considering the type of the bond (single, double, triple).
        /// </summary>
        /// <param name="bond">the bond to use when generating elements</param>
        /// <param name="model">the renderer model</param>
        /// <returns>one or more rendering elements</returns>
        public virtual IRenderingElement GenerateBondElement(IBond bond, RendererModel model)
        {
            return GenerateBondElement(bond, bond.Order, model);
        }

        /// <summary>
        /// Generate a LineElement or an ElementGroup of LineElements for this bond.
        /// This version should be used if you want to override the type - for
        /// example, for ring double bonds.
        /// </summary>
        /// <param name="bond">the bond to generate for</param>
        /// <param name="type">the type of the bond - single, double, etc</param>
        /// <param name="model">the renderer model</param>
        /// <returns>one or more rendering elements</returns>
        public virtual IRenderingElement GenerateBondElement(IBond bond, BondOrder type, RendererModel model)
        {
            // More than 2 atoms per bond not supported by this module
            if (bond.Atoms.Count > 2)
                return null;

            // is object right? if not replace with a good one
            var point1 = bond.Begin.Point2D.Value;
            var point2 = bond.End.Point2D.Value;
            var color = this.GetColorForBond(bond, model);
            var bondWidth = this.GetWidthForBond(bond, model);
            var bondDistance = model.GetBondDistance() / model.GetScale();
            if (type == BondOrder.Single)
            {
                return new LineElement(ToPoint(point1), ToPoint(point2), bondWidth, color);
            }
            else
            {
                var group = new ElementGroup();
                switch (type)
                {
                    case BondOrder.Double:
                        CreateLines(point1, point2, bondWidth, bondDistance, color, group);
                        break;
                    case BondOrder.Triple:
                        CreateLines(point1, point2, bondWidth, bondDistance * 2, color, group);
                        group.Add(new LineElement(ToPoint(point1), ToPoint(point2), bondWidth, color));
                        break;
                    case BondOrder.Quadruple:
                        CreateLines(point1, point2, bondWidth, bondDistance, color, group);
                        CreateLines(point1, point2, bondWidth, bondDistance * 4, color, group);
                        break;
                    default:
                        break;
                }
                return group;
            }
        }

        private void CreateLines(Vector2 point1, Vector2 point2, double width, double dist, Color color, ElementGroup group)
        {
            var output = GenerateDistanceData(point1, point2, dist);
            var l1 = new LineElement(ToPoint(output[0]), ToPoint(output[2]), width, color);
            var l2 = new LineElement(ToPoint(output[1]), ToPoint(output[3]), width, color);
            group.Add(l1);
            group.Add(l2);
        }

        private Vector2[] GenerateDistanceData(Vector2 point1, Vector2 point2, double dist)
        {
            var normal = point2 - point1;
            normal = new Vector2(-normal.Y, normal.X);
            normal = Vector2.Normalize(normal) * dist;

            var line1p1 = point1 + normal;
            var line1p2 = point2 + normal;

            normal = -normal;
            var line2p1 = point1 + normal;
            var line2p2 = point2 + normal;

            return new Vector2[] { line1p1, line2p1, line1p2, line2p2, };
        }

        /// <summary>
        /// Generate ring elements, such as inner-ring bonds or ring stereo elements.
        /// </summary>
        /// <param name="bond">the ring bond to use when generating elements</param>
        /// <param name="ring">the ring that the bond is in</param>
        /// <param name="model">the renderer model</param>
        /// <returns>one or more rendering elements</returns>
        public virtual IRenderingElement GenerateRingElements(IBond bond, IRing ring, RendererModel model)
        {
            if (IsSingle(bond) && IsStereoBond(bond))
            {
                return GenerateStereoElement(bond, model);
            }
            else if (IsDouble(bond))
            {
                var pair = new ElementGroup
                {
                    GenerateBondElement(bond, BondOrder.Single, model),
                    GenerateInnerElement(bond, ring, model)
                };
                return pair;
            }
            else
            {
                return GenerateBondElement(bond, model);
            }
        }

        /// <summary>
        /// Make the inner ring bond, which is slightly shorter than the outer bond.
        /// </summary>
        /// <param name="bond">the ring bond</param>
        /// <param name="ring">the ring that the bond is in</param>
        /// <param name="model">the renderer model</param>
        /// <returns>the line element</returns>
        public virtual LineElement GenerateInnerElement(IBond bond, IRing ring, RendererModel model)
        {
            var center = GeometryUtil.Get2DCenter(ring);
            var a = bond.Begin.Point2D.Value;
            var b = bond.End.Point2D.Value;

            // the proportion to move in towards the ring center
            double distanceFactor = model.GetTowardsRingCenterProportion();
            double ringDistance = distanceFactor * IDEAL_RINGSIZE / ring.Atoms.Count;
            if (ringDistance < distanceFactor / MIN_RINGSIZE_FACTOR) ringDistance = distanceFactor / MIN_RINGSIZE_FACTOR;

            var w = Vector2.Lerp(a, center, ringDistance);
            var u = Vector2.Lerp(b, center, ringDistance);

            double alpha = 0.2;
            var ww = Vector2.Lerp(w, u, alpha);
            var uu = Vector2.Lerp(u, w, alpha);

            var width = GetWidthForBond(bond, model);
            var color = GetColorForBond(bond, model);

            return new LineElement(ToPoint(u), ToPoint(w), width, color);
        }

        private IRenderingElement GenerateStereoElement(IBond bond, RendererModel model)
        {
            var stereo = bond.Stereo;
            var type = WedgeLineElement.WedgeType.Wedged;
            var dir = BondDirection.ToSecond;
            switch (stereo)
            {
                case BondStereo.Down:
                case BondStereo.DownInverted:
                    type = WedgeLineElement.WedgeType.Dashed;
                    break;
                case BondStereo.UpOrDown:
                case BondStereo.UpOrDownInverted:
                    type = WedgeLineElement.WedgeType.Indiff;
                    break;
            }
            switch (stereo)
            {
                case BondStereo.DownInverted:
                case BondStereo.UpInverted:
                case BondStereo.UpOrDownInverted:
                    dir = BondDirection.ToFirst;
                    break;
            }

            var base_ = GenerateBondElement(bond, BondOrder.Single, model);
            return new WedgeLineElement((LineElement)base_, type, dir, GetColorForBond(bond, model));
        }

        /// <summary>
        /// Check to see if a bond is a double bond.
        /// </summary>
        /// <param name="bond">the bond to check</param>
        /// <returns>true if its order is double</returns>
        private bool IsDouble(IBond bond)
        {
            return bond.Order == BondOrder.Double;
        }

        /// <summary>
        /// Check to see if a bond is a single bond.
        /// </summary>
        /// <param name="bond">the bond to check</param>
        /// <returns>true if its order is single</returns>
        private bool IsSingle(IBond bond)
        {
            return bond.Order == BondOrder.Single;
        }

        /// <summary>
        /// Check to see if a bond is a stereo bond.
        /// </summary>
        /// <param name="bond">the bond to check</param>
        /// <returns>true if the bond has stereo information</returns>
        private bool IsStereoBond(IBond bond)
        {
            return bond.Stereo != BondStereo.None 
                && bond.Stereo != BondStereo.None
                && bond.Stereo != BondStereo.EZByCoordinates;
        }

        /// <summary>
        /// Check to see if any of the atoms in this bond are hydrogen atoms.
        /// </summary>
        /// <param name="bond">the bond to check</param>
        /// <returns>true if any atom has an element symbol of "H"</returns>
        protected virtual bool BindsHydrogen(IBond bond)
        {
            for (int i = 0; i < bond.Atoms.Count; i++)
            {
                var atom = bond.Atoms[i];
                if (atom.AtomicNumber == 1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Generate stereo or bond elements for this bond.
        /// </summary>
        /// <param name="bond">the bond to use when generating elements</param>
        /// <param name="model">the renderer model</param>
        /// <returns>one or more rendering elements</returns>
        public virtual IRenderingElement GenerateBond(IBond bond, RendererModel model)
        {
            bool showExplicitHydrogens = model.GetShowExplicitHydrogens();

            if (!showExplicitHydrogens && BindsHydrogen(bond))
            {
                return null;
            }

            if (IsStereoBond(bond))
            {
                return GenerateStereoElement(bond, model);
            }
            else
            {
                return GenerateBondElement(bond, model);
            }
        }
    }
}
