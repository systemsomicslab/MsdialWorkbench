using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class ChromatogramXicFE : FrameworkElement
    {
        //ViewModel
        private ChromatogramXicViewModel chromatogramXicViewModel;

        //UI
        private ChromatogramXicUI chromatogramXicUI;

        //Visual property
        private VisualCollection visualCollection;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;

        // Scale
        private double longScaleSize = 5; // Scale Size (Long)
        private double shortScaleSize = 2; // Scale Size (Short)
        private double axisFromGraphArea = 1; // Location of Axis from Graph Area
        private double graphLinewidth = 1; // Graph Line Width
        private double labelYDistance = 14; // Label Distance From Peak Top
        private double edgeBoxSize = 5;

        // Constant for Graph Scale
        private decimal xMajorScale = -1; // X-Axis Major Scale (min.)
        private decimal xMinorScale = -1; // X-Axis Minor Scale (min.)
        private decimal yMajorScale = -1; // Y-Axis Major Scale (Intensity)
        private decimal yMinorScale = -1; // Y-Axis Minor Scale (Intensity)

        // Graph Color & Font Settings
        private FormattedText formattedText; // FormatText for Scale    
        private Brush graphBackGround = Brushes.WhiteSmoke; // Graph Background
        private Pen graphBorder = new Pen(Brushes.LightGray, 1.0); // Graph Border
        private Pen graphAxis = new Pen(Brushes.Black, 0.5);
        private Pen areaDefinitionLinePen = new Pen(Brushes.Red, 1.0);
        private Pen areaDefinitionOutLinePen = new Pen(Brushes.Pink, 0.7);

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  

        // Drawing Coordinates
        private double xs, ys, xt, yt, xe, ye;

        // Drawing Packet
        private double xPacket;
        private double yPacket;

        // Coefficient of mrmprobs
        private double[] oneTransitionCoefficient = new double[] { 18.98513, -20.21246, -4.925962 };
        private double[] multiTransitionCoefficientWithRatio = new double[] { 16.00116, -7.522224, -5.4539, -1.02464, -1.767802, -5.41667 };
        private double[] multiTransitionCoefficientWithoutRatio = new double[] { 17.23965, -9.246801, -5.507964, -2.293532, -5.387508 };

        public ChromatogramXicFE(ChromatogramXicViewModel chromatogramXicViewModel, ChromatogramXicUI chromatogramXicUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.chromatogramXicViewModel = chromatogramXicViewModel;
            this.chromatogramXicUI = chromatogramXicUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void ChromatogramDraw()
        {
            this.visualCollection.Clear();
            this.drawingVisual = chromatogramDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual chromatogramDrawingVisual(double drawWidth, double drawHeight)
        {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.chromatogramXicUI.LeftMargin + this.chromatogramXicUI.RightMargin || drawHeight < this.chromatogramXicUI.BottomMargin + this.chromatogramXicUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;
            Pen graphPenPeakEdge;

            //Bean
            ChromatogramBean chromatogramBean;
            PeakAreaBean peakAreaBean;

            // Graph Line and Area Draw
            PathFigure pathFigure;
            PathFigure areaPathFigure;
            PathFigure areaTriangleFigure;
            PathGeometry pathGeometry;
            PathGeometry areaPathGeometry;
            PathGeometry areaTriangleGeometry;
            CombinedGeometry combinedGeometry;

            //data point
            int scanNumber;
            float retentionTime;
            float intensity;
            float mzValue;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region 
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.chromatogramXicUI.LeftMargin, this.chromatogramXicUI.TopMargin), new Size(drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin, drawHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramXicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramXicUI.BottomMargin), new Point(drawWidth - this.chromatogramXicUI.RightMargin, drawHeight - this.chromatogramXicUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramXicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramXicUI.BottomMargin), new Point(this.chromatogramXicUI.LeftMargin - this.axisFromGraphArea, this.chromatogramXicUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramXicViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin) / 100;
                yPacket = (drawHeight - this.chromatogramXicUI.TopMargin - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("");
                drawCaptionOnAxis(drawWidth, drawHeight, ChromatogramIntensityMode.Relative, 0, 100);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight, ChromatogramIntensityMode.Relative, 0, 100); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin) / (double)(this.chromatogramXicViewModel.DisplayRangeRtMax - this.chromatogramXicViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.chromatogramXicUI.TopMargin - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMarginForLabel) / (double)(this.chromatogramXicViewModel.DisplayRangeIntensityMax - this.chromatogramXicViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.chromatogramXicViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, this.chromatogramXicViewModel.IntensityMode, (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramXicViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.chromatogramXicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramXicViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.chromatogramXicViewModel.IntensityMode, this.chromatogramXicViewModel.MinIntensity, this.chromatogramXicViewModel.MaxIntensity);                              
            drawScaleOnXAxis((float)this.chromatogramXicViewModel.DisplayRangeRtMin, (float)this.chromatogramXicViewModel.DisplayRangeRtMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.chromatogramXicUI.LeftMargin, this.chromatogramXicUI.BottomMargin, drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin, drawHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMargin)));

            // 5. Reference chromatogram
            #region
            chromatogramBean = this.chromatogramXicViewModel.ChromatogramBean;

            // 5-1. Initialize Graph Plot Start
            #region
            pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine 
            areaPathFigure = new PathFigure(); // PathFigure for GraphLine 
            graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
            graphPenPeakEdge = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness * 1.5); // Set Graph Pen
            graphPenPeakEdge.Freeze();
            graphBrush.Freeze();
            graphPen.Freeze();

            var flagLeft = true;
            var flagRight = true;
            var flagFill = false;
            #endregion

            // 5-2. Draw datapoints
            #region
            if (this.chromatogramXicViewModel.FillPeakArea) {

                var minRtDiff = double.MaxValue;
                var minRtId = -1;

                for (int i = 0; i < chromatogramBean.ChromatogramDataPointCollection.Count; i++) {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[i][0];
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[i][1];
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[i][3];
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[i][2];
                    if (retentionTime < this.chromatogramXicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramXicUI.LeftMargin + (retentionTime - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramXicUI.BottomMargin + (intensity - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });

                    if (flagFill) {
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                    }
                    if (flagLeft && retentionTime >= this.chromatogramXicViewModel.TargetLeftRt) {
                        areaPathFigure.StartPoint = new Point(this.xs, this.chromatogramXicUI.BottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket); // PathFigure for GraphLine 
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                        flagFill = true; flagLeft = false;
                    }
                    else if (flagRight && retentionTime >= this.chromatogramXicViewModel.TargetRightRt) {
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.chromatogramXicUI.BottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket) }); // PathFigure for GraphLine 
                        flagFill = false; flagRight = false;
                    }

                    var rtDiff = Math.Abs(retentionTime - this.chromatogramXicViewModel.TargetRt);
                    if (rtDiff < minRtDiff) {
                        minRtDiff = rtDiff;
                        minRtId = i;
                    }
                    if (i == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramXicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
                }

                if (minRtId >= 0 && minRtDiff < 0.001) {
                    var topRt = (float)chromatogramBean.ChromatogramDataPointCollection[minRtId][1];
                    var topIntensity = (float)chromatogramBean.ChromatogramDataPointCollection[minRtId][3];
                    var topX = this.chromatogramXicUI.LeftMargin + (topRt - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    var topY = this.chromatogramXicUI.BottomMargin + (topIntensity - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.5), new Point(topX, topY),
                           new Point(topX, this.chromatogramXicUI.BottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                }

                areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, 0) }); // PathFigure for GraphLine 
                areaPathFigure.Freeze();
                areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
                areaPathGeometry.Freeze();

                this.drawingContext.DrawGeometry(graphBrush, graphPenPeakEdge, areaPathGeometry);
            }
            else {
                for (int i = 0; i < chromatogramBean.ChromatogramDataPointCollection.Count; i++) {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[i][0];
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[i][1];
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[i][3];
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[i][2];
                    if (retentionTime < this.chromatogramXicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramXicUI.LeftMargin + (retentionTime - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramXicUI.BottomMargin + (intensity - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });

                    if (Math.Abs(retentionTime - this.chromatogramXicViewModel.TargetRt) < 0.0001) this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xs, this.ys), new Point(this.xs, this.chromatogramXicUI.BottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    if (i == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramXicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
                }
            }
            #endregion

            // 5-3. Close Graph Path (When Loop Finish or Display range exceeded)
            #region
            pathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, 0.0) });
            pathFigure.Freeze();
            pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
            pathGeometry.Freeze();
            #endregion

            this.drawingContext.DrawGeometry(null, graphPen, pathGeometry); // Draw Chromatogram Graph Line  
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

       
        private void drawGraphTitle(string graphTitle)
        {
            if (this.chromatogramXicViewModel == null) return;

            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramXicUI.LeftMargin, this.chromatogramXicUI.TopMargin - 17));
        }

        private void drawScaleOnXAxis(float xAxisMinValue, float xAxisMaxValue, double drawWidth, double drawHeight)
        {
            getXaxisScaleInterval((double)xAxisMinValue, (double)xAxisMaxValue, drawWidth);
            int xStart = (int)(xAxisMinValue / (double)this.xMinorScale) - 1;
            int xEnd = (int)(xAxisMaxValue / (double)this.xMinorScale) + 1;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++)
            {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = this.chromatogramXicUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.chromatogramXicUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.chromatogramXicUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramXicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramXicUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.chromatogramXicUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramXicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramXicUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max == min) max += 0.9;
            if (min > max) {
                var temp = min;
                min = max;
                max = temp;
            }
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin);
            int xStart, xEnd;
            double xScaleWidth, totalPixelWidth;

            do
            {
                xScale *= 2;

                xStart = (int)(min / xScale) - 1;
                xEnd = (int)(max / xScale) + 1;

                if (xScale < 1)
                    formattedText = new FormattedText(xScale.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                else
                    formattedText = new FormattedText(xScale.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);

                xScaleWidth = formattedText.Width;
                totalPixelWidth = xScaleWidth * (xEnd - xStart + 1);

            }
            while (totalPixelWidth > (double)xAxisPixelRange);

            this.xMajorScale = (decimal)xScale;
            this.xMinorScale = (decimal)((double)this.xMajorScale * 0.25);
        }

        private void drawScaleOnYAxis(float yAxisMinValue, float yAxisMaxValue, double drawWidth, double drawHeight, ChromatogramIntensityMode chromatogramIntensityMode, float lowestIntensity, float highestIntensity)
        {
            string yString = ""; // String for Y-Scale Value
            int foldChange = -1;
            double yscale_max;
            double yscale_min;

            if (chromatogramIntensityMode == ChromatogramIntensityMode.Absolute)
            {
                yscale_max = yAxisMaxValue; // Absolute Abundunce
                yscale_min = yAxisMinValue; // Absolute Abundunce
            }
            else
            {
                yscale_max = (double)(((yAxisMaxValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
                yscale_min = (double)(((yAxisMinValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
            }
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

            double yspacket = (float)(((double)(drawHeight - this.chromatogramXicUI.TopMargin - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.chromatogramXicUI.BottomMargin - (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.chromatogramXicUI.BottomMargin) continue;
                if (yPixelValue < this.chromatogramXicUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange <= 0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else
                    {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramXicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramXicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramXicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramXicUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramXicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }
            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (drawHeight < 120)
                return;
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.chromatogramXicUI.TopMargin - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMarginForLabel);
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

        private void drawCaptionOnAxis(double drawWidth, double drawHeight, ChromatogramIntensityMode chromatogramIntensityMode, float yAxisMinValue, float yAxisMaxValue)
        {
            // Set Caption "Min." to X-Axis  
            if (this.chromatogramXicViewModel == null || this.chromatogramXicViewModel.ChromatogramBean == null) {
                this.formattedText = new FormattedText("Retention time [min]", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            else {
                var chrom = this.chromatogramXicViewModel.ChromatogramBean;
                if (this.chromatogramXicViewModel.XAxisTitle != null && this.chromatogramXicViewModel.XAxisTitle != string.Empty && this.chromatogramXicViewModel.XAxisTitle != "")
                    this.formattedText = new FormattedText(this.chromatogramXicViewModel.XAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                else
                    this.formattedText = new FormattedText("Retention time [min]", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramXicUI.LeftMargin + 0.5 * (drawWidth - this.chromatogramXicUI.LeftMargin - this.chromatogramXicUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.chromatogramXicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMargin)));
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

            if (chromatogramIntensityMode == ChromatogramIntensityMode.Absolute)
            {
                if (figure > 3)
                {
                    formattedText = new FormattedText("Ion abundance (1e+" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else if (figure < -1)
                {
                    formattedText = new FormattedText("Ion abundance (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else
                {
                    formattedText = new FormattedText("Ion abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
            }
            else
            {
                if (figure > 1) {
                    formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else {
                    formattedText = new FormattedText("Relative abundance (1e" + (figure - 2).ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                //formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.chromatogramXicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.TopMargin))));
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
            this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.MinIntensity;
            this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.MaxIntensity;
            this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.MinRt;
            this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.MaxRt;

            ChromatogramDraw();
        }

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.chromatogramXicUI.RightButtonStartClickPoint.X - this.chromatogramXicUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.chromatogramXicUI.RightButtonStartClickPoint.Y - this.chromatogramXicUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.chromatogramXicUI.RightButtonStartClickPoint.X - this.chromatogramXicUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.chromatogramXicUI.RightButtonStartClickPoint.X > this.chromatogramXicUI.RightButtonEndClickPoint.X)
            {
                if (this.chromatogramXicUI.RightButtonStartClickPoint.X > this.chromatogramXicUI.LeftMargin)
                {
                    if (this.chromatogramXicUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.chromatogramXicUI.RightMargin)
                    {
                        this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.DisplayRangeRtMin + (float)((this.chromatogramXicUI.RightButtonStartClickPoint.X - this.chromatogramXicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramXicUI.RightButtonEndClickPoint.X >= this.chromatogramXicUI.LeftMargin)
                    {
                        this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.DisplayRangeRtMin + (float)((this.chromatogramXicUI.RightButtonEndClickPoint.X - this.chromatogramXicUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.chromatogramXicUI.RightButtonEndClickPoint.X > this.chromatogramXicUI.LeftMargin)
                {
                    if (this.chromatogramXicUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.chromatogramXicUI.RightMargin)
                    {
                        this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.DisplayRangeRtMin + (float)((this.chromatogramXicUI.RightButtonEndClickPoint.X - this.chromatogramXicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramXicUI.RightButtonStartClickPoint.X >= this.chromatogramXicUI.LeftMargin)
                    {
                        this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.DisplayRangeRtMin + (float)((this.chromatogramXicUI.RightButtonStartClickPoint.X - this.chromatogramXicUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.chromatogramXicUI.RightButtonStartClickPoint.Y > this.chromatogramXicUI.RightButtonEndClickPoint.Y)
            {
                this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramXicUI.BottomMargin - this.chromatogramXicUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.chromatogramXicUI.LeftButtonStartClickPoint.X == -1 || this.chromatogramXicUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.chromatogramXicViewModel.DisplayRangeRtMin == null || this.chromatogramXicViewModel.DisplayRangeRtMax == null)
            {
                this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.MinRt;
                this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.MaxRt;
            }

            if (this.chromatogramXicViewModel.DisplayRangeIntensityMin == null || this.chromatogramXicViewModel.DisplayRangeIntensityMax == null)
            {
                this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.MinIntensity;
                this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.MaxIntensity;
            }

            float durationX = (float)this.chromatogramXicViewModel.DisplayRangeRtMax - (float)this.chromatogramXicViewModel.DisplayRangeRtMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.chromatogramXicUI.LeftButtonStartClickPoint.X > this.chromatogramXicUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.chromatogramXicUI.LeftButtonStartClickPoint.X - this.chromatogramXicUI.LeftButtonEndClickPoint.X;

                this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.chromatogramXicViewModel.DisplayRangeRtMax > this.chromatogramXicViewModel.MaxRt)
                {
                    this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.MaxRt;
                    this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.MaxRt - durationX;
                }
            }
            else
            {
                distanceX = this.chromatogramXicUI.LeftButtonEndClickPoint.X - this.chromatogramXicUI.LeftButtonStartClickPoint.X;

                this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.chromatogramXicViewModel.DisplayRangeRtMin < this.chromatogramXicViewModel.MinRt)
                {
                    this.chromatogramXicViewModel.DisplayRangeRtMin = this.chromatogramXicViewModel.MinRt;
                    this.chromatogramXicViewModel.DisplayRangeRtMax = this.chromatogramXicViewModel.MinRt + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.chromatogramXicViewModel.DisplayRangeIntensityMax - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin;
            if (this.chromatogramXicUI.LeftButtonStartClickPoint.Y < this.chromatogramXicUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.chromatogramXicUI.LeftButtonEndClickPoint.Y - this.chromatogramXicUI.LeftButtonStartClickPoint.Y;

                this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.chromatogramXicViewModel.DisplayRangeIntensityMax > this.chromatogramXicViewModel.MaxIntensity)
                {
                    this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.MaxIntensity;
                    this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.chromatogramXicUI.LeftButtonStartClickPoint.Y - this.chromatogramXicUI.LeftButtonEndClickPoint.Y;

                this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.chromatogramXicViewModel.DisplayRangeIntensityMin < this.chromatogramXicViewModel.MinIntensity)
                {
                    this.chromatogramXicViewModel.DisplayRangeIntensityMin = this.chromatogramXicViewModel.MinIntensity;
                    this.chromatogramXicViewModel.DisplayRangeIntensityMax = this.chromatogramXicViewModel.MinIntensity + durationY;
                }
            }
            ChromatogramDraw();
        }

        public void PeakLeftEdgeClickCheck()
        {
            //int targetTransitionIndex = this.chromatogramXicViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramXicViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float leftEdgeRt = this.chromatogramXicViewModel.ChromatogramBean.PeakAreaBeanCollection[selectedPeakId].RtAtLeftPeakEdge;
            //int leftEdgeInt = this.chromatogramXicViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtLeftPeakEdge;

            Point leftEdgePoint = new Point(this.chromatogramXicUI.LeftMargin + (leftEdgeRt - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket, this.chromatogramXicUI.TopMargin + this.chromatogramXicUI.TopMarginForLabel);

            if (Math.Abs(leftEdgePoint.X - this.chromatogramXicUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(leftEdgePoint.Y - this.chromatogramXicUI.LeftButtonStartClickPoint.Y) < 5) this.chromatogramXicUI.LeftMouseButtonLeftEdgeCapture = true;
            else this.chromatogramXicUI.LeftMouseButtonLeftEdgeCapture = false;
        }

        public void PeakRightEdgeClickCheck()
        {
            //int targetTransitionIndex = this.chromatogramXicViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramXicViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float rightEdgeRt = this.chromatogramXicViewModel.ChromatogramBean.PeakAreaBeanCollection[selectedPeakId].RtAtRightPeakEdge;
            //int rightEdgeInt = this.chromatogramXicViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtRightPeakEdge;

            Point rightEdgePoint = new Point(this.chromatogramXicUI.LeftMargin + (rightEdgeRt - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket, this.chromatogramXicUI.TopMargin + this.chromatogramXicUI.TopMarginForLabel);

            if (Math.Abs(rightEdgePoint.X - this.chromatogramXicUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(rightEdgePoint.Y - this.chromatogramXicUI.LeftButtonStartClickPoint.Y) < 5) this.chromatogramXicUI.LeftMouseButtonRightEdgeCapture = true;
            else this.chromatogramXicUI.LeftMouseButtonRightEdgeCapture = false;
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramXicUI.RightButtonStartClickPoint.X, this.chromatogramXicUI.RightButtonStartClickPoint.Y), new Point(this.chromatogramXicUI.RightButtonEndClickPoint.X, this.chromatogramXicUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void NewPeakGenerateRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramXicUI.RightButtonStartClickPoint.X, this.ActualHeight - this.chromatogramXicUI.BottomMargin), new Point(this.chromatogramXicUI.RightButtonEndClickPoint.X, this.chromatogramXicUI.TopMargin + this.chromatogramXicUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void PeakEdgeEditRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramXicUI.LeftButtonStartClickPoint.X, this.ActualHeight - this.chromatogramXicUI.BottomMargin), new Point(this.chromatogramXicUI.LeftButtonEndClickPoint.X, this.chromatogramXicUI.TopMargin + this.chromatogramXicUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        private void setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            List<PeakAreaBean> peakAreaBeanList = peakAreaBeanCollection.ToList();
            peakAreaBeanList = peakAreaBeanList.OrderByDescending(n => n.IntensityAtPeakTop).ToList();

            float maxIntensity = peakAreaBeanList[0].IntensityAtPeakTop;
            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                peakAreaBeanList[i].AmplitudeScoreValue = peakAreaBeanList[i].IntensityAtPeakTop / maxIntensity;
                peakAreaBeanList[i].AmplitudeOrderValue = i + 1;
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.ScanNumberAtPeakTop).ToList();
            for (int i = 0; i < peakAreaBeanList.Count; i++) { peakAreaBeanList[i].PeakID = i; }
            peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>(peakAreaBeanList);
        }

        private void peakDelete(ObservableCollection<PeakAreaBean> peakAreaBeanCollection, int deleteId)
        {
            peakAreaBeanCollection.RemoveAt(deleteId);

            double amplitudeMax = double.MinValue;
            for (int i = 0; i < peakAreaBeanCollection.Count; i++)
                if (peakAreaBeanCollection[i].IntensityAtPeakTop > amplitudeMax) amplitudeMax = peakAreaBeanCollection[i].IntensityAtPeakTop;

            int amplitudeOrder;
            for (int i = 0; i < peakAreaBeanCollection.Count; i++)
            {
                peakAreaBeanCollection[i].PeakID = i;
                peakAreaBeanCollection[i].AmplitudeScoreValue = (float)(Math.Log10(peakAreaBeanCollection[i].IntensityAtPeakTop) / Math.Log10(amplitudeMax));
                amplitudeOrder = 1;
                for (int j = 0; j < peakAreaBeanCollection.Count; j++)
                    if (peakAreaBeanCollection[i].IntensityAtPeakTop < peakAreaBeanCollection[j].IntensityAtPeakTop && i != j) amplitudeOrder++;
                peakAreaBeanCollection[i].AmplitudeOrderValue = amplitudeOrder;
            }
        }


        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.chromatogramXicViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.chromatogramXicViewModel.DisplayRangeRtMin + (float)((mousePoint.X - this.chromatogramXicUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.chromatogramXicUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { scanNumber, retentionTime, mzValue, intensity };

            return peakInformation;
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
