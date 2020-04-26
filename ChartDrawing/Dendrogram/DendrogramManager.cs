using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Common.DataStructure;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Dendrogram
{
    public class DendrogramManager : IChartManager
    {
        readonly Pen graphLine  = new Pen(Brushes.Black, 1);

        /*
        public Rect ElementArea
        {
            get => elementArea;
            set => elementArea = value;
        }
        Rect elementArea;
        public Transform TransformElement { get; set; } = Transform.Identity;
        */

        public Rect ChartArea { get; }
        public IReadOnlyList<double> XPositions { get; }
        public IReadOnlyList<double> YPositions { get; }

        DendrogramElement dendrogramElement;

        public DendrogramManager(DirectedTree tree, IReadOnlyList<double> xPositions, IReadOnlyList<double> yPositions)
        {
            var root = tree.Root;
            var XPositions_ = xPositions?.ToArray();
            if (XPositions_ == null)
            {
                XPositions_ = new double[tree.Count];
                var leaves = tree.Leaves.ToHashSet();
                var leafId = 0;
                tree.PostOrder(root, e =>
                {
                    if (leaves.Contains(e.To))
                        XPositions_[e.To] = leafId++;
                    else
                        XPositions_[e.To] = tree[e.To].Average(z => XPositions_[z.To]);
                });
                XPositions_[root] = leaves.Contains(root) ? 0d : tree[root].Average(z => XPositions_[z.To]);
            }
            XPositions = XPositions_;

            var YPositions_ = yPositions?.ToArray();
            if(YPositions_ == null)
            {
                YPositions_ = new double[tree.Count];
                tree.PreOrder(root, e => YPositions_[e.To] = YPositions_[e.From] + e.Distance);
            }
            var maxy = YPositions_.Max();
            YPositions = YPositions_.Select(pos => maxy - pos).ToArray();

            dendrogramElement = new DendrogramElement(tree, XPositions, YPositions);
            var area = dendrogramElement.ElementArea;
            area.Inflate(area.Width * 0.05, 0);
            area.Height *= 1.05;
            ChartArea = area;
        }

        public Drawing CreateChart(Rect rect, Size size)
        {
            var geometry = dendrogramElement.GetGeometry(rect, size);
            // geometry.Transform = new MatrixTransform(1, 0, 0, -1, -ElementArea.Left, ElementArea.Bottom);
            geometry.Transform = new ScaleTransform(1, -1, 0, size.Height / 2);
            return new GeometryDrawing(null, graphLine, geometry);
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return new Point(point.X / size.Width * area.Width + area.X,
                             (size.Height - point.Y) / size.Height * area.Height + area.Y);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return new Vector(vector.X / size.Width * area.Width,
                              vector.Y / size.Height * - area.Height);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return new Rect(Translate(rect.TopLeft, area, size),
                            Translate(rect.BottomRight, area, size));
        }

        public Point Inverse(Point point, Rect area, Size size){
            return new Point((point.X - area.X) / area.Width * size.Width,
                             (1 - (point.Y - area.Y) / area.Height) * size.Height);
        }
        public Vector Inverse(Vector vector, Rect area, Size size){
            return new Vector(vector.X / area.Width * size.Width,
                              - vector.Y / area.Height * size.Height);
        }
        public Rect Inverse(Rect rect, Rect area, Size size){
            return new Rect(Inverse(rect.TopLeft, area, size),
                            Inverse(rect.BottomRight, area, size));
        }

        /*
        public Vector Move(Vector vector)
        {
            var vec = (Vector)TransformElement.Transform((Point)vector);
            return new Matrix(elementArea.Width, 0, 0, elementArea.Height, 0, 0).Transform(vec);
        }
        public Rect Reset()
        {
            var area = dendrogramElement.ElementArea;
            area.Height *= 1.05;
            area.Inflate(area.Width * 0.05, 0);
            return area;
        }
        public Rect UpdateRange(Rect rect)
        {
            var rateArea = TransformElement.TransformBounds(rect);
            return new MatrixTransform(ElementArea.Width, 0, 0, ElementArea.Height, ElementArea.X, ElementArea.Y)
                            .TransformBounds(rateArea);
        }
        public void SizeChanged(Size size)
        {
            // TransformElement = new ScaleTransform(1 / size.Width, - 1 / size.Height, 0, size.Height / 2);
            TransformElement = new ScaleTransform(1 / size.Width, - 1 / size.Height);
        }
        */
    }
}
