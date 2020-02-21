using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace ChartDrawing
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

    public class Series {
        public float MaxY { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MinX { get; set; }

        public Legend Legend { get; set; }
        public ChartType ChartType { get; set; }
        public MarkerType MarkerType { get; set; }
        public XaxisUnit XaxisUnit { get; set; }
        public Size MarkerSize { get; set; } = new Size(2,2);
        public Brush MarkerFill { get; set; } = Brushes.Red;
        public Brush Brush { get; set; } = Brushes.Black;
        public Pen Pen { get; set; } = new Pen(Brushes.Black, 1.0);
        public Typeface FontType { get; set; } = new Typeface("Caribri");
        public int FontSize { get; set; } = 13;

        public List<XY> Points { get; set; } = new List<XY>();
        public bool IsLabelVisible { get; set; } = false;
        public Accessory Accessory { get; set; }

        public void SetValues() {
            if (Points == null || Points.Count == 0) return;
            MaxY = Points.Max(x => x.Y);
            MinY = Points.Min(x => x.Y);
            MaxX = Points.Max(x => x.X);
            MinX = Points.Min(x => x.X);
        }
        public void AddPoint(float x, float y){
            Points.Add(new XY() { X = x, Y = y });
        }
        public void AddPoint(float x, float y, string s) {
            Points.Add(new XY() { X = x, Y = y, Label = s });
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

        public class PeakInfo {
            public float RtTop { get; set; }
            public float RtLeft { get; set; }
            public float RtRight { get; set; }

            public float AreaFactor { get; set; } = 1.0F; // in GCMS, if RI is used for drawing chromatogram, the area is calculated by (RtEnd - RtBegin)/(RiEnd - RiBegin)
            public float SignalToNoise { get; set; } = 1.0F; 
            public float EstimatedNoise { get; set; } = 1.0F;
        }
    }

    public class SeriesList
    {
        public float MaxY { get; set; } = float.MinValue;
        public float MinY { get; set; } = float.MaxValue;
        public float MaxX { get; set; } = float.MinValue;
        public float MinX { get; set; } = float.MaxValue;
        public bool AreLegendsVisible { get; set; } = false;
        public bool AreLabelsVisible { get; set; } = false;
        public bool AreLegendsInGraphArea { get; set; } = true;
        public List<Series> Series { get; set; } = new List<Series>();
        public void SetValues() {
            foreach(var s in Series) {
                if (MaxY < s.MaxY) MaxY = s.MaxY;
                if (MaxX < s.MaxX) MaxX = s.MaxX;
                if (MinY > s.MinY) MinY = s.MinY;
                if (MinX > s.MinX) MinX = s.MinX;
                if (s.IsLabelVisible) AreLabelsVisible = true;
                if (s.Legend != null && s.Legend.IsVisible == true) AreLegendsVisible = true;
                if (s.Legend != null && s.Legend.InGraphicArea == false) AreLegendsInGraphArea = false;
            }
        }
    }
}
