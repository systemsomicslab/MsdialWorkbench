using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Rfx.Riken.OsakaUniv;

namespace CompMs.Graphics.Core.Base
{
    public interface IDrawVisual
    {
        Area Area { get; set; }
        SeriesList SeriesList { get; set; }
        Title Title { get; set; }

        float MaxY { get; set; }
        float MinY { get; set; }
        float MaxX { get; set; }
        float MinX { get; set; }
        float Offset { get; set; }
        bool IsTargetManualPickMode { get; set; }
        float yPacket { get; set; }
        float xPacket { get; set; }

        void Initialize();
        DrawingVisual GetChart();
        void ChangeChartArea(double x, double y);
    }

    public class DrawVisual : ViewModelBase, IDrawVisual
    {
        public DrawingContext drawingContext;

        public Area Area { get; set; }
        public SeriesList SeriesList { get; set; }
        public Title Title { get; set; }

        public float MaxY { get; set; }
        public float MinY { get; set; }
        public float MaxX { get; set; }
        public float MinX { get; set; }
        public float Offset { get; set; } = 5;
        public bool IsTargetManualPickMode { get; set; }

        public decimal yMajorScale;
        public decimal yMinorScale;
        public decimal xMajorScale;
        public decimal xMinorScale;

        public double halfDrawHeight = 0;
        public double alpha = 0.1;

        public bool isMSwithRef = false;
        public bool isArticleFormat;

        public float yPacket { get; set; }
        public float xPacket { get; set; }

        public DrawVisual() {
            Area = new Area();
            SeriesList = new SeriesList();
            Title = new Title();
            Initialize();
        }

        public DrawVisual(Area area, Title title, SeriesList seriesList, bool isArticleFormat = false) {
            this.Area = area;
            this.SeriesList = seriesList;
            this.Title = title;
            this.isArticleFormat = isArticleFormat;
            InitializeElements();
        }

        public virtual void Initialize() {
            MaxY = SeriesList.MaxY;
            MinY = SeriesList.MinY;
            MaxX = SeriesList.MaxX;
            MinX = SeriesList.MinX;
            if (MinX == MaxX) MaxX += 1;
            if (MinY == MaxY) MaxY += 1;
        }


        public virtual void InitializeElements() {
            if (SeriesList.Series.Count == 0) { Initialize(); return; }
            // foreach (var s in SeriesList.Series) {
            //     s.SetValues();
            // }
            // SeriesList.SetValues();
            Initialize();
        }

        public void ChangeChartArea(double width, double height) {
            Area.Width = (float)width;
            Area.Height = (float)height;
        }

        #region Save chart
        public virtual void SaveChart(DrawingVisual dv, string path, int width, int height, int dpiX, int dpiY ) {
            var bitmap = new RenderTargetBitmap(width * dpiX / 96, height * dpiY / 96, dpiX, dpiY, PixelFormats.Pbgra32);
            bitmap.Render(dv); 

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = System.IO.File.Create(path)) {
                encoder.Save(stream);
            }
            bitmap.Clear();
        }

        public virtual void SaveDrawingAsEmf(DrawingVisual dv, string path) {
            var drawing = dv.Drawing;

            using (var stream = File.Create(path)) {
                using (var graphics = BCDev.XamlToys.Utility.CreateEmf(stream, drawing.Bounds)) {
                    BCDev.XamlToys.Utility.RenderDrawingToGraphics(drawing, graphics);
                }
            }
        }
        #endregion

        #region Draw background, graphRegion, x-axis, y-axis 
        protected void drawBackground() {
            drawBackgroundWhite();
            drawBackgroundColor();
            drawAxisLines();
        }

