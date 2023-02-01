/* Copyright (C) 2008 Gilleain Torrance <gilleain.torrance@gmail.com>
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

using NCDK.Numerics;
using NCDK.Renderers.Elements;
using NCDK.Renderers.Fonts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Media;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;
using WPF = System.Windows;

namespace NCDK.Renderers.Visitors
{
    /// <summary>
    /// Implementation of the <see cref="IDrawVisitor"/> interface for the WPF,
    /// allowing molecules to be rendered with toolkits based on WPF.
    /// </summary>
    public class WPFDrawVisitor : AbstractWPFDrawVisitor
    {
        /// <summary>
        /// The font manager cannot be set by the constructor as it needs to
        /// be managed by the Renderer.
        /// </summary>
        private WPFFontManager fontManager;

        /// <summary>
        /// The renderer model cannot be set by the constructor as it needs to
        /// be managed by the Renderer.
        /// </summary>
        private RendererModel rendererModel;

        /// <inheritdoc/>
        public override RendererModel RendererModel
        {
            get => rendererModel;
            set => rendererModel = value;
        }

        internal readonly Dictionary<Color, Brush> brushMap = null;
        internal Brush GetBrush(Color color)
        {
            Brush brush;
            if (brushMap == null)
            {
                brush = new SolidColorBrush(color);
                brush.Freeze();
            }
            else
            {
                var key = color;
                if (!brushMap.TryGetValue(key, out brush))
                {
                    brush = new SolidColorBrush(color);
                    brush.Freeze();
                    brushMap.Add(key, brush);
                }
            }
            return brush;
        }

        internal readonly Dictionary<Color, Pen> roundPenMap;
        internal Pen GetRoundPen(Color color)
        {
            Pen pen;
            if (roundPenMap == null)
            {
                pen = new Pen(new SolidColorBrush(color), 1)
                {
                    StartLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    EndLineCap = PenLineCap.Round
                };
                pen.Freeze();
            }
            else
            {
                var key = color;
                if (!roundPenMap.TryGetValue(key, out pen))
                {
                    pen = new Pen(new SolidColorBrush(color), 1)
                    {
                        StartLineCap = PenLineCap.Round,
                        LineJoin = PenLineJoin.Round,
                        EndLineCap = PenLineCap.Round
                    };
                    pen.Freeze();
                    roundPenMap.Add(key, pen);
                }
            }
            return pen;
        }

        internal readonly Dictionary<Tuple<Color, int>, Pen> strokeMap;
        internal Pen GetPen(Color color, double thickness)
        {
            Pen pen;
            if (strokeMap == null)
            {
                pen = new Pen(new SolidColorBrush(color), thickness);
                pen.Freeze();
            }
            else
            {
                var width = thickness;
                if (width < minStroke)
                    width = minStroke;
                var key = new Tuple<Color, int>(color, (int)(width * 4));

                if (!strokeMap.TryGetValue(key, out pen))
                {
                    pen = new Pen(GetBrush(color), thickness);
                    pen.Freeze();
                    strokeMap.Add(key, pen);
                }
            }
            return pen;
        }

        private readonly double minStroke;

        /// <summary>
        /// The <see cref="DrawingContext"/> for this visitor.
        /// </summary>
        internal readonly DrawingContext dc;

        /// <summary>
        /// Constructs a new <see cref="IDrawVisitor"/>
        /// taking a <see cref="DrawingContext"/> object to which the chemical content
        /// is drawn.
        /// </summary>
        /// <param name="graphics"><see cref="DrawingContext"/> to which will be drawn</param>
        public WPFDrawVisitor(DrawingContext graphics)
            : this(graphics, true, double.NegativeInfinity)
        { }

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="dc">the graphics instance</param>
        /// <param name="strokeCache">cache strokes internally, only sizes at 0.25 increments are stored</param>
        /// <param name="minStroke">the minimum stroke, strokes smaller than this are automatically resized</param>
        private WPFDrawVisitor(DrawingContext dc, bool strokeCache, double minStroke)
        {
            this.dc = dc;
            this.fontManager = null;
            this.rendererModel = null;
            if (strokeCache)
            {
                this.brushMap = new Dictionary<Color, Brush>();
                this.strokeMap = new Dictionary<Tuple<Color, int>, Pen>();
                this.roundPenMap = new Dictionary<Color, Pen>();
            }
            this.minStroke = minStroke;
        }

        /// <summary>
        /// Create a draw visitor that will be rendering to a vector graphics output. This disables
        /// the minimum stroke size and stroke caching when drawing lines.
        /// </summary>
        /// <param name="dc">drawing context</param>
        /// <returns>draw visitor</returns>
        public static WPFDrawVisitor ForVectorGraphics(DrawingContext dc)
        {
            return new WPFDrawVisitor(dc, true, double.NegativeInfinity);
        }

        private void Visit(ElementGroup elementGroup)
        {
            elementGroup.Visit(this);
        }

        private void Visit(LineElement line)
        {
            // scale the stroke by zoom + scale (both included in the AffineTransform)
            var width = line.Width;
            if (width < minStroke)
                width = minStroke;

            var pen = GetPen(line.Color, width);

            var linePoints = new WPF::Point[] { line.FirstPoint, line.SecondPoint };

            linePoints[0] = linePoints[0];
            linePoints[1] = linePoints[1];
            dc.DrawLine(pen, linePoints[0], linePoints[1]);
        }

        private void Visit(OvalElement oval)
        {
            var radius = oval.Radius;
            var diameter = oval.Radius * 2;
            var center = oval.Coord;

            if (oval.Fill)
            {
                this.dc.DrawEllipse(
                    GetBrush(oval.Color),
                    null,
                    new WPF.Point(center.X, center.Y), diameter, diameter);
            }
            else
            {
                // scale the stroke by zoom + scale (both included in the AffineTransform)
                var width = oval.Stroke;
                if (width < minStroke)
                    width = minStroke;

                var pen = GetPen(oval.Color, width);

                this.dc.DrawEllipse(
                    null,
                    pen,
                    new WPF.Point(center.X, center.Y), diameter, diameter);
            }
        }

        private Color BackgroundColor
        {
            get
            {
                return rendererModel == null 
                     ? RendererModelTools.DefaultBackgroundColor 
                     : rendererModel.GetBackgroundColor();
            }
        }

        private void Visit(TextElement textElement)
        {
            var point = GetTextBasePoint(textElement.Text, textElement.Coord, this.fontManager.Typeface, this.fontManager.Size);
            var textBounds = GetTextBounds(textElement.Text, textElement.Coord, this.fontManager.Typeface, this.fontManager.Size);
            var backColor = this.BackgroundColor;
            this.dc.DrawRectangle(GetBrush(backColor), null, textBounds);
            this.dc.DrawText(new FormattedText(
                textElement.Text,
                CultureInfo.InvariantCulture,
                WPF.FlowDirection.LeftToRight,
                this.fontManager.CureentTypeface,
                this.fontManager.Size,
                GetBrush(textElement.Color)), point);
        }

        private void Visit(WedgeLineElement wedge)
        {
            // make the vector normal to the wedge axis
            var normal = new Vector2(wedge.FirstPoint.Y - wedge.SecondPoint.Y, wedge.SecondPoint.X - wedge.FirstPoint.X);
            normal = Vector2.Normalize(normal);
            normal *= (rendererModel.GetWedgeWidth() / rendererModel.GetScale());

            // make the triangle corners
            var vertexA = new Vector2(wedge.FirstPoint.X, wedge.FirstPoint.Y);
            var vertexB = new Vector2(wedge.SecondPoint.X, wedge.SecondPoint.Y) + normal;
            var vertexC = vertexB - normal;

            if (wedge.BondType == WedgeLineElement.WedgeType.Dashed)
            {
                var pen = GetPen(wedge.Color, 1);

                // calculate the distances between lines
                var distance = Vector2.Distance(vertexB, vertexA);
                const double gapFactor = 0.1;
                var gap = distance * gapFactor;
                var numberOfDashes = distance / gap;
                double displacement = 0;

                // draw by interpolating along the edges of the triangle
                for (int i = 0; i < numberOfDashes; i++)
                {
                    var point1 = Vector2.Lerp(vertexA, vertexB, displacement);
                    var point2 = Vector2.Lerp(vertexA, vertexC, displacement);
                    var p1T = ToPoint(point1);
                    var p2T = ToPoint(point2);
                    this.dc.DrawLine(pen, p1T, p2T);
                    if (distance * (displacement + gapFactor) >= distance)
                    {
                        break;
                    }
                    else
                    {
                        displacement += gapFactor;
                    }
                }
            }
            else if (wedge.BondType == WedgeLineElement.WedgeType.Wedged)
            {
                var pointB = ToPoint(vertexB);
                var pointC = ToPoint(vertexC);
                var pointA = ToPoint(vertexA);

                var figure = new PathFigure
                {
                    StartPoint = pointB
                };
                figure.Segments.Add(new LineSegment(pointC, false));
                figure.Segments.Add(new LineSegment(pointA, false));
                var g = new PathGeometry(new[] { figure });
                this.dc.DrawGeometry(new SolidColorBrush(wedge.Color), null, g);
            }
            else if (wedge.BondType == WedgeLineElement.WedgeType.Indiff)
            {
                var pen = GetRoundPen(wedge.Color);

                // calculate the distances between lines
                var distance = Vector2.Distance(vertexB, vertexA);
                const double gapFactor = 0.05;
                var gap = distance * gapFactor;
                var numberOfDashes = distance / gap;
                double displacement = 0;

                // draw by interpolating along the edges of the triangle
                var point1 = Vector2.Lerp(vertexA, vertexB, displacement);
                bool flip = false;
                var p1T = ToPoint(point1);
                displacement += gapFactor;
                for (int i = 0; i < numberOfDashes; i++)
                {
                    Vector2 point2;
                    if (flip)
                    {
                        point2 = Vector2.Lerp(vertexA, vertexC, displacement);
                    }
                    else
                    {
                        point2 = Vector2.Lerp(vertexA, vertexB, displacement);
                    }
                    flip = !flip;
                    var p2T = ToPoint(point2);
                    this.dc.DrawLine(pen, p1T, p2T);
                    if (distance * (displacement + gapFactor) >= distance)
                    {
                        break;
                    }
                    else
                    {
                        p1T = p2T;
                        displacement += gapFactor;
                    }
                }
            }
        }

        private void Visit(AtomSymbolElement atomSymbol)
        {
            var xy = atomSymbol.Coord;

            var bounds = GetTextBounds(atomSymbol.Text, this.fontManager.CureentTypeface, this.fontManager.Size);

            var w = bounds.Width;
            var h = bounds.Height;

            var xOffset = bounds.X;
            var yOffset = bounds.Y + bounds.Height;

            bounds = new WPF.Rect(xy.X - (w / 2), xy.Y - (h / 2), w, h);

            var backgroundBrush = GetBrush(this.BackgroundColor);
            var atomSymbolBrush = GetBrush(atomSymbol.Color);

            var padding = h / 4;
            this.dc.DrawRoundedRectangle(
                backgroundBrush, null,
                new WPF::Rect(
                    bounds.X - (padding / 2), bounds.Y - (padding / 2),
                    bounds.Width + padding, bounds.Height + padding), padding, padding);
            this.dc.DrawText(new FormattedText(
                    atomSymbol.Text,
                    CultureInfo.CurrentCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    atomSymbolBrush),
                    new WPF::Point(bounds.X - xOffset, bounds.Y + h - yOffset));

            int offset = 10; // XXX
            string chargeString;
            if (atomSymbol.FormalCharge == 0)
            {
                return;
            }
            else if (atomSymbol.FormalCharge == 1)
            {
                chargeString = "+";
            }
            else if (atomSymbol.FormalCharge > 1)
            {
                chargeString = atomSymbol.FormalCharge + "+";
            }
            else if (atomSymbol.FormalCharge == -1)
            {
                chargeString = "-";
            }
            else
            {
                int absCharge = Math.Abs(atomSymbol.FormalCharge);
                chargeString = absCharge + "-";
            }

            var xCoord = bounds.CenterX();
            var yCoord = bounds.CenterY();
            if (atomSymbol.Alignment == 1)
            { // RIGHT
                this.dc.DrawText(new FormattedText(
                    chargeString,
                    CultureInfo.CurrentCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    atomSymbolBrush),
                    new WPF::Point(xCoord + offset, bounds.Top));
            }
            else if (atomSymbol.Alignment == -1)
            { // LEFT
                this.dc.DrawText(new FormattedText(
                    chargeString,
                    CultureInfo.CurrentCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    atomSymbolBrush),
                    new WPF::Point(xCoord - offset, bounds.Top));
            }
            else if (atomSymbol.Alignment == 2)
            { // TOP
                this.dc.DrawText(new FormattedText(
                    chargeString,
                    CultureInfo.CurrentCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    atomSymbolBrush),
                    new WPF::Point(xCoord, yCoord - offset));
            }
            else if (atomSymbol.Alignment == -2)
            { // BOT
                this.dc.DrawText(new FormattedText(
                    chargeString,
                    CultureInfo.CurrentCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    atomSymbolBrush),
                    new WPF::Point(xCoord, yCoord + offset));
            }
        }

        private void Visit(RectangleElement rectangle)
        {
            var width = rectangle.Width;
            var height = rectangle.Height;
            var p = rectangle.Coord;
            var rect = new WPF.Rect(p.X, p.Y, width, height);
            if (rectangle.Filled)
            {
                this.dc.DrawRectangle(GetBrush(rectangle.Color), null, rect);
            }
            else
            {
                this.dc.DrawRectangle(null, GetPen(rectangle.Color, 1), rect);
            }
        }

        private void Visit(GeneralPath path)
        {
            if (path.Fill)
            {
                this.dc.DrawGeometry(
                    GetBrush(path.Color),
                    null,
                    path.Elements);
            }
            else
            {
                this.dc.DrawGeometry(null, GetPen(path.Color, path.StrokeWith), path.Elements);
            }
        }

        private void Visit(ArrowElement line)
        {
            var pen = GetPen(line.Color, line.Width);

            var a = line.Start;
            var b = line.End;
            dc.DrawLine(pen, a, b);
            double aW = rendererModel.GetArrowHeadWidth();
            if (line.Direction)
            {
                var c = new WPF.Point(line.Start.X - aW, line.Start.Y - aW);
                var d = new WPF.Point(line.Start.X - aW, line.Start.Y + aW);
                dc.DrawLine(pen, a, c);
                dc.DrawLine(pen, a, d);
            }
            else
            {
                var c = new WPF.Point(line.End.X + aW, line.End.Y - aW);
                var d = new WPF.Point(line.End.X + aW, line.End.Y + aW);
                dc.DrawLine(pen, b, c);
                dc.DrawLine(pen, b, d);
            }
        }

        private void Visit(TextGroupElement textGroup)
        {
            var point = GetTextBasePoint(textGroup.Text, textGroup.Coord, fontManager.CureentTypeface, fontManager.Size);
            var textBounds = GetTextBounds(textGroup.Text, textGroup.Coord, fontManager.CureentTypeface, fontManager.Size);
            this.dc.DrawRectangle(GetBrush(this.BackgroundColor), null, textBounds);
            this.dc.DrawText(new FormattedText(
                textGroup.Text,
                CultureInfo.CurrentCulture,
                WPF.FlowDirection.LeftToRight,
                this.fontManager.CureentTypeface,
                this.fontManager.Size,
                GetBrush(textGroup.Color)),
                new WPF::Point(point.X, point.Y));

            var coord = new WPF::Point(textBounds.CenterX(), textBounds.CenterY());
            var coord1 = textBounds.TopLeft;
            var coord2 = new WPF::Point(point.X + textBounds.Width, textBounds.Bottom);

            var vo = coord2 - coord1;
            var o = new WPF::Size(vo.X, vo.Y);
            foreach (var child in textGroup.children)
            {
                WPF::Point p;

                switch (child.position)
                {
                    case TextGroupElement.Position.NE:
                        p = new WPF::Point(coord2.X, coord1.Y);
                        break;
                    case TextGroupElement.Position.N:
                        p = new WPF::Point(coord1.X, coord1.Y);
                        break;
                    case TextGroupElement.Position.NW:
                        p = new WPF::Point(coord1.X - o.Width, coord1.Y);
                        break;
                    case TextGroupElement.Position.W:
                        p = new WPF::Point(coord1.X - o.Width, coord.Y);
                        break;
                    case TextGroupElement.Position.SW:
                        p = new WPF::Point(coord1.X - o.Width, coord1.Y + o.Height);
                        break;
                    case TextGroupElement.Position.S:
                        p = new WPF::Point(coord1.X, coord2.Y + o.Height);
                        break;
                    case TextGroupElement.Position.SE:
                        p = new WPF::Point(coord2.X, coord2.Y + o.Height);
                        break;
                    case TextGroupElement.Position.E:
                        p = new WPF::Point(coord2.X, coord.Y);
                        break;
                    default:
                        p = new WPF::Point(coord.X, coord.Y);
                        break;
                }

                this.dc.DrawText(new FormattedText(
                    child.text,
                    CultureInfo.InvariantCulture,
                    WPF.FlowDirection.LeftToRight,
                    this.fontManager.CureentTypeface,
                    this.fontManager.Size,
                    GetBrush(textGroup.Color)),
                    p);

                if (child.subscript != null)
                {
                    var childBounds = GetTextBounds(child.text, p, fontManager.CureentTypeface, fontManager.Size);
                    var scx = p.X + (childBounds.Width * 0.75);
                    var scy = p.Y + (childBounds.Height / 3);
                    this.dc.DrawText(new FormattedText(
                        child.subscript,
                        CultureInfo.InvariantCulture,
                        WPF.FlowDirection.LeftToRight,
                        this.fontManager.CureentTypeface,
                        this.fontManager.Size - 2,
                        GetBrush(textGroup.Color)),
                        p);
                }
            }
        }

        /// <inheritdoc/>
        public override void Visit(IRenderingElement element, Transform transform)
        {
            this.dc.PushTransform(transform);
            Visit(element);
            this.dc.Pop();
        }

        /// <inheritdoc/>
        public override void Visit(IRenderingElement element)
        {
            switch (element)
            {
                case ElementGroup e:
                    Visit(e);
                    break;
                case WedgeLineElement e:
                    Visit(e);
                    break;
                case LineElement e:
                    Visit(e);
                    break;
                case OvalElement e:
                    Visit(e);
                    break;
                case TextGroupElement e:
                    Visit(e);
                    break;
                case AtomSymbolElement e:
                    Visit(e);
                    break;
                case TextElement e:
                    Visit(e);
                    break;
                case RectangleElement e:
                    Visit(e);
                    break;
                case GeneralPath e:
                    Visit(e);
                    break;
                case ArrowElement e:
                    Visit(e);
                    break;
                case Bounds e:
                    Visit(e.Root);
                    break;
                case MarkedElement e:
                    Visit(e.Element());
                    break;
                default:
                    Console.Error.WriteLine($"Visitor method for {element.GetType()} is not implemented.");
                    break;
            }
        }

        public override IFontManager FontManager
        {
            get => this.fontManager;
            set => this.fontManager = (WPFFontManager)value;
        }
    }
}
