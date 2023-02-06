using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rfx.Riken.OsakaUniv
{
    public class MassSpectrogramWithReferenceFE: FrameworkElement
    {
        //ViewModel
        private MassSpectrogramViewModel massSpectrogramViewModel;

        //UI
        private MassSpectrogramWithReferenceUI massSpectrogramWithReferenceUI;

        //Visual property
        private VisualCollection visualCollection;
        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;
        private BitmapImage bitmapImage;

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
        private double xPacketReference;
        private double yPacketReference;
        private MassSpectrogramRightRotateUI massSpectrogramRotateUI;

        public MassSpectrogramWithReferenceFE(MassSpectrogramViewModel massSpectrogramViewModel, MassSpectrogramWithReferenceUI massSpectrogramWithReferenceUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.massSpectrogramViewModel = massSpectrogramViewModel;
            this.massSpectrogramWithReferenceUI = massSpectrogramWithReferenceUI;

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
            this.visualCollection.Insert(0, this.drawingVisual);
        }

        private DrawingVisual MassSpectrogramDrawingVisual(double drawWidth, double drawHeight)
        {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.massSpectrogramWithReferenceUI.LeftMargin + this.massSpectrogramWithReferenceUI.RightMargin || drawHeight < this.massSpectrogramWithReferenceUI.BottomMargin + this.massSpectrogramWithReferenceUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen, referencePen, boldReferencePen, graphPenBold;

            //Bean
            MassSpectrogramBean measuredMassSpectrogramBean;
            MassSpectrogramBean referenceMassSpectrogramBean;

            //data point
            float mzValue;
            float intensity;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.TopMargin), new Size(drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin), new Point(drawWidth - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin), new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, this.massSpectrogramWithReferenceUI.TopMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, this.massSpectrogramWithReferenceUI.TopMargin + (drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin) * 0.5), new Point(drawWidth - this.massSpectrogramWithReferenceUI.RightMargin, this.massSpectrogramWithReferenceUI.TopMargin + (drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin) * 0.5));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.massSpectrogramViewModel == null)
            {
                // Calculate Packet Size
                this.xPacket = (drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) / 100;
                this.yPacket = (drawHeight - this.massSpectrogramWithReferenceUI.TopMargin - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMarginForLabel - this.massSpectrogramWithReferenceUI.BottomMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("Measurement vs. Reference");
                drawCaptionOnAxis(drawWidth, drawHeight, MassSpectrogramIntensityMode.Absolute, -1, 100);
                drawScaleOnYAxis(0, 100, 0, 1, drawWidth, drawHeight, MassSpectrogramIntensityMode.Absolute, 0, 100, 0, 1); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 100, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            double halfDrawHeight = (drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin) * 0.5;
            this.xPacket = (drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeMassMax - this.massSpectrogramViewModel.DisplayRangeMassMin);
            this.yPacket = (drawHeight - this.massSpectrogramWithReferenceUI.TopMargin - this.massSpectrogramWithReferenceUI.TopMarginForLabel - this.massSpectrogramWithReferenceUI.BottomMargin - halfDrawHeight) / (double)(this.massSpectrogramViewModel.DisplayRangeIntensityMax - this.massSpectrogramViewModel.DisplayRangeIntensityMin);
            this.xPacketReference = (drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) / (double)(this.massSpectrogramViewModel.DisplayRangeMassMax - this.massSpectrogramViewModel.DisplayRangeMassMin);
            this.yPacketReference = (drawHeight - this.massSpectrogramWithReferenceUI.TopMargin - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.BottomMarginForLabel - halfDrawHeight) / (double)(this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference - this.massSpectrogramViewModel.DisplayRangeIntensityMinReference);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.massSpectrogramViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, this.massSpectrogramViewModel.IntensityMode, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax);
            drawScaleOnXAxis((float)this.massSpectrogramViewModel.DisplayRangeMassMin, (float)this.massSpectrogramViewModel.DisplayRangeMassMax, drawWidth, drawHeight);
            drawScaleOnYAxis((float)this.massSpectrogramViewModel.DisplayRangeIntensityMin, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMax, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMinReference, (float)this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference, drawWidth, drawHeight, this.massSpectrogramViewModel.IntensityMode, this.massSpectrogramViewModel.MinIntensity, this.massSpectrogramViewModel.MaxIntensity, this.massSpectrogramViewModel.MinIntensityReference, this.massSpectrogramViewModel.MaxIntensityReference);
            #endregion

            // push transform
            #region
            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.BottomMargin, drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));
            #endregion

            // 5-0. MassSpectrogramBean set
            #region
            measuredMassSpectrogramBean = this.massSpectrogramViewModel.MeasuredMassSpectrogramBean;
            referenceMassSpectrogramBean = this.massSpectrogramViewModel.ReferenceMassSpectrogramBean;
            #endregion

            // 5-1. Initialize Graph Plot Start
            #region
            graphBrush = combineAlphaAndColor(0.25, measuredMassSpectrogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(measuredMassSpectrogramBean.DisplayBrush, measuredMassSpectrogramBean.LineTickness); // Set Graph Pen
            graphPenBold = new Pen(measuredMassSpectrogramBean.DisplayBrush, measuredMassSpectrogramBean.LineTickness); // Set Graph Pen
            graphBrush.Freeze();
            graphPen.Freeze();
            graphPenBold.Freeze();

            referencePen = new Pen(Brushes.Red, 1.0); // Set Graph Pen
            referencePen.Freeze();

            boldReferencePen = new Pen(Brushes.Red, 3.0);
            boldReferencePen.Freeze();
            #endregion

            // 5-2. Draw datapoints of measured mass spectrogram
            drawMassSpectrum(measuredMassSpectrogramBean, graphPen, halfDrawHeight);

            // 5-3. Draw label and line of measured mass spectrogram
            drawMassSpectrumLabels(measuredMassSpectrogramBean, graphPenBold, halfDrawHeight, drawWidth, drawHeight);

            // 5.4. Draw datapoints and label of reference mass spectrogram
            drawReferenceSpectrum(referenceMassSpectrogramBean, referencePen, boldReferencePen, halfDrawHeight, drawHeight, drawWidth);

            // push transform refresh
            #region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext
            #endregion
            
            return this.drawingVisual;
        }

        private void drawReferenceSpectrum(MassSpectrogramBean refSpectrogramBean, Pen referencePen, Pen boldReferencePen, double halfDrawHeight, double drawHeight, double drawWidth)
        {
            double mzValue, intensity;
            if (refSpectrogramBean != null && refSpectrogramBean.MassSpectraCollection != null)
            {
                for (int i = 0; i < refSpectrogramBean.MassSpectraCollection.Count; i++)
                {
                    mzValue = (float)refSpectrogramBean.MassSpectraCollection[i][0];
                    intensity = (float)refSpectrogramBean.MassSpectraCollection[i][1];

                    if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin - 5) continue; // Use Data -5 second beyond

                    this.xt = this.massSpectrogramWithReferenceUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMinReference) * this.xPacketReference;// Calculate x Plot Coordinate
                    this.yt = this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight - (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMinReference) * this.yPacketReference;// Calculate y Plot Coordinate

                    if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error

                    if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                    var midpoint = this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight - (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMinReference) * this.yPacketReference;
                    this.drawingContext.DrawLine(referencePen, new Point(this.xt, this.yt), new Point(this.xt, midpoint));

                    if (Math.Abs(this.xt - this.massSpectrogramWithReferenceUI.CurrentMousePoint.X) < this.massSpectrogramWithReferenceUI.BoldLineWidth * 2 
                        && drawHeight - this.yt > this.massSpectrogramWithReferenceUI.CurrentMousePoint.Y
                        && this.massSpectrogramWithReferenceUI.CurrentMousePoint.Y > midpoint - this.massSpectrogramWithReferenceUI.BottomMarginForLabel)
                    {
                        if (refSpectrogramBean.MassSpectraDisplayLabelCollection[i].PeakFragmentPair == null 
                            || refSpectrogramBean.MassSpectraDisplayLabelCollection[i].PeakFragmentPair.MatchedFragmentInfo.Smiles == null
                            || refSpectrogramBean.MassSpectraDisplayLabelCollection[i].PeakFragmentPair.MatchedFragmentInfo.Smiles == string.Empty) return;
                        
                        this.drawingContext.DrawLine(boldReferencePen, new Point(this.xt, this.yt), new Point(this.xt, midpoint));

                        var smiles = refSpectrogramBean.MassSpectraDisplayLabelCollection[i].PeakFragmentPair.MatchedFragmentInfo.Smiles;

                        Mouse.OverrideCursor = Cursors.Wait;

                        //var image = MoleculeImage.SmilesToImage(smiles, (int)halfDrawHeight, (int)halfDrawHeight);
                        var image = MoleculeImage.SmilesToMediaImageSource(smiles, (int)halfDrawHeight, (int)halfDrawHeight);
                        imageShow(image);

                        Mouse.OverrideCursor = null;
                    }
                }
                // Draw label
                drawReferenceSpectrumLabels(refSpectrogramBean, halfDrawHeight, drawWidth, drawHeight);
            }
            else
            {
                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region
                this.drawingContext.Pop();// Reset Drawing Region

                this.formattedText = new FormattedText("No information", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 20, Brushes.Black);
                this.formattedText.TextAlignment = TextAlignment.Center;
                this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramWithReferenceUI.LeftMargin + (drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) * 0.5, this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight * 1.5 - 10));
            }
        }

        private void drawReferenceSpectrumLabels(MassSpectrogramBean refSpectrogramBean, double halfDrawHeight, double drawWidth, double drawHeight)
        {
            double mzValue, intensity;
            if (refSpectrogramBean.MassSpectraDisplayLabelCollection != null && refSpectrogramBean.MassSpectraDisplayLabelCollection.Count != 0)
            {
                List<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new List<MassSpectrogramDisplayLabel>();
                this.formattedText = new FormattedText("@@.@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
                double textWidth = this.formattedText.Width;
                int maxLabelNumber = (int)((drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) / this.formattedText.Width);

                if (maxLabelNumber >= 1)
                {
                    for (int i = 0; i < refSpectrogramBean.MassSpectraDisplayLabelCollection.Count; i++)
                    {
                        mzValue = (float)refSpectrogramBean.MassSpectraDisplayLabelCollection[i].Mass;
                        intensity = (float)refSpectrogramBean.MassSpectraDisplayLabelCollection[i].Intensity;

                        if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin) continue;

                        this.xt = this.massSpectrogramWithReferenceUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMinReference) * this.xPacketReference;// Calculate x Plot Coordinate
                        this.yt = this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight - (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMinReference) * this.yPacketReference;// Calculate y Plot Coordinate

                        if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error
                        if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                        massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = this.xt, Intensity = this.yt, Label = refSpectrogramBean.MassSpectraDisplayLabelCollection[i].Label });
                    }

                    if (massSpectrogramDisplayLabelCollection.Count >= 1)
                    {
                        massSpectrogramDisplayLabelCollection = massSpectrogramDisplayLabelCollection.OrderBy(n => n.Intensity).ToList();

                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.TopMargin, drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));

                        bool overlap = false;
                        int backtrace = 0;
                        int counter = 0;

                        for (int i = 0; i < massSpectrogramDisplayLabelCollection.Count; i++)
                        {
                            if (counter > maxLabelNumber) break;

                            this.formattedText = new FormattedText(massSpectrogramDisplayLabelCollection[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Red);
                            this.formattedText.TextAlignment = TextAlignment.Center;

                            textWidth = this.formattedText.Width;

                            overlap = false;
                            backtrace = 0;

                            while (backtrace < i)
                            {
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth * 0.5) { overlap = true; break; }
                                if (Math.Abs(massSpectrogramDisplayLabelCollection[backtrace].Mass - massSpectrogramDisplayLabelCollection[i].Mass) < textWidth && massSpectrogramDisplayLabelCollection[backtrace].Intensity >= massSpectrogramDisplayLabelCollection[i].Intensity - 10) { overlap = true; break; }
                                if (backtrace > massSpectrogramDisplayLabelCollection.Count) break;
                                backtrace++;
                            }

                            if (overlap == false)
                            {
                                this.drawingContext.DrawText(formattedText, new Point(massSpectrogramDisplayLabelCollection[i].Mass, drawHeight - massSpectrogramDisplayLabelCollection[i].Intensity));
                                counter++;
                            }
                        }

                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.BottomMargin, drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));
                    }
                }
            }
        }

        private void drawMassSpectrumLabels(MassSpectrogramBean massSpectrogramBean, Pen graphPenBold, double halfDrawHeight, double drawWidth, double drawHeight)
        {
            double mzValue, intensity;
            if (massSpectrogramBean.MassSpectraDisplayLabelCollection != null && massSpectrogramBean.MassSpectraDisplayLabelCollection.Count != 0)
            {
                var massSpectrogramDisplayLabelCollection = new List<MassSpectrogramDisplayLabel>();
                
                this.formattedText = new FormattedText("@@@.@@@", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
                double textWidth = this.formattedText.Width;
                int maxLabelNumber = (int)((drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin) / this.formattedText.Width);

                if (maxLabelNumber >= 1)
                {
                    for (int i = 0; i < massSpectrogramBean.MassSpectraDisplayLabelCollection.Count; i++)
                    {
                        mzValue = (float)massSpectrogramBean.MassSpectraDisplayLabelCollection[i].Mass;
                        intensity = (float)massSpectrogramBean.MassSpectraDisplayLabelCollection[i].Intensity;

                        if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin) continue;

                        this.xt = this.massSpectrogramWithReferenceUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                        this.yt = this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                        if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error
                        if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax) break;// Use Data till +5 second beyond   

                        massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = this.xt, Intensity = this.yt, Label = massSpectrogramBean.MassSpectraDisplayLabelCollection[i].Label });
                        this.drawingContext.DrawLine(graphPenBold, new Point(this.xt, this.yt), new Point(this.xt, this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    }

                    if (massSpectrogramDisplayLabelCollection.Count >= 1)
                    {
                        massSpectrogramDisplayLabelCollection = massSpectrogramDisplayLabelCollection.OrderByDescending(n => n.Intensity).ToList();

                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region
                        this.drawingContext.Pop();// Reset Drawing Region

                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.TopMargin, drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));

                        bool overlap = false;
                        int backtrace = 0;
                        int counter = 0;

                        for (int i = 0; i < massSpectrogramDisplayLabelCollection.Count; i++)
                        {
                            if (counter > maxLabelNumber) break;

                            this.formattedText = new FormattedText(massSpectrogramDisplayLabelCollection[i].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, this.massSpectrogramViewModel.MeasuredMassSpectrogramBean.DisplayBrush);
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
                        this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.BottomMargin, drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));
                    }
                }
            }
        }

        private void drawMassSpectrum(MassSpectrogramBean massSpectrogramBean, Pen graphPen, double halfDrawHeight)
        {
            double mzValue, intensity;

            for (int i = 0; i < massSpectrogramBean.MassSpectraCollection.Count; i++)
            {
                mzValue = (float)massSpectrogramBean.MassSpectraCollection[i][0];
                intensity = (float)massSpectrogramBean.MassSpectraCollection[i][1];

                if (mzValue < this.massSpectrogramViewModel.DisplayRangeMassMin - 5) continue; // Use Data -5 second beyond

                this.xt = this.massSpectrogramWithReferenceUI.LeftMargin + (mzValue - (float)this.massSpectrogramViewModel.DisplayRangeMassMin) * this.xPacket;// Calculate x Plot Coordinate
                this.yt = this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight + (intensity - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                if (this.xt < double.MinValue || this.xt > double.MaxValue || this.yt < double.MinValue || this.yt > double.MaxValue) continue;// Avoid Calculation Error

                if (mzValue > this.massSpectrogramViewModel.DisplayRangeMassMax + 5) break;// Use Data till +5 second beyond   

                this.drawingContext.DrawLine(graphPen, new Point(this.xt, this.yt), new Point(this.xt, this.massSpectrogramWithReferenceUI.BottomMargin + halfDrawHeight + (0 - (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin) * this.yPacket));
            }
        }

        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramWithReferenceUI.LeftMargin, this.massSpectrogramWithReferenceUI.TopMargin - 19));

            this.formattedText = new FormattedText("Actual MS/MS", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Right;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.massSpectrogramWithReferenceUI.RightMargin, this.massSpectrogramWithReferenceUI.TopMargin - 19));

            this.formattedText = new FormattedText("In silico MS/MS", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Right;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.massSpectrogramWithReferenceUI.RightMargin, this.ActualHeight - 17));
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
                xPixelValue = this.massSpectrogramWithReferenceUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.massSpectrogramWithReferenceUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.massSpectrogramWithReferenceUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin), new Point(xPixelValue, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin), new Point(xPixelValue, drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin + this.shortScaleSize));
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
            int xAxisPixelRange = (int)(drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin);
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

        private void drawScaleOnYAxis(float yAxisMinValue, float yAxisMaxValue, float yAxisMinValueReference, float yAxisMaxValueReference, double drawWidth, double drawHeight, MassSpectrogramIntensityMode massSpectrogramIntensityMode, float lowestIntensity, float highestIntensity, float lowestIntensityReference, float highestIntensityReference)
        {
            string yString = ""; // String for Y-Scale Value
            int foldChangeActual = -1, foldChangeRef = -1;
            double yscale_max;
            double yscale_min;

            double yscale_max_reference;
            double yscale_min_reference;

            yscale_max = (double)(((yAxisMaxValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
            yscale_min = (double)(((yAxisMinValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance

            yscale_max_reference = (double)(((yAxisMaxValueReference - lowestIntensityReference) * 100) / (highestIntensityReference - lowestIntensityReference));
            yscale_min_reference = (double)(((yAxisMinValueReference - lowestIntensityReference) * 100) / (highestIntensityReference - lowestIntensityReference));

            if (yscale_max == yscale_min) yscale_max += 0.9;
            if (yscale_max_reference == yscale_min_reference) yscale_max_reference += 0.9;

            // Check Figure of Displayed Max Intensity
            if (yscale_max < 1)
                foldChangeActual = (int)toRoundUp(Math.Log10(yscale_max), 0);
            else
                foldChangeActual = (int)toRoundDown(Math.Log10(yscale_max), 0);

            if (yscale_max < 1)
                foldChangeRef = (int)toRoundUp(Math.Log10(highestIntensityReference), 0);
            else
                foldChangeRef = (int)toRoundDown(Math.Log10(highestIntensityReference), 0);

            
            double yspacket = (float)(((double)(drawHeight - this.massSpectrogramWithReferenceUI.TopMargin - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMarginForLabel - this.massSpectrogramWithReferenceUI.BottomMarginForLabel)) / (yscale_max + yscale_max_reference)); // Packet for Y-Scale For Zooming

            yscale_min_reference = -1 * yscale_max_reference;
            yAxisMinValueReference = -1 * yAxisMaxValueReference;
            getYaxisScaleInterval(yscale_min_reference, yscale_max, drawHeight);
            
            int yStart = (int)(yscale_min_reference / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue, yFold;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = this.massSpectrogramWithReferenceUI.TopMargin + this.massSpectrogramWithReferenceUI.TopMarginForLabel - 1 * (yAxisValue + yscale_min_reference) * yspacket;
                if (yPixelValue > drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin) continue;
                if (yPixelValue < this.massSpectrogramWithReferenceUI.TopMargin - 10) break;

                if (massSpectrogramIntensityMode == MassSpectrogramIntensityMode.Relative)
                    yFold = 1.0; 
                else if (i < 0)
                    yFold = highestIntensityReference * 0.01;
                else yFold = 1.0;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    yAxisValue = yAxisValue * yFold;
                    if (yAxisValue < 0) yAxisValue = -1 * yAxisValue;

                    if (i < 0)
                    {
                        if (foldChangeRef > 3 || foldChangeRef < -1)
                        {
                            yString = (yAxisValue / Math.Pow(10, foldChangeRef)).ToString("f2");
                        }
                        else
                        {
                            if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f2");
                            else yString = yAxisValue.ToString("f3");
                        }
                    }
                    else
                    {
                        if (foldChangeActual > 3 || foldChangeActual < -1)
                        {
                            yString = (yAxisValue / Math.Pow(10, foldChangeActual)).ToString("f2");
                        }
                        else
                        {
                            if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                            else yString = yAxisValue.ToString("f3");
                        }
                    }

                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, yPixelValue));

                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.massSpectrogramWithReferenceUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.massSpectrogramWithReferenceUI.TopMargin - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMarginForLabel - this.massSpectrogramWithReferenceUI.BottomMarginForLabel);
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
            this.drawingContext.DrawText(formattedText, new Point(this.massSpectrogramWithReferenceUI.LeftMargin + 0.5 * (drawWidth - this.massSpectrogramWithReferenceUI.LeftMargin - this.massSpectrogramWithReferenceUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.massSpectrogramWithReferenceUI.TopMargin + 0.5 * (drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin)));
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

            #region
            //if (massSpectrogramIntensityMode == MassSpectrogramIntensityMode.Absolute)
            //{
            //    if (figure > 3)
            //    {
            //        formattedText = new FormattedText("Absolute abundance (1e+" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            //    }
            //    else if (figure < -1)
            //    {
            //        formattedText = new FormattedText("Absolute abundance (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            //    }
            //    else
            //    {
            //        formattedText = new FormattedText("Absolute abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            //    }
            //}
            //else
            //{
            //    formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            //}
            #endregion

            var yPoint = drawHeight * 0.2;
            formattedText = new FormattedText("Relative abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(yPoint, -5));

            formattedText = new FormattedText("Fragment score", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(-yPoint, -5));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.massSpectrogramWithReferenceUI.TopMargin + 0.5 * (drawHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin))));
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

            this.massSpectrogramViewModel.DisplayRangeIntensityMinReference = this.massSpectrogramViewModel.MinIntensityReference;
            this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.MaxIntensityReference;
            this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.MinMassReference;
            this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.MaxMassReference;

            MassSpectrogramDraw();
        }

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y - this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
                return;

            double halfDrawHeight = (this.ActualHeight - this.massSpectrogramWithReferenceUI.BottomMargin - this.massSpectrogramWithReferenceUI.TopMargin) * 0.5;

            if (this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight < this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y && this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight < this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y)
                return;

            // Zoom X-Coordinate        
            if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X > this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X)
            {
                if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X > this.massSpectrogramWithReferenceUI.LeftMargin)
                {
                    if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.massSpectrogramWithReferenceUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacket);
                        this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.DisplayRangeMassMinReference + (float)((this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacketReference);
                    }
                    if (this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X >= this.massSpectrogramWithReferenceUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacket);
                        this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.DisplayRangeMassMinReference + (float)((this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacketReference);
                    }
                }

            }
            else
            {
                if (this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X > this.massSpectrogramWithReferenceUI.LeftMargin)
                {
                    if (this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.massSpectrogramWithReferenceUI.RightMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacket);
                        this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.DisplayRangeMassMinReference + (float)((this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacketReference);
                    }
                    if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X >= this.massSpectrogramWithReferenceUI.LeftMargin)
                    {
                        this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacket);
                        this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.DisplayRangeMassMinReference + (float)((this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacketReference);
                    }
                }
            }

            // Zoom Y-Coordinate  
            double rightYstart;
            double rightYend;

            if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y > this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y)
            {
                if (this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y < this.massSpectrogramWithReferenceUI.TopMargin) rightYstart = this.massSpectrogramWithReferenceUI.TopMargin;
                else rightYstart = this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y;

                if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y > this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight) rightYend = this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight;
                else rightYend = this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y;
            }
            else
            {
                if (this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y < this.massSpectrogramWithReferenceUI.TopMargin) rightYstart = this.massSpectrogramWithReferenceUI.TopMargin;
                else rightYstart = this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y;

                if (this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y > this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight) rightYend = this.massSpectrogramWithReferenceUI.TopMargin + halfDrawHeight;
                else rightYend = this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y;
            }
           
            this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramWithReferenceUI.BottomMargin - halfDrawHeight - rightYstart) / this.yPacket);
            this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.DisplayRangeIntensityMinReference + (float)((this.ActualHeight - this.massSpectrogramWithReferenceUI.BottomMargin - halfDrawHeight - rightYstart) / this.yPacketReference);
            this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.massSpectrogramWithReferenceUI.BottomMargin - halfDrawHeight - rightYend) / this.yPacket);
            this.massSpectrogramViewModel.DisplayRangeIntensityMinReference = this.massSpectrogramViewModel.DisplayRangeIntensityMinReference + (float)((this.ActualHeight - this.massSpectrogramWithReferenceUI.BottomMargin - halfDrawHeight - rightYend) / this.yPacketReference);

        }

        public void GraphScroll()
        {
            if (this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X == -1 || this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.massSpectrogramViewModel.DisplayRangeMassMin == null || this.massSpectrogramViewModel.DisplayRangeMassMax == null)
            {
                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.MinMassReference;
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.MaxMassReference;
            }

            if (this.massSpectrogramViewModel.DisplayRangeIntensityMin == null || this.massSpectrogramViewModel.DisplayRangeIntensityMax == null)
            {
                this.massSpectrogramViewModel.DisplayRangeIntensityMin = this.massSpectrogramViewModel.MinIntensity;
                this.massSpectrogramViewModel.DisplayRangeIntensityMinReference = this.massSpectrogramViewModel.MinIntensityReference;
                this.massSpectrogramViewModel.DisplayRangeIntensityMax = this.massSpectrogramViewModel.MaxIntensity;
                this.massSpectrogramViewModel.DisplayRangeIntensityMaxReference = this.massSpectrogramViewModel.MaxIntensityReference;
            }

            double distanceX = 0;
            double distanceXReference = 0;
            float durationX = (float)this.massSpectrogramViewModel.DisplayRangeMassMax - (float)this.massSpectrogramViewModel.DisplayRangeMassMin;
            float durationXReference = (float)this.massSpectrogramViewModel.DisplayRangeMassMaxReference - (float)this.massSpectrogramViewModel.DisplayRangeMassMinReference;

            float durationY;
            float durationYReference;
            double distanceY = 0;
            double distanceYReference = 0;

            // X-Direction
            if (this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X > this.massSpectrogramWithReferenceUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftButtonEndClickPoint.X;
                distanceXReference = this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X - this.massSpectrogramWithReferenceUI.LeftButtonEndClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMin + (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMinReference + (float)(distanceXReference / this.xPacketReference);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMax + (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMaxReference + (float)(distanceXReference / this.xPacketReference);

                if (this.massSpectrogramViewModel.DisplayRangeMassMax > this.massSpectrogramViewModel.MaxMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MaxMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.MaxMassReference;
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MaxMass - durationX;
                    this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.MaxMassReference - durationXReference;
                }
            }
            else
            {
                distanceX = this.massSpectrogramWithReferenceUI.LeftButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X;
                distanceXReference = this.massSpectrogramWithReferenceUI.LeftButtonEndClickPoint.X - this.massSpectrogramWithReferenceUI.LeftButtonStartClickPoint.X;

                this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMin - (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMinReference - (float)(distanceXReference / this.xPacketReference);
                this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMax - (float)(distanceX / this.xPacket);
                this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramWithReferenceUI.GraphScrollInitialMassMaxReference - (float)(distanceXReference / this.xPacketReference);

                if (this.massSpectrogramViewModel.DisplayRangeMassMin < this.massSpectrogramViewModel.MinMass)
                {
                    this.massSpectrogramViewModel.DisplayRangeMassMin = this.massSpectrogramViewModel.MinMass;
                    this.massSpectrogramViewModel.DisplayRangeMassMinReference = this.massSpectrogramViewModel.MinMassReference;
                    this.massSpectrogramViewModel.DisplayRangeMassMax = this.massSpectrogramViewModel.MinMass + durationX;
                    this.massSpectrogramViewModel.DisplayRangeMassMaxReference = this.massSpectrogramViewModel.MinMassReference + durationXReference;
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
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.X, this.massSpectrogramWithReferenceUI.RightButtonStartClickPoint.Y), new Point(this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.X, this.massSpectrogramWithReferenceUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.massSpectrogramViewModel == null)
                return null;

            float[] peakInformation;
            float mzValue, intensity;

            mzValue = (float)this.massSpectrogramViewModel.DisplayRangeMassMin + (float)((mousePoint.X - this.massSpectrogramWithReferenceUI.LeftMargin) / this.xPacket);
            intensity = (float)this.massSpectrogramViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.massSpectrogramWithReferenceUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { mzValue, intensity };

            return peakInformation;
        }

        private void smilesShow(string smiles, double drawWidth, double drawHeight, double midpoint)
        {
            this.formattedText = new FormattedText(smiles, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Red);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.MaxTextWidth = 200;

            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

            this.drawingContext.DrawText(this.formattedText, new Point(this.xt - this.formattedText.Width * 0.5, midpoint - this.formattedText.Height * 2));

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region

        }

        private void imageShow(BitmapImage image)
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();

            //this.bitmapImage = MoleculeImage.ConvertDrawingImageToBitmap(image);
            this.bitmapImage = image;

            double xStart;
            if (this.massSpectrogramWithReferenceUI.CurrentMousePoint.X < this.ActualWidth * 0.5) xStart = this.massSpectrogramWithReferenceUI.CurrentMousePoint.X;
            else xStart = this.massSpectrogramWithReferenceUI.CurrentMousePoint.X - this.bitmapImage.Width;

            drawingContext.DrawImage(this.bitmapImage, new Rect(xStart, this.massSpectrogramWithReferenceUI.TopMargin, this.bitmapImage.Width, this.bitmapImage.Height));
            //drawingContext.DrawImage(this.bitmapImage, new Rect(xStart, this.massSpectrogramWithReferenceUI.TopMargin, 200, 200));
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
