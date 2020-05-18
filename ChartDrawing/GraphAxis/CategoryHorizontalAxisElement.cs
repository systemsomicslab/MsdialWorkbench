using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    class CategoryHorizontalAxisTickElement : IDrawingElement
    {
        public double Ticksize { get; } = 5;
        int limit;

        List<double> positions;
        public CategoryHorizontalAxisTickElement(IReadOnlyList<double> xPositions, int limit_ = -1)
        {
            limit = limit_;
            positions = xPositions.ToList();
            positions.Sort();
            ElementArea = new Rect(new Point(xPositions.Min(), 0), new Point(xPositions.Max(), 100));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(size.Width, 0)));
            var inrange = positions.SkipWhile(p => p < rect.Left).TakeWhile(p => p <= rect.Right);
            var n = inrange.Count();
            var lim = limit == -1 ? n : Math.Min(n, limit);
            if (lim == 0) return geometryGroup;
            var pertext = (n + lim - 1) / lim;
            var counter = 0;
            foreach(var pos in inrange)
            {
                if(counter++ % pertext == 0)
                {
                    var x = (pos - rect.X) / rect.Width * size.Width;
                    geometryGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x, Ticksize)));
                }
            }
            return geometryGroup;
        }
    }

    class CategoryHorizontalAxisLabelElement : IDrawingElement
    {
        List<(FormattedText text, double xpos)> texts;
        int limit = 20;

        public CategoryHorizontalAxisLabelElement(IReadOnlyList<double> xPositions, IReadOnlyList<string> labels_, int lim)
        {
            limit = lim;
            texts = new List<(FormattedText, double)>(xPositions.Count);
            string[] labels = labels_?.Concat(Enumerable.Repeat(String.Empty, xPositions.Count)).ToArray();
            if (labels_ == null)
                labels = Enumerable.Repeat(String.Empty, xPositions.Count).ToArray();
            foreach((double pos, string label) in xPositions.Zip(labels, Tuple.Create))
            {
                texts.Add((
                    new FormattedText(label, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibli"), 12, Brushes.Black, 1),
                    pos
                    ));
            }
            texts.Sort((a, b) => a.xpos.CompareTo(b.xpos));
            ElementArea = new Rect(new Point(xPositions.Min(), 0), new Point(xPositions.Max(), 100));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            var intexts = texts.SkipWhile(p => p.xpos < rect.Left).TakeWhile(p => p.xpos <= rect.Right);
            var n = intexts.Count();
            var lim = limit == -1 ? n : Math.Min(n, limit);
            if (lim == 0) return geometryGroup;
            var pertext = (n + lim - 1) / lim;
            var counter = 0;
            var maxwidth = size.Width / (lim + 1);
            foreach(var text in intexts)
            {
                if(counter++ % pertext == 0)
                {
                    var x = (text.xpos - rect.X) / rect.Width * size.Width;
                    text.text.MaxTextWidth = maxwidth;
                    text.text.MaxTextHeight = size.Height * 0.8;
                    var height = text.text.Height;
                    var width = text.text.Width;
                    var geotext = text.text.BuildGeometry(new Point(x - width / 2, 0));
                    geometryGroup.Children.Add(geotext);
                }
            }
            return geometryGroup;
        }
    }
}
