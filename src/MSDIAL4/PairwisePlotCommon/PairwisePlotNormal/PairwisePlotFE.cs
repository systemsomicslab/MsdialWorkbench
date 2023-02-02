using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xaml;

namespace Rfx.Riken.OsakaUniv
{
    public class PairwisePlotFE : FrameworkElement
    {
        private VisualCollection visualCollection;//絵を描くための画用紙みたいなもの
        private DrawingVisual drawingVisual;//絵を描くための筆とかパレットみたいなもの
        private DrawingContext drawingContext;//絵を描く人

        private PairwisePlotUI pairwisePlotUI;
        private PairwisePlotBean pairwisePlotBean;

        //Graph area format
        private double longScaleSize = 5; // Scale Size (Long)
        private double shortScaleSize = 2; // Scale Size (Short)
        private double axisFromGraphArea = 1; // Location of Axis from Graph Area
        //private double graphLinewidth = 1; // Graph Line Width
        private double labelYDistance = 25; // Label Distance From Peak Top

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

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  
        
        // Drawing Coordinates
        //private double xs, ys, xe, ye;
		private double xt, yt;

		// Drawing Packet
		private double xPacket;
        private double yPacket;

        public PairwisePlotFE(PairwisePlotBean pairwisePlotBean, PairwisePlotUI pairwisePlotUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.pairwisePlotBean = pairwisePlotBean;
            this.pairwisePlotUI = pairwisePlotUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void PairwisePlotDraw()
        {
            this.visualCollection.Clear();
            this.drawingVisual = pairwisePlotDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual pairwisePlotDrawingVisual(double drawWidth, double drawHeight)
        {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.pairwisePlotUI.LeftMargin + this.pairwisePlotUI.RightMargin || drawHeight < this.pairwisePlotUI.BottomMargin + this.pairwisePlotUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            FormattedText formattedText;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.pairwisePlotUI.LeftMargin, this.pairwisePlotUI.TopMargin), new Size(drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin, drawHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotUI.BottomMargin), new Point(drawWidth - this.pairwisePlotUI.RightMargin, drawHeight - this.pairwisePlotUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotUI.BottomMargin), new Point(this.pairwisePlotUI.LeftMargin - this.axisFromGraphArea, this.pairwisePlotUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.pairwisePlotBean.XAxisDatapointCollection == null)
            {
                // Calculate Packet Size
                this.xPacket = (drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin) / 100;
                this.yPacket = (drawHeight - this.pairwisePlotUI.TopMargin - this.pairwisePlotUI.BottomMargin) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle(this.pairwisePlotBean.GraphTitle);
                drawCaptionOnAxis(drawWidth, drawHeight);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();
                return this.drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxX - this.pairwisePlotBean.DisplayRangeMinX);
            this.yPacket = (drawHeight - this.pairwisePlotUI.TopMargin - this.pairwisePlotUI.BottomMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxY - this.pairwisePlotBean.DisplayRangeMinY);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.pairwisePlotBean.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight);
            drawScaleOnYAxis((float)this.pairwisePlotBean.DisplayRangeMinY, (float)this.pairwisePlotBean.DisplayRangeMaxY, drawWidth, drawHeight);
            drawScaleOnXAxis((float)this.pairwisePlotBean.DisplayRangeMinX, (float)this.pairwisePlotBean.DisplayRangeMaxX, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.pairwisePlotUI.LeftMargin, this.pairwisePlotUI.BottomMargin, drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin, drawHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.TopMargin)));

