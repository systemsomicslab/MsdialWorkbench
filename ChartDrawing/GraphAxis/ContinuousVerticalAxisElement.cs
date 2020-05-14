using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    class ContinuousVerticalAxisTickElement : IDrawingElement
    {
        public double LongTicksize { get; } = 10;
        public double ShortTicksize { get; } = 5;

        public double MinY { get; }
        public double MaxY { get; }

        public ContinuousVerticalAxisTickElement(double minY, double maxY)
        {
            MinY = minY;
            MaxY = maxY;
            ElementArea = new Rect(new Point(0, MinY), new Point(100, MaxY));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, 0), new Point(size.Width, size.Height)));

            double exp = Math.Floor(Math.Log10(rect.Height));
            decimal LongTickInterval = (decimal)Math.Pow(10, exp);
            decimal ShortTickInterval;

            if (LongTickInterval == 0) return geometryGroup;
            var fold = (decimal)rect.Height / LongTickInterval;
            if (fold >= 5) ShortTickInterval = LongTickInterval * (decimal)0.5;
            else if (fold >= 2) ShortTickInterval = LongTickInterval * (decimal)0.25;
            else ShortTickInterval = LongTickInterval * (decimal)0.1;

            for(var i = Math.Ceiling((decimal)rect.Top / LongTickInterval); i * LongTickInterval <= (decimal)rect.Bottom; ++i)
            {
                var y = (rect.Bottom - (double)(i * LongTickInterval)) / rect.Height * size.Height;
                geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, y), new Point(size.Width - LongTicksize, y)));
            }
            if (ShortTickInterval == 0) return geometryGroup;
            for(var i = Math.Ceiling((decimal)rect.Top / ShortTickInterval); i * ShortTickInterval <= (decimal)rect.Bottom; ++i)
            {
                var y = (rect.Bottom - (double)(i * ShortTickInterval)) / rect.Height * size.Height;
                geometryGroup.Children.Add(new LineGeometry(new Point(size.Width, y), new Point(size.Width - ShortTicksize, y)));
            }
            return geometryGroup;
        }
    }

    class ContinuousVerticalAxisLabelElement : IDrawingElement
    {
        public double MinY { get; }
        public double MaxY { get; }

        public ContinuousVerticalAxisLabelElement(double minY, double maxY)
        {
            MinY = minY;
            MaxY = maxY;
            ElementArea = new Rect(new Point(0, MinY), new Point(100, MaxY));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var exp = Math.Floor(Math.Log10(rect.Bottom));
            var TickInterval = (decimal)Math.Pow(10, Math.Floor(Math.Log10(rect.Height)));
            string f = exp > 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";

            var geometryGroup = new GeometryGroup();

            if (TickInterval == 0) return geometryGroup;
            for(var i = Math.Ceiling((decimal)rect.Top / TickInterval); i * TickInterval <= (decimal)rect.Bottom; ++i)
            {
                var y = (rect.Bottom - (double)(i * TickInterval)) / rect.Height * size.Height;
                var formattedtext = new FormattedText(
                    ((double)(i * TickInterval)).ToString(f), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface("Calibii"), 12, Brushes.Black, 1
                    );
                formattedtext.MaxTextWidth = size.Width;
                var height = formattedtext.Height;
                var width = formattedtext.Width;
                var geotext = formattedtext.BuildGeometry(new Point(size.Width - width, y - height / 2));
                geometryGroup.Children.Add(geotext);
            }
            return geometryGroup;
        }
    }
}