        private void drawBackgroundWhite() {
            if (!isArticleFormat) {
                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, Area.Width, Area.Height));
            }
        }

        private void drawBackgroundColor() {
            if (!isArticleFormat) {
                drawingContext.DrawRectangle(Area.BackGroundColor, Area.GraphBorder, new Rect(new Point(Area.Margin.Left, Area.Margin.Top), new Size(Area.ActualGraphWidth, Area.ActualGraphHeight)));
            }
        }

        private void drawAxisLines() {
            drawingContext.DrawLine(Area.AxisX.Pen, new Point(Area.Margin.Left, Area.Height - Area.Margin.Bottom), new Point(Area.Width - Area.Margin.Right, Area.Height - Area.Margin.Bottom));
            drawingContext.DrawLine(Area.AxisY.Pen, new Point(Area.Margin.Left, Area.Height - Area.Margin.Bottom), new Point(Area.Margin.Left, Area.Margin.Top));
            if (isMSwithRef)
                drawingContext.DrawLine(Area.AxisX.Pen, new Point(Area.Margin.Left, Area.Margin.Top + Area.LabelSpace.Top + halfDrawHeight), new Point(Area.Width - Area.Margin.Right, Area.Margin.Top + Area.LabelSpace.Top + halfDrawHeight));
        }

        #endregion

        #region Graph Titile
        protected void drawGraphTitle() {
            var formattedText = new FormattedText(Title.Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Title.FontType, Title.FontSize, Title.FontColor);
            formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(Area.Margin.Left, Area.Margin.Top - (Title.FontSize + Offset)));
        }

        #endregion

        #region draw each series. Point and Line
        protected void drawMarker(Series series) {
            if (series.MarkerType == MarkerType.Circle) {
                drawPointSeries_Circle(series);
            }
            else if (series.MarkerType == MarkerType.Square) {
                drawPointSeries_Square(series);
            }
            else if (series.MarkerType == MarkerType.Cross) {
                drawPointSeries_Cross(series);
            }
        }

        protected void drawPointSeries_Square(Series series) {
            foreach (var XY in series.Points) {
                var xs = Area.Margin.Left + (XY.X - MinX) * xPacket - series.MarkerSize.Width * 0.5;
                var ys = Area.Margin.Bottom + Area.LabelSpace.Bottom + (XY.Y - MinY) * yPacket - series.MarkerSize.Height * 0.5;
                drawingContext.DrawRectangle(series.Brush, series.Pen, new Rect(new Point(xs, ys), series.MarkerSize));
            }
        }

        protected void drawPointSeries_Circle(Series series) {
            foreach (var XY in series.Points) {
                var xs = Area.Margin.Left + (XY.X - MinX) * xPacket;
                var ys = Area.Margin.Bottom + Area.LabelSpace.Bottom + (XY.Y - MinY) * yPacket;
                drawingContext.DrawEllipse(series.Brush, series.Pen, new Point(xs, ys), series.MarkerSize.Height * 0.5, series.MarkerSize.Width * 0.5);
            }
        }

        protected void drawPointSeries_Cross(Series series) {
            foreach (var XY in series.Points) {
                var xs = Area.Margin.Left + (XY.X - MinX) * xPacket - series.MarkerSize.Width * 0.5;
                var ys = Area.Margin.Bottom + Area.LabelSpace.Bottom + (XY.Y - MinY) * yPacket - series.MarkerSize.Height * 0.5;
                var xe = xs + series.MarkerSize.Width;
                var ye = ys + series.MarkerSize.Height;
                drawingContext.DrawLine(series.Pen, new Point(xs, ys), new Point(xe, ye));
                drawingContext.DrawLine(series.Pen, new Point(xs, ye), new Point(xe, ys));
            }
        }

        protected void drawLineSeries(Series series) {
            var pathFigure = new PathFigure(); var flag = true;
            foreach (var XY in series.Points) {
                var xs = Area.Margin.Left + (XY.X - MinX) * xPacket;
                var ys = Area.Margin.Bottom + Area.LabelSpace.Bottom + (XY.Y - MinY) * yPacket;
                if (flag) {
                    pathFigure.StartPoint = new Point(xs, ys);
                    flag = false;
                }
                else {
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                }
            }
            pathFigure.Freeze();
            var pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
            pathGeometry.Freeze();

            // Draw Chromatogram & Area
            this.drawingContext.DrawGeometry(null, series.Pen, pathGeometry); // Draw Chromatogram Graph Line  
        }

        protected virtual void drawChromatogram(Series series, bool isManualPicking = false) {
            var pathFigure = new PathFigure();
            var areaPath = new PathFigure();

            var flag = true;
            var flagLeft = true;
            var flagRight = true;
            var flagFill = false;

            var graphBrush = Utility.CombineAlphaAndColor(this.alpha, (SolidColorBrush)series.Brush);// Set Graph Brush

            var rtwidth = series.Accessory.Chromatogram.RtRight - series.Accessory.Chromatogram.RtLeft;

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

                if (flagLeft && XY.X >= series.Accessory.Chromatogram.RtLeft) {
                    areaPath.StartPoint = new Point(xs, Area.Margin.Bottom); // PathFigure for GraphLine 
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                    flagFill = true; flagLeft = false;
                }

                if (flagFill) {
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, ys) });
                }

                if (flagRight && XY.X >= series.Accessory.Chromatogram.RtRight) {
                    areaPath.Segments.Add(new LineSegment() { Point = new Point(xs, Area.Margin.Bottom) }); // PathFigure for GraphLine 
                    flagRight = false; flagFill = false;
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

        }
        #endregion

        protected void drawLegend() {
            if (!SeriesList.AreLegendsVisible) return;
            var targetSeries = this.SeriesList.Series.Where(x => x.Legend.IsVisible == true).ToList();
            var maxWidth = targetSeries.Max(x => x.Legend.MaxWidth);
            //var formattedText = new FormattedText(firstSeries.Legend.Text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, firstSeries.FontType, firstSeries.Legend.FontSize, firstSeries.Brush);
            var LineMarker = "- ";
            var textHeight = 0.0;
            var formattedTextList = new List<FormattedText>();
            foreach (var s in targetSeries) {
                var formattedText = new FormattedText(LineMarker + s.Legend.Text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.Legend.FontSize, s.Brush);
                formattedText.TextAlignment = TextAlignment.Left;
                formattedText.MaxTextWidth = maxWidth;
                formattedText.MaxLineCount = 1;
                formattedTextList.Add(formattedText);
            }
            var maxFormattedWidth = formattedTextList.Max(x => x.Width);
            if (maxWidth > maxFormattedWidth) maxWidth = (int)maxFormattedWidth;
            if (!SeriesList.AreLegendsInGraphArea) Area.Margin.Right += maxWidth + Offset * 2;
            for (var i = 0; i < targetSeries.Count; i++) {
                var text = formattedTextList[i];
                var series = targetSeries[i];
                if (series.Legend.Position == Position.Right) {                    
                    if (series.Legend.InGraphicArea) {
                        var pointX = Area.Margin.Left + Area.ActualGraphWidth - maxWidth - Offset;
                        var pointY = Area.Margin.Top + Offset + textHeight;
                        this.drawingContext.DrawText(text, new Point(pointX, pointY));
                        textHeight += text.Height;
                    }
                    else {
                        var pointX = Area.Margin.Left + Area.ActualGraphWidth + Offset;
                        var pointY = Area.Margin.Top + Offset + textHeight;
                        this.drawingContext.DrawText(text, new Point(pointX, pointY));
                        textHeight += text.Height;
                    }
                }
            }
        }


        #region Draw Axis detail
        protected void drawCaptionOnAxis() {
            // Set Caption to X-Axis 
            if (Area.AxisX.Enabled) {
                int figure = -1;
                var xAxisMaxValue = Math.Max(MaxX, Math.Abs(MinX));

                if (xAxisMaxValue < 1)
                    figure = (int)Utility.RoundUp(Math.Log10(xAxisMaxValue), 0);
                else
                    figure = (int)Utility.RoundDown(Math.Log10(xAxisMaxValue), 0);

                var axisXLabel = Area.AxisX.AxisLabel;
                if (figure > 3)
                    axisXLabel = axisXLabel + " (1e" + figure + ")";
                else if (figure < -1)
                    axisXLabel = axisXLabel + " (1e" + figure + ")";

                var formattedText = new FormattedText(axisXLabel, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisX.FontType, Area.AxisX.FontSize, Area.AxisX.FontColor);
                formattedText.TextAlignment = TextAlignment.Center;
                if(Area.AxisX.IsItalicLabel)
                    formattedText.SetFontStyle(FontStyles.Italic);
                drawingContext.DrawText(formattedText, new Point(Area.Margin.Left + 0.5 * Area.ActualGraphWidth, Area.Height - Area.AxisX.FontSize));

            }
            if (Area.AxisY.Enabled) {
                // Set Caption to Y-Axis                                                
                this.drawingContext.PushTransform(new TranslateTransform(Offset, Area.Margin.Top + 0.5 * Area.ActualGraphHeight));
                this.drawingContext.PushTransform(new RotateTransform(270.0));

                var figure = -1;
                var yAxisMaxValue = Math.Max(MaxY, Math.Abs(MinY));

                if (yAxisMaxValue < 1)
                    figure = (int)Utility.RoundUp(Math.Log10(yAxisMaxValue), 0);
                else
                    figure = (int)Utility.RoundDown(Math.Log10(yAxisMaxValue), 0);

                var axisYLabel = Area.AxisY.AxisLabel;
                if (figure > 3)
                    axisYLabel = axisYLabel + " (1e" + figure + ")";
                else if (figure < -1)
                    axisYLabel = axisYLabel + " (1e" + figure + ")";


                var formattedText = new FormattedText(axisYLabel, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisX.FontType, Area.AxisX.FontSize, Area.AxisX.FontColor);
                formattedText.TextAlignment = TextAlignment.Center;
                if (Area.AxisY.IsItalicLabel)
                    formattedText.SetFontStyle(FontStyles.Italic);
                this.drawingContext.DrawText(formattedText, new Point(0, 0));
                this.drawingContext.PushTransform(new RotateTransform(-270.0));
                this.drawingContext.PushTransform(new TranslateTransform(-Offset, -(Area.Margin.Top + 0.5 * Area.ActualGraphHeight)));
            }
         }

        protected void drawScaleOnYAxis() {
            string yString = ""; // String for Y-Scale Value
            int foldChange = -1;
            double yscale_max;
            double yscale_min;

            yscale_max = MaxY;
            yscale_min = MinY;

            if (yscale_max == yscale_min) yscale_max += 0.9;
            var absMax = Math.Max(yscale_max, Math.Abs(yscale_min));

            // Check Figure of Displayed Max Intensity
            if (absMax < 1) {
                foldChange = (int)Utility.RoundUp(Math.Log10(absMax), 0);
            }
            else {
                foldChange = (int)Utility.RoundDown(Math.Log10(absMax), 0);
            }
            if (isMSwithRef) {
                yscale_min = -1 * yscale_max;
            }
            getYaxisScaleInterval((float)yscale_max, (float)yscale_min);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++) {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = Area.Height - Area.Margin.Bottom - Area.LabelSpace.Bottom - (yAxisValue - yscale_min) * yPacket;
                if (yPixelValue > Area.Height - Area.Margin.Bottom - Area.LabelSpace.Bottom) continue;
                if (yPixelValue < Area.Margin.Top) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (isMSwithRef) {
                        if (foldChange > 3) { yString = (Math.Abs(yAxisValue) / Math.Pow(10, foldChange)).ToString("f2"); }
                        else if (foldChange < -1) { yString = (Math.Abs(yAxisValue) / Math.Pow(10, foldChange)).ToString("f1"); }
                        else {
                            if (this.yMajorScale >= 10) yString = Math.Abs(yAxisValue).ToString("f0");
                            else if (this.yMajorScale < (decimal)0.1) yString = Math.Abs(yAxisValue).ToString("f2");
                            else yString = Math.Abs(yAxisValue).ToString("f1");
                        }
                    }
                    else {
                        if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                        else if (foldChange < -1) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                        else {
                            if (this.yMajorScale >= 10) yString = yAxisValue.ToString("f0");
                            else if (this.yMajorScale < (decimal)0.1) yString = yAxisValue.ToString("f2");
                            else yString = yAxisValue.ToString("f1");
                        }
                    }
                    this.drawingContext.DrawLine(Area.AxisY.Pen, new Point(Area.Margin.Left - Area.AxisY.MajorScaleSize, yPixelValue), new Point(Area.Margin.Left, yPixelValue));
                    var formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisY.FontType, Area.AxisY.FontSize, Area.AxisY.FontColor);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(Area.Margin.Left - Area.AxisY.MajorScaleSize - Area.AxisY.Pen.Thickness - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else {
                    if(Area.AxisY.MinorScaleEnabled)
                        this.drawingContext.DrawLine(Area.AxisY.Pen, new Point(Area.Margin.Left - Area.AxisY.MinorScaleSize, yPixelValue), new Point(Area.Margin.Left, yPixelValue));
                }
            }
        }

        protected void getYaxisScaleInterval(float max, float min) {
            if (max == min) max += 0.9f;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(Area.ActualGraphHeight - Area.LabelSpace.Top - Area.LabelSpace.Bottom);
            int yStart, yEnd;
            double yScaleHeight = 0.0, totalPixelWidth = 0.0;

            while (totalPixelWidth > yAxisPixelRange) { 
                yScale *= 2;

                yStart = (int)(min / yScale) - 1;
                yEnd = (int)(max / yScale) + 1;

                formattedText = new FormattedText(yScale.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisY.FontType, Area.AxisY.FontSize, Brushes.Black);

                yScaleHeight = formattedText.Height * 0.5;
                totalPixelWidth = yScaleHeight * (yEnd - yStart + 1);

            }
            yScale *= 2;

            this.yMajorScale = (decimal)yScale;
            this.yMinorScale = (decimal)((double)this.yMajorScale * 0.25);
        }

        protected void drawScaleOnXAxis() {
            var xString = "";
            getXaxisScaleInterval();
            

            int xStart = (int)(MinX / (double)this.xMinorScale) - 1;
            int xEnd = (int)(MaxX / (double)this.xMinorScale) + 1;
            var foldChange = -1;

            var absMax = Math.Max(MaxX, Math.Abs(MinX));

            // Check Figure of Displayed Max Intensity
            if (absMax < 1) {
                foldChange = (int)Utility.RoundUp(Math.Log10(absMax), 0);
            }
            else {
                foldChange = (int)Utility.RoundDown(Math.Log10(absMax), 0);
            }
        


        FormattedText formattedText;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++) {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = Area.Margin.Left + (xAxisValue - MinX) * this.xPacket;
                if (xPixelValue < Area.Margin.Left) continue;
                if (xPixelValue > Area.Width - Area.Margin.Right) break;
                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { xString = (xAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange < -1) { xString = (xAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else {
                        if (this.xMajorScale >= 10) xString = xAxisValue.ToString("f0");
                        else if (this.xMajorScale < (decimal)0.1) xString = xAxisValue.ToString("f2");
                        else xString = xAxisValue.ToString("f1");
                    }
                    this.drawingContext.DrawLine(Area.AxisX.Pen, new Point(xPixelValue, Area.Height - Area.Margin.Bottom), new Point(xPixelValue, Area.Height - Area.Margin.Bottom + Area.AxisX.MajorScaleSize));
                    formattedText = new FormattedText(xString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisX.FontType, Area.AxisX.FontSize, Area.AxisX.FontColor);
                    formattedText.TextAlignment = TextAlignment.Center;
                    drawingContext.DrawText(formattedText, new Point(xPixelValue, Area.Height - Area.Margin.Bottom + Area.AxisX.MajorScaleSize + 1));
                }
                else//Minor scale
                {
                    if(Area.AxisX.MinorScaleEnabled)
                        this.drawingContext.DrawLine(Area.AxisX.Pen, new Point(xPixelValue, Area.Height - Area.Margin.Bottom), new Point(xPixelValue, Area.Height - Area.Margin.Bottom + Area.AxisX.MinorScaleSize));
                }
            }
        }

        protected void getXaxisScaleInterval() {
            var max = MaxX;
            var min = MinX;

            if (Math.Abs(max) > 1000000000 || Math.Abs(min) > 1000000000) return;
            if (max <= min) { max = min + 0.9f; }
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)Area.ActualGraphWidth;
            int xStart, xEnd;
            double xScaleWidth = 0, totalPixelWidth = 0;

            while (totalPixelWidth > (double)xAxisPixelRange) { 
                xScale *= 2;

                xStart = (int)(min / xScale) - 1;
                xEnd = (int)(max / xScale) + 1;

                formattedText = new FormattedText(xScale.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, Area.AxisX.FontType, Area.AxisX.FontSize, Brushes.Black);

                xScaleWidth = formattedText.Width;
                totalPixelWidth = xScaleWidth * (xEnd - xStart + 1);
            }
            xScale *= 2;
            if (Area.Width < 250) xScale *= 2;
            this.xMajorScale = (decimal)xScale;
            this.xMinorScale = (decimal)((double)this.xMajorScale * 0.25);
        }

        #endregion

        #region Draw Label
        protected void drawLabel(Series series) {
            this.drawingContext.PushTransform(new ScaleTransform(1, -1)); 
            this.drawingContext.PushTransform(new TranslateTransform(0, -Area.Height));

            for (var l = 0; l < series.Points.Count; l++) {
                var xs = Area.Margin.Left + (series.Points[l].X - MinX) * xPacket;
                var ys = Area.Height - ( Area.Margin.Bottom + Area.LabelSpace.Bottom + (series.Points[l].Y - MinY) * yPacket);

                var label = series.Points[l].Label;
                var formattedText = new FormattedText(label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    series.FontType, series.FontSize, series.Brush);
                formattedText.TextAlignment = TextAlignment.Center;
                drawingContext.DrawText(formattedText, new Point(xs, ys - series.FontSize - Offset * 2));

            }
            //this.drawingContext.Pop();// Reset Drawing Region

            this.drawingContext.PushTransform(new TranslateTransform(0, Area.Height)); 
            this.drawingContext.PushTransform(new ScaleTransform(1, -1)); 
 
        }
        #endregion

        #region Draw Label for MS
        public void DrawLabel_MS(Series series) {
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushTransform(new TranslateTransform(0, -Area.Height));
            var s = series;

            if (s.IsLabelVisible == true && s.Points.Count != 0) {
                var LabelList = new List<XY>();
                var formattedText = new FormattedText("@@@.@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, Brushes.Black);
                double textWidth = formattedText.Width;
                int maxLabelNumber = (int)((Area.ActualGraphWidth) / textWidth);

                if (maxLabelNumber >= 1) {

                    for (int i = 0; i < s.Points.Count; i++) {
                        var mzValue = (float)s.Points[i].X;
                        var intensity = (float)s.Points[i].Y;

                        if (mzValue < MinX) continue;
                        if (mzValue > MaxX) break;

                        var xt = Area.Margin.Left + (mzValue - (float)MinX) * this.xPacket;// Calculate x Plot Coordinate
                        var yt = Area.Margin.Bottom + Area.LabelSpace.Bottom + (intensity - (float)MinY) * this.yPacket;// Calculate y Plot Coordinate

                        if (xt < double.MinValue || xt > double.MaxValue || yt < double.MinValue || yt > double.MaxValue) continue;// Avoid Calculation Error

                        LabelList.Add(new XY() { X = xt, Y = yt, Label = s.Points[i].Label });
                    }
                    var s_ordered = LabelList.OrderByDescending(x => x.Y).ToList();

                    bool overlap = false;
                    int backtrace = 0;
                    int counter = 0;

                    for (int i = 0; i < s_ordered.Count; i++) {
                        if (counter > maxLabelNumber) break;

                        formattedText = new FormattedText(s_ordered[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, s.Brush);
                        formattedText.TextAlignment = TextAlignment.Center;
                        textWidth = formattedText.Width;

                        if (s_ordered[i].X - Area.Margin.Left < textWidth * 0.5) continue;
                        if (Area.Margin.Left + Area.ActualGraphWidth - s_ordered[i].X < textWidth * 0.5) continue;

                        overlap = false;
                        backtrace = 0;

                        while (backtrace < i) {
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth * 0.5) { overlap = true; break; }
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth && s_ordered[backtrace].Y <= s_ordered[i].Y + 10) { overlap = true; break; }
                            if (backtrace > s_ordered.Count) break;
                            backtrace++;
                        }

                        if (overlap == false) {
                            this.drawingContext.DrawText(formattedText, new Point(s_ordered[i].X, Area.Height - s_ordered[i].Y - formattedText.Height));
                            counter++;
                        }
                    }
                }
            }
        }

        #endregion

        #region Draw Label for MS with ref
        public void DrawLabel_MSwithRef(List<Series> series) {
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushTransform(new TranslateTransform(0, -Area.Height));

            #region drawLabel for measured MS
            var s = series[0];
            
            if (s.IsLabelVisible == true && s.Points.Count != 0) {
                var LabelList = new List<XY>();
                var formattedText = new FormattedText("@@@.@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, Brushes.Black);
                double textWidth = formattedText.Width;
                int maxLabelNumber = (int)((Area.ActualGraphWidth) / textWidth);

                if (maxLabelNumber >= 1) {

                    for (int i = 0; i < s.Points.Count; i++) {
                        var mzValue = (float)s.Points[i].X;
                        var intensity = (float)s.Points[i].Y;

                        if (mzValue < MinX) continue;
                        if (mzValue > MaxX) break;

                        var xt = Area.Margin.Left + (mzValue - (float)MinX) * this.xPacket;// Calculate x Plot Coordinate
                        var yt = Area.Margin.Bottom + Area.LabelSpace.Bottom + (float)halfDrawHeight + (intensity - (float)MinY) * this.yPacket;// Calculate y Plot Coordinate

                        if (xt < double.MinValue || xt > double.MaxValue || yt < double.MinValue || yt > double.MaxValue) continue;// Avoid Calculation Error

                        LabelList.Add(new XY() { X = xt, Y = yt, Label = s.Points[i].Label });
                    }
                    var s_ordered = LabelList.OrderByDescending(x => x.Y).ToList();

                    bool overlap = false;
                    int backtrace = 0;
                    int counter = 0;

                    for (int i = 0; i < s_ordered.Count; i++) {
                        if (counter > maxLabelNumber) break;

                        formattedText = new FormattedText(s_ordered[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, s.Brush);
                        formattedText.TextAlignment = TextAlignment.Center;

                        overlap = false;
                        backtrace = 0;

                        while (backtrace < i) {
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth * 0.5) { overlap = true; break; }
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth && s_ordered[backtrace].Y <= s_ordered[i].Y + 10) { overlap = true; break; }
                            if (backtrace > s_ordered.Count) break;
                            backtrace++;
                        }

                        if (overlap == false) {
                            this.drawingContext.DrawText(formattedText, new Point(s_ordered[i].X, Area.Height - s_ordered[i].Y - formattedText.Height));
                            counter++;
                        }
                    }
                }
            }

            s = series[1];
            if (s.IsLabelVisible == true && s.Points.Count != 0) {
                var LabelList = new List<XY>();
                var formattedText = new FormattedText("@@@.@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, s.Brush);
                double textWidth = formattedText.Width;
                int maxLabelNumber = (int)((Area.ActualGraphWidth) / textWidth);

                if (maxLabelNumber >= 1) {

                    for (int i = 0; i < s.Points.Count; i++) {
                        var mzValue = (float)s.Points[i].X;
                        var intensity = (float)s.Points[i].Y;

                        if (mzValue < MinX) continue;
                        if (mzValue > MaxX) break;

                        var xt = Area.Margin.Left + (mzValue - (float)MinX) * this.xPacket;// Calculate x Plot Coordinate
                        var yt = Area.Margin.Top + Area.LabelSpace.Top + (float)halfDrawHeight + (intensity - (float)MinY) * this.yPacket;// Calculate y Plot Coordinate

                        if (xt < double.MinValue || xt > double.MaxValue || yt < double.MinValue || yt > double.MaxValue) continue;// Avoid Calculation Error

                        LabelList.Add(new XY() { X = xt, Y = yt, Label = s.Points[i].Label });
                    }
                    var s_ordered = LabelList.OrderByDescending(x => x.Y).ToList();

                    bool overlap = false;
                    int backtrace = 0;
                    int counter = 0;

                    for (int i = 0; i < s_ordered.Count; i++) {
                        if (counter > maxLabelNumber) break;

                        formattedText = new FormattedText(s_ordered[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, s.FontType, s.FontSize, s.Brush);
                        formattedText.TextAlignment = TextAlignment.Center;

                        overlap = false;
                        backtrace = 0;

                        while (backtrace < i) {
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth * 0.5) { overlap = true; break; }
                            if (Math.Abs(s_ordered[backtrace].X - s_ordered[i].X) < textWidth && s_ordered[backtrace].Y <= s_ordered[i].Y + 10) { overlap = true; break; }
                            if (backtrace > s_ordered.Count) break;
                            backtrace++;
                        }

                        if (overlap == false) {
                            this.drawingContext.DrawText(formattedText, new Point(s_ordered[i].X, s_ordered[i].Y ));
                            counter++;
                        }
                    }

                    this.drawingContext.PushTransform(new TranslateTransform(0, Area.Height));
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                }
            }
                #endregion
            }
        #endregion
        
        protected virtual void InitializeGetChart(DrawingVisual drawingVisual) {
            yPacket = (Area.ActualGraphHeight - Area.LabelSpace.Top - Area.LabelSpace.Bottom) / (MaxY - MinY);
            xPacket = Area.ActualGraphWidth / (MaxX - MinX);
            if(SeriesList.Series.Count == 2 && SeriesList.Series[0].ChartType == ChartType.MSwithRef && SeriesList.Series[1].ChartType == ChartType.MSwithRef)
            {
                isMSwithRef = true;
                yPacket /= 2;
                halfDrawHeight = (Area.ActualGraphHeight - Area.LabelSpace.Top - Area.LabelSpace.Bottom) / 2;
            }
            if (SeriesList.AreLegendsInGraphArea) {
                drawBackground();
                drawLegend();
            }
            else {
                drawBackgroundWhite();
                drawLegend();
                yPacket = (Area.ActualGraphHeight - Area.LabelSpace.Top - Area.LabelSpace.Bottom) / (MaxY - MinY);
                xPacket = Area.ActualGraphWidth / (MaxX - MinX);

                if (isMSwithRef)
                    yPacket /= 2;

                drawBackgroundColor();
                drawAxisLines();
            }
            drawGraphTitle();
            drawCaptionOnAxis();
            if (Area.AxisY.Enabled && Area.AxisY.ScaleEnabled)
                drawScaleOnYAxis();
            if (Area.AxisX.Enabled && Area.AxisX.ScaleEnabled)
                drawScaleOnXAxis();

            this.drawingContext.PushTransform(new TranslateTransform(0, Area.Height)); // 最終的な出力をずらすかどうか。0, drawHeightだと移動なし
            this.drawingContext.PushTransform(new ScaleTransform(1, -1)); // スケールの変更。-1がついているのでY軸で反転
            if (!isArticleFormat)
                this.drawingContext.PushClip(new RectangleGeometry(new Rect(Area.Margin.Left, Area.Margin.Bottom, Area.ActualGraphWidth, Area.ActualGraphHeight))); //指定した領域にだけ描画できる。
         }

        public virtual DrawingVisual GetChart() {
            var drawingVisual = new DrawingVisual();

            // return null
            if ((MaxY == MinY && MaxY == 0) || (MaxX == MinX && MaxX == 0)) return drawingVisual;
            if (Area.Width < 2 * (Area.Margin.Left + Area.Margin.Right) || Area.Height < 1.5 * (Area.Margin.Bottom + Area.Margin.Top)) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();
            if (SeriesList.Series.Count == 0) return SetDefaultDrawingVisual(drawingVisual);

            InitializeGetChart(drawingVisual);

            #region mass spectrum 
            if (SeriesList.Series[0].ChartType == ChartType.MS) {
                foreach (var xy in SeriesList.Series[0].Points) {
                    this.drawingContext.DrawLine(SeriesList.Series[0].Pen, new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, Area.LabelSpace.Bottom + Area.Margin.Bottom), new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, Area.LabelSpace.Bottom + Area.Margin.Bottom + (xy.Y - MinY) * yPacket));
                }
                DrawLabel_MS(SeriesList.Series[0]);
            }
            #endregion

            #region mass spectrum with reference
            else if (isMSwithRef) {
                foreach (var xy in SeriesList.Series[0].Points) {
                    this.drawingContext.DrawLine(SeriesList.Series[0].Pen, new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, halfDrawHeight + Area.LabelSpace.Bottom + Area.Margin.Bottom), new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, halfDrawHeight + Area.LabelSpace.Bottom + Area.Margin.Bottom + (xy.Y - MinY) * yPacket));
                }
                foreach (var xy in SeriesList.Series[1].Points) {
                    this.drawingContext.DrawLine(SeriesList.Series[1].Pen, new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, halfDrawHeight + Area.LabelSpace.Bottom + Area.Margin.Bottom), new Point(this.Area.Margin.Left + (xy.X - MinX) * xPacket, halfDrawHeight + Area.LabelSpace.Bottom + Area.Margin.Bottom - (xy.Y - MinY) * yPacket));
                }
                DrawLabel_MSwithRef(SeriesList.Series);
            }
            #endregion

            #region others
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

            this.drawingContext.Close();// Close DrawingContext
            
            return drawingVisual;
        }

        #region default drawing visual         
        protected DrawingVisual SetDefaultDrawingVisual(DrawingVisual drawingVisual) {
            MaxX = 10; MaxY = 10; MinY = 0;MinX = 0;
            if (string.IsNullOrWhiteSpace(Title.Label)) {
                Title.Label = "No Data";
                Title.FontSize = 15;
            }
            drawBackground();
            drawGraphTitle();
            drawCaptionOnAxis();
            drawScaleOnYAxis();
            drawScaleOnXAxis();
            this.drawingContext.Close();// Close DrawingContext
            return drawingVisual;
        }
        #endregion
    }
}
