using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Common.DataStructure;

namespace Msdial.Dendrogram
{
    public class DendrogramPlotFE : FrameworkElement
    {
        private VisualCollection visualCollection;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;

        private DendrogramPlotUI dendrogramPlotUI;
        private DendrogramPlotBean dendrogramPlotBean;

        private double longScaleSize = 5;

        private readonly Brush graphBackGround = Brushes.WhiteSmoke;
        private readonly Pen graphBorder = new Pen(Brushes.Black, 0.5);
        private readonly Pen graphLine = new Pen(Brushes.Black, 1);
        private readonly Pen graphLineSelected = new Pen(Brushes.Red, 2);
        private readonly Pen graphAxis = new Pen(Brushes.Black, 1);
        private readonly Brush rubberForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        private readonly Pen rubberBorder = new Pen(Brushes.DarkGray, 1);

        public DendrogramPlotFE(DendrogramPlotBean dendrogramPlotBean, DendrogramPlotUI dendrogramPlotUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.dendrogramPlotBean = dendrogramPlotBean;
            this.dendrogramPlotUI = dendrogramPlotUI;
        }

        public void DendrogramPlotDraw()
        {
            this.visualCollection.Clear();
            this.drawingVisual = dendrogramPlotDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual dendrogramPlotDrawingVisual(double drawWidth, double drawHeight)
        {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.dendrogramPlotUI.LeftMargin + this.dendrogramPlotUI.RightMargin ||
                drawHeight < this.dendrogramPlotUI.BottomMargin + this.dendrogramPlotUI.TopMargin)
                return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // 1. Draw background, graphRegion
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(
                this.graphBackGround, this.graphBorder,
                new Rect(
                    new Point(this.dendrogramPlotUI.LeftMargin, this.dendrogramPlotUI.TopMargin),
                    new Size(
                        drawWidth - this.dendrogramPlotUI.LeftMargin - this.dendrogramPlotUI.RightMargin,
                        drawHeight - this.dendrogramPlotUI.BottomMargin - this.dendrogramPlotUI.TopMargin
                    )
                )
            );

            // 2. Draw graph title
            drawGraphTitle(this.dendrogramPlotBean.GraphTitle);

            // 3. Check null of dendrogramPlotBean
            if (this.dendrogramPlotBean.Dendrogram == null)
            {
                this.drawingContext.Close();
                return this.drawingVisual;
            }

            // 4. Draw dendrogram
            this.drawingContext.PushTransform(new TranslateTransform(
                this.dendrogramPlotUI.LeftMargin, this.dendrogramPlotUI.TopMargin
            ));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(
                0, 0,
                drawWidth - this.dendrogramPlotUI.LeftMargin - this.dendrogramPlotUI.RightMargin,
                drawHeight - this.dendrogramPlotUI.BottomMargin - this.dendrogramPlotUI.TopMargin
            )));
            this.drawingContext.PushTransform(new ScaleTransform(
                1, -1, 0, (drawHeight-this.dendrogramPlotUI.TopMargin-this.dendrogramPlotUI.BottomMargin)/2
            ));
            drawGraph(
                drawWidth - this.dendrogramPlotUI.LeftMargin - this.dendrogramPlotUI.RightMargin,
                drawHeight - this.dendrogramPlotUI.BottomMargin - this.dendrogramPlotUI.TopMargin,
                this.dendrogramPlotBean.Dendrogram, this.dendrogramPlotBean.Root
            );
            this.drawingContext.Pop();
            this.drawingContext.Pop();
            this.drawingContext.Pop();

