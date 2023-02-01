using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramFE : FrameworkElement
    {
        //ViewModel
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //UI
        private MassSpectrogramUI massSpectrogramUI;

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
        private MassSpectrogramRightRotateUI massSpectrogramRotateUI;

        public MassSpectrogramFE(MassSpectrogramViewModel massSpectrogramViewModel, MassSpectrogramUI massSpectrogramUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramUI = massSpectrogramUI;

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
            if (drawWidth < this.massSpectrogramUI.LeftMargin + this.massSpectrogramUI.RightMargin || drawHeight < this.massSpectrogramUI.BottomMargin + this.massSpectrogramUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen, referencePen;

            //Bean
            MassSpectrogramBean measuredMassSpectrogram;
            MassSpectrogramBean referenceMassSpectrogram;

            //data point
            float mzValue;
            float intensity;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.massSpectrogramUI.LeftMargin, this.massSpectrogramUI.TopMargin), new Size(drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin, drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramUI.BottomMargin), new Point(drawWidth - this.massSpectrogramUI.RightMargin, drawHeight - this.massSpectrogramUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramUI.BottomMargin), new Point(this.massSpectrogramUI.LeftMargin - this.axisFromGraphArea, this.massSpectrogramUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.massSpectrogramViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin) / 100;
                yPacket = (drawHeight - this.massSpectrogramUI.TopMargin - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("Mass chromatogram");
                drawCaptionOnAxis(drawWidth, drawHeight, MassSpectrogramIntensityMode.Relative, 0, 100);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight, MassSpectrogramIntensityMode.Relative, 0, 100); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeMassMax - this.massSpectrogramViewModel.DisplayRangeMassMin);
            this.yPacket = (drawHeight - this.massSpectrogramUI.TopMargin - this.massSpectrogramUI.TopMarginForLabel - this.massSpectrogramUI.BottomMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeIntensityMax - this.massSpectrogramViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.massSpectrogramViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, MassSpectrogramIntensityMode.Relative, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.massSpectrogramViewModel.IntensityMode, this.massSpectrogramViewModel.MinIntensity, this.massSpectrogramViewModel.MaxIntensity);
            drawScaleOnXAxis((float)this.massSpectrogramViewModel.DisplayRangeMassMin, (float)this.massSpectrogramViewModel.DisplayRangeMassMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramUI.LeftMargin, this.massSpectrogramUI.BottomMargin, drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin, drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin)));

            // 5. Mass spectrogram Draw
            #region
            measuredMassSpectrogram = this.massSpectrogramViewModel.MeasuredMassSpectrogramBean;
            referenceMassSpectrogram = this.massSpectrogramViewModel.ReferenceMassSpectrogramBean;
            #endregion

            // 5-1. Initialize Graph Plot Start
            #region
            graphBrush = combineAlphaAndColor(0.25, measuredMassSpectrogram.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(measuredMassSpectrogram.DisplayBrush, measuredMassSpectrogram.LineTickness); // Set Graph Pen
            graphBrush.Freeze();
            graphPen.Freeze();

            referencePen = new Pen(Brushes.Red, 1.0); // Set Graph Pen
            referencePen.Freeze();
            #endregion

            // 5-2. Draw datapoints of measured mass spectrogram
            drawMassSpectrum(measuredMassSpectrogram, graphPen, 0, drawHeight);

            // 5-3. Draw datapoints of reference mass spectrogram if needed
            if (referenceMassSpectrogram != null && referenceMassSpectrogram.MassSpectraCollection != null && referenceMassSpectrogram.MassSpectraCollection.Count != 0)
            {
                drawMassSpectrum(referenceMassSpectrogram, referencePen, 1, drawHeight);
                drawMassSpectrumLabels(referenceMassSpectrogram, drawWidth, drawHeight, referencePen);
            }
            else if (this.massSpectrogramViewModel.NeutralLosses != null && this.massSpectrogramViewModel.NeutralLosses.Count != 0)
            {
                drawNeutralLosses(this.massSpectrogramViewModel.NeutralLosses, this.massSpectrogramViewModel.TargetMz, new Pen(Brushes.Red, 0.5), drawHeight);
            }
            else
            {
                // Draw label of measured mass spectrogram
                drawMassSpectrumLabels(measuredMassSpectrogram, drawWidth, drawHeight, graphPen);
            }
         
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

        private void drawNeutralLosses(List<NeutralLoss> neutralLosses, double ms1PrecursorMz, Pen pen, double drawHeight)
        {
            double productMz, productInt, precursorMz, precursorInt, maxInt, currentMz = double.MaxValue, currentInt = double.MaxValue;
            double proXt, preXt, maxYt, lossYt, lossXt, zeroYt;
            int offset = 0;

            //for precursor mz drawing
            #region
            
            var precursorPen = new Pen(Brushes.Red, 1.0) { DashStyle = DashStyles.Dash };
            this.xt = this.massSpectrogramUI.LeftMargin + (ms1PrecursorMz - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
            this.drawingContext.DrawLine(precursorPen, new Point(this.xt, this.ActualHeight - this.massSpectrogramUI.TopMargin - 20), new Point(this.xt, this.massSpectrogramUI.BottomMargin + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket));

            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

            this.formattedText = new FormattedText("Precursor m/z", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Red);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(this.formattedText, new Point(this.xt - 1, this.massSpectrogramUI.TopMargin + 3));

            this.drawingContext.Pop();
            this.drawingContext.Pop();
            #endregion

            zeroYt = this.massSpectrogramUI.BottomMargin + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;

            foreach (var loss in neutralLosses)
            {
                productMz = loss.ProductMz;
                productInt = loss.ProductIntensity;
                precursorMz = loss.PrecursorMz;
                precursorInt = loss.PrecursorIntensity;
                maxInt = Math.Max(precursorInt, productInt);

                if (currentInt == maxInt) offset += 7;
                else { currentInt = maxInt; offset = 0; }

                proXt = this.massSpectrogramUI.LeftMargin + (productMz - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                preXt = this.massSpectrogramUI.LeftMargin + (precursorMz - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                maxYt = this.massSpectrogramUI.BottomMargin + (maxInt - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                if (proXt < double.MinValue || proXt > double.MaxValue || maxYt < double.MinValue || maxYt > double.MaxValue) continue;// Avoid Calculation Error
                if (Math.Abs(preXt - proXt) < 20) continue;

                lossYt = (maxYt - offset - zeroYt) * 0.9 + zeroYt;
                if (lossYt < zeroYt + 10) lossYt = zeroYt + 10;

                lossXt = (preXt - proXt) * 0.5 + proXt;

                this.drawingContext.DrawLine(pen, new Point(proXt, lossYt), new Point(preXt, lossYt));
                this.drawingContext.DrawLine(pen, new Point(proXt, lossYt), new Point(proXt + 3, lossYt - 3));
                this.drawingContext.DrawLine(pen, new Point(proXt, lossYt), new Point(proXt + 3, lossYt + 3));
                this.drawingContext.DrawLine(pen, new Point(preXt, lossYt), new Point(preXt - 3, lossYt - 3));
                this.drawingContext.DrawLine(pen, new Point(preXt, lossYt), new Point(preXt - 3, lossYt + 3));

                this.formattedText = new FormattedText(loss.Formula.FormulaString + "\r\n" + Math.Round(loss.MassLoss, 4)
                    + " from " + Math.Round(loss.PrecursorMz, 4) + " to " + Math.Round(loss.ProductMz, 4) + "\r\n" + loss.Comment, 
                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Red);
                this.formattedText.TextAlignment = TextAlignment.Left;
                
                if (this.massSpectrogramUI.CurrentMousePoint.X >= proXt && this.massSpectrogramUI.CurrentMousePoint.X <= preXt && this.massSpectrogramUI.CurrentMousePoint.Y > drawHeight - lossYt -2 && this.massSpectrogramUI.CurrentMousePoint.Y < drawHeight - lossYt + 2)
                {
                    this.drawingContext.DrawLine(new Pen(Brushes.Red, 2.0), new Point(proXt, lossYt), new Point(preXt, lossYt));

                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    //this.drawingContext.DrawText(this.formattedText, new Point(this.massSpectrogramUI.CurrentMousePoint.X, drawHeight - lossYt - this.formattedText.Height));
                    this.drawingContext.DrawText(this.formattedText, new Point(this.massSpectrogramUI.LeftMargin + 5, this.massSpectrogramUI.TopMargin + 5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
        }

        private void drawMassSpectrumLabels(MassSpectrogramBean massSpectrogram, double drawWidth, double drawHeight, Pen pen)
        {
            double mzValue, intensity;
            if (massSpectrogram.MassSpectraDisplayLabelCollection != null && massSpectrogram.MassSpectraDisplayLabelCollection.Count != 0)
            {
                List<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new List<MassSpectrogramDisplayLabel>();
                this.formattedText = new FormattedText("@@@.@@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
                double textWidth = this.formattedText.Width;
                int maxLabelNumber = (int)((drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin) / this.formattedText.Width);

                if (maxLabelNumber >= 1)
                {
                    for (int i = 0; i < massSpectrogram.MassSpectraDisplayLabelCollection.Count; i++)
                    {
                        mzValue = (float)massSpectrogram.MassSpectraDisplayLabelCollection[i].Mass;
                        intensity = (float)massSpectrogram.MassSpectraDisplayLabelCollection[i].Intensity;

                        if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin) continue;

                        this.xt = this.massSpectrogramUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                        this.yt = this.massSpectrogramUI.BottomMargin + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                        if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error
                        if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax) break;// Use Data till +5 second beyond   

                        massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = this.xt, Intensity = this.yt, Label = massSpectrogram.MassSpectraDisplayLabelCollection[i].Label });
                    }

                    if (massSpectrogramDisplayLabelCollection.Count >= 1)
                    {
                        massSpectrogramDisplayLabelCollection = massSpectrogramDisplayLabelCollection.OrderByDescending(n => n.Intensity).ToList();

                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramUI.LeftMargin, this.massSpectrogramUI.TopMargin, drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin, drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin)));

                        bool overlap = false;
                        int backtrace = 0;
                        int counter = 0;

                        for (int i = 0; i < massSpectrogramDisplayLabelCollection.Count; i++)
                        {
                            if (counter > maxLabelNumber) break;

                            if (massSpectrogramDisplayLabelCollection[i].Label == null) break;
                            this.formattedText = new FormattedText(massSpectrogramDisplayLabelCollection[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, pen.Brush);
                            this.formattedText.TextAlignment = TextAlignment.Center;

                            overlap = false;
                            backtrace = 0;

                            while (backtrace < i)
                            {
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth * 0.5) { overlap = true; break; }
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth && massSpectrogramDisplayLabelCollection[backtrace].Intensity <= massSpectrogramDisplayLabelCollection[i].Intensity + 10) { overlap = true; break; }
                                if (backtrace > massSpectrogramDisplayLabelCollection.Count) break;
                                backtrace++;
                            }

                            if (overlap == false)
                            {
                                this.drawingContext.DrawText(formattedText, new Point(massSpectrogramDisplayLabelCollection[i].Mass, drawHeight - massSpectrogramDisplayLabelCollection[i].Intensity - 20));
                                counter++;
                            }
                        }

                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramUI.LeftMargin, this.massSpectrogramUI.BottomMargin, drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin, drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin)));
                    }
                }
            }
        }

        private void drawMassSpectrum(MassSpectrogramBean massSpectrogram, Pen graphPen, double xOffset, double drawHeight)
        {
            double mzValue, intensity;

            this.ys = this.massSpectrogramUI.BottomMargin + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;

            for (int i = 0; i < massSpectrogram.MassSpectraCollection.Count; i++)
            {
                mzValue = (float)massSpectrogram.MassSpectraCollection[i][0];
                intensity = (float)massSpectrogram.MassSpectraCollection[i][1];

                if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin - 5) continue; // Use Data -5 second beyond

                this.xt = this.massSpectrogramUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                this.yt = this.massSpectrogramUI.BottomMargin + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error

                if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                this.drawingContext.DrawLine(graphPen, new Point(this.xt + xOffset, this.yt), new Point(this.xt + xOffset, this.ys));

                if (massSpectrogram.MassSpectraDisplayLabelCollection != null && massSpectrogram.MassSpectraDisplayLabelCollection.Count != 0 && massSpectrogram.MassSpectraDisplayLabelCollection[i].ProductIon != null) {

                    var productIon = massSpectrogram.MassSpectraDisplayLabelCollection[i].ProductIon;
                    if (productIon.Comment != string.Empty) {

                        var highlightPen = new Pen(Brushes.Navy, 2.0);
                        this.drawingContext.DrawLine(highlightPen, new Point(this.xt + xOffset, this.yt), new Point(this.xt + xOffset, this.ys));

                        if (this.massSpectrogramUI.CurrentMousePoint.X >= this.xt - 2 && this.massSpectrogramUI.CurrentMousePoint.X <= this.xt + 2 &&
                            this.massSpectrogramUI.CurrentMousePoint.Y > drawHeight - this.yt && this.massSpectrogramUI.CurrentMousePoint.Y < drawHeight - this.massSpectrogramUI.BottomMargin) {

                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 2.0), new Point(this.xt, this.yt), new Point(this.xt, this.ys));

                            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                            this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                            this.formattedText = new FormattedText(productIon.Formula.FormulaString + "\r\n" + Math.Round(productIon.Mass, 4) + "\r\n" + productIon.Comment, 
                                CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Red);
                            this.formattedText.TextAlignment = TextAlignment.Left;

                            this.drawingContext.DrawText(this.formattedText, new Point(this.massSpectrogramUI.LeftMargin + 5, this.massSpectrogramUI.TopMargin + 5));

                            this.drawingContext.Pop();
                            this.drawingContext.Pop();
                        }
                    }
                }
            }
        }

        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramUI.LeftMargin, this.massSpectrogramUI.TopMargin - 17));
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
                xPixelValue = this.massSpectrogramUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.massSpectrogramUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.massSpectrogramUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.massSpectrogramUI.BottomMargin), new Point(xPixelValue, drawHeight - this.massSpectrogramUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.massSpectrogramUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.massSpectrogramUI.BottomMargin), new Point(xPixelValue, drawHeight - this.massSpectrogramUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max < min) max = min + 0.9;
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin);
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

        private void drawScaleOnYAxis(float yAxisMinValue, float yAxisMaxValue, double drawWidth, double drawHeight, MassSpectrogramIntensityMode massSpectrogramIntensityMode, float lowestIntensity, float highestIntensity)
        {
            string yString = ""; // String for Y-Scale Value
            int foldChange = -1;
            double yscale_max;
            double yscale_min;

            if (massSpectrogramIntensityMode == MassSpectrogramIntensityMode.Absolute)
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

            double yspacket = (float)(((double)(drawHeight - this.massSpectrogramUI.TopMargin - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.massSpectrogramUI.BottomMargin - 1 * (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.massSpectrogramUI.BottomMargin) continue;
                if (yPixelValue < this.massSpectrogramUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange <= 0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else
                    {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramUI.LeftMargin - this.axisFromGraphArea, yPixelValue));

                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.massSpectrogramUI.TopMargin - this.massSpectrogramUI.BottomMargin);
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

        private void drawCaptionOnAxis(double drawWidth, double drawHeight, MassSpectrogramIntensityMode massSpectrogramIntensityMode, float yAxisMinValue, float yAxisMaxValue)
        {
            // Set Caption "Min." to X-Axis            
            this.formattedText = new FormattedText("m/z", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramUI.LeftMargin + 0.5 * (drawWidth - this.massSpectrogramUI.LeftMargin - this.massSpectrogramUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.massSpectrogramUI.TopMargin + 0.5 * (drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin)));
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

            if (massSpectrogramIntensityMode == MassSpectrogramIntensityMode.Absolute)
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
                if (figure > -1)
                {
                    formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else
                {
                    formattedText = new FormattedText("Relative abundance (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                //formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, - 7));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.massSpectrogramUI.TopMargin + 0.5 * (drawHeight - this.massSpectrogramUI.BottomMargin - this.massSpectrogramUI.TopMargin))));
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
            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
            this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
            this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
            this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;

            MassSpectrogramDraw();
        }

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.massSpectrogramUI.RightButtonStartClickPoint.X - this.massSpectrogramUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.massSpectrogramUI.RightButtonStartClickPoint.Y - this.massSpectrogramUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.massSpectrogramUI.RightButtonStartClickPoint.X - this.massSpectrogramUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.massSpectrogramUI.RightButtonStartClickPoint.X > this.massSpectrogramUI.RightButtonEndClickPoint.X)
            {
                if (this.massSpectrogramUI.RightButtonStartClickPoint.X > this.massSpectrogramUI.LeftMargin)
                {
                    if (this.massSpectrogramUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.massSpectrogramUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramUI.RightButtonStartClickPoint.X - this.massSpectrogramUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramUI.RightButtonEndClickPoint.X >= this.massSpectrogramUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramUI.RightButtonEndClickPoint.X - this.massSpectrogramUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.massSpectrogramUI.RightButtonEndClickPoint.X > this.massSpectrogramUI.LeftMargin)
                {
                    if (this.massSpectrogramUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.massSpectrogramUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramUI.RightButtonEndClickPoint.X - this.massSpectrogramUI.LeftMargin) / this.xPacket);
                    }
                    if (this.massSpectrogramUI.RightButtonStartClickPoint.X >= this.massSpectrogramUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramUI.RightButtonStartClickPoint.X - this.massSpectrogramUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate  
            double rightYstart;
            double rightYend;

            if (this.massSpectrogramUI.RightButtonStartClickPoint.Y > this.massSpectrogramUI.RightButtonEndClickPoint.Y)
            {
                rightYstart = this.massSpectrogramUI.RightButtonEndClickPoint.Y;
                rightYend = this.massSpectrogramUI.RightButtonStartClickPoint.Y;
            }
            else
            {
                rightYstart = this.massSpectrogramUI.RightButtonStartClickPoint.Y;
                rightYend = this.massSpectrogramUI.RightButtonEndClickPoint.Y;
            }

            float newIntMax = (float)(this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramUI.BottomMargin - rightYstart) / this.yPacket));
            float newIntMin = (float)(this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramUI.BottomMargin - rightYend) / this.yPacket));

            if (newIntMax > this.massSpectrogramViewModel.MaxIntensity) newIntMax = this.massSpectrogramViewModel.MaxIntensity;
            if (newIntMin < this.massSpectrogramViewModel.MinIntensity) newIntMin = this.massSpectrogramViewModel.MinIntensity;

            this.massSpectrogramViewModel.DisplayRangeIntensityMax = newIntMax;
            this.massSpectrogramViewModel.DisplayRangeIntensityMin = newIntMin;
        }

        public void GraphScroll()
        {
            if (this.massSpectrogramUI.LeftButtonStartClickPoint.X == -1 || this.massSpectrogramUI.LeftButtonStartClickPoint.Y == -1)
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
            float newMinRange, newMaxRange;

            if (this.massSpectrogramUI.LeftButtonStartClickPoint.X > this.massSpectrogramUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.massSpectrogramUI.LeftButtonStartClickPoint.X - this.massSpectrogramUI.LeftButtonEndClickPoint.X;

                newMinRange = this.massSpectrogramUI.GraphScrollInitialMassMin + (float)(distanceX / this.xPacket);
                newMaxRange = this.massSpectrogramUI.GraphScrollInitialMassMax + (float)(distanceX / this.xPacket);
                
                if (newMaxRange > this.massSpectrogramViewModel.MaxMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MaxMass - durationX;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = newMinRange;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxRange;
                }
            }
            else
            {
                distanceX = this.massSpectrogramUI.LeftButtonEndClickPoint.X - this.massSpectrogramUI.LeftButtonStartClickPoint.X;

                newMinRange = this.massSpectrogramUI.GraphScrollInitialMassMin - (float)(distanceX / this.xPacket);
                newMaxRange = this.massSpectrogramUI.GraphScrollInitialMassMax - (float)(distanceX / this.xPacket);

                
                if (newMinRange < this.massSpectrogramViewModel.MinMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MinMass + durationX;
                }
                else
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = newMinRange;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = newMaxRange;
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
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.massSpectrogramUI.RightButtonStartClickPoint.X, this.massSpectrogramUI.RightButtonStartClickPoint.Y), new Point(this.massSpectrogramUI.RightButtonEndClickPoint.X, this.massSpectrogramUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.massSpectrogramViewModel == null)
                return null;

            float[] peakInformation;
            float mzValue, intensity;

            mzValue = (float)this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((mousePoint.X - this.massSpectrogramUI.LeftMargin) / this.xPacket);
            intensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.massSpectrogramUI.BottomMargin) / this.yPacket);

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
