using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramRightRotateFE : FrameworkElement
    {
         //ViewModel
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //UI
        private MassSpectrogramRightRotateUI massSpectrogramRotateUI;

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

        public MassSpectrogramRightRotateFE(MassSpectrogramViewModel massSpectrogramViewModel, MassSpectrogramRightRotateUI massSpectrogramRotateUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramRotateUI = massSpectrogramRotateUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void MassSpectrogramDraw()
        {
            this.visualCollection.Clear();
            this.drawingVisual = MassSpectrogramDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual MassSpectrogramDrawingVisual(double drawWidth, double drawHeight)
        {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.massSpectrogramRotateUI.LeftMargin + this.massSpectrogramRotateUI.RightMargin || drawHeight < this.massSpectrogramRotateUI.BottomMargin + this.massSpectrogramRotateUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;

            //Bean
            MassSpectrogramBean measuredMassSpectrogramBean;

            //data point
            float mzValue;
            float intensity;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.massSpectrogramRotateUI.LeftMargin, this.massSpectrogramRotateUI.TopMargin), new Size(drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin, drawHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramRotateUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramRotateUI.BottomMargin), new Point(drawWidth - this.massSpectrogramRotateUI.RightMargin, drawHeight - this.massSpectrogramRotateUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramRotateUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramRotateUI.BottomMargin), new Point(this.massSpectrogramRotateUI.LeftMargin - this.axisFromGraphArea, this.massSpectrogramRotateUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.massSpectrogramViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin) / 100;
                yPacket = (drawHeight - this.massSpectrogramRotateUI.TopMargin - this.massSpectrogramRotateUI.BottomMargin) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("No Info.");
                drawCaptionOnAxis(drawWidth, drawHeight);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight, 0, 100, MassSpectrogramIntensityMode.Absolute);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeIntensityMax - this.massSpectrogramViewModel.DisplayRangeIntensityMin);
            this.yPacket = (drawHeight - this.massSpectrogramRotateUI.TopMargin - this.massSpectrogramRotateUI.BottomMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeMassMax - this.massSpectrogramViewModel.DisplayRangeMassMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.massSpectrogramViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight);
            drawScaleOnXAxis((float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.massSpectrogramViewModel.MinIntensity, this.massSpectrogramViewModel.MaxIntensity, this.massSpectrogramViewModel.IntensityMode);
            drawScaleOnYAxis((float)this.massSpectrogramViewModel.DisplayRangeMassMin, (float)this.massSpectrogramViewModel.DisplayRangeMassMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramRotateUI.LeftMargin, this.massSpectrogramRotateUI.BottomMargin, drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin, drawHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.TopMargin)));

            // 5. Mass spectrogram Draw
            #region
            measuredMassSpectrogramBean = this.massSpectrogramViewModel.MeasuredMassSpectrogramBean;
            #endregion

            // 5-1. Initialize Graph Plot Start
            #region
            graphBrush = combineAlphaAndColor(0.25, measuredMassSpectrogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(measuredMassSpectrogramBean.DisplayBrush, measuredMassSpectrogramBean.LineTickness); // Set Graph Pen
            graphBrush.Freeze();
            graphPen.Freeze();
            #endregion

            // 5-2. Draw datapoints
            #region
            for (int i = 0; i < measuredMassSpectrogramBean.MassSpectraCollection.Count; i++)
            {
                mzValue = (float)measuredMassSpectrogramBean.MassSpectraCollection[i][0];
                intensity = (float)measuredMassSpectrogramBean.MassSpectraCollection[i][1];

                if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin - 5) continue; // Use Data -5 second beyond

                this.xs = this.massSpectrogramRotateUI.LeftMargin + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket;// Calculate x Plot Coordinate
                this.ys = this.massSpectrogramRotateUI.BottomMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.yPacket;// Calculate y Plot Coordinate

                if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                
                if (i == -1 + measuredMassSpectrogramBean.MassSpectraCollection.Count || mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                this.drawingContext.DrawLine(graphPen, new Point(this.xt, this.yt), new Point(this.massSpectrogramRotateUI.LeftMargin + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket, this.yt));
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
            if (this.massSpectrogramViewModel == null)
                this.formattedText = new FormattedText("Name: ", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            else
                this.formattedText = new FormattedText("Name: " + graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);

            this.formattedText.TextAlignment = TextAlignment.Left;

            this.drawingContext.PushTransform(new TranslateTransform(this.ActualWidth, 0));
            this.drawingContext.PushTransform(new RotateTransform(90.0));

            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramRotateUI.TopMargin, 5));

            this.drawingContext.Pop();
            this.drawingContext.Pop();
        }

        private void drawScaleOnXAxis(float xAxisMinValue, float xAxisMaxValue, double drawWidth, double drawHeight, float lowestIntensity, float highestIntensity, MassSpectrogramIntensityMode intensityMode)
        {
            int foldChange = -1;
            double xscale_max;
            double xscale_min;

            if (intensityMode == MassSpectrogramIntensityMode.Absolute)
            {
                xscale_max = xAxisMaxValue; // Absolute Abundunce
                xscale_min = xAxisMinValue; // Absolute Abundunce
            }
            else
            {
                xscale_max = (double)(((xAxisMaxValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
                xscale_min = (double)(((xAxisMinValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
            }
            if (xscale_max == xscale_min) xscale_max += 0.9;


            // Check Figure of Displayed Max Intensity
            if (xscale_max < 1)
            {
                foldChange = (int)toRoundUp(Math.Log10(xscale_max), 0);
            }
            else
            {
                foldChange = (int)toRoundDown(Math.Log10(xscale_max), 0);
            }

            double xspacket = (float)(((double)(drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin)) / (xscale_max - xscale_min)); // Packet for Y-Scale For Zooming


            getXaxisScaleInterval(xscale_min, xscale_max, drawWidth);
            int xStart = (int)(xAxisMinValue / (double)this.xMinorScale) - 1;
            int xEnd = (int)(xAxisMaxValue / (double)this.xMinorScale) + 1;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++)
            {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = this.massSpectrogramRotateUI.LeftMargin + (xAxisValue - xAxisMinValue) * xspacket;
                if (xPixelValue < this.massSpectrogramRotateUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.massSpectrogramRotateUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, this.massSpectrogramRotateUI.TopMargin), new Point(xPixelValue, this.massSpectrogramRotateUI.TopMargin - this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, this.massSpectrogramRotateUI.TopMargin * 0.3));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, this.massSpectrogramRotateUI.TopMargin), new Point(xPixelValue, this.massSpectrogramRotateUI.TopMargin - this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.massSpectrogramRotateUI.LeftMargin - this.massSpectrogramRotateUI.RightMargin);
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
                yPixelValue = drawHeight - this.massSpectrogramRotateUI.BottomMargin - (yAxisValue - yAxisMinValue) * this.yPacket;
                if (yPixelValue > drawHeight - this.massSpectrogramRotateUI.BottomMargin) continue;
                if (yPixelValue < this.massSpectrogramRotateUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramRotateUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramRotateUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    if (this.yMajorScale < 1)
                        this.formattedText = new FormattedText(yAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(yAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.PushTransform(new TranslateTransform(drawWidth, 0));
                    this.drawingContext.PushTransform(new RotateTransform(90.0));
                    this.drawingContext.DrawText(formattedText, new Point(yPixelValue, drawWidth - this.massSpectrogramRotateUI.TopMargin - this.longScaleSize));
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramRotateUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramRotateUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }

            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.massSpectrogramRotateUI.TopMargin - this.massSpectrogramRotateUI.BottomMargin);
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
            // Set Caption "m/z" to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(0, 0));
            this.drawingContext.PushTransform(new RotateTransform(90.0));

            this.formattedText = new FormattedText("m/z", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;

            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramRotateUI.TopMargin + 0.5 * (drawHeight - this.massSpectrogramRotateUI.TopMargin - this.massSpectrogramRotateUI.BottomMargin), - this.massSpectrogramRotateUI.LeftMargin * 0.7));

            this.drawingContext.Pop();
            this.drawingContext.Pop();
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
            this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
            this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
            this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;

            MassSpectrogramDraw();
        }

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.massSpectrogramRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramRotateUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.massSpectrogramRotateUI.RightButtonStartClickPoint.Y - this.massSpectrogramRotateUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.massSpectrogramRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramRotateUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.massSpectrogramRotateUI.RightButtonStartClickPoint.X > this.massSpectrogramRotateUI.RightButtonEndClickPoint.X)
            {
                if (this.massSpectrogramRotateUI.RightButtonStartClickPoint.X > this.massSpectrogramRotateUI.LeftMargin)
                {
                    if (this.massSpectrogramRotateUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.massSpectrogramRotateUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramRotateUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramRotateUI.RightButtonEndClickPoint.X >= this.massSpectrogramRotateUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramRotateUI.RightButtonEndClickPoint.X - this.massSpectrogramRotateUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.massSpectrogramRotateUI.RightButtonEndClickPoint.X > this.massSpectrogramRotateUI.LeftMargin)
                {
                    if (this.massSpectrogramRotateUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.massSpectrogramRotateUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramRotateUI.RightButtonEndClickPoint.X - this.massSpectrogramRotateUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramRotateUI.RightButtonStartClickPoint.X >= this.massSpectrogramRotateUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramRotateUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.massSpectrogramRotateUI.RightButtonStartClickPoint.Y > this.massSpectrogramRotateUI.RightButtonEndClickPoint.Y)
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramRotateUI.BottomMargin - this.massSpectrogramRotateUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.massSpectrogramRotateUI.LeftButtonStartClickPoint.X == -1 || this.massSpectrogramRotateUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.massSpectrogramViewModel.DisplayRangeMassMin == null || this.massSpectrogramViewModel.DisplayRangeMassMax == null)
            {
                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
            }

            if (this.massSpectrogramViewModel.DisplayRangeIntensityMin == null || this.massSpectrogramViewModel.DisplayRangeIntensityMax == null)
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
            }

            float durationX = (float)this.massSpectrogramViewModel.DisplayRangeMassMax - (float)this.massSpectrogramViewModel.DisplayRangeMassMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.massSpectrogramRotateUI.LeftButtonStartClickPoint.X > this.massSpectrogramRotateUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.massSpectrogramRotateUI.LeftButtonStartClickPoint.X - this.massSpectrogramRotateUI.LeftButtonEndClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramRotateUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramRotateUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.massSpectrogramViewModel.DisplayRangeMassMax > this.massSpectrogramViewModel.MaxMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MaxMass - durationX;
                }
            }
            else
            {
                distanceX = this.massSpectrogramRotateUI.LeftButtonEndClickPoint.X - this.massSpectrogramRotateUI.LeftButtonStartClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramRotateUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramRotateUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.massSpectrogramViewModel.DisplayRangeMassMin < this.massSpectrogramViewModel.MinMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MinMass + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin;
            if (this.massSpectrogramRotateUI.LeftButtonStartClickPoint.Y < this.massSpectrogramRotateUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.massSpectrogramRotateUI.LeftButtonEndClickPoint.Y - this.massSpectrogramRotateUI.LeftButtonStartClickPoint.Y;

                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramRotateUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramRotateUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.massSpectrogramViewModel.DisplayRangeIntensityMax > this.massSpectrogramViewModel.MaxIntensity)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.massSpectrogramRotateUI.LeftButtonStartClickPoint.Y - this.massSpectrogramRotateUI.LeftButtonEndClickPoint.Y;

                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramRotateUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramRotateUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.massSpectrogramViewModel.DisplayRangeIntensityMin < this.massSpectrogramViewModel.MinIntensity)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MinIntensity + durationY;
                }
            }
            MassSpectrogramDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.massSpectrogramRotateUI.RightButtonStartClickPoint.X, this.massSpectrogramRotateUI.RightButtonStartClickPoint.Y), new Point(this.massSpectrogramRotateUI.RightButtonEndClickPoint.X, this.massSpectrogramRotateUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.massSpectrogramViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((mousePoint.X - this.massSpectrogramRotateUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.massSpectrogramRotateUI.BottomMargin) / this.yPacket);

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