            // 5. Draw plot
            #region
            for (int i = 0; i < this.pairwisePlotBean.XAxisDatapointCollection.Count; i++)
            {
                this.xt = this.pairwisePlotUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                if (this.xt < this.pairwisePlotUI.LeftMargin || this.xt > drawWidth - this.pairwisePlotUI.RightMargin) continue;
                if (this.yt < this.pairwisePlotUI.BottomMargin || this.yt > drawHeight - this.pairwisePlotUI.TopMargin) continue;

                this.drawingContext.DrawEllipse(this.pairwisePlotBean.PlotBrushCollection[i], new Pen(Brushes.Black, 0.1), new Point(this.xt, this.yt), this.pairwisePlotUI.PlotSize, this.pairwisePlotUI.PlotSize);

                if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.None)
                {

                }
                else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Label)
                {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText(this.pairwisePlotBean.LabelCollection[i], CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.X)
                {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    if (this.pairwisePlotBean.XAxisDatapointCollection[i] >= 0)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 2).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else if (Math.Abs(this.pairwisePlotBean.XAxisDatapointCollection[i]) >= 0.01)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 4).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else if (Math.Abs(this.pairwisePlotBean.XAxisDatapointCollection[i]) >= 0.0001)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 6).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else
                        formattedText = new FormattedText(this.pairwisePlotBean.XAxisDatapointCollection[i].ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);

                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Y)
                {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    if (this.pairwisePlotBean.YAxisDatapointCollection[i] >= 0)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 2).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else if (Math.Abs(this.pairwisePlotBean.YAxisDatapointCollection[i]) >= 0.01)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 4).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else if (Math.Abs(this.pairwisePlotBean.YAxisDatapointCollection[i]) >= 0.0001)
                        formattedText = new FormattedText(Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 6).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    else
                        formattedText = new FormattedText(this.pairwisePlotBean.YAxisDatapointCollection[i].ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);

                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }

                if (Math.Abs(this.xt - this.pairwisePlotUI.CurrentMousePoint.X) < this.pairwisePlotUI.PlotSize && Math.Abs(this.yt - (drawHeight - this.pairwisePlotUI.CurrentMousePoint.Y)) < this.pairwisePlotUI.PlotSize)
                {
                    this.drawingContext.DrawEllipse(null, new Pen(this.pairwisePlotBean.PlotBrushCollection[i], 1), new Point(this.xt, this.yt), this.pairwisePlotUI.PlotSize + 2, this.pairwisePlotUI.PlotSize + 2);

                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText(this.pairwisePlotBean.LabelCollection[i], CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt - this.labelYDistance));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            
            
            }
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotUI.LeftMargin, this.pairwisePlotUI.TopMargin - 17));
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
                xPixelValue = this.pairwisePlotUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.pairwisePlotUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.pairwisePlotUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.pairwisePlotUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max == min) { max += 0.9; }
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin);
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

        private void drawScaleOnYAxis(float yAxisMinValue, float yAxisMaxValue, double drawWidth, double drawHeight)
        {
            getYaxisScaleInterval((double)yAxisMinValue, (double)yAxisMaxValue, drawHeight);
            int yStart = (int)(yAxisMinValue / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yAxisMaxValue / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            // Set Caption to Y-Axis                                                
            for (int i = yStart; i < yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.pairwisePlotUI.BottomMargin - (yAxisValue - yAxisMinValue) * this.yPacket;
                if (yPixelValue > drawHeight - this.pairwisePlotUI.BottomMargin) continue;
                if (yPixelValue < this.pairwisePlotUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    if (this.yMajorScale < 1)
                        this.formattedText = new FormattedText(yAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(yAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                    this.drawingContext.PushTransform(new RotateTransform(270.0));
                    this.drawingContext.DrawText(formattedText, new Point(drawHeight - yPixelValue, this.pairwisePlotUI.LeftMargin - this.formattedText.Height - this.longScaleSize - this.axisFromGraphArea - 1));
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }

            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (max == min) { max += 0.9; }

            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.pairwisePlotUI.TopMargin - this.pairwisePlotUI.BottomMargin);
            int yStart, yEnd;
            double yScaleWidth, totalPixelWidth;

            do
            {
                yScale *= 2;

                yStart = (int)(min / yScale) - 1;
                yEnd = (int)(max / yScale) + 1;


                if (yScale < 1)
                    formattedText = new FormattedText(yScale.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                else
                    formattedText = new FormattedText(yScale.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);

                yScaleWidth = formattedText.Width;
                totalPixelWidth = yScaleWidth * (yEnd - yStart + 1);
            }
            while (totalPixelWidth > (double)yAxisPixelRange);

            this.yMajorScale = (decimal)yScale;
            this.yMinorScale = (decimal)((double)this.yMajorScale * 0.25);
        }

        private void drawCaptionOnAxis(double drawWidth, double drawHeight)
        {
            // Set Caption "Min." to X-Axis            
            this.formattedText = new FormattedText(this.pairwisePlotBean.XAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotUI.LeftMargin + 0.5 * (drawWidth - this.pairwisePlotUI.LeftMargin - this.pairwisePlotUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.pairwisePlotUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.TopMargin)));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            formattedText = new FormattedText(this.pairwisePlotBean.YAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.pairwisePlotUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.TopMargin))));
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

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.pairwisePlotUI.RightButtonStartClickPoint.X - this.pairwisePlotUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.pairwisePlotUI.RightButtonStartClickPoint.Y - this.pairwisePlotUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.pairwisePlotUI.RightButtonStartClickPoint.X - this.pairwisePlotUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.pairwisePlotUI.RightButtonStartClickPoint.X > this.pairwisePlotUI.RightButtonEndClickPoint.X)
            {
                if (this.pairwisePlotUI.RightButtonStartClickPoint.X > this.pairwisePlotUI.LeftMargin)
                {
                    if (this.pairwisePlotUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.pairwisePlotUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotUI.RightButtonStartClickPoint.X - this.pairwisePlotUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotUI.RightButtonEndClickPoint.X >= this.pairwisePlotUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotUI.RightButtonEndClickPoint.X - this.pairwisePlotUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.pairwisePlotUI.RightButtonEndClickPoint.X > this.pairwisePlotUI.LeftMargin)
                {
                    if (this.pairwisePlotUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.pairwisePlotUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotUI.RightButtonEndClickPoint.X - this.pairwisePlotUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotUI.RightButtonStartClickPoint.X >= this.pairwisePlotUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotUI.RightButtonStartClickPoint.X - this.pairwisePlotUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.pairwisePlotUI.RightButtonStartClickPoint.Y > this.pairwisePlotUI.RightButtonEndClickPoint.Y)
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotUI.BottomMargin - this.pairwisePlotUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.pairwisePlotUI.LeftButtonStartClickPoint.X == -1 || this.pairwisePlotUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.pairwisePlotBean.DisplayRangeMinX == null || this.pairwisePlotBean.DisplayRangeMaxX == null)
            {
                this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.MinX;
                this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.MaxX;
            }

            if (this.pairwisePlotBean.DisplayRangeMinY == null || this.pairwisePlotBean.DisplayRangeMaxY == null)
            {
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.MinY;
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.MaxY;
            }

            float durationX = (float)this.pairwisePlotBean.DisplayRangeMaxX - (float)this.pairwisePlotBean.DisplayRangeMinX;
            double distanceX = 0;

            float newMinX = 0, newMaxX = 0, newMinY = 0, newMaxY = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.pairwisePlotUI.LeftButtonStartClickPoint.X > this.pairwisePlotUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.pairwisePlotUI.LeftButtonStartClickPoint.X - this.pairwisePlotUI.LeftButtonEndClickPoint.X;

                newMinX = this.pairwisePlotUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (newMaxX > this.pairwisePlotBean.MaxX)
                {
                }
                else
                {
                    this.pairwisePlotBean.DisplayRangeMinX = newMinX;
                    this.pairwisePlotBean.DisplayRangeMaxX = newMaxX;
                }
            }
            else
            {
                distanceX = this.pairwisePlotUI.LeftButtonEndClickPoint.X - this.pairwisePlotUI.LeftButtonStartClickPoint.X;

                newMinX = this.pairwisePlotUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (newMinX < this.pairwisePlotBean.MinX)
                {
                }
                else
                {
                    this.pairwisePlotBean.DisplayRangeMinX = newMinX;
                    this.pairwisePlotBean.DisplayRangeMaxX = newMaxX;
                }
            }

            // Y-Direction
            durationY = (float)this.pairwisePlotBean.DisplayRangeMaxY - (float)this.pairwisePlotBean.DisplayRangeMinY;
            if (this.pairwisePlotUI.LeftButtonStartClickPoint.Y < this.pairwisePlotUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.pairwisePlotUI.LeftButtonEndClickPoint.Y - this.pairwisePlotUI.LeftButtonStartClickPoint.Y;

                newMinY = this.pairwisePlotUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (newMaxY > this.pairwisePlotBean.MaxY)
                {
                }
                else
                {
                    this.pairwisePlotBean.DisplayRangeMinY = newMinY;
                    this.pairwisePlotBean.DisplayRangeMaxY = newMaxY;
                }
            }
            else
            {
                distanceY = this.pairwisePlotUI.LeftButtonStartClickPoint.Y - this.pairwisePlotUI.LeftButtonEndClickPoint.Y;

                newMinY = this.pairwisePlotUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (newMinY < this.pairwisePlotBean.MinY)
                {
                }
                else
                {
                    this.pairwisePlotBean.DisplayRangeMinY = newMinY;
                    this.pairwisePlotBean.DisplayRangeMaxY = newMaxY;
                }
            }
            PairwisePlotDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.pairwisePlotUI.RightButtonStartClickPoint.X, this.pairwisePlotUI.RightButtonStartClickPoint.Y), new Point(this.pairwisePlotUI.RightButtonEndClickPoint.X, this.pairwisePlotUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange()
        {
            this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.MinY;
            this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.MaxY;
            this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.MinX;
            this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.MaxX;

            PairwisePlotDraw();
        }

        public Point GetDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.pairwisePlotBean == null) return new Point();
                
            float x_Value, y_Value;

            x_Value = (float)this.pairwisePlotBean.DisplayRangeMinX + (float)((mousePoint.X - this.pairwisePlotUI.LeftMargin) / this.xPacket);
            y_Value = (float)this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - mousePoint.Y - this.pairwisePlotUI.BottomMargin) / this.yPacket);

            return new Point(x_Value, y_Value);
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
