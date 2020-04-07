using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Common.DataStructure;

namespace PlottingControls.Dendrogram
{
    static class DendrogramPainter
    {
        private static readonly Pen graphLine  = new Pen(Brushes.Black, 1);

        static public void DrawTree(
            DrawingContext drawingContext,
            DirectedTree tree, int root,
            IEnumerable<double> xPositions, IEnumerable<double> yPositions,
            Point point, Vector vector,
            double xMin, double xMax,
            double yMin, double yMax
        )
        {
            if (xMax == xMin) return;
            if (yMax == yMin) return;

            double mapX(double x) => vector.X / (xMax - xMin) * (x - xMin) + point.X;
            double mapY(double y) => vector.Y / (yMax - yMin) * (y - yMin) + point.Y;

            var xPos = xPositions.Select(mapX).ToArray();
            var yPos = yPositions.Select(mapY).ToArray();

            void drawDfs(int v, int p)
            {
                foreach (var e in tree[v])
                {
                    var vpoint = new Point(xPos[v], yPos[v]);
                    if (e.To != p)
                    {
                        drawDfs(e.To, v);
                        var midpoint = new Point(xPos[e.To], yPos[v]);
                        drawingContext.DrawLine(graphLine, new Point(xPos[e.To], yPos[e.To]), midpoint);
                        drawingContext.DrawLine(graphLine, midpoint, vpoint);
                    }
                }
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(point, vector)));
            drawingContext.PushTransform(new MatrixTransform(1, 0, 0, -1, 0, point.Y * 2 + vector.Y));
            drawDfs(root, -1);
            drawingContext.Pop();
            drawingContext.Pop();
        }
    }
}
