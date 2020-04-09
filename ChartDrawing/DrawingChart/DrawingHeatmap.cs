using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Core.ChartData;

namespace CompMs.Graphics.Core.Heatmap
{
    public class DrawingHeatmap : DefaultDrawingChart
    {
        public override void Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData)
        {
            if (chartData is MatrixChartData)
            {
                var data = chartData as MatrixChartData;
                var m = data.MatrixData.GetLength(0);
                var n = data.MatrixData.GetLength(1);

                // double mapX(double x) => vector.X / (xMax - xMin) * (x - xMin) + point.X;
                // double mapY(double y) => vector.Y / (yMax - yMin) * (y - yMin) + point.Y;

                var brushMemo = new Dictionary<(byte, byte, byte, byte), Brush>();

                drawingContext.PushClip(new RectangleGeometry(new Rect(point, size)));
                if (zMax - zMin > Math.Pow(10, -10))
                {
                    for(int i=0; i < m; ++i)
                    {
                        for(int j = 0; j < n; ++j)
                        {
                            var c = getGradientColor(
                                gbrush.GradientStops,
                                (Math.Max(Math.Min(matrix[i, j], zMax), zMin) - zMin) / (zMax - zMin)
                            );
                            Brush brush;
                            if (brushMemo.ContainsKey((c.A, c.R, c.G, c.B)))
                            {
                                brush = brushMemo[(c.A, c.R, c.G, c.B)];
                            }
                            else
                            {
                                brush = new SolidColorBrush(c);
                                brushMemo[(c.A, c.R, c.G, c.B)] = brush;
                                brushMemo[(c.A, c.R, c.G, c.B)].Freeze();
                            }
                            drawingContext.DrawRectangle(
                                brush, null, //borderLine,
                                new Rect(
                                    new Point(mapX(xBorders[i]), mapY(yBorders[n-j])),
                                    new Point(mapX(xBorders[i+1]), mapY(yBorders[n-j-1])))
                            );
                        }
                    }
                }
                drawingContext.Pop();
                drawingContext.Pop();
            }
        }

        public void Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData, Brush brush)
        {

        }

        static Color blendColors(Color ca, Color cb, double factor)
        {
            var f = (float)factor;
            return cb * f + ca * (1 - f);
        }

        static Color getGradientColor(GradientStopCollection gsc, double offset)
        {
            var lowers = gsc.Where(gs => gs.Offset <= offset).ToArray();
            var highers = gsc.Where(gs => gs.Offset > offset).ToArray();
            if (offset < 0) return highers.Min(gs => (gs.Offset, gs.Color)).Color;
            if (offset >= 1) return lowers.Max(gs => (gs.Offset, gs.Color)).Color;

            var lo = lowers.Max(gs => (gs.Offset, gs.Color));
            var hi = highers.Min(gs => (gs.Offset, gs.Color));
            var o = ((offset - lo.Offset) / (hi.Offset - lo.Offset));
            return blendColors(lo.Color, hi.Color, o);
        }
    }
}
