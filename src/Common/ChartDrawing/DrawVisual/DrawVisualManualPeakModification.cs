using CompMs.Common.Utility;
using CompMs.Graphics.Core.Base;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;


namespace CompMs.Graphics.Chromatogram.ManualPeakModification
{
    public class ChromatogramDetectedPeakProperty
    {

        public int ScanLeft { get; set; }
        public int ScanTop { get; set; }
        public int ScanRight { get; set; }

        public int ScanMinLeft { get; set; } // this is scan number of left minimum intensity
        public int ScanMinRight { get; set; } // scan number of right minimum intensity

        public float RtLeft { get; set; }
        public float RtTop { get; set; }
        public float RtRight { get; set; }

        public float RtMinLeft { get; set; } // rt of left minimum intensity;
        public float RtMinRight { get; set; } // rt of right minimum intensity;

        public float HeightLeftFromZero { get; set; }
        public float HeightRightFromZero { get; set; }

        public float HeightMinLeftFromZero { get; set; } // intensity of left minimum intensity
        public float HeightMinRightFromZero { get; set; } // intensity of right minimum intensity

        public float HeightFromZero { get; set; }
        public float HeightFromBaseline { get; set; }
        public float HeightFromParallelBaseline { get; set; }

        public float AreaFromZero { get; set; }
        public float AreaFromBaseline { get; set; }
        public float AreaFromParallelBaseline { get; set; }

        public float SignalToNoise { get; set; }
        public float EstimatedNoise { get; set; }
    }

    public class DrawVisualManualPeakModification : DrawVisual
    {
        public ChromatogramDetectedPeakProperty ChromPeakProperty { get; set; } = null;
        public bool isPeakDetected { get; set; }

        public float ChromLeftEdgeX { get; set; }
        public float ChromLeftEdgeY { get; set; }
        public float ChromRightEdgeX { get; set; }
        public float ChromRightEdgeY { get; set; }

        // Point for Mouse Event
        public Point CurrentMousePoint { get; set; } = new Point(-1, -1); // Current Location of Mouse Pointer
        public Point LeftButtonStartClickPoint { get; set; } = new Point(-1, -1); // for Mouse Left Button        
        public Point LeftButtonEndClickPoint { get; set; } = new Point(-1, -1); // for Mouse Left Button (this coordinates are changed by MouseMove)
        public Point RightButtonStartClickPoint { get; set; } = new Point(-1, -1);

        public bool LeftMouseButtonLeftEdgeCapture { get; set; } = false;
        public bool LeftMouseButtonRightEdgeCapture { get; set; } = false;

        public bool IsMouseLeftUpDone { get; set; } = false;
        public bool IsMouseDoubleClicked { get; set; } = false;
        public bool IsShiftRightFolding { get; set; } = false;
        public bool IsShiftRightReleased { get; set; } = false;
        public bool IsPeakDeleteCommand { get; set; } = false;

        public DrawVisualManualPeakModification() { }
        public DrawVisualManualPeakModification(Area area, Title title, SeriesList seriesList, bool isArticleFormat = false) : base(area, title, seriesList, isArticleFormat) {
            this.ChromPeakProperty = new ChromatogramDetectedPeakProperty();
        }


        protected override void drawChromatogram(Series series, bool isManualPicking = false) {
            var pathFigure = new PathFigure();
            var areaPath = new PathFigure();

            var flag = true;
            var flagLeft = true;
            var flagRight = true;
            var flagFill = false;

            var graphBrush = Core.Base.Utility.CombineAlphaAndColor(this.alpha, (SolidColorBrush)series.Brush);// Set Graph Brush

            var rtwidth = series.Accessory.Chromatogram.RtRight - series.Accessory.Chromatogram.RtLeft;
            if (series.Accessory.Chromatogram.RtLeft >= 0 &&
                series.Accessory.Chromatogram.RtRight > 0 &&
                series.Accessory.Chromatogram.RtTop > 0 && rtwidth > 0.00001) {
                this.isPeakDetected = true;
            }

            foreach (var XY in series.Points) {
                var xs = Area.Margin.Left + (XY.X - MinX) * xPacket;
                var ys = Area.Margin.Bottom + Area.LabelSpace.Bottom + (XY.Y - MinY) * yPacket;
                if (flag) {
                    pathFigure.StartPoint = new Point(xs, ys);
                    flag = false; continue;
                }
                else {
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                }

                if (isPeakDetected == false) continue;
                if (flagLeft && XY.X >= series.Accessory.Chromatogram.RtLeft) {
                    areaPath.StartPoint = new Point(xs, Area.Margin.Bottom); // PathFigure for GraphLine 
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                    flagFill = true; flagLeft = false;

                    this.ChromLeftEdgeX = xs;
                    this.ChromLeftEdgeY = ys;
                }

                if (flagFill) {
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                }

                if (flagRight && XY.X >= series.Accessory.Chromatogram.RtRight) {
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, Area.Margin.Bottom) }); // PathFigure for GraphLine 
                    flagRight = false; flagFill = false;

