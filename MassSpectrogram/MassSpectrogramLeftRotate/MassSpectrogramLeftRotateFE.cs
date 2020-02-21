using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramLeftRotateFE : FrameworkElement
    {
        //ViewModel
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //UI
        private MassSpectrogramLeftRotateUI massSpectrogramLeftRotateUI;

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

        public MassSpectrogramLeftRotateFE(MassSpectrogramViewModel massSpectrogramViewModel, MassSpectrogramLeftRotateUI massSpectrogramLeftRotateUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramLeftRotateUI = massSpectrogramLeftRotateUI;

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
            if (drawWidth < this.massSpectrogramLeftRotateUI.LeftMargin + this.massSpectrogramLeftRotateUI.RightMargin || drawHeight < this.massSpectrogramLeftRotateUI.BottomMargin + this.massSpectrogramLeftRotateUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen, graphPenBold;

            //Bean
            MassSpectrogramBean measuredMassSpectrogramBean;

            //data point
            float mzValue;
            float intensity;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
			// outer background
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
			// MS spectrum graph area
			this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.massSpectrogramLeftRotateUI.LeftMargin, this.massSpectrogramLeftRotateUI.TopMargin), new Size(drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.RightMargin, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.TopMargin)));
			// intensity axis
			this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramLeftRotateUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin));
			// m/z axis
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramLeftRotateUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin), new Point(this.massSpectrogramLeftRotateUI.LeftMargin - this.axisFromGraphArea, this.massSpectrogramLeftRotateUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.massSpectrogramViewModel == null || this.massSpectrogramViewModel.MeasuredMassSpectrogramBean == null
                || this.massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || this.massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) {
                // Calculate Packet Size
                xPacket = (drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.LeftMarginForLabel - this.massSpectrogramLeftRotateUI.RightMargin) / 100;
                yPacket = (drawHeight - this.massSpectrogramLeftRotateUI.TopMargin - this.massSpectrogramLeftRotateUI.BottomMargin) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("No Info.");
                drawCaptionOnAxis(drawWidth, drawHeight, 0, 100, MassSpectrogramIntensityMode.Relative);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight, 0, 100, MassSpectrogramIntensityMode.Relative);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.LeftMarginForLabel - this.massSpectrogramLeftRotateUI.RightMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeIntensityMax - this.massSpectrogramViewModel.DisplayRangeIntensityMin);
            this.yPacket = (drawHeight - this.massSpectrogramLeftRotateUI.TopMargin - this.massSpectrogramLeftRotateUI.BottomMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeMassMax - this.massSpectrogramViewModel.DisplayRangeMassMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.massSpectrogramViewModel.GraphTitle);
//<<<<<<< HEAD
            drawCaptionOnAxis(drawWidth, drawHeight, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, this.massSpectrogramViewModel.IntensityMode);
