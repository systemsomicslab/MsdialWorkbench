using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    /*
    class CategoryVerticalAxisElement : IDrawingElement
    {
        static readonly double ticksize = 5;

        List<(FormattedText text, double ypos)> texts;
        int limit = 20;

        public CategoryVerticalAxisElement(IReadOnlyList<double> yPositions, IReadOnlyList<string> labels_, int lim)
        {
            limit = lim;
            texts = new List<(FormattedText, double)>(yPositions.Count);
            string[] labels = labels_?.ToArray() ?? Enumerable.Repeat(string.Empty, yPositions.Count).ToArray();
            foreach((double pos, string label) in yPositions.Zip(labels, Tuple.Create))
            {
                texts.Add((
                    new FormattedText(label, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibli"), 12, Brushes.Black, 1),
                    pos
                    ));
            }
            texts.Sort((a, b) => a.ypos.CompareTo(b.ypos));
            ElementArea = new Rect(new Point(0, yPositions.Min()), new Point(100, yPositions.Max()));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, 0), new Point(size.Width, size.Height)));
            var intexts = texts.SkipWhile(p => p.ypos < rect.Top).TakeWhile(p => p.ypos <= rect.Bottom);
            var n = intexts.Count();
            var pertext = (int)(n / limit);
            if (pertext < 1)
                pertext = 1;
            var counter = 0;
            foreach(var text in intexts)
            {
                var y = (text.ypos - rect.Y) / rect.Height * size.Height;
                geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, y), new Point(size.Width - ticksize, y)));
                if(counter++ % pertext == 0)
                {
                    var height = text.text.Height;
                    var width = text.text.Width;
                    text.text.MaxTextWidth = size.Width - ticksize;
                    var geotext = text.text.BuildGeometry(new Point(size.Width - width - 2, y - height / 2));
                    geometryGroup.Children.Add(geotext);
                }
            }
            return geometryGroup;
        }
    }
    */

    class CategoryVerticalAxisTickElement : IDrawingElement
    {
        public double Ticksize { get; } = 5;
        int limit;

        List<double> positions;
        public CategoryVerticalAxisTickElement(IReadOnlyList<double> yPositions, int limit_ = -1)
        {
            limit = limit_;
            positions = yPositions.ToList();
            positions.Sort();
            ElementArea = new Rect(new Point(0, yPositions.Min()), new Point(100, yPositions.Max()));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, 0), new Point(size.Width, size.Height)));
            var inrange = positions.SkipWhile(p => p < rect.Top).TakeWhile(p => p <= rect.Bottom);
            var n = inrange.Count();
            var lim = limit == -1 ? n : limit;
            if (lim == 0) return geometryGroup;
            var pertext = (int)(n / Math.Min(n, lim));
            var counter = 0;
            foreach(var pos in inrange)
            {
                if(counter++ % pertext == 0)
                {
                    var y = (pos - rect.Y) / rect.Height * size.Height;
                    geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, y), new Point(size.Width - Ticksize, y)));
                }
            }
            return geometryGroup;
        }
    }

    class CategoryVerticalAxisLabelElement : IDrawingElement
    {
        List<(FormattedText text, double ypos)> texts;
        int limit = 20;

        public CategoryVerticalAxisLabelElement(IReadOnlyList<double> yPositions, IReadOnlyList<string> labels_, int lim)
        {
            limit = lim;
            texts = new List<(FormattedText, double)>(yPositions.Count);
            string[] labels = labels_?.ToArray() ?? Enumerable.Repeat(string.Empty, yPositions.Count).ToArray();
            foreach((double pos, string label) in yPositions.Zip(labels, Tuple.Create))
            {
                texts.Add((
                    new FormattedText(label, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibli"), 12, Brushes.Black, 1),
                    pos
                    ));
            }
            texts.Sort((a, b) => a.ypos.CompareTo(b.ypos));
            ElementArea = new Rect(new Point(0, yPositions.Min()), new Point(100, yPositions.Max()));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            var intexts = texts.SkipWhile(p => p.ypos < rect.Top).TakeWhile(p => p.ypos <= rect.Bottom);
            var n = intexts.Count();
            var pertext = (int)(n / limit);
            if (pertext < 1)
                pertext = 1;
            var counter = 0;
            foreach(var text in intexts)
            {
                var y = (text.ypos - rect.Y) / rect.Height * size.Height;
                if(counter++ % pertext == 0)
                {
                    text.text.MaxTextWidth = size.Width;
                    var height = text.text.Height;
                    var width = text.text.Width;
                    var geotext = text.text.BuildGeometry(new Point(size.Width - width, y - height / 2));
                    geometryGroup.Children.Add(geotext);
                }
            }
            return geometryGroup;
        }
    }
}