                    this.ChromRightEdgeX = xs;
                    this.ChromRightEdgeY = ys;
                }
            }

            areaPath.Freeze();
            var areaPathGeometry = new PathGeometry(new PathFigure[] { areaPath });
            areaPathGeometry.Freeze();

            this.drawingContext.DrawGeometry(graphBrush, series.Pen, areaPathGeometry);

            pathFigure.Freeze();
            var pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
            pathGeometry.Freeze();

            // Draw Chromatogram & Area
            this.drawingContext.DrawGeometry(null, series.Pen, pathGeometry); // Draw Chromatogram Graph Line  

            if (isManualPicking) {
                drawChromatogramBuilder(series, pathFigure, areaPath);
            }
        }

        protected void drawChromatogramBuilder(Series series, PathFigure pathFigure, PathFigure areaPath) {

            // drawing edges
            var leftPoint = new Point(this.ChromLeftEdgeX, this.ChromLeftEdgeY);
            var rightPoint = new Point(this.ChromRightEdgeX, this.ChromRightEdgeY);

            var mouseX = this.CurrentMousePoint.X;
            var mouseY = this.Area.Height - this.CurrentMousePoint.Y;
            var mousePoint = new Point(mouseX, mouseY);


            var capturedSizeFactor = 1.3;
            var uncapturedSizeFactor = 0.8;

            var okBrush = Brushes.Blue;
            var okPen = new Pen(okBrush, 1.0);

            var banBrush = Brushes.Red;
            var banPen = new Pen(banBrush, 1.0);

            var nonstageBrush = Brushes.Gray;
            var nonstagePen = new Pen(nonstageBrush, 1.0);

            #region // execute mouse actions
            if (this.IsMouseDoubleClicked) {
                DetectNearestPeak(mouseX, mouseY, series, pathFigure);
                return;
            }

            if (this.IsShiftRightFolding) {
                // Debug.WriteLine("start X {0}, current X {1}", this.RightButtonStartClickPoint.X, mouseX);
                drawRubberShowingManualPickingRange(mouseX, okBrush, okPen, banBrush, banPen, pathFigure);
                return;
            }

            if (this.IsShiftRightReleased) {
                // Debug.WriteLine("final start X {0}, final current X {1}", this.RightButtonStartClickPoint.X, mouseX);
                DetectRtDefinedPeak(this.RightButtonStartClickPoint.X, mouseX, series, pathFigure);
                return;
            }

            if (this.IsPeakDeleteCommand) {
                DeletePeak(series);
                return;
            }
            #endregion

            // drawing area points
            if (areaPath != null && areaPath.Segments != null && areaPath.Segments.Count > 1) {
                for (int i = 0; i < areaPath.Segments.Count - 1; i++) {
                    var seg = areaPath.Segments[i];
                    var point = ((LineSegment)seg).Point;
                    this.drawingContext.DrawEllipse(series.Brush, series.Pen, point, series.MarkerSize.Width * 0.5, series.MarkerSize.Width * 0.5);
                }
            }
            else {
                return;
            }

            //  Debug.WriteLine(this.LeftMouseButtonLeftEdgeCapture);

            if (!this.LeftMouseButtonLeftEdgeCapture && !this.LeftMouseButtonRightEdgeCapture) {
                // Debug.WriteLine("Both none");

                this.drawingContext.DrawEllipse(series.MarkerFill, series.Pen, leftPoint, series.MarkerSize.Width, series.MarkerSize.Width);
                this.drawingContext.DrawEllipse(series.MarkerFill, series.Pen, rightPoint, series.MarkerSize.Width, series.MarkerSize.Width);
                // this.drawingContext.DrawLine(series.Pen, leftPoint, rightPoint);

                if (isOnChromEdge(leftPoint, series)) {
                    this.drawingContext.DrawEllipse(null, series.Pen, leftPoint, series.MarkerSize.Width + series.Pen.Thickness * 2, series.MarkerSize.Width + series.Pen.Thickness * 2);

                    if (isClickOnChromEdge(leftPoint, series)) {
                        this.LeftMouseButtonLeftEdgeCapture = true;
                    }
                }
                else if (isOnChromEdge(rightPoint, series)) {
                    this.drawingContext.DrawEllipse(null, series.Pen, rightPoint, series.MarkerSize.Width + series.Pen.Thickness * 2, series.MarkerSize.Width + series.Pen.Thickness * 2);

                    if (isClickOnChromEdge(rightPoint, series)) {
                        this.LeftMouseButtonRightEdgeCapture = true;
                    }
                }
            }
            else if (this.LeftMouseButtonLeftEdgeCapture) {
                // Debug.WriteLine("left");
                var stagePoint = new XY();
                if (isOnPathlines(mouseX, mouseY, pathFigure, series, out stagePoint)) {

                    // currently all range is accepted.
                    //this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                    //this.drawingContext.DrawLine(okPen, mousePoint, rightPoint);

                    if (stagePoint.X < series.Accessory.Chromatogram.RtRight) {
                        this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                        this.drawingContext.DrawLine(okPen, mousePoint, rightPoint);
                    }
                    else {
                        this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y - 5), new Point(mousePoint.X + 5, mousePoint.Y + 5));
                        this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y + 5), new Point(mousePoint.X + 5, mousePoint.Y - 5));
                        this.drawingContext.DrawLine(banPen, mousePoint, rightPoint);
                    }

                    if (this.IsMouseLeftUpDone) {
                        this.LeftMouseButtonLeftEdgeCapture = false;

                        if (stagePoint.X < series.Accessory.Chromatogram.RtRight) {
                            series.Accessory.Chromatogram.RtLeft = stagePoint.X;
                            UpdatePeaktopProperty(series);
                            CalculateChromPeakProperties(series);
                        }
                    }

                    //if (this.IsMouseLeftUpDone) {
                    //    this.LeftMouseButtonLeftEdgeCapture = false;

                    //    series.Accessory.Chromatogram.RtLeft = stagePoint.X;
                    //    UpdatePeaktopProperty(series);
                    //    CalculateChromPeakProperties(series);
                    //}

                    //if (stagePoint.X < series.Accessory.Chromatogram.RtTop) {
                    //    this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                    //    this.drawingContext.DrawLine(okPen, mousePoint, rightPoint);
                    //}
                    //else {
                    //    this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y - 5), new Point(mousePoint.X + 5, mousePoint.Y + 5));
                    //    this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y + 5), new Point(mousePoint.X + 5, mousePoint.Y - 5));
                    //    this.drawingContext.DrawLine(banPen, mousePoint, rightPoint);
                    //}

                    //if (this.IsMouseLeftUpDone) {
                    //    this.LeftMouseButtonLeftEdgeCapture = false;

                    //    if (stagePoint.X < series.Accessory.Chromatogram.RtTop) {
                    //        series.Accessory.Chromatogram.RtLeft = stagePoint.X;
                    //        UpdatePeaktopProperty(series);
                    //        CalculateChromPeakProperties(series);
                    //    }
                    //}
                }
                else {

                    this.drawingContext.DrawEllipse(nonstageBrush, nonstagePen, mousePoint, series.MarkerSize.Width * uncapturedSizeFactor, series.MarkerSize.Width * uncapturedSizeFactor);
                    this.drawingContext.DrawEllipse(nonstageBrush, nonstagePen, rightPoint, series.MarkerSize.Width * uncapturedSizeFactor, series.MarkerSize.Width * uncapturedSizeFactor);
                    this.drawingContext.DrawLine(nonstagePen, mousePoint, rightPoint);

                    if (this.IsMouseLeftUpDone) {
                        this.LeftMouseButtonLeftEdgeCapture = false;
                    }
                }
            }
            else if (this.LeftMouseButtonRightEdgeCapture) {
                //Debug.WriteLine("right");
                var stagePoint = new XY();
                if (isOnPathlines(mouseX, mouseY, pathFigure, series, out stagePoint)) {

                    // currently all range is accepted.
                    //this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                    //this.drawingContext.DrawLine(okPen, mousePoint, leftPoint);

                    if (stagePoint.X > series.Accessory.Chromatogram.RtLeft) {
                        this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                        this.drawingContext.DrawLine(okPen, mousePoint, leftPoint);
                    }
                    else {
                        this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y - 5), new Point(mousePoint.X + 5, mousePoint.Y + 5));
                        this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y + 5), new Point(mousePoint.X + 5, mousePoint.Y - 5));
                        this.drawingContext.DrawLine(banPen, mousePoint, leftPoint);
                    }

                    if (this.IsMouseLeftUpDone) {
                        this.LeftMouseButtonRightEdgeCapture = false;

                        if (stagePoint.X > series.Accessory.Chromatogram.RtLeft) {
                            series.Accessory.Chromatogram.RtRight = stagePoint.X;
                            UpdatePeaktopProperty(series);
                            CalculateChromPeakProperties(series);
                        }
                    }


                    //if (this.IsMouseLeftUpDone) {
                    //    this.LeftMouseButtonRightEdgeCapture = false;
                    //    series.Accessory.Chromatogram.RtRight = stagePoint.X;
                    //    UpdatePeaktopProperty(series);
                    //    CalculateChromPeakProperties(series);
                    //}

                    //if (stagePoint.X > series.Accessory.Chromatogram.RtTop) {
                    //    this.drawingContext.DrawEllipse(okBrush, okPen, mousePoint, series.MarkerSize.Width * capturedSizeFactor, series.MarkerSize.Width * capturedSizeFactor);
                    //    this.drawingContext.DrawLine(okPen, mousePoint, leftPoint);
                    //}
                    //else {
                    //    this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y - 5), new Point(mousePoint.X + 5, mousePoint.Y + 5));
                    //    this.drawingContext.DrawLine(banPen, new Point(mousePoint.X - 5, mousePoint.Y + 5), new Point(mousePoint.X + 5, mousePoint.Y - 5));
                    //    this.drawingContext.DrawLine(banPen, mousePoint, leftPoint);
                    //}

                    //if (this.IsMouseLeftUpDone) {
                    //    this.LeftMouseButtonRightEdgeCapture = false;

                    //    if (stagePoint.X > series.Accessory.Chromatogram.RtTop) {
                    //        series.Accessory.Chromatogram.RtRight = stagePoint.X;
                    //        UpdatePeaktopProperty(series);
                    //        CalculateChromPeakProperties(series);
                    //    }

                    //}
                }
                else {
                    this.drawingContext.DrawEllipse(nonstageBrush, nonstagePen, leftPoint, series.MarkerSize.Width * uncapturedSizeFactor, series.MarkerSize.Width * uncapturedSizeFactor);
                    this.drawingContext.DrawEllipse(nonstageBrush, nonstagePen, mousePoint, series.MarkerSize.Width * uncapturedSizeFactor, series.MarkerSize.Width * uncapturedSizeFactor);
                    this.drawingContext.DrawLine(nonstagePen, leftPoint, mousePoint);

                    if (this.IsMouseLeftUpDone) {
                        this.LeftMouseButtonRightEdgeCapture = false;
                    }
                }
            }
        }

        protected void drawRubberShowingManualPickingRange(double mouseX, SolidColorBrush okBrush, Pen okPen,
            SolidColorBrush banBrush, Pen banPen, PathFigure pathFigure) {

            var mouseStartX = this.RightButtonStartClickPoint.X;
            var transparency = (byte)120;

            var yBottom = Area.Margin.Bottom + Area.LabelSpace.Bottom;
            var yUpper = Area.Height - Area.Margin.Top - Area.LabelSpace.Top;

            var startX = Math.Min(mouseStartX, mouseX);
            var endX = Math.Max(mouseStartX, mouseX);

            Debug.WriteLine("Shift pressed");
            this.drawingContext.DrawRectangle(
                   new SolidColorBrush(Color.FromArgb(transparency, okBrush.Color.R, okBrush.Color.G, okBrush.Color.B)),
                   okPen, new Rect(new Point(startX, yBottom), new Point(endX, yUpper)));

            //if (isExistLocalMaximum(startX, endX, pathFigure)) {
            //    this.drawingContext.DrawRectangle(
            //        new SolidColorBrush(Color.FromArgb(transparency, okBrush.Color.R, okBrush.Color.G, okBrush.Color.B)),
            //        okPen, new Rect(new Point(startX, yBottom), new Point(endX, yUpper)));
            //}
            //else {
            //    this.drawingContext.DrawRectangle(
            //       new SolidColorBrush(Color.FromArgb(transparency, banBrush.Color.R, banBrush.Color.G, banBrush.Color.B)),
            //       banPen, new Rect(new Point(startX, yBottom), new Point(endX, yUpper)));
            //}
        }

        public void DeletePeak(Series series) {
            series.Accessory.Chromatogram = new Accessory.PeakInfo();
            CalculateChromPeakProperties(series);
        }

        // finding maximum peak top
        public void UpdatePeaktopProperty(Series series) {

            var rtLeft = series.Accessory.Chromatogram.RtLeft;
            var rtRight = series.Accessory.Chromatogram.RtRight;
            var rtTop = series.Accessory.Chromatogram.RtTop;
            var localMaximumPeak = double.MinValue;

            var maxIntensity = double.MinValue;
            var rtIntensityTop = (rtRight + rtLeft) * 0.5;

            for (int i = 1; i < series.Points.Count - 1; i++) {
                var point = series.Points[i];
                if (point.X < rtLeft) continue;
                if (point.X > rtRight) break;

                var pointL = series.Points[i - 1];
                var pointR = series.Points[i + 1];

                if ((point.Y > pointL.Y && point.Y >= pointR.Y) || (point.Y >= pointL.Y && point.Y > pointR.Y)) {
                    if (localMaximumPeak < point.Y) {
                        rtTop = point.X;
                        localMaximumPeak = point.Y;
                    }
                }
                if (maxIntensity < point.Y) {
                    rtIntensityTop = point.X;
                    maxIntensity = point.Y;
                }
            }

            if (localMaximumPeak > 0)
                series.Accessory.Chromatogram.RtTop = rtTop;
            else
                series.Accessory.Chromatogram.RtTop = (float)rtIntensityTop;
        }

        // calculate peak properties from rt left/top/right values
        public void CalculateChromPeakProperties(Series series) {

            if (series == null || series.Points == null || series.Points.Count == 0 || series.Accessory.Chromatogram == null) return;
            if (series.Accessory.Chromatogram.RtLeft >= 0 &&
                series.Accessory.Chromatogram.RtRight > 0 &&
                series.Accessory.Chromatogram.RtTop > 0) {
                this.isPeakDetected = true;
            }
            else {
                this.isPeakDetected = false;
            }
            if (this.isPeakDetected == false) {
                this.ChromPeakProperty = new ChromatogramDetectedPeakProperty();
                OnPropertyChanged("IsPeakPropertyChanged");
                return;
            }
            var chromAccessory = series.Accessory.Chromatogram;
            var chromPoints = series.Points;

            var property = new ChromatogramDetectedPeakProperty() {
                RtLeft = chromAccessory.RtLeft,
                RtTop = chromAccessory.RtTop,
                RtRight = chromAccessory.RtRight,
                EstimatedNoise = chromAccessory.EstimatedNoise,
                SignalToNoise = chromAccessory.SignalToNoise
            };

            var leftMinIntensity = double.MaxValue;
            var rightMinIntensity = double.MaxValue;

            // finding left, top, right scan IDs
            var left = chromPoints.LowerBound(property.RtLeft, (p, l) => p.X.CompareTo(l));
            var right = chromPoints.UpperBound(property.RtRight, left, chromPoints.Count, (p, r) => p.X.CompareTo(r));
            var top = chromPoints.LowerBound(property.RtTop, left, chromPoints.Count, (p, t) => p.X.CompareTo(t));
            for (int i = left; i <= top; i++) {
                var point = chromPoints[i];
                if (leftMinIntensity >= point.Y) {
                    leftMinIntensity = point.Y;

                    property.ScanMinLeft = i;
                    property.RtMinLeft = point.X;
                    property.HeightMinLeftFromZero = point.Y;
                }
            }

            for (int i = top; i < right; i++) {
                var point = chromPoints[i];
                if (rightMinIntensity >= point.Y) {
                    rightMinIntensity = point.Y;

                    property.ScanMinRight = i;
                    property.RtMinRight = point.X;
                    property.HeightMinRightFromZero = point.Y;
                }
            }
            property.ScanLeft = left;
            property.ScanRight = right - 1;
            property.ScanTop = top;
            property.HeightLeftFromZero = chromPoints[left].Y;
            property.HeightRightFromZero = chromPoints[right - 1].Y;
            property.HeightFromZero = chromPoints[top].Y;

            var areaFromZero = 0.0;
            var areaFromBaseline = 0.0;
            var areaFromParallelBaseline = 0.0;

            var rtFactor = series.XaxisUnit == XaxisUnit.Minuites ? 60.0 : 1.0;
            var slope = (property.HeightMinRightFromZero - property.HeightMinLeftFromZero)
                / (property.RtMinRight - property.RtMinLeft);
            var rectangle = Math.Min(property.HeightMinLeftFromZero, property.HeightMinRightFromZero)
                * (property.RtRight - property.RtLeft);

            var heightFromBaseline = 0.0;
            var heightFromParallelBaseline = property.HeightFromZero - Math.Min(property.HeightMinLeftFromZero, property.HeightMinRightFromZero);

            for (int i = property.ScanLeft; i <= property.ScanRight - 1; i++) {

                var yL = chromPoints[i].Y;
                var yR = chromPoints[i + 1].Y;

                var xL = chromPoints[i].X;
                var xR = chromPoints[i + 1].X;

                var diffRt = xR - xL;
                var trapezoid = (yL + yR) * diffRt * 0.5;
                areaFromZero += trapezoid;

                var baselineYL = slope * (xL - property.RtMinLeft) + property.HeightMinLeftFromZero;
                var baselineYR = slope * (xL - property.RtMinLeft) + property.HeightMinLeftFromZero;

                var baselineTrape = (baselineYL + baselineYR) * diffRt * 0.5;
                if (baselineTrape < 0) baselineTrape = 0;

                var diffTrape = trapezoid - baselineTrape;
                if (diffTrape > 0) {
                    areaFromBaseline += diffTrape;
                }

                if (i == property.ScanTop) {
                    heightFromBaseline = property.HeightFromZero - baselineYL;
                }
            }

            areaFromParallelBaseline = areaFromZero - rectangle;

            if (series.XaxisUnit == XaxisUnit.Minuites) {
                areaFromZero *= 60.0;
                areaFromBaseline *= 60.0;
                areaFromParallelBaseline *= 60.0;
            }

            var areaFactor = chromAccessory.AreaFactor;

            property.AreaFromZero = (float)areaFromZero * areaFactor;
            property.AreaFromBaseline = (float)areaFromBaseline * areaFactor;
            property.AreaFromParallelBaseline = (float)areaFromParallelBaseline * areaFactor;

            property.HeightFromBaseline = (float)heightFromBaseline;
            property.HeightFromParallelBaseline = (float)heightFromParallelBaseline;
            property.SignalToNoise = heightFromParallelBaseline / property.EstimatedNoise;

            this.ChromPeakProperty = property;

            OnPropertyChanged("IsPeakPropertyChanged");
        }

        public void DetectNearestPeak(double mouseX, double mouseY, Series series, PathFigure pathFigure) {

            // seeking nearest local maximum from mouseX

            var nearID = -1;
            var nearDist = double.MaxValue;

            for (int i = 1; i < pathFigure.Segments.Count - 1; i++) {

                var segtop = pathFigure.Segments[i];
                var pointtop = ((LineSegment)segtop).Point;

                var segleft = pathFigure.Segments[i - 1];
                var pointleft = ((LineSegment)segleft).Point;

                var segright = pathFigure.Segments[i + 1];
                var pointright = ((LineSegment)segright).Point;

                if ((pointtop.Y > pointleft.Y && pointtop.Y >= pointright.Y) || (pointtop.Y >= pointleft.Y && pointtop.Y > pointright.Y)) {
                    var dist = Math.Abs(mouseX - pointtop.X);
                    if (nearDist > dist) {
                        nearDist = dist;
                        nearID = i;
                    }
                }
            }

            if (nearID == -1) return;

            var leftID = nearID - 1;

            for (int i = nearID - 1; i >= 1; i--) {

                var segR = pathFigure.Segments[i + 1];
                var pointR = ((LineSegment)segR).Point;

                var segC = pathFigure.Segments[i];
                var pointC = ((LineSegment)segC).Point;

                var segL = pathFigure.Segments[i - 1];
                var pointL = ((LineSegment)segL).Point;

                if (pointC.Y <= pointR.Y && pointC.Y <= pointL.Y) {
                    leftID = i;
                    break;
                }
            }

            var rightID = nearID + 1;
            for (int i = nearID + 1; i < pathFigure.Segments.Count - 1; i++) {

                var segR = pathFigure.Segments[i + 1];
                var pointR = ((LineSegment)segR).Point;

                var segC = pathFigure.Segments[i];
                var pointC = ((LineSegment)segC).Point;

                var segL = pathFigure.Segments[i - 1];
                var pointL = ((LineSegment)segL).Point;

                if (pointC.Y <= pointR.Y && pointC.Y <= pointL.Y) {
                    rightID = i;
                    break;
                }
            }

            series.Accessory.Chromatogram.RtTop = series.Points[nearID + 1].X;
            series.Accessory.Chromatogram.RtLeft = series.Points[leftID + 1].X;
            series.Accessory.Chromatogram.RtRight = series.Points[rightID + 1].X;

            this.isPeakDetected = true;
            CalculateChromPeakProperties(series);
        }

        public void DetectRtDefinedPeak(double mouseBegin, double mouseEnd, Series series, PathFigure pathFigure) {

            var startX = Math.Min(mouseBegin, mouseEnd);
            var endX = Math.Max(mouseBegin, mouseEnd);

            //if (!isExistLocalMaximum(startX, endX, pathFigure)) return;

            // finding rt begin- and end scan IDs
            int pointBegin = 0, pointEnd = 0;
            bool findRtBegin = false;

            for (int i = 0; i < pathFigure.Segments.Count; i++) {

                var segtop = pathFigure.Segments[i];
                var pointtop = ((LineSegment)segtop).Point;

                if (pointtop.X < startX) continue;
                if (startX <= pointtop.X && !findRtBegin) {
                    findRtBegin = true;
                    pointBegin = i;
                }
                if (pointtop.X > endX) {
                    pointEnd = i - 1;
                    break;
                }
            }

            if (Math.Abs(pointBegin - pointEnd) < 3) return;

            var rtBegin = series.Points[pointBegin + 1].X;
            var rtEnd = series.Points[pointEnd + 1].X;

            DetectRtDefinedPeak(rtBegin, rtEnd, series);
        }

        protected void DetectRtDefinedPeak(double rtBegin, double rtEnd, Series series) {
            series.Accessory.Chromatogram.RtLeft = (float)rtBegin;
            series.Accessory.Chromatogram.RtRight = (float)rtEnd;

            // just a temp value, find 'maximum' value (instead of local maximum) for peak top determination
            var maxintensity = double.MinValue;
            var chromPoints = series.Points;
            for (int i = 0; i < chromPoints.Count; i++) {
                var point = chromPoints[i];
                if (point.Y > maxintensity) {
                    maxintensity = point.Y;
                    series.Accessory.Chromatogram.RtTop = point.X;
                }
            }

            UpdatePeaktopProperty(series);
            CalculateChromPeakProperties(series);
        }

        protected bool isExistLocalMaximum(double mouseBegin, double mouseEnd, PathFigure pathFigure) {

            for (int i = 1; i < pathFigure.Segments.Count - 1; i++) {

                var segtop = pathFigure.Segments[i];
                var pointtop = ((LineSegment)segtop).Point;

                if (pointtop.X < mouseBegin) continue;
                if (pointtop.X > mouseEnd) break;

                var segleft = pathFigure.Segments[i - 1];
                var pointleft = ((LineSegment)segleft).Point;

                var segright = pathFigure.Segments[i + 1];
                var pointright = ((LineSegment)segright).Point;

                if ((pointtop.Y > pointleft.Y && pointtop.Y >= pointright.Y) || (pointtop.Y >= pointleft.Y && pointtop.Y > pointright.Y)) {
                    return true;
                }
            }
            return false;
        }

        protected bool isOnPathlines(double mouseX, double mouseY, PathFigure pathFigure, Series series, out XY xy) {

            xy = new XY();
            var size = series.MarkerSize.Width;

            for (int i = 0; i < pathFigure.Segments.Count; i++) {
                var seg = pathFigure.Segments[i];
                var point = ((LineSegment)seg).Point;

                if (Math.Abs(point.X - mouseX) < size && Math.Abs(point.Y - mouseY) < size) {
                    xy.X = series.Points[i + 1].X;
                    xy.Y = series.Points[i + 1].Y;
                    return true;
                }
            }
            return false;
        }

        protected bool isClickOnChromEdge(Point edgePoint, Series series) {
            var mouse = this.LeftButtonStartClickPoint;
            var size = series.MarkerSize.Width;
            var tMouseY = this.Area.Height - mouse.Y;

            if (Math.Abs(mouse.X - edgePoint.X) < size && Math.Abs(tMouseY - edgePoint.Y) < size) {
                return true;
            }
            else {
                return false;
            }
        }

        protected bool isOnChromEdge(Point edgePoint, Series series) {
            var mouse = this.CurrentMousePoint;
            var size = series.MarkerSize.Width;
            var tMouseY = this.Area.Height - mouse.Y;

            //Debug.WriteLine("mouse X: {0}, edge X: {1}, mouse Y: {2}, edge Y: {3}", mouse.X, edgePoint.X, tMouseY, edgePoint.Y);

            if (Math.Abs(mouse.X - edgePoint.X) < size && Math.Abs(tMouseY - edgePoint.Y) < size) {
                return true;
            }
            else {
                return false;
            }

        }

        public override DrawingVisual GetChart() {
            var drawingVisual = new DrawingVisual();
            if ((MaxY == MinY && MaxY == 0) || (MaxX == MinX && MaxX == 0)) return drawingVisual;
            if (Area.Width < 2 * (Area.Margin.Left + Area.Margin.Right) || Area.Height < 1.5 * (Area.Margin.Bottom + Area.Margin.Top)) return drawingVisual;

            yPacket = (Area.ActualGraphHeight - Area.LabelSpace.Top - Area.LabelSpace.Bottom) / (MaxY - MinY);
            xPacket = Area.ActualGraphWidth / (MaxX - MinX);
            this.drawingContext = drawingVisual.RenderOpen();
            if (SeriesList.Series.Count == 0) return SetDefaultDrawingVisual(drawingVisual);

            InitializeGetChart(drawingVisual);

            //Console.WriteLine(this.drawingContext);

            #region manual peak modification      
            if (IsTargetManualPickMode) {
                var series = SeriesList.Series[0];
                if (series != null && series.Points != null && series.Points.Count > 0)
                    drawChromatogram(series, true);
            }
            else {
                var numSamples = SeriesList.Series.Count;
                if (numSamples > 10 && numSamples < 100) alpha = 1.0 / numSamples;
                else if (numSamples > 100) alpha = 0;
                for (var i = 0; i < SeriesList.Series.Count; i++) {
                    var series = SeriesList.Series[i];
                    if (series.ChartType == ChartType.Point) {
                        drawMarker(series);
                    }
                    else if (series.ChartType == ChartType.Line) {
                        drawLineSeries(series);
                        if (series.MarkerType != MarkerType.None) drawMarker(series);
                    }
                    else if (series.ChartType == ChartType.Chromatogram) {
                        if (series.Accessory != null && series.Accessory.Chromatogram != null) {
                            drawChromatogram(series);
                        }
                        else {
                            drawLineSeries(series);
                        }
                    }
                    if (series.IsLabelVisible) {
                        drawLabel(series);
                    }
                }
            }
            #endregion
            drawingContext.Close();
            return drawingVisual;
        }
    }
}