//=======
//            drawCaptionOnAxis(drawWidth - 20 , drawHeight, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, this.massSpectrogramViewModel.IntensityMode);
//>>>>>>> diego
            drawScaleOnXAxis((float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.massSpectrogramViewModel.MinIntensity, this.massSpectrogramViewModel.MaxIntensity, this.massSpectrogramViewModel.IntensityMode);
            drawScaleOnYAxis((float)this.massSpectrogramViewModel.DisplayRangeMassMin, (float)this.massSpectrogramViewModel.DisplayRangeMassMax, drawWidth, drawHeight);
            #endregion

            #region // push transform
            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramLeftRotateUI.LeftMargin, this.massSpectrogramLeftRotateUI.BottomMargin, drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.RightMargin, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.TopMargin)));
            #endregion

            // 5. Mass spectrogram Draw
            #region
            measuredMassSpectrogramBean = this.massSpectrogramViewModel.MeasuredMassSpectrogramBean;
            #endregion

            // 5-1. Initialize Graph Plot Start
            #region
            graphBrush = combineAlphaAndColor(0.25, measuredMassSpectrogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(measuredMassSpectrogramBean.DisplayBrush, measuredMassSpectrogramBean.LineTickness); // Set Graph Pen
            graphPenBold = new Pen(measuredMassSpectrogramBean.DisplayBrush, measuredMassSpectrogramBean.LineTickness * 2); // Set Graph Pen
            graphBrush.Freeze();
            graphPen.Freeze();
            graphPenBold.Freeze();
            #endregion

            // 5-2. Draw datapoints
            #region
            for (int i = 0; i < measuredMassSpectrogramBean.MassSpectraCollection.Count; i++)
            {
                mzValue = (float)measuredMassSpectrogramBean.MassSpectraCollection[i][0];
                intensity = (float)measuredMassSpectrogramBean.MassSpectraCollection[i][1];

                if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin - 5) continue; // Use Data -5 second beyond

                this.xt = this.massSpectrogramLeftRotateUI.RightMargin + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket;// Calculate x Plot Coordinate
                this.yt = this.massSpectrogramLeftRotateUI.BottomMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.yPacket;// Calculate y Plot Coordinate

                if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error
                
                if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                if(Math.Abs(mzValue - this.massSpectrogramViewModel.TargetMz) < 0.000001)
                    this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(drawWidth - this.xt, this.yt), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin - (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket, this.yt));
                else
                    this.drawingContext.DrawLine(graphPen, new Point(drawWidth - this.xt, this.yt), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin - (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket, this.yt));
            }
            #endregion

            // 5-3. Draw label of measured mass spectrogram
            #region
            if (measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection != null && measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection.Count != 0)
            {
                List<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new List<MassSpectrogramDisplayLabel>();
                this.formattedText = new FormattedText("@@@.@@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
                double textWidth = this.formattedText.Width;
                int maxLabelNumber = (int)((drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.TopMargin) / this.formattedText.Width);

                if (maxLabelNumber >= 1)
                {
                    for (int i = 0; i < measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection.Count; i++)
                    {
                        mzValue = (float)measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection[i].Mass;
                        intensity = (float)measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection[i].Intensity;

                        if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin) continue;

                        this.xt = this.massSpectrogramLeftRotateUI.RightMargin + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket;// Calculate x Plot Coordinate
                        this.yt = this.massSpectrogramLeftRotateUI.BottomMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.yPacket;// Calculate y Plot Coordinate

                        if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error
                        if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax) break;// Use Data till +5 second beyond   

                        massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = this.yt, Intensity = this.xt, Label = measuredMassSpectrogramBean.MassSpectraDisplayLabelCollection[i].Label });
                        this.drawingContext.DrawLine(graphPenBold, new Point(drawWidth - this.xt, this.yt), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin - (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.xPacket, this.yt));
                    }

                    if (massSpectrogramDisplayLabelCollection.Count >= 1)
                    {
                        massSpectrogramDisplayLabelCollection = massSpectrogramDisplayLabelCollection.OrderByDescending(n => n.Intensity).ToList();

                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region

                        // Set Caption to Y-Axis                                                
                        this.drawingContext.PushTransform(new RotateTransform(270.0));
                        this.drawingContext.PushTransform(new TranslateTransform(- drawHeight, 0));
                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramLeftRotateUI.BottomMargin, this.massSpectrogramLeftRotateUI.LeftMargin, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.TopMargin, drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.RightMargin)));

                        bool overlap = false;
                        int backtrace = 0;
                        int counter = 0;

                        for (int i = 0; i < massSpectrogramDisplayLabelCollection.Count; i++)
                        {
                            if (counter > maxLabelNumber) break;

                            this.formattedText = new FormattedText(massSpectrogramDisplayLabelCollection[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Blue);
                            this.formattedText.TextAlignment = TextAlignment.Center;

                            overlap = false;
                            backtrace = 0;

                            while (backtrace < i)
                            {
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth * 0.5) { overlap = true; break; }
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth && massSpectrogramDisplayLabelCollection[backtrace].Intensity <= massSpectrogramDisplayLabelCollection[i].Intensity - 20) { overlap = true; break; }
                                if (backtrace > massSpectrogramDisplayLabelCollection.Count) break;
                                backtrace++;
                            }

                            if (overlap == false)
                            {
                                this.drawingContext.DrawText(formattedText, new Point(massSpectrogramDisplayLabelCollection[i].Mass, drawWidth - massSpectrogramDisplayLabelCollection[i].Intensity - 20));
                                counter++;
                            }
                        }

                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramLeftRotateUI.LeftMargin, this.massSpectrogramLeftRotateUI.BottomMargin, drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.RightMargin, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.TopMargin)));
                    }
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
            if (this.massSpectrogramViewModel == null)
                this.formattedText = new FormattedText("", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            else
                this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(0, this.ActualHeight));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramLeftRotateUI.BottomMargin, 10));

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

            double xspacket = (float)(((double)(drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.LeftMarginForLabel - this.massSpectrogramLeftRotateUI.RightMargin)) / (xscale_max - xscale_min)); // Packet for Y-Scale For Zooming


            getXaxisScaleInterval(xscale_min, xscale_max, drawWidth);
            int xStart = (int)(xscale_min / (double)this.xMinorScale) - 1;
            int xEnd = (int)(xscale_max / (double)this.xMinorScale) + 1;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++)
            {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = this.massSpectrogramLeftRotateUI.RightMargin + (xAxisValue - xscale_min) * xspacket;
                if (xPixelValue < this.massSpectrogramLeftRotateUI.RightMargin) continue;
                if (xPixelValue > drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(drawWidth - xPixelValue, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin), new Point(drawWidth - xPixelValue, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(drawWidth - xPixelValue, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(drawWidth - xPixelValue, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin), new Point(drawWidth - xPixelValue, drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin + this.shortScaleSize));
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
            int xAxisPixelRange = (int)(drawWidth - this.massSpectrogramLeftRotateUI.LeftMargin - this.massSpectrogramLeftRotateUI.LeftMarginForLabel - this.massSpectrogramLeftRotateUI.RightMargin);
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
                yPixelValue = drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin - (yAxisValue - yAxisMinValue) * this.yPacket;
                if (yPixelValue > drawHeight - this.massSpectrogramLeftRotateUI.BottomMargin) continue;
                if (yPixelValue < this.massSpectrogramLeftRotateUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin + this.longScaleSize, yPixelValue), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin, yPixelValue));
                    if (this.yMajorScale < 1)
                        this.formattedText = new FormattedText(yAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(yAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                    this.drawingContext.PushTransform(new RotateTransform(270.0));
                    this.drawingContext.DrawText(formattedText, new Point(drawHeight - yPixelValue, drawWidth - this.massSpectrogramLeftRotateUI.RightMargin + this.longScaleSize));
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin + this.shortScaleSize, yPixelValue), new Point(drawWidth - this.massSpectrogramLeftRotateUI.RightMargin, yPixelValue));
                }

            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (max < min) { max += min - max; }
            if (max - min == 0) { max += 0.9; }

            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.massSpectrogramLeftRotateUI.TopMargin - this.massSpectrogramLeftRotateUI.BottomMargin);
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

        private void drawCaptionOnAxis(double drawWidth, double drawHeight, float yAxisMinValue, float yAxisMaxValue, MassSpectrogramIntensityMode intensityMode)
        {
            // Set Caption "Intensity" to X-axis
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

            if (intensityMode == MassSpectrogramIntensityMode.Absolute)
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
                formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(drawWidth * 0.5, drawHeight - 20));

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
            if (Math.Abs(this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.Y - this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X > this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X)
            {
                if (this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X > this.massSpectrogramLeftRotateUI.LeftMargin)
                {
                    if (this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.massSpectrogramLeftRotateUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramLeftRotateUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X >= this.massSpectrogramLeftRotateUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X - this.massSpectrogramLeftRotateUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X > this.massSpectrogramLeftRotateUI.LeftMargin)
                {
                    if (this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.massSpectrogramLeftRotateUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X - this.massSpectrogramLeftRotateUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X >= this.massSpectrogramLeftRotateUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X - this.massSpectrogramLeftRotateUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.Y > this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.Y)
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramLeftRotateUI.BottomMargin - this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.X == -1 || this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.Y == -1)
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
            if (this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.X > this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.X - this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramLeftRotateUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramLeftRotateUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.massSpectrogramViewModel.DisplayRangeMassMax > this.massSpectrogramViewModel.MaxMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MaxMass - durationX;
                }
            }
            else
            {
                distanceX = this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.X - this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramLeftRotateUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramLeftRotateUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.massSpectrogramViewModel.DisplayRangeMassMin < this.massSpectrogramViewModel.MinMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MinMass + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin;
            if (this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.Y < this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.Y - this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.Y;

                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramLeftRotateUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramLeftRotateUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.massSpectrogramViewModel.DisplayRangeIntensityMax > this.massSpectrogramViewModel.MaxIntensity)
                {
                    this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                    this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.massSpectrogramLeftRotateUI.LeftButtonStartClickPoint.Y - this.massSpectrogramLeftRotateUI.LeftButtonEndClickPoint.Y;

                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramLeftRotateUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramLeftRotateUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

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
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.X, this.massSpectrogramLeftRotateUI.RightButtonStartClickPoint.Y), new Point(this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.X, this.massSpectrogramLeftRotateUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.massSpectrogramViewModel == null)
                return null;

            float[] peakInformation;
            float mzValue, intensity;

            mzValue = (float)this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((mousePoint.X - this.massSpectrogramLeftRotateUI.LeftMargin) / this.xPacket);
            intensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.massSpectrogramLeftRotateUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { mzValue, intensity };

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
