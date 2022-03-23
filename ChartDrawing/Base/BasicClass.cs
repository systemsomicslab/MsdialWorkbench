using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;

namespace CompMs.Graphics.Core.Base
{
    public enum ChartType { Line, Point, Chromatogram, MS, MSwithRef }
    public enum MarkerType { None, Circle, Square, Cross }
    public enum XaxisUnit { Minuites, Seconds }
    public enum Position { Left, Right, Top, Bottom }
    
    public class MouseActionSetting
    {
        public bool CanMouseAction { get; set; } = true;
        public bool CanZoomRubber { get; set; } = true;
        public bool FixMinX { get; set; } = false;
        public bool FixMaxX { get; set; } = false;
        public bool FixMinY { get; set; } = false;
        public bool FixMaxY { get; set; } = false;
    }

    public class Area {
        public AxisX AxisX { get; set; } = new AxisX();
        public AxisY AxisY { get; set; } = new AxisY();
        public Margin Margin { get; set; } = new Margin(60, 30, 10, 40);
        public LabelSpace LabelSpace { get; set; } = new LabelSpace(0, 0, 0, 0);
        public float Height { get; set; }
        public float Width { get; set; }
        public Brush BackGroundColor { get; set; } = Brushes.WhiteSmoke;
        public Pen GraphBorder { get; set; } = new Pen(Brushes.LightGray, 1.0);

        public float ActualGraphHeight {
            get { return Height - Margin.Top - Margin.Bottom; } }
        public float ActualGraphWidth {
            get { return Width - Margin.Left - Margin.Right; }
        }

    }

    public class AxisX : Axis
    {
        public override string AxisLabel { get; set; } = "X axis";
    }

    public class AxisY : Axis
    {
        public override string AxisLabel { get; set; } = "Y axis";
    }

    public class Margin
    {
        public float Top { get; set; }
        public float Bottom { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public Margin() { }
        public Margin(float left, float top, float right, float bottom) {
            Top = top; Bottom = bottom; Left = left; Right = right;
        }
    }

    public class LabelSpace : Margin
    {
        public LabelSpace() { }
        public LabelSpace(float left, float top, float right, float bottom) {
            Top = top; Bottom = bottom; Left = left; Right = right;
        }
    }

    public class Title
    {
        public string Label { get; set; }
        public Typeface FontType { get; set; } = new Typeface("Caribri");
        public int FontSize { get; set; } = 12;
        public Brush FontColor { get; set; } = Brushes.Black;
        public bool FitFontSize { get; set; } = false;
        public bool Enabled { get; set; } = true;
    }

    public abstract class Axis
    {
        public virtual string AxisLabel { get; set; }
        public float MinorScaleSize { get; set; } = 2;
        public float MajorScaleSize { get; set; } = 5;

        public Typeface FontType { get; set; } = new Typeface("Caribri");
        public int FontSize { get; set; } = 13;
        public Brush FontColor { get; set; } = Brushes.Black;

        public Pen Pen { get; set; } = new Pen(Brushes.Black, 0.5);

        public bool Enabled { get; set; } = true;
        public bool ScaleEnabled { get; set; } = true;
        public bool MinorScaleEnabled { get; set; } = true;

        public bool IsItalicLabel { get; set; } = false;
    }

    public class Legend {
        public string Text { get; set; }
        public bool IsVisible { get; set; } = false;

        // TO DO: Implement Left, Top, Bottom
        public Position Position { get; set; } = Position.Right;
        public bool InGraphicArea { get; set; } = true;
        public int MaxWidth { get; set; } = 100;
        public int FontSize { get; set; } = 13;
    }

    public class Series : IList<XY>, IReadOnlyList<XY>
    {
        public XY this[int index] { get => ((IList<XY>)Points)[index]; set => ((IList<XY>)Points)[index] = value; }

        public List<XY> Points { get; set; } = new List<XY>();

        public Legend Legend { get; set; }
        public bool IsLabelVisible { get; set; } = false;

        public ChartType ChartType { get; set; }
        public MarkerType MarkerType { get; set; }
        public XaxisUnit XaxisUnit { get; set; }
        public Size MarkerSize { get; set; } = new Size(2,2);
        public Brush MarkerFill { get; set; } = Brushes.Red;
        public Brush Brush { get; set; } = Brushes.Black;
        public Pen Pen { get; set; } = new Pen(Brushes.Black, 1.0);
        public Typeface FontType { get; set; } = new Typeface("Caribri");
        public int FontSize { get; set; } = 13;
        public Accessory Accessory { get; set; }

        public float MaxX => Points == null ? 0 : Points.Select(xy => xy.X).Max();
        public float MinX => Points == null ? 0 : Points.Select(xy => xy.X).Min();
        public float MaxY => Points == null ? 0 : Points.Select(xy => xy.Y).Max();
        public float MinY => Points == null ? 0 : Points.Select(xy => xy.Y).Min();

        public IReadOnlyList<float> XList => Points?.Select(xy => xy.X).ToList();
        public IReadOnlyList<float> YList => Points?.Select(xy => xy.Y).ToList();

        public int Count => ((IList<XY>)Points).Count;

        public bool IsReadOnly => ((IList<XY>)Points).IsReadOnly;

