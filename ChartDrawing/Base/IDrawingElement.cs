using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;


namespace CompMs.Graphics.Core.Base
{
    public interface IDrawingElement
    {
        Rect ElementArea { get; }
        Geometry GetGeometry(Rect rect, Size size);
    }

    public class LineElement : IDrawingElement
    {
        public Rect ElementArea => new Rect(lineElement.StartPoint, lineElement.EndPoint);

        LineGeometry lineElement;

        public LineElement(Point start, Point end)
        {
            lineElement = new LineGeometry(start, end);
        }
        public Geometry GetGeometry(Rect rect, Size size)
        {
            LineGeometry element = lineElement;
            var transforms = new TransformGroup();
            // transforms.Children.Add(new TranslateTransform(-rect.X, -rect.Y));
            transforms.Children.Add(new TranslateTransform(-rect.Left, -rect.Top));
            transforms.Children.Add(new ScaleTransform(size.Width / rect.Width, size.Height / rect.Height));
            // transforms.Children.Add(new ScaleTransform(10, 10));

            // element.Transform = new MatrixTransform(size.Width / rect.Width, 0, 0, size.Height / rect.Height, rect.X, rect.Y);
            element.Transform = transforms;
            return element;
        }
    }

    public class AreaElement : IDrawingElement
    {
        public Rect ElementArea => rectangleElement.Rect;

        RectangleGeometry rectangleElement;
        public AreaElement(Rect rect)
        {
            rectangleElement = new RectangleGeometry(rect);
        }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            RectangleGeometry element = rectangleElement;
            var transforms = new TransformGroup();
            transforms.Children.Add(new TranslateTransform(-rect.Left, -rect.Top));
            transforms.Children.Add(new ScaleTransform(size.Width / rect.Width, size.Height / rect.Height));
            element.Transform = transforms;
            return element;
        }
    }

    public class TextElement : IDrawingElement
    {
        public Rect ElementArea => new Rect(origin, new Size(textElement.Width, textElement.Height));
        public double MaxTextWidth
        {
            get => textElement.MaxTextWidth;
            set => textElement.MaxTextWidth = value;
        }
        public double MaxTextHeight
        {
            get => textElement.MaxTextHeight;
            set => textElement.MaxTextHeight = value;
        }

        Point origin;

        FormattedText textElement;

        public TextElement(string text, Point topleft)
        {
            origin = topleft;
            textElement = new FormattedText(
                text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                new Typeface("Calibli"), 12, null, 1);
        }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            FormattedText text = textElement;
            var point = new TranslateTransform(-rect.Left, -rect.Top).Transform(origin);
            point = new ScaleTransform(size.Width / rect.Width, size.Height / rect.Height).Transform(point);
            return textElement.BuildGeometry(point);
        }
    }

    public class DrawingElementGroup : IDrawingElement, ICollection<IDrawingElement>, IReadOnlyCollection<IDrawingElement>
    {
        public Rect ElementArea => area;

        public int Count => ((ICollection<IDrawingElement>)elements).Count;

        public bool IsReadOnly => ((ICollection<IDrawingElement>)elements).IsReadOnly;

        Rect area;
        List<IDrawingElement> elements = new List<IDrawingElement>();

        public DrawingElementGroup() { }
        public DrawingElementGroup(IEnumerable<IDrawingElement> elements_)
        {
            foreach (var element in elements_)
            {
                elements.Add(element);
                area.Union(element.ElementArea);
            }
        }

        public Geometry GetGeometry(Rect rect, Size size)
        {
            var geometryGroup = new GeometryGroup();
            foreach(var element in elements)
            {
                geometryGroup.Children.Add(element.GetGeometry(rect, size));
            }
            return geometryGroup;
        }

        public void Add(IDrawingElement item)
        {
            ((ICollection<IDrawingElement>)elements).Add(item);
            area.Union(item.ElementArea);
        }

        public void Clear()
        {
            ((ICollection<IDrawingElement>)elements).Clear();
        }

        public bool Contains(IDrawingElement item)
        {
            return ((ICollection<IDrawingElement>)elements).Contains(item);
        }

        public void CopyTo(IDrawingElement[] array, int arrayIndex)
        {
            ((ICollection<IDrawingElement>)elements).CopyTo(array, arrayIndex);
        }

        public bool Remove(IDrawingElement item)
        {
            return ((ICollection<IDrawingElement>)elements).Remove(item);
        }

        public IEnumerator<IDrawingElement> GetEnumerator()
        {
            return ((ICollection<IDrawingElement>)elements).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ICollection<IDrawingElement>)elements).GetEnumerator();
        }
    }
}