            // 5. Draw X axis
            this.drawingContext.PushTransform(new TranslateTransform(
                this.dendrogramPlotUI.LeftMargin, drawHeight - this.dendrogramPlotUI.BottomMargin
            ));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(
                0, 0,
                drawWidth - this.dendrogramPlotUI.LeftMargin - this.dendrogramPlotUI.RightMargin,
                drawHeight - this.dendrogramPlotUI.TopMargin
            )));
            drawScaleOnXAxis(
                drawWidth - this.dendrogramPlotUI.LeftMargin - this.dendrogramPlotUI.RightMargin,
                this.dendrogramPlotUI.BottomMargin,
                this.dendrogramPlotBean.LabelCollection,
                this.dendrogramPlotBean.LeafIdxs.Select(i => this.dendrogramPlotBean.XPositions[i])
            );
            this.drawingContext.Pop();
            this.drawingContext.Pop();

            // 6. Draw Y axis
            this.drawingContext.PushTransform(new TranslateTransform(0, this.dendrogramPlotUI.TopMargin));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(
                0, 0,
                this.dendrogramPlotUI.LeftMargin,
                drawHeight - this.dendrogramPlotUI.TopMargin - this.dendrogramPlotUI.BottomMargin
            )));
            drawScaleOnYAxis(
                this.dendrogramPlotUI.LeftMargin,
                drawHeight - this.dendrogramPlotUI.TopMargin - this.dendrogramPlotUI.BottomMargin
            );
            this.drawingContext.Pop();
            this.drawingContext.Pop();

            this.drawingContext.Close();

            return this.drawingVisual;
        }

        void drawGraphTitle(string graphTitle)
        {
            var formattedText = new FormattedText(
                graphTitle, CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight, new Typeface("Calibri"),
                15, Brushes.Black
            )
            {
                TextAlignment = TextAlignment.Left
            };
            this.drawingContext.DrawText(
                formattedText,
                new Point(this.dendrogramPlotUI.LeftMargin, this.dendrogramPlotUI.TopMargin - formattedText.Height - 2)
            );
        }

        void drawScaleOnXAxis(double drawWidth, double drawHeight, IEnumerable<string> labels, IEnumerable<double> points)
        {
            var xMin = this.dendrogramPlotBean.DisplayMinX;
            var xMax = this.dendrogramPlotBean.DisplayMaxX;
            var pls = points.Select(p => drawWidth / (xMax - xMin) * (p - xMin)).Zip(labels, Tuple.Create).OrderBy(pl => pl.Item1).ToArray();
            var n = pls.Length;
            var centers = new List<double>(n + 2) { -(1 << 30) };
            centers.AddRange(pls.Select(p => p.Item1));
            centers.Add(1 << 30);
            var orderedLabels = pls.Select(p => p.Item2).ToArray();

            for(int i = 0; i < n; ++i)
            {
                (double pos, string label) = pls[i];
                double labelWidth = Math.Min(centers[i+1]-centers[i], centers[i+2]-centers[i+1]);
                var formattedText = new FormattedText(
                    label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibri"), 13, Brushes.Black
                )
                {
                    MaxTextWidth = labelWidth,
                    MaxTextHeight = drawHeight,
                    TextAlignment = TextAlignment.Center
                };
                this.drawingContext.DrawText(
                    formattedText,
                    new Point(centers[i + 1] - labelWidth / 2, this.longScaleSize)
                );
            }
        }
        void drawScaleOnYAxis(double drawWidth, double drawHeight)
        {
            this.drawingContext.DrawLine(
                this.graphAxis,
                new Point(drawWidth - 5, 0),
                new Point(drawWidth - 5, drawHeight)
            );

            double yMax = this.dendrogramPlotBean.DisplayMaxY;
            double yMin = this.dendrogramPlotBean.DisplayMinY;

            double dtick = Math.Pow(10, Math.Floor(Math.Log10(yMax - yMin)));

            double axMin = Math.Floor(yMin / dtick) * dtick;
            double axMax = Math.Ceiling(yMax / dtick) * dtick;

            double pos = axMin;
            while (pos <= axMax)
            {
                var formattedText = new FormattedText(
                    pos.ToString(), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface("Calibri"),
                    13, Brushes.Black
                )
                {
                    TextAlignment = TextAlignment.Right
                };
                var y = drawHeight - (pos - yMin) / (yMax - yMin) * drawHeight;
                this.drawingContext.DrawText(
                    formattedText,
                    new Point(drawWidth - 10, y - formattedText.Height / 2)
                );
                this.drawingContext.DrawLine(
                    this.graphAxis,
                    new Point(drawWidth - 10, y),
                    new Point(drawWidth - 5, y)
                );

                pos += dtick;
            }
        }

        void drawGraph(double drawWidth, double drawHeight, DirectedTree tree, int root)
        {
            var xMin = this.dendrogramPlotBean.DisplayMinX;
            var xMax = this.dendrogramPlotBean.DisplayMaxX;
            var yMin = this.dendrogramPlotBean.DisplayMinY;
            var yMax = this.dendrogramPlotBean.DisplayMaxY;
            var xPosition = this.dendrogramPlotBean.XPositions.Select(p => drawWidth / (xMax - xMin) * (p - xMin)).ToArray();
            var yPosition = this.dendrogramPlotBean.YPositions.Select(p => drawHeight / (yMax - yMin) * (p - yMin)).ToArray();
            var selected = this.dendrogramPlotBean.SelectedIdx;
            bool drawDfs(int v, int p)
            {
                var res = selected != -1 && v == this.dendrogramPlotBean.LeafIdxs[selected];
                foreach (var e in tree[v])
                {
                    var vpoint = new Point(xPosition[v], yPosition[v]);
                    if (e.To != p)
                    {
                        var b = drawDfs(e.To, v);
                        res |= b;
                        var pen = b ? this.graphLineSelected : this.graphLine;
                        var midpoint = new Point(xPosition[e.To], yPosition[v]);
                        this.drawingContext.DrawLine(pen, new Point(xPosition[e.To], yPosition[e.To]), midpoint);
                        this.drawingContext.DrawLine(pen, midpoint, vpoint);
                    }
                }
                return res;
            }
            drawDfs(root, -1);
        }

        public void UpdateGraphRange(Point p, Point q)
        {
            var displayMinX = this.dendrogramPlotBean.DisplayMinX;
            var displayMaxX = this.dendrogramPlotBean.DisplayMaxX;
            var displayMinY = this.dendrogramPlotBean.DisplayMinY;
            var displayMaxY = this.dendrogramPlotBean.DisplayMaxY;

            var minFocusX = Math.Min(p.X, q.X);
            var maxFocusX = Math.Max(p.X, q.X);
            var minFocusY = Math.Min(p.Y, q.Y);
            var maxFocusY = Math.Max(p.Y, q.Y);

            if (maxFocusX - minFocusX < 5 || maxFocusY - minFocusY < 5)
                return;

            var minX = this.dendrogramPlotUI.LeftMargin;
            var maxX = this.ActualWidth - this.dendrogramPlotUI.RightMargin;
            var minY = this.dendrogramPlotUI.TopMargin;
            var maxY = this.ActualHeight - this.dendrogramPlotUI.BottomMargin;

            if (maxFocusX - minFocusX < maxX - minX &&
                displayMaxX - displayMinX < (this.dendrogramPlotBean.ValueMaxX - this.dendrogramPlotBean.ValueMinX) * 0.01 ||
                maxFocusY - minFocusY < maxY - minY &&
                displayMaxY - displayMinY < (this.dendrogramPlotBean.ValueMaxY - this.dendrogramPlotBean.ValueMinY) * 0.01)
                return;

            this.dendrogramPlotBean.DisplayMinX = (displayMaxX - displayMinX) / (maxX - minX) * (minFocusX - minX) + displayMinX;
            this.dendrogramPlotBean.DisplayMaxX = (displayMaxX - displayMinX) / (maxX - minX) * (maxFocusX - minX) + displayMinX;
            this.dendrogramPlotBean.DisplayMinY = displayMaxY - (displayMaxY - displayMinY) / (maxY - minY) * (maxFocusY - minY);
            this.dendrogramPlotBean.DisplayMaxY = displayMaxY - (displayMaxY - displayMinY) / (maxY - minY) * (minFocusY - minY);
        }

        public void ZoomGraph(Point p, int delta)
        {
            var scale = 1 + 0.1 * Math.Sign(delta);

            var minNextX = p.X - (p.X - this.dendrogramPlotUI.LeftMargin) * scale;
            var maxNextX = p.X + (this.dendrogramPlotUI.ActualWidth - this.dendrogramPlotUI.RightMargin - p.X) * scale;
            var minNextY = p.Y - (p.Y - this.dendrogramPlotUI.TopMargin) * scale;
            var maxNextY = p.Y + (this.dendrogramPlotUI.ActualHeight - this.dendrogramPlotUI.BottomMargin - p.Y) * scale;

            UpdateGraphRange(new Point(minNextX, minNextY), new Point(maxNextX, maxNextY));
        }

        public void MoveGraph(Vector offset)
        {
            UpdateGraphRange(new Point(this.dendrogramPlotUI.LeftMargin, this.dendrogramPlotUI.TopMargin) + offset,
                             new Point(this.ActualWidth - this.dendrogramPlotUI.RightMargin, this.ActualHeight - this.dendrogramPlotUI.BottomMargin) + offset);
        }

        public void DrawZoomRubber(Point p, Point q)
        {
            if (this.visualCollection.Count > 1) this.visualCollection.RemoveAt(this.visualCollection.Count - 1);

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.DrawRectangle(this.rubberForeground, this.rubberBorder, new Rect(p, q));
            }
            this.visualCollection.Add(drawingVisual);
        }

        #region // Required Method for VisualCollection Object
        protected override int VisualChildrenCount
        {
            get => visualCollection.Count;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= visualCollection.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return visualCollection[index];
        }
        #endregion
    }
}
