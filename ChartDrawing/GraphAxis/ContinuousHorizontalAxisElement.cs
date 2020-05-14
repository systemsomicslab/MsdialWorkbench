using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.GraphAxis
{
    class ContinuousHorizontalAxisTickElement : IDrawingElement
    {
        public double LongTicksize { get; } = 10;
        public double ShortTicksize { get; } = 5;

        public double MinX { get; }
        public double MaxX { get; }

        public ContinuousHorizontalAxisTickElement(double minX, double maxX)
        {
            MinX = minX;
            MaxX = maxX;
            ElementArea = new Rect(new Point(MinX, 0), new Point(MaxX, 100));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new LineGeometry(new Point(0, 0), new Point(size.Width, 0)));

            double exp = Math.Floor(Math.Log10(rect.Width));
            decimal LongTickInterval = (decimal)Math.Pow(10, exp);
            decimal ShortTickInterval;

            if (LongTickInterval == 0) return geometryGroup;
            var fold = (decimal)rect.Width / LongTickInterval;
            if (fold >= 5) ShortTickInterval = LongTickInterval * (decimal)0.5;
            else if (fold >= 2) ShortTickInterval = LongTickInterval * (decimal)0.25;
            else ShortTickInterval = LongTickInterval * (decimal)0.1;

            for(var i = Math.Ceiling((decimal)rect.Left / LongTickInterval); i * LongTickInterval <= (decimal)rect.Right; ++i)
            {
                var x = ((double)(i * LongTickInterval) - rect.X) / rect.Width * size.Width;
                geometryGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x, LongTicksize)));
            }
            if (ShortTickInterval == 0) return geometryGroup;
            for(var i = Math.Ceiling((decimal)rect.Left / ShortTickInterval); i * ShortTickInterval <= (decimal)rect.Right; ++i)
            {
                var x = ((double)(i * ShortTickInterval) - rect.X) / rect.Width * size.Width;
                geometryGroup.Children.Add(new LineGeometry(new Point(x, 0), new Point(x, ShortTicksize)));
            }
            return geometryGroup;
        }
    }

    class ContinuousHorizontalAxisLabelElement : IDrawingElement
    {
        public double MinX { get; }
        public double MaxX { get; }

        public ContinuousHorizontalAxisLabelElement(double minX, double maxX)
        {
            MinX = minX;
            MaxX = maxX;
            ElementArea = new Rect(new Point(0, MinX), new Point(100, MaxX));
        }

        public Rect ElementArea { get; }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var exp = Math.Floor(Math.Log10(rect.Right));
            var TickInterval = (decimal)Math.Pow(10, Math.Floor(Math.Log10(rect.Width)));
            string f = exp > 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";

            var geometryGroup = new GeometryGroup();

            if (TickInterval == 0) return geometryGroup;
            var maxWidth = (double)TickInterval / rect.Width * size.Width;
            for(var i = Math.Ceiling((decimal)rect.Left / TickInterval); i * TickInterval <= (decimal)rect.Right; ++i)
            {
                var x = ((double)(i * TickInterval) - rect.X) / rect.Width * size.Width;
                var formattedtext = new FormattedText(
                    ((double)(i * TickInterval)).ToString(f), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface("Calibii"), 12, Brushes.Black, 1
                    );
                formattedtext.MaxTextWidth = maxWidth;
                formattedtext.MaxTextHeight = size.Height * 0.8;
                var height = formattedtext.Height;
                var width = formattedtext.Width;
                var geotext = formattedtext.BuildGeometry(new Point(x - width / 2, 0));
                geometryGroup.Children.Add(geotext);
            }
            return geometryGroup;
        }
    }
}