        public void Add(XY item)
        {
            ((IList<XY>)Points).Add(item);
        }

        public void AddPoint(float x = 0, float y = 0, string label = null)
        {
            Points.Add(new XY() { X = x, Y = y, Label = label });
        }

        public void Clear()
        {
            ((IList<XY>)Points).Clear();
        }

        public bool Contains(XY item)
        {
            return ((IList<XY>)Points).Contains(item);
        }

        public void CopyTo(XY[] array, int arrayIndex)
        {
            ((IList<XY>)Points).CopyTo(array, arrayIndex);
        }

        public IEnumerator<XY> GetEnumerator()
        {
            return ((IList<XY>)Points).GetEnumerator();
        }

        public int IndexOf(XY item)
        {
            return ((IList<XY>)Points).IndexOf(item);
        }

        public void Insert(int index, XY item)
        {
            ((IList<XY>)Points).Insert(index, item);
        }

        public bool Remove(XY item)
        {
            return ((IList<XY>)Points).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<XY>)Points).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<XY>)Points).GetEnumerator();
        }
    }

    public class XY
    {
        public string Label { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    public class Accessory
    {
        public PeakInfo Chromatogram { get; set; }

        public Accessory() {
            this.Chromatogram = new PeakInfo() {
                RtTop = -1,
                RtLeft = -1,
                RtRight = -1
            };
        }

        public void SetChromatogram(float rtTop, float rtLeft, float rtRight,
            float estimatedNoise = 1.0F, float signalToNoise = 1.0F, float areaFactore = 1.0F) {
            var width = rtRight - rtLeft;
            if (width < 0.00001) {
                //this.Chromatogram = new PeakInfo();
            }
            else {
                this.Chromatogram = new PeakInfo() {
                    RtTop = rtTop,
                    RtLeft = rtLeft,
                    RtRight = rtRight,
                    AreaFactor = areaFactore,
                    SignalToNoise = signalToNoise,
                    EstimatedNoise = estimatedNoise
                };
            }
        }

        public void SetChromatogram(double rtTop, double rtLeft, double rtRight,
            double estimatedNoise = 1.0, double signalToNoise = 1.0, double areaFactore = 1.0) {
            var width = rtRight - rtLeft;
            if (width < 0.00001) {
                //this.Chromatogram = new PeakInfo();
            }
            else {
                this.Chromatogram = new PeakInfo() {
                    RtTop = (float)rtTop,
                    RtLeft = (float)rtLeft,
                    RtRight = (float)rtRight,
                    AreaFactor = (float)areaFactore,
                    SignalToNoise = (float)signalToNoise,
                    EstimatedNoise = (float)estimatedNoise
                };
            }
        }

        public class PeakInfo {
            public float RtTop { get; set; }
            public float RtLeft { get; set; }
            public float RtRight { get; set; }

            public float AreaFactor { get; set; } = 1.0F; // in GCMS, if RI is used for drawing chromatogram, the area is calculated by (RtEnd - RtBegin)/(RiEnd - RiBegin)
            public float SignalToNoise { get; set; } = 1.0F; 
            public float EstimatedNoise { get; set; } = 1.0F;
        }
    }

    public class SeriesList : IList<Series>, IReadOnlyList<Series>
    {
        public Series this[int index] { get => ((IList<Series>)Series)[index]; set => ((IList<Series>)Series)[index] = value; }

        public List<Series> Series { get; set; } = new List<Series>();

        public float MaxX => Series.Any() ? Series.Select(series => series.MaxX).Max() : float.MaxValue;
        public float MinX => Series.Any() ? Series.Select(series => series.MinX).Min() : float.MinValue;
        public float MaxY => Series.Any() ? Series.Select(series => series.MaxY).Max() : float.MaxValue;
        public float MinY => Series.Any() ? Series.Select(series => series.MinY).Min() : float.MinValue;
        public bool AreLabelsVisible => Series != null && Series.All(series => series.IsLabelVisible);
        public bool AreLegendsVisible => Series != null && Series.All(series => series.Legend != null && series.Legend.IsVisible);
        public bool AreLegendsInGraphArea => Series == null || Series.All(series => series.Legend == null || series.Legend.InGraphicArea);

        public int Count => ((IList<Series>)Series).Count;

        public bool IsReadOnly => ((IList<Series>)Series).IsReadOnly;

        public void Add(Series item)
        {
            ((IList<Series>)Series).Add(item);
        }

        public void Clear()
        {
            ((IList<Series>)Series).Clear();
        }

        public bool Contains(Series item)
        {
            return ((IList<Series>)Series).Contains(item);
        }

        public void CopyTo(Series[] array, int arrayIndex)
        {
            ((IList<Series>)Series).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Series> GetEnumerator()
        {
            return ((IList<Series>)Series).GetEnumerator();
        }

        public int IndexOf(Series item)
        {
            return ((IList<Series>)Series).IndexOf(item);
        }

        public void Insert(int index, Series item)
        {
            ((IList<Series>)Series).Insert(index, item);
        }

        public bool Remove(Series item)
        {
            return ((IList<Series>)Series).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Series>)Series).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Series>)Series).GetEnumerator();
        }
    }
}
