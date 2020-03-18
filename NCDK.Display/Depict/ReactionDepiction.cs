/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Renderers;
using NCDK.Renderers.Elements;
using NCDK.Renderers.Visitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Depict
{
    /// <summary>
    /// Internal - depiction of a single reaction. We divide the reaction into two draw steps.
    /// The first step draws the main components (reactants and products) whilst the second
    /// draws the side components (agents: catalysts, solvents, spectators, etc). Reaction
    /// direction is drawn a single headed arrow (forward and backward) or an equilibrium
    /// (bidirectional).
    /// </summary>
    sealed class ReactionDepiction : Depiction
    {
        private readonly RendererModel model;
        private readonly Dimensions dimensions;

        // molecule sets and titles
        private readonly List<Bounds> mainComp = new List<Bounds>();
        private readonly List<Bounds> sideComps = new List<Bounds>();
        private readonly Bounds conditions;
        private readonly Bounds title;

        // arrow info
        private readonly int arrowIdx;
        private readonly ReactionDirection direction;
        private readonly double arrowHeight;
        private readonly double minArrowWidth;

        // dimensions and spacing of side components
        private readonly Dimensions sideDim;
        private readonly Dimensions mainDim;
        private readonly Dimensions condDim;

        private readonly double[] xOffsets, yOffsets;
        private readonly double[] xOffsetSide, yOffsetSide;

        private readonly int nRow, nCol;

        private readonly Color fgcol;

        public ReactionDepiction(RendererModel model,
                                 List<Bounds> reactants,
                                 List<Bounds> products,
                                 List<Bounds> agents,
                                 Bounds plus,
                                 ReactionDirection direction,
                                 Dimensions dimensions,
                                 IList<Bounds> reactantTitles,
                                 IList<Bounds> productTitles,
                                 Bounds title,
                                 Bounds conditions,
                                 Color fgcol)
            : base(model)
        {
            this.model = model;
            this.dimensions = dimensions;
            this.title = title;
            this.fgcol = fgcol;

            // side components (catalysts, solvents, etc) note we deliberately
            // swap sideGrid width and height as we to stack agents on top of
            // each other. By default determineGrid tries to make the grid
            // wide but we want it tall
            this.sideComps.AddRange(agents);
            var sideGrid = Dimensions.DetermineGrid(sideComps.Count);
            var prelimSideDim = Dimensions.OfGrid(sideComps,
                                                  yOffsetSide = new double[sideGrid.Width + 1],
                                                  xOffsetSide = new double[sideGrid.Height + 1]);

            // build the main components, we add a 'plus' between each molecule
            foreach (var reactant in reactants)
            {
                this.mainComp.Add(reactant);
                this.mainComp.Add(plus);
            }

            // replacing trailing plus with placeholder for arrow
            if (!reactants.Any())
                this.mainComp.Add(new Bounds());
            else
                this.mainComp[this.mainComp.Count - 1] = new Bounds();

            foreach (var product in products)
            {
                this.mainComp.Add(product);
                this.mainComp.Add(plus);
            }

            // trailing plus not needed
            if (products.Any())
                this.mainComp.RemoveAt(this.mainComp.Count - 1);

            // add title if supplied, we simply line them up with
            // the main components and the add them as an extra
            // row
            if (reactantTitles.Any() || productTitles.Any())
            {
                if (reactantTitles.Any() && reactantTitles.Count != reactants.Count)
                    throw new ArgumentException("Number of reactant titles differed from number of reactants");
                if (productTitles.Any() && productTitles.Count != products.Count)
                    throw new ArgumentException("Number of product titles differed from number of products");
                var mainTitles = new List<Bounds>();
                foreach (var reactantTitle in reactantTitles)
                {
                    mainTitles.Add(reactantTitle);
                    mainTitles.Add(new Bounds());
                }
                if (!reactants.Any())
                    mainTitles.Add(new Bounds()); // gap for arrow
                foreach (var productTitle in productTitles)
                {
                    mainTitles.Add(productTitle);
                    mainTitles.Add(new Bounds());
                }
                // remove trailing space for plus
                if (products.Any())
                    mainTitles.RemoveAt(mainTitles.Count - 1);

                Trace.Assert(mainTitles.Count == mainComp.Count);
                this.mainComp.AddRange(mainTitles);
                this.nRow = 2;
                this.nCol = mainComp.Count / 2;
            }
            else
            {
                this.nRow = 1;
                this.nCol = mainComp.Count;
            }

            this.conditions = conditions;

            // arrow parameters
            this.arrowIdx = Math.Max(reactants.Count + reactants.Count - 1, 0);
            this.direction = direction;
            this.arrowHeight = plus.Height * 2;
            this.minArrowWidth = 4 * arrowHeight;

            mainDim = Dimensions.OfGrid(mainComp,
                                        yOffsets = new double[nRow + 1],
                                        xOffsets = new double[nCol + 1]);

            double middleRequired = Math.Max(prelimSideDim.width, conditions.Width);

            // avoid v. small arrows, we take in to account the padding provided by the arrow head height/length
            if (middleRequired < minArrowWidth - arrowHeight - arrowHeight)
            {
                // adjust x-offset so side components are centered
                double xAdjust = (minArrowWidth - middleRequired) / 2;
                for (int i = 0; i < xOffsetSide.Length; i++)
                    xOffsetSide[i] += xAdjust;
                // need to recenter agents
                if (conditions.Width > prelimSideDim.width)
                {
                    for (int i = 0; i < xOffsetSide.Length; i++)
                        xOffsetSide[i] += (conditions.Width - prelimSideDim.width) / 2;
                }
                // update side dims
                this.sideDim = new Dimensions(minArrowWidth, prelimSideDim.height);
                this.condDim = new Dimensions(minArrowWidth, conditions.Height);
            }
            else
            {
                // arrow padding
                for (int i = 0; i < xOffsetSide.Length; i++)
                    xOffsetSide[i] += arrowHeight;

                // need to recenter agents
                if (conditions.Width > prelimSideDim.width)
                {
                    for (int i = 0; i < xOffsetSide.Length; i++)
                        xOffsetSide[i] += (conditions.Width - prelimSideDim.width) / 2;
                }

                this.sideDim = new Dimensions(2 * arrowHeight + middleRequired,
                                              prelimSideDim.height);
                this.condDim = new Dimensions(2 * arrowHeight + middleRequired,
                                              conditions.Height);
            }
        }

        public override Size Draw(DrawingContext g2)
        {
            // we use the AWT for vector graphics if though we're raster because
            // fractional strokes can be figured out by interpolation, without
            // when we shrink diagrams bonds can look too bold/chubby
            // format margins and padding for raster images
            var scale = model.GetScale();
            var zoom = model.GetZoomFactor();
            var margin = GetMarginValue(DepictionGenerator.DefaultPixelMargin);
            var padding = GetPaddingValue(DefaultPaddingFactor * margin);

            // work out the required space of the main and side components separately
            // will draw these in two passes (main then side) hence want different offsets for each
            var nSideCol = xOffsetSide.Length - 1;
            var nSideRow = yOffsetSide.Length - 1;

            var sideRequired = sideDim.Scale(scale * zoom);
            var mainRequired = mainDim.Scale(scale * zoom);
            var condRequired = condDim.Scale(scale * zoom);
            var titleRequired = new Dimensions(title.Width, title.Height).Scale(scale * zoom);

            var firstRowHeight = scale * zoom * yOffsets[1];
            var total = CalcTotalDimensions(margin, padding, mainRequired, sideRequired, titleRequired, firstRowHeight, null);
            var fitting = CalcFitting(margin, padding, mainRequired, sideRequired, titleRequired, firstRowHeight, null);

            var visitor = WPFDrawVisitor.ForVectorGraphics(g2);

            if (model.GetBackgroundColor() != Colors.Transparent)
                visitor.Visit(new RectangleElement(new Point(0, 0), total.width, total.height, true, model.GetBackgroundColor()), Transform.Identity);

            // compound the zoom, fitting and scaling into a single value
            var rescale = zoom * fitting * scale;
            double mainCompOffset = 0;

            // shift product x-offset to make room for the arrow / side components
            mainCompOffset = fitting * sideRequired.height + nSideRow * padding - fitting * firstRowHeight / 2;
            for (int i = arrowIdx + 1; i < xOffsets.Length; i++)
            {
                xOffsets[i] += sideRequired.width * 1 / (scale * zoom);
            }

            // MAIN COMPONENTS DRAW
            // x,y base coordinates include the margin and centering (only if fitting to a size)
            var totalRequiredWidth = 2 * margin + Math.Max(0, nCol - 1) * padding + Math.Max(0, nSideCol - 1) * padding + (rescale * xOffsets[nCol]);
            var totalRequiredHeight = 2 * margin + Math.Max(0, nRow - 1) * padding + (!title.IsEmpty() ? padding : 0) + Math.Max(mainCompOffset, 0) + fitting * mainRequired.height + fitting * Math.Max(0, titleRequired.height);
            var xBase = margin + (total.width - totalRequiredWidth) / 2;
            var yBase = margin + Math.Max(mainCompOffset, 0) + (total.height - totalRequiredHeight) / 2;
            for (int i = 0; i < mainComp.Count; i++)
            {
                var row = i / nCol;
                var col = i % nCol;

                // calculate the 'view' bounds:
                //  amount of padding depends on which row or column we are in.
                //  the width/height of this col/row can be determined by the next offset
                var x = xBase + col * padding + rescale * xOffsets[col];
                var y = yBase + row * padding + rescale * yOffsets[row];
                var w = rescale * (xOffsets[col + 1] - xOffsets[col]);
                var h = rescale * (yOffsets[row + 1] - yOffsets[row]);

                // intercept arrow draw and make it as big as need
                if (i == arrowIdx)
                {
                    w = rescale * (xOffsets[i + 1] - xOffsets[i]) + Math.Max(0, nSideCol - 1) * padding;
                    Draw(visitor,
                         1, // no zoom since arrows is drawn as big as needed
                         CreateArrow(w, arrowHeight * rescale),
                         MakeRect(x, y, w, h));
                    continue;
                }

                // extra padding from the side components
                if (i > arrowIdx)
                    x += Math.Max(0, nSideCol - 1) * padding;

                // skip empty elements
                Bounds bounds = this.mainComp[i];
                if (bounds.IsEmpty())
                    continue;

                Draw(visitor, zoom, bounds, MakeRect(x, y, w, h));
            }

            // RXN TITLE DRAW
            if (!title.IsEmpty())
            {
                var y = yBase + nRow * padding + rescale * yOffsets[nRow];
                var h = rescale * title.Height;
                Draw(visitor, zoom, title, MakeRect(0, y, total.width, h));
            }

            // SIDE COMPONENTS DRAW
            xBase += arrowIdx * padding + rescale * xOffsets[arrowIdx];
            yBase -= mainCompOffset;
            for (int i = 0; i < sideComps.Count; i++)
            {
                var row = i / nSideCol;
                var col = i % nSideCol;

                // calculate the 'view' bounds:
                //  amount of padding depends on which row or column we are in.
                //  the width/height of this col/row can be determined by the next offset
                var x = xBase + col * padding + rescale * xOffsetSide[col];
                var y = yBase + row * padding + rescale * yOffsetSide[row];
                var w = rescale * (xOffsetSide[col + 1] - xOffsetSide[col]);
                var h = rescale * (yOffsetSide[row + 1] - yOffsetSide[row]);

                Draw(visitor, zoom, sideComps[i], MakeRect(x, y, w, h));
            }

            // CONDITIONS DRAW
            if (!conditions.IsEmpty())
            {
                yBase += mainCompOffset;        // back to top
                yBase += (fitting * mainRequired.height) / 2;    // now on center line (arrow)
                yBase += arrowHeight;           // now just bellow
                Draw(visitor, zoom, conditions, MakeRect(xBase,
                                                     yBase,
                                                     fitting * condRequired.width, fitting * condRequired.height));
            }

            // reset shared xOffsets
            for (int i = arrowIdx + 1; i < xOffsets.Length; i++)
                xOffsets[i] -= sideRequired.width * 1 / (scale * zoom);

            return new Size(total.width, total.height);
        }

        internal override string ToVectorString(string fmt, string units)
        {
            // format margins and padding for raster images
            double scale = model.GetScale();

            double margin = GetMarginValue(units.Equals(Depiction.UnitsMM) ? 
                DepictionGenerator.DefaultMillimeterMargin :
                DepictionGenerator.DefaultPixelMargin);
            double padding = GetPaddingValue(DefaultPaddingFactor * margin);

            // All vector graphics will be written in mm not px to we need to
            // adjust the size of the molecules accordingly. For now the rescaling
            // is fixed to the bond length proposed by ACS 1996 guidelines (~5mm)
            double zoom = model.GetZoomFactor();
            if (units.Equals(Depiction.UnitsMM))
                zoom *= RescaleForBondLength(Depiction.ACS1996BondLength);

            // work out the required space of the main and side components separately
            // will draw these in two passes (main then side) hence want different offsets for each
            var nSideCol = xOffsetSide.Length - 1;
            var nSideRow = yOffsetSide.Length - 1;

            var sideRequired = sideDim.Scale(scale * zoom);
            var mainRequired = mainDim.Scale(scale * zoom);
            var condRequired = condDim.Scale(scale * zoom);
            var titleRequired = new Dimensions(title.Width, title.Height).Scale(scale * zoom);

            var firstRowHeight = scale * zoom * yOffsets[1];
            var total = CalcTotalDimensions(margin, padding, mainRequired, sideRequired, titleRequired, firstRowHeight, fmt);
            var fitting = CalcFitting(margin, padding, mainRequired, sideRequired, titleRequired, firstRowHeight, fmt);

            var visitor = new SvgDrawVisitor(total.width, total.height, units);
            if (fmt.Equals(SvgFormatKey))
            {
                SvgPrevisit(fmt, scale * zoom * fitting, (SvgDrawVisitor)visitor, mainComp);
            }
            else
            {
                // TODO: handle reounding
                // pdf can handle fraction coordinates just fine
                //((WPFDrawVisitor) visitor).SetRounding(false);
            }

            // background color
            visitor.Visit(new RectangleElement(new Point(0, -total.height), total.width, total.height, true, model.GetBackgroundColor()), new ScaleTransform(1, -1));

            // compound the zoom, fitting and scaling into a single value
            var rescale = zoom * fitting * scale;
            double mainCompOffset = 0;

            // shift product x-offset to make room for the arrow / side components
            mainCompOffset = fitting * sideRequired.height + nSideRow * padding - fitting * firstRowHeight / 2;
            for (int i = arrowIdx + 1; i < xOffsets.Length; i++)
            {
                xOffsets[i] += sideRequired.width * 1 / (scale * zoom);
            }

            // MAIN COMPONENTS DRAW
            // x,y base coordinates include the margin and centering (only if fitting to a size)
            var totalRequiredWidth = 2 * margin + Math.Max(0, nCol - 1) * padding + Math.Max(0, nSideCol - 1) * padding + (rescale * xOffsets[nCol]);
            var totalRequiredHeight = 2 * margin + Math.Max(0, nRow - 1) * padding + (!title.IsEmpty() ? padding : 0) + Math.Max(mainCompOffset, 0) + fitting * mainRequired.height + fitting * Math.Max(0, titleRequired.height);
            var xBase = margin + (total.width - totalRequiredWidth) / 2;
            var yBase = margin + Math.Max(mainCompOffset, 0) + (total.height - totalRequiredHeight) / 2;
            for (int i = 0; i < mainComp.Count; i++)
            {
                var row = i / nCol;
                var col = i % nCol;

                // calculate the 'view' bounds:
                //  amount of padding depends on which row or column we are in.
                //  the width/height of this col/row can be determined by the next offset
                var x = xBase + col * padding + rescale * xOffsets[col];
                var y = yBase + row * padding + rescale * yOffsets[row];
                var w = rescale * (xOffsets[col + 1] - xOffsets[col]);
                var h = rescale * (yOffsets[row + 1] - yOffsets[row]);

                // intercept arrow draw and make it as big as need
                if (i == arrowIdx)
                {
                    w = rescale * (xOffsets[i + 1] - xOffsets[i]) + Math.Max(0, nSideCol - 1) * padding;
                    Draw(visitor,
                         1, // no zoom since arrows is drawn as big as needed
                         CreateArrow(w, arrowHeight * rescale),
                         MakeRect(x, y, w, h));
                    continue;
                }

                // extra padding from the side components
                if (i > arrowIdx)
                    x += Math.Max(0, nSideCol - 1) * padding;

                // skip empty elements
                var bounds = this.mainComp[i];
                if (bounds.IsEmpty())
                    continue;

                Draw(visitor, zoom, bounds, MakeRect(x, y, w, h));
            }

            // RXN TITLE DRAW
            if (!title.IsEmpty())
            {
                var y = yBase + nRow * padding + rescale * yOffsets[nRow];
                var h = rescale * title.Height;
                Draw(visitor, zoom, title, MakeRect(0, y, total.width, h));
            }

            // SIDE COMPONENTS DRAW
            xBase += arrowIdx * padding + rescale * xOffsets[arrowIdx];
            yBase -= mainCompOffset;
            for (int i = 0; i < sideComps.Count; i++)
            {
                int row = i / nSideCol;
                int col = i % nSideCol;

                // calculate the 'view' bounds:
                //  amount of padding depends on which row or column we are in.
                //  the width/height of this col/row can be determined by the next offset
                var x = xBase + col * padding + rescale * xOffsetSide[col];
                var y = yBase + row * padding + rescale * yOffsetSide[row];
                var w = rescale * (xOffsetSide[col + 1] - xOffsetSide[col]);
                var h = rescale * (yOffsetSide[row + 1] - yOffsetSide[row]);

                Draw(visitor, zoom, sideComps[i], MakeRect(x, y, w, h));
            }

            // CONDITIONS DRAW
            if (!conditions.IsEmpty())
            {
                yBase += mainCompOffset;         // back to top
                yBase += (fitting * mainRequired.height) / 2;     // now on center line (arrow)
                yBase += arrowHeight;            // now just bellow
                Draw(visitor, zoom, conditions, MakeRect(xBase,
                                                     yBase,
                                                     fitting * condRequired.width, fitting * condRequired.height));
            }

            // reset shared xOffsets
            if (sideComps.Any())
            {
                for (int i = arrowIdx + 1; i < xOffsets.Length; i++)
                    xOffsets[i] -= sideRequired.width * 1 / (scale * zoom);
            }

            return visitor.ToString();
        }

        private double CalcFitting(double margin, double padding, Dimensions mainRequired, Dimensions sideRequired,
                                   Dimensions titleRequired,
                                   double firstRowHeight, string fmt)
        {
            if (dimensions == Dimensions.Automatic)
                return 1; // no fitting

            var nSideCol = xOffsetSide.Length - 1;
            var nSideRow = yOffsetSide.Length - 1;

            // need padding in calculation
            var mainCompOffset = sideRequired.height > 0 ? sideRequired.height + (nSideRow * padding) - (firstRowHeight / 2) : 0;
            if (mainCompOffset < 0)
                mainCompOffset = 0;

            Dimensions required = mainRequired.Add(sideRequired.width, mainCompOffset)
                                              .Add(0, Math.Max(0, titleRequired.height));

            // We take out the padding height of the side components but in reality
            // some of it overlaps, since reactions are normally wider then they are
            // tall we won't normally bit fitting by this parameter. If do fit by this
            // parameter we might make the depiction smaller then it needs to be but thats
            // better than cutting bits off
            var targetDim = dimensions;

            targetDim = targetDim.Add(-2 * margin, -2 * margin)
                                 .Add(-((nCol - 1) * padding), -((nRow - 1) * padding))
                                 .Add(-(nSideCol - 1) * padding, -(nSideRow - 1) * padding)
                                 .Add(0, titleRequired.height > 0 ? -padding : 0);

            var resize = Math.Min(targetDim.width / required.width,
                                  targetDim.height / required.height);

            if (resize > 1 && !model.GetFitToScreen())
                resize = 1;
            return resize;
        }

        private Dimensions CalcTotalDimensions(double margin, double padding, Dimensions mainRequired,
                                               Dimensions sideRequired, Dimensions titleRequired,
                                               double firstRowHeight,
                                               string fmt)
        {
            if (dimensions == Dimensions.Automatic)
            {
                var nSideCol = xOffsetSide.Length - 1;
                var nSideRow = yOffsetSide.Length - 1;

                var mainCompOffset = sideRequired.height + (nSideRow * padding) - (firstRowHeight / 2);
                if (mainCompOffset < 0)
                    mainCompOffset = 0;

                var titleExtra = Math.Max(0, titleRequired.height);
                if (titleExtra > 0)
                    titleExtra += padding;

                return mainRequired.Add(2 * margin, 2 * margin)
                                   .Add(Math.Max(0, nCol - 1) * padding, (nRow - 1) * padding)
                                   .Add(Math.Max(0, sideRequired.width), 0)           // side component extra width
                                   .Add(Math.Max(0, nSideCol - 1) * padding, 0) // side component padding
                                   .Add(0, mainCompOffset)
                                   .Add(0, titleExtra);

            }
            else
            {
                return dimensions;
            }
        }

        private Rect MakeRect(double x, double y, double w, double h)
        {
            return new Rect(x, y, w, h);
        }

        private Bounds CreateArrow(double minWidth, double minHeight)
        {
            var arrow = new Bounds();
            var headThickness = minHeight / 3;
            var inset = 0.8;
            var headLength = minHeight;
            switch (direction)
            {
                case ReactionDirection.Forward:
                    {
                        var fp = new PathFigure();
                        arrow.Add(new LineElement(new Point(0, 0), new Point(minWidth - 0.5 * headLength, 0), minHeight / 14, fgcol));
                        fp.StartPoint = new Point(minWidth, 0);
                        fp.Segments.Add(new LineSegment(new Point(minWidth - headLength, +headThickness), true));
                        fp.Segments.Add(new LineSegment(new Point(minWidth - inset * headLength, 0), true));
                        fp.Segments.Add(new LineSegment(new Point(minWidth - headLength, -headThickness), true));
                        fp.IsClosed = true;
                        var path = new PathGeometry(new[] { fp });
                        arrow.Add(GeneralPath.ShapeOf(path, fgcol));
                    }
                    break;
                case ReactionDirection.Backward:
                    {
                        var fp = new PathFigure();
                        arrow.Add(new LineElement(new Point(0.5 * headLength, 0), new Point(minWidth, 0), minHeight / 14, fgcol));
                        fp.StartPoint = new Point(0, 0);
                        fp.Segments.Add(new LineSegment(new Point(minHeight, +headThickness), true));
                        fp.Segments.Add(new LineSegment(new Point(minHeight - (1 - inset) * minHeight, 0), true));
                        fp.Segments.Add(new LineSegment(new Point(minHeight, -headThickness), true));
                        fp.IsClosed = true;
                        var path = new PathGeometry(new[] { fp });
                        arrow.Add(GeneralPath.ShapeOf(path, fgcol));
                    }
                    break;
                case ReactionDirection.Bidirectional: // equilibrium?
                    {
                        var fp1 = new PathFigure
                        {
                            StartPoint = new Point(0, 0.5 * +headThickness)
                        };
                        fp1.Segments.Add(new LineSegment(new Point(minWidth + minHeight + minHeight, 0.5 * +headThickness), true));
                        fp1.Segments.Add(new LineSegment(new Point(minWidth + minHeight, 1.5 * +headThickness), true));
                        var fp2 = new PathFigure
                        {
                            StartPoint = new Point(minWidth + minHeight + minHeight, 0.5 * -headThickness)
                        };
                        fp2.Segments.Add(new LineSegment(new Point(0, 0.5 * -headThickness), true));
                        fp2.Segments.Add(new LineSegment(new Point(minHeight, 1.5 * -headThickness), true));
                        var path = new PathGeometry(new[] { fp1, fp2 });
                        arrow.Add(GeneralPath.OutlineOf(path, minHeight / 14, fgcol));
                    }
                    break;
            }

            return arrow;
        }
    }
}
