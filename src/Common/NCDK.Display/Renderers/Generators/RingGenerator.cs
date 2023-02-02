/*  Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
 *                2011  Jonty Lawson <jontyl@users.sourceforge.net>
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
using NCDK.Renderers.Elements;
using System;
using System.Collections.Generic;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generates just the aromatic indicators for rings : circles, or light-gray inner bonds, depending on the value of CDKStyleAromaticity.
    /// </summary>
    // @cdk.module renderbasic
    public class RingGenerator : BasicBondGenerator
    {
        /// <summary>
        /// The rings that have already been painted - that is, a ring element
        /// has been generated for it.
        /// </summary>
        private ISet<IRing> painted_rings;

        /// <summary>
        /// Make a generator for ring elements.
        /// </summary>
        public RingGenerator()
        {
            this.painted_rings = new HashSet<IRing>();
        }

        /// <inheritdoc/>
        public override IRenderingElement GenerateRingElements(IBond bond, IRing ring, RendererModel model)
        {
            if (RingIsAromatic(ring)
             && model.GetShowAromaticity()
             && ring.Atoms.Count < model.GetMaxDrawableAromaticRing())
            {
                var pair = new ElementGroup();
                if (model.GetCDKStyleAromaticity())
                {
                    pair.Add(GenerateBondElement(bond, BondOrder.Single, model));
                    base.SetOverrideColor(WPF.Media.Colors.LightGray);
                    pair.Add(GenerateInnerElement(bond, ring, model));
                    base.SetOverrideColor(null);
                }
                else
                {
                    pair.Add(GenerateBondElement(bond, BondOrder.Single, model));
                    if (!painted_rings.Contains(ring))
                    {
                        painted_rings.Add(ring);
                        pair.Add(GenerateRingRingElement(bond, ring, model));
                    }
                }
                return pair;
            }
            else
            {
                return base.GenerateRingElements(bond, ring, model);
            }
        }

        private IRenderingElement GenerateRingRingElement(IBond bond, IRing ring, RendererModel model)
        {
            var c = ToPoint(GeometryUtil.Get2DCenter(ring));

            var minmax = GeometryUtil.GetMinMax(ring);
            var width = minmax[2] - minmax[0];
            var height = minmax[3] - minmax[1];
            var radius = Math.Min(width, height) * model.GetRingProportion();
            var color = GetColorForBond(bond, model);

            return new OvalElement(c, radius, false, color);
        }

        private bool RingIsAromatic(IRing ring)
        {
            bool isAromatic = true;
            foreach (var atom in ring.Atoms)
            {
                if (!atom.IsAromatic)
                {
                    isAromatic = false;
                    break;
                }
            }
            if (!isAromatic)
            {
                isAromatic = true;
                foreach (var b in ring.Bonds)
                {
                    if (!b.IsAromatic)
                    {
                        return false;
                    }
                }
            }
            return isAromatic;
        }
    }
}
