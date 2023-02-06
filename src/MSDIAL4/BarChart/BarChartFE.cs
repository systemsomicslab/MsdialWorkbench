using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Common.BarChart
{
    public class BarChartFE : FrameworkElement
    {
        private VisualCollection visualCollection;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;

        private BarChartBean barChartBean;
        private BarChartUI barChartUI;

        //Graph area format
        private double longScaleSize = 5; // Scale Size (Long)
        private double shortScaleSize = 2; // Scale Size (Short)
        private double axisFromGraphArea = 1; // Location of Axis from Graph Area
        private double labelYDistance = 25; // Label Distance From Peak Top

        // Constant for Graph Scale
        private decimal yMajorScale = -1; // Y-Axis Major Scale (Intensity)
        private decimal yMinorScale = -1; // Y-Axis Minor Scale (Intensity)

        // Graph Color & Font Settings
        private FormattedText formattedText; // FormatText for Scale    
        private Brush graphBackGround = Brushes.WhiteSmoke; // Graph Background
        private Pen graphBorder = new Pen(Brushes.LightGray, 1.0); // Graph Border
        private Pen graphAxis = new Pen(Brushes.Black, 0.5);

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  

        // Drawing Packet
        private double yPacket;

        // Bar width
        private double drawSpace;
        private double barSpace;
        private double barWidth;
        private double barMargin;
        private double elementNumber;
        private double errorLineLength;
        private double errorWidth;

        public BarChartFE(BarChartBean barChartBean, BarChartUI barChartUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.barChartBean = barChartBean;
            this.barChartUI = barChartUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void BarChartDraw()
        {
            this.visualCollection.Clear();
            this.drawingVisual = GetBarChartDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        public DrawingVisual GetBarChartDrawingVisual(double drawWidth, double drawHeight)
        {
            var drawingVisual = new DrawingVisual();

            // Check Drawing Size
            //if (drawWidth < 2 * (this.barChartUI.LeftMargin + this.barChartUI.RightMargin) || drawHeight < 1.5 * (this.barChartUI.BottomMargin + this.barChartUI.TopMargin)) return drawingVisual;
            if (drawWidth < this.barChartUI.LeftMargin + this.barChartUI.RightMargin + 30 || drawHeight < this.barChartUI.BottomMargin + this.barChartUI.TopMargin + 30) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            drawBackground(drawWidth, drawHeight);
            if (this.barChartBean == null) return drawNullBarChart(this.drawingVisual, drawWidth, drawHeight);
            this.yPacket = (drawHeight - this.barChartUI.TopMargin - this.barChartUI.TopMarginForLabel - this.barChartUI.BottomMargin) / (double)(this.barChartBean.MaxValue - this.barChartBean.MinValue);

            drawingSetting(drawWidth, drawHeight, this.barChartBean.DisplayedBarElements.Count);
            drawGraphTitle(this.barChartBean.MainTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, this.barChartBean.MinValue, this.barChartBean.MaxValue, this.barChartBean.XAxisTitle, this.barChartBean.YAxisTitle);
            drawScaleOnYAxis(this.barChartBean.MinValue, this.barChartBean.MaxValue, drawWidth, drawHeight);
            drawScaleOnXAxis(drawWidth, drawHeight, this.barChartBean.DisplayedBarElements, this.barChartBean.XAxisTitle);

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.barChartUI.LeftMargin, this.barChartUI.BottomMargin, drawWidth - this.barChartUI.LeftMargin - this.barChartUI.RightMargin, drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin)));

            // 5-1. Initialize Graph Plot Start
            #region
            // Graph Brush and Pen
            Pen errorPen;
            errorPen = new Pen(Brushes.Black, this.errorLineLength); // Set Graph Pen
            errorPen.Freeze();

            var graphBrush = combineAlphaAndColor(0.85, Brushes.Blue);
            #endregion

            var elements = this.barChartBean.DisplayedBarElements;
            for (int i = 0; i < elements.Count; i++) {
                if (elements[i].IsBoxPlot == false) {
                    var barMax = elements[i].Value + elements[i].Error;
                    var barValue = elements[i].Value;
                    var barMin = elements[i].Value - elements[i].Error;
                    var minValue = this.barChartBean.MinValue;

                    var yPointMax = this.barChartUI.BottomMargin + (barMax - minValue) * this.yPacket;
                    var yPointCenter = this.barChartUI.BottomMargin + (barValue - minValue) * this.yPacket;
                    var yPointMin = this.barChartUI.BottomMargin + (barMin - minValue) * this.yPacket;
                    var xPoint = this.barSpace * 0.5 + i * this.barSpace + this.barChartUI.LeftMargin;

                    var barBrush = combineAlphaAndColor(0.75, elements[i].Brush);

                    this.drawingContext.DrawLine(new Pen(barBrush, this.barWidth), new Point(xPoint, yPointCenter), new Point(xPoint, this.barChartUI.BottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.barWidth * 0.5, yPointCenter), new Point(xPoint - this.barWidth * 0.5, this.barChartUI.BottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint + this.barWidth * 0.5, yPointCenter), new Point(xPoint + this.barWidth * 0.5, this.barChartUI.BottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.barWidth * 0.5, yPointCenter), new Point(xPoint + this.barWidth * 0.5, yPointCenter));

                    // check mouse pointer focus
                    mouseFocusDraw(elements[i], xPoint, yPointCenter, drawHeight, drawWidth);

                    if (elements[i].Error <= 0) continue;
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPointMin), new Point(xPoint, yPointMax));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.errorWidth, yPointMax), new Point(xPoint + this.errorWidth, yPointMax));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.errorWidth, yPointMin), new Point(xPoint + this.errorWidth, yPointMin));
                }
                else {
                    var minValue = this.barChartBean.MinValue;

                    var boxMax = elements[i].MaxValue;
                    var box75 = elements[i].SeventyFiveValue;
                    var boxMedian = elements[i].Median;
                    var box25 = elements[i].TwentyFiveValue;
                    var boxMin = elements[i].MinValue;

                    var yPointMax = this.barChartUI.BottomMargin + (boxMax - minValue) * this.yPacket;
                    var yPoint75 = this.barChartUI.BottomMargin + (box75 - minValue) * this.yPacket;
                    var yPointMedian = this.barChartUI.BottomMargin + (boxMedian - minValue) * this.yPacket;
                    var yPoint25 = this.barChartUI.BottomMargin + (box25 - minValue) * this.yPacket;
                    var yPointMin = this.barChartUI.BottomMargin + (boxMin - minValue) * this.yPacket;
                    var xPoint = this.barSpace * 0.5 + i * this.barSpace + this.barChartUI.LeftMargin;

                    //Console.WriteLine("ID={0}, Min={1}, 25%={2}, Mid={3}, 75%={4}, Max={5}", i, yPointMin, yPoint25, yPointMedian, yPoint75, yPointMax);

                    var barBrush = combineAlphaAndColor(0.75, elements[i].Brush);

                    // draw box
                    this.drawingContext.DrawLine(new Pen(barBrush, this.barWidth),
                        new Point(xPoint, yPoint75), 
                        new Point(xPoint, yPoint25));

                    // draw median
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPointMedian),
                        new Point(xPoint + this.barWidth * 0.5, yPointMedian));

                    // draw border
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint75),
                        new Point(xPoint - this.barWidth * 0.5, yPoint25));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint + this.barWidth * 0.5, yPoint75),
                        new Point(xPoint + this.barWidth * 0.5, yPoint25));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint75),
                        new Point(xPoint + this.barWidth * 0.5, yPoint75));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint25),
                        new Point(xPoint + this.barWidth * 0.5, yPoint25));

                    // check mouse pointer focus
                    mouseFocusDraw(elements[i], xPoint, yPoint25, yPoint75, drawHeight, drawWidth);

                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPoint75), new Point(xPoint, yPointMax));
                    this.drawingContext.DrawLine(errorPen, 
                        new Point(xPoint - this.errorWidth, yPointMax), 
                        new Point(xPoint + this.errorWidth, yPointMax));


                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPoint25), new Point(xPoint, yPointMin));
                    this.drawingContext.DrawLine(errorPen, 
                        new Point(xPoint - this.errorWidth, yPointMin), 
                        new Point(xPoint + this.errorWidth, yPointMin));
                }
            }

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return drawingVisual;
        }

        private void mouseFocusDraw(BarElement element, double xPoint, double yPointCenter, double drawHeight, double drawWidth) {

            var mouseX = this.barChartUI.CurrentMousePoint.X;
            var mouseY = this.barChartUI.CurrentMousePoint.Y;
            if (mouseY >= drawHeight - yPointCenter && mouseY <= drawHeight - this.barChartUI.BottomMargin &&
                mouseX >= xPoint - this.barWidth * 0.5 && mouseX <= xPoint + this.barWidth * 0.5) {

                var focusPen = new Pen(Brushes.Blue, this.errorLineLength * 2.0); // Set Graph Pen

                this.drawingContext.DrawLine(focusPen, 
                    new Point(xPoint - this.barWidth * 0.5, yPointCenter), 
                    new Point(xPoint - this.barWidth * 0.5, this.barChartUI.BottomMargin));
                this.drawingContext.DrawLine(focusPen, 
                    new Point(xPoint + this.barWidth * 0.5, yPointCenter), 
                    new Point(xPoint + this.barWidth * 0.5, this.barChartUI.BottomMargin));
                this.drawingContext.DrawLine(focusPen, 
                    new Point(xPoint - this.barWidth * 0.5, yPointCenter),
                    new Point(xPoint + this.barWidth * 0.5, yPointCenter));

                var valueString = element.Value >= 1 ? Math.Round(element.Value, 0).ToString() : Math.Round(element.Value, 5).ToString();

                this.formattedText = new FormattedText(element.Legend + "; Value " + valueString,
                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibri"), 13, Brushes.Black);

                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region

                this.formattedText.TextAlignment = TextAlignment.Right;
                this.drawingContext.DrawText(
                    this.formattedText, new Point(drawWidth - this.barChartUI.RightMargin - 5, this.barChartUI.TopMargin));

                this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.barChartUI.LeftMargin, this.barChartUI.BottomMargin, drawWidth - this.barChartUI.LeftMargin - this.barChartUI.RightMargin, drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin)));
            }
        }

        private void mouseFocusDraw(BarElement element, double xPoint, double yPoint25, double yPoint75, double drawHeight, double drawWidth) {

            var mouseX = this.barChartUI.CurrentMousePoint.X;
            var mouseY = this.barChartUI.CurrentMousePoint.Y;
            if (mouseY >= drawHeight - yPoint75 && mouseY <= drawHeight - yPoint25 &&
                mouseX >= xPoint - this.barWidth * 0.5 && mouseX <= xPoint + this.barWidth * 0.5) {

                var focusPen = new Pen(Brushes.Blue, this.errorLineLength * 2.0); // Set Graph Pen

                this.drawingContext.DrawLine(focusPen,
                    new Point(xPoint - this.barWidth * 0.5, yPoint25),
                    new Point(xPoint - this.barWidth * 0.5, yPoint75));
                this.drawingContext.DrawLine(focusPen,
                    new Point(xPoint + this.barWidth * 0.5, yPoint25),
                    new Point(xPoint + this.barWidth * 0.5, yPoint75));
                this.drawingContext.DrawLine(focusPen,
                    new Point(xPoint - this.barWidth * 0.5, yPoint25),
                    new Point(xPoint + this.barWidth * 0.5, yPoint25));
                this.drawingContext.DrawLine(focusPen,
                   new Point(xPoint - this.barWidth * 0.5, yPoint75),
                   new Point(xPoint + this.barWidth * 0.5, yPoint75));

                var valueString = element.Median >= 1 ? Math.Round(element.Median, 0).ToString() : Math.Round(element.Median, 5).ToString();

                this.formattedText = new FormattedText(element.Legend + "; Median " + valueString,
                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibri"), 13, Brushes.Black);

                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region

                this.formattedText.TextAlignment = TextAlignment.Right;
                this.drawingContext.DrawText(
                    this.formattedText, new Point(drawWidth - this.barChartUI.RightMargin - 5, this.barChartUI.TopMargin));

                this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.barChartUI.LeftMargin, this.barChartUI.BottomMargin, drawWidth - this.barChartUI.LeftMargin - this.barChartUI.RightMargin, drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin)));
            }
        }

        private DrawingVisual drawNullBarChart(DrawingVisual drawingVisual, double drawWidth, double drawHeight)
        {
            drawingSetting(drawWidth, drawHeight, 2);
            // Calculate Packet Size
            this.yPacket = (drawHeight - this.barChartUI.TopMargin - this.barChartUI.BottomMargin - this.barChartUI.TopMarginForLabel) / 100;

            // Draw Graph Title, Y scale, X scale
            drawGraphTitle("Null");
            drawCaptionOnAxis(drawWidth, drawHeight, 0, 100, "Sample name", "Intensity");
            drawScaleOnYAxis(0, 100, drawWidth, drawHeight); // Draw Y-Axis Scale
            drawScaleOnXAxis(drawWidth, drawHeight, new List<BarElement>(){ new BarElement(){ Legend = "No. 1" }, new BarElement(){ Legend = "No. 2" } }, "Sample name");

            // Close DrawingContext
            this.drawingContext.Close();

            return drawingVisual;
        }


        private void drawingSetting(double drawWidth, double drawHeight, int elementNum)
        {
            this.elementNumber = elementNum;
            this.barMargin = this.barChartUI.BarMargin;
            this.drawSpace = drawWidth - this.barChartUI.RightMargin - this.barChartUI.LeftMargin;
            this.barSpace = this.drawSpace / this.elementNumber;
            this.barWidth = this.barSpace - this.barMargin;
            this.errorWidth = this.barWidth * 0.25;
            this.errorLineLength = this.errorWidth * 0.5; if (this.errorLineLength > 2) this.errorLineLength = 2.0;
            if (this.barWidth < 0) return;
        }


        private void drawScaleOnXAxis(double drawWidth, double drawHeight, List<BarElement> DisplayedBarElements, string xAxisTitle)
        {
            if (xAxisTitle == string.Empty) return;
            for (int i = 0; i < this.elementNumber; i++)
            {
                var barCenter = this.barSpace * 0.5 + i * this.barSpace + this.barChartUI.LeftMargin;
                this.drawingContext.DrawLine(this.graphAxis, new Point(barCenter, drawHeight - this.barChartUI.BottomMargin), new Point(barCenter, drawHeight - this.barChartUI.BottomMargin + this.longScaleSize));

                this.formattedText = new FormattedText(DisplayedBarElements[i].Legend, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                if (this.formattedText.Width > this.barSpace)
                {
                    var limitLentgh = (int)(this.barSpace * (double)DisplayedBarElements[i].Legend.Length / this.formattedText.Width);
                    if (limitLentgh < 5) break;
                    var label = string.Empty;
                    for (int j = 0; j < limitLentgh; j++)
                    {
                        if (DisplayedBarElements[i].Legend.Length - 1 < j) break;
                        label += DisplayedBarElements[i].Legend[j];
                    }
                    this.formattedText = new FormattedText(label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                this.formattedText.TextAlignment = TextAlignment.Center;
                this.drawingContext.DrawText(formattedText, new Point(barCenter, drawHeight - this.barChartUI.BottomMargin + this.longScaleSize));
            }
        }

        private void drawBackground(double drawWidth, double drawHeight)
        {
            // Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.barChartUI.LeftMargin, this.barChartUI.TopMargin), new Size(drawWidth - this.barChartUI.LeftMargin - this.barChartUI.RightMargin, drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.barChartUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.barChartUI.BottomMargin), new Point(drawWidth - this.barChartUI.RightMargin, drawHeight - this.barChartUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.barChartUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.barChartUI.BottomMargin), new Point(this.barChartUI.LeftMargin - this.axisFromGraphArea, this.barChartUI.TopMargin));
            #endregion
        }

        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), 
                FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black); // 30 > temp, 15 > default
            this.formattedText.TextAlignment = TextAlignment.Left;
            //this.drawingContext.DrawText(formattedText, new Point(this.barChartUI.LeftMargin, this.barChartUI.TopMargin - 17));
            this.drawingContext.DrawText(formattedText, new Point(15, -2));
        }

        private void drawCaptionOnAxis(double drawWidth, double drawHeight, double yAxisMinValue, double yAxisMaxValue, string xAxisTitle, string yAxisTitle)
        {
            if (xAxisTitle != string.Empty) {
                // Set Caption "Min." to X-Axis            
                this.formattedText = new FormattedText(xAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                this.formattedText.TextAlignment = TextAlignment.Center;
                this.formattedText.SetFontStyle(FontStyles.Italic);
                this.drawingContext.DrawText(formattedText, new Point(this.barChartUI.LeftMargin + 0.5 * (drawWidth - this.barChartUI.LeftMargin - this.barChartUI.RightMargin), drawHeight - 20));
            }

            if (yAxisTitle == string.Empty) return;
            if (yAxisMaxValue - yAxisMinValue <= 0) return;
            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.barChartUI.TopMargin + 0.5 * (drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin)));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            int figure = -1;
            if (yAxisMinValue >= 0)
            {
                if (yAxisMaxValue < 1)
                    figure = (int)toRoundUp(Math.Log10(yAxisMaxValue), 0);
                else
                    figure = (int)toRoundDown(Math.Log10(yAxisMaxValue), 0);
            }
            else
            {
                figure = 0;
            }

            if (figure > 2)
            {
                formattedText = new FormattedText(yAxisTitle + " (1e+" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            else if (figure < 0)
            {
                formattedText = new FormattedText(yAxisTitle + " (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            else
            {
                formattedText = new FormattedText(yAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
           
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, -7));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.barChartUI.TopMargin + 0.5 * (drawHeight - this.barChartUI.BottomMargin - this.barChartUI.TopMargin))));
        }

        private void drawScaleOnYAxis(double yAxisMinValue, double yAxisMaxValue, double drawWidth, double drawHeight)
        {
            string yString = ""; // String for Y-Scale Value
            int foldChange = -1;
            double yscale_max;
            double yscale_min;

            yscale_max = yAxisMaxValue; // Absolute Abundunce
            yscale_min = yAxisMinValue; // Absolute Abundunce

            if (yscale_max == yscale_min) yscale_max += 0.9;

            // Check Figure of Displayed Max Intensity
            if (yscale_max < 1)
            {
                foldChange = (int)toRoundUp(Math.Log10(yscale_max), 0);
            }
            else
            {
                foldChange = (int)toRoundDown(Math.Log10(yscale_max), 0);
            }

            double yspacket = (float)(((double)(drawHeight - this.barChartUI.TopMargin - this.barChartUI.BottomMargin - this.barChartUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.barChartUI.BottomMargin - 1 * (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.barChartUI.BottomMargin) continue;
                if (yPixelValue < this.barChartUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 2 || foldChange < -0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else
                    {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f1");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.barChartUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.barChartUI.LeftMargin - this.axisFromGraphArea, yPixelValue));

                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.barChartUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.barChartUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.barChartUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }
            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (max < min) max = min + 0.9;
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.barChartUI.TopMargin - this.barChartUI.BottomMargin);
            int yStart, yEnd;
            double yScaleHeight, totalPixelWidth;

            do
            {
                yScale *= 2;

                yStart = (int)(min / yScale) - 1;
                yEnd = (int)(max / yScale) + 1;

                formattedText = new FormattedText(yScale.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);

                yScaleHeight = formattedText.Height * 0.5;
                totalPixelWidth = yScaleHeight * (yEnd - yStart + 1);

            }
            while (totalPixelWidth > (double)yAxisPixelRange);

            this.yMajorScale = (decimal)yScale;
            this.yMinorScale = (decimal)((double)this.yMajorScale * 0.25);
        }

        private double toRoundUp(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Ceiling(dValue * dCoef) / dCoef :
                                System.Math.Floor(dValue * dCoef) / dCoef;
        }

        private double toRoundDown(double dValue, int iDigits)
        {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                                System.Math.Ceiling(dValue * dCoef) / dCoef;
        }

        protected static SolidColorBrush combineAlphaAndColor(double opacity, SolidColorBrush baseBrush)
        {
            Color color = baseBrush.Color;
            SolidColorBrush returnSolidColorBrush;

            // Deal with )pacity
            if (opacity > 1.0)
                opacity = 1.0;

            if (opacity < 0.0)
                opacity = 0.0;

            // Get the Hex value of the Alpha Chanel (Opacity)
            byte a = (byte)(Convert.ToInt32(255 * opacity));

            try
            {
                byte r = color.R;
                byte g = color.G;
                byte b = color.B;

                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            catch
            {
                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            return returnSolidColorBrush;
        }

        public void ResetGraphDisplayRange()
        {
            if (this.barChartBean == null || this.barChartBean.BarElements == null || this.barChartBean.BarElements.Count == 0) return;
            this.barChartBean.DisplayElementRearrangement(0, this.barChartBean.BarElements.Count - 1);
          
            this.BarChartDraw();
        }

        public void GraphZoom()
        {
            // Zoom X-Coordinate        

            var beginClickPoint = this.barChartUI.RightButtonStartClickPoint.X;
            var endClickPoint = this.barChartUI.RightButtonEndClickPoint.X;
            var currentBeginID = this.barChartBean.DisplayedBeginID;
            var currentEndID = this.barChartBean.DisplayedEndID;
            var newBeginID = currentBeginID;
            var newEndID = currentEndID; 

            if (beginClickPoint > endClickPoint) {
                if (beginClickPoint > this.barChartUI.LeftMargin) {
                    if (beginClickPoint <= this.ActualWidth - this.barChartUI.RightMargin) {

                        var endPercent = (this.ActualWidth - this.barChartUI.RightMargin - beginClickPoint) / (this.ActualWidth - this.barChartUI.RightMargin - this.barChartUI.LeftMargin);
                        newEndID = currentEndID - (int)(this.elementNumber * endPercent);
                    }
                    if (endClickPoint >= this.barChartUI.LeftMargin) {

                        var beginPercent = (endClickPoint - this.barChartUI.LeftMargin) / (this.ActualWidth - this.barChartUI.RightMargin - this.barChartUI.LeftMargin);
                        newBeginID = currentBeginID + (int)(this.elementNumber * beginPercent);
                    }
                }

            }
            else {
                
                if (endClickPoint > this.barChartUI.LeftMargin) {

                    if (endClickPoint <= this.ActualWidth - this.barChartUI.RightMargin) {
                        var endPercent = (this.ActualWidth - this.barChartUI.RightMargin - endClickPoint) / (this.ActualWidth - this.barChartUI.RightMargin - this.barChartUI.LeftMargin);
                        newEndID = currentEndID - (int)(this.elementNumber * endPercent);
                    }
                    if (beginClickPoint >= this.barChartUI.LeftMargin) {
                        var beginPercent = (beginClickPoint - this.barChartUI.LeftMargin) / (this.ActualWidth - this.barChartUI.RightMargin - this.barChartUI.LeftMargin);
                        newBeginID = currentBeginID + (int)(this.elementNumber * beginPercent);
                    }
                }
            }

            this.barChartBean.DisplayElementRearrangement(newBeginID, newEndID);
            this.BarChartDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, 
                new Rect(new Point(this.barChartUI.RightButtonStartClickPoint.X, this.barChartUI.TopMargin), 
                    new Point(this.barChartUI.RightButtonEndClickPoint.X, this.ActualHeight - this.barChartUI.BottomMargin)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        #region // Required Methods for VisualCollection Object
        protected override int VisualChildrenCount
        {
            get { return visualCollection.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= visualCollection.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return visualCollection[index];
        }
        #endregion // Required Methods for VisualCollection Object
    }
}
