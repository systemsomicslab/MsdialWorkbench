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
    public class ChromatogramTicEicFE : FrameworkElement
    {
        //ViewModel
        private ChromatogramTicEicViewModel chromatogramTicEicViewModel;

        //UI
        private ChromatogramTicEicUI chromatogramTicEicUI;

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

        public ChromatogramTicEicFE(ChromatogramTicEicViewModel chromatogramMrmBean, ChromatogramTicEicUI ChromatogramTicEicUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.chromatogramTicEicViewModel = chromatogramMrmBean;
            this.chromatogramTicEicUI = ChromatogramTicEicUI;

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
            if (drawWidth < this.chromatogramTicEicUI.LeftMargin + this.chromatogramTicEicUI.RightMargin || drawHeight < this.chromatogramTicEicUI.BottomMargin + this.chromatogramTicEicUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;

            //Bean
            ChromatogramBean chromatogramBean;

            //ObservableCollection
            ObservableCollection<PeakLabelBean> peakLabelBeanCollection = new ObservableCollection<PeakLabelBean>();

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
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.chromatogramTicEicUI.LeftMargin, this.chromatogramTicEicUI.TopMargin), new Size(drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin, drawHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramTicEicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramTicEicUI.BottomMargin), new Point(drawWidth - this.chromatogramTicEicUI.RightMargin, drawHeight - this.chromatogramTicEicUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramTicEicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramTicEicUI.BottomMargin), new Point(this.chromatogramTicEicUI.LeftMargin - this.axisFromGraphArea, this.chromatogramTicEicUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramTicEicViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin) / 10;
                yPacket = (drawHeight - this.chromatogramTicEicUI.TopMargin - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("TIC, EIC, or BPC chromatograms");
                drawCaptionOnAxis(drawWidth, drawHeight, ChromatogramIntensityMode.Relative, 0, 100);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight, ChromatogramIntensityMode.Relative, 0, 100); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 10, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin) / (double)(this.chromatogramTicEicViewModel.DisplayRangeRtMax - this.chromatogramTicEicViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.chromatogramTicEicUI.TopMargin - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMarginForLabel) / (double)(this.chromatogramTicEicViewModel.DisplayRangeIntensityMax - this.chromatogramTicEicViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.chromatogramTicEicViewModel.GraphTitle + ": " + this.chromatogramTicEicViewModel.FileName);
            drawCaptionOnAxis(drawWidth, drawHeight, this.chromatogramTicEicViewModel.IntensityMode, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.chromatogramTicEicViewModel.IntensityMode, this.chromatogramTicEicViewModel.MinIntensity, this.chromatogramTicEicViewModel.MaxIntensity);
            drawScaleOnXAxis((float)this.chromatogramTicEicViewModel.DisplayRangeRtMin, (float)this.chromatogramTicEicViewModel.DisplayRangeRtMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.chromatogramTicEicUI.LeftMargin, this.chromatogramTicEicUI.BottomMargin, drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin, drawHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMargin)));

            // 5. Draw Chromatograms
            # region
            for (int i = 0; i < this.chromatogramTicEicViewModel.ChromatogramBeanCollection.Count; i++)
            {
                if (this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;
                chromatogramBean = this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i];

                // Initialize Graph Plot Start
                pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine                    
                graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
                graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
                graphBrush.Freeze();
                graphPen.Freeze();

                // 6. Plot DataPoint by DataPoint
                #region
                for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++)
                {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[j][0];
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[j][1];
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[j][3];
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[j][2];

                    if (retentionTime < this.chromatogramTicEicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramTicEicUI.LeftMargin + (retentionTime - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramTicEicUI.BottomMargin + (intensity - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                    if (j == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramTicEicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
                }
                #endregion

                // 7. Close Graph Path (When Loop Finish or Display range exceeded)
                #region
                //Final set of chromatogram lines
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, 0.0) });
                pathFigure.Freeze();
                pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
                pathGeometry.Freeze();
                #endregion

                // 8. Set Peak Areas and Labels
                #region
                areaPathFigure = new PathFigure() { StartPoint = new Point(0, drawHeight) }; //Draw peak area
                #endregion

                // 9. Close Area Path
                #region
                areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, drawHeight) });
                areaPathFigure.Freeze();
                areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
                areaPathGeometry.Freeze();
                #endregion

                // 10. Combine graph path and area path
                #region
                combinedGeometry = new CombinedGeometry(pathGeometry.GetFlattenedPathGeometry(), areaPathGeometry.GetFlattenedPathGeometry());  // CombinedGeometry is SLOW
                combinedGeometry.GeometryCombineMode = GeometryCombineMode.Intersect;
                combinedGeometry.Freeze();
                #endregion

                // Draw Chromatogram & Area
                this.drawingContext.DrawGeometry(graphBrush, graphPen, combinedGeometry); // Draw Chromatogram Graph Area   
                this.drawingContext.DrawGeometry(null, graphPen, pathGeometry); // Draw Chromatogram Graph Line  
            }
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

        private PeakLabelBean getPeakLabel(ChromatogramDisplayLabel chromatogramDisplayLabel, PeakAreaBean peakAreaBean, double x, double y, SolidColorBrush solidColorBrush)
        {
            PeakLabelBean peakLabelBean = new PeakLabelBean();
            if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.None)
                peakLabelBean = null;
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakID)
                peakLabelBean = new PeakLabelBean(peakAreaBean.PeakID.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtLeftPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtLeftPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtLeftPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtRightPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtRightPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtRightPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtPeakTop)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtPeakTop.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtPeakTop, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtPeakTop, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveZero)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveZero, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveBaseline)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveBaseline, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakPureValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakPureValue, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.ShapenessValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.ShapenessValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.GauusianSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.GaussianSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.IdealSlopeValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IdealSlopeValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.BasePeakValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.BasePeakValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.SymmetryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.SymmetryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeScoreValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeScoreValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeOrderValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeOrderValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeRatioSimilatiryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeRatioSimilatiryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.RtSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakShapeSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakShapeSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakTopDifferencialValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakTopDifferencialValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.TotalScore)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.TotalScore, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramTicEicViewModel.DisplayLabel == ChromatogramDisplayLabel.AlignedRetentionTime)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AlignedRetentionTime, 2).ToString(), x, y, solidColorBrush);
            return peakLabelBean;
        }

        private void drawGraphTitle(string graphTitle)
        {
            double stringLength = 0;

            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramTicEicUI.LeftMargin, this.chromatogramTicEicUI.TopMargin - 18));

            if (this.chromatogramTicEicViewModel == null) return;
            if (this.chromatogramTicEicViewModel.GraphTitle == "TIC") return;

            stringLength += this.formattedText.Width + 6;

            double textHeight = 0;

            for (int i = 0; i < this.chromatogramTicEicViewModel.ChromatogramBeanCollection.Count; i++)
            {
                if (this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;

                this.formattedText = new FormattedText(this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].MetaboliteName + "; Exact mass: " + this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].Mz + "; Tolerance: " + this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].MassTolerance, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].DisplayBrush);

                this.formattedText.TextAlignment = TextAlignment.Right;
                this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.chromatogramTicEicUI.RightMargin, this.chromatogramTicEicUI.TopMargin + textHeight));

                textHeight += this.formattedText.Height;
            }
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
                xPixelValue = this.chromatogramTicEicUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.chromatogramTicEicUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.chromatogramTicEicUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramTicEicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramTicEicUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.chromatogramTicEicUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramTicEicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramTicEicUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max < min) { max = min + 0.9; }
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin);
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

            double yspacket = (float)(((double)(drawHeight - this.chromatogramTicEicUI.TopMargin - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.chromatogramTicEicUI.BottomMargin - (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.chromatogramTicEicUI.BottomMargin) continue;
                if (yPixelValue < this.chromatogramTicEicUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange <= 0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramTicEicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramTicEicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramTicEicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramTicEicUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramTicEicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.chromatogramTicEicUI.TopMargin - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMarginForLabel);
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
            this.formattedText = new FormattedText("Min.", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramTicEicUI.LeftMargin + 0.5 * (drawWidth - this.chromatogramTicEicUI.LeftMargin - this.chromatogramTicEicUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.chromatogramTicEicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMargin)));
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
                    formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else {
                    formattedText = new FormattedText("Relative Abundance (1e" + (figure - 2).ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                //formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.chromatogramTicEicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.TopMargin))));
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
            if (Math.Abs(this.chromatogramTicEicUI.RightButtonStartClickPoint.X - this.chromatogramTicEicUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.chromatogramTicEicUI.RightButtonStartClickPoint.Y - this.chromatogramTicEicUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.chromatogramTicEicUI.RightButtonStartClickPoint.X - this.chromatogramTicEicUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.chromatogramTicEicUI.RightButtonStartClickPoint.X > this.chromatogramTicEicUI.RightButtonEndClickPoint.X)
            {
                if (this.chromatogramTicEicUI.RightButtonStartClickPoint.X > this.chromatogramTicEicUI.LeftMargin)
                {
                    if (this.chromatogramTicEicUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.chromatogramTicEicUI.RightMargin)
                    {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramTicEicUI.RightButtonStartClickPoint.X - this.chromatogramTicEicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramTicEicUI.RightButtonEndClickPoint.X >= this.chromatogramTicEicUI.LeftMargin)
                    {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramTicEicUI.RightButtonEndClickPoint.X - this.chromatogramTicEicUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.chromatogramTicEicUI.RightButtonEndClickPoint.X > this.chromatogramTicEicUI.LeftMargin)
                {
                    if (this.chromatogramTicEicUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.chromatogramTicEicUI.RightMargin)
                    {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramTicEicUI.RightButtonEndClickPoint.X - this.chromatogramTicEicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramTicEicUI.RightButtonStartClickPoint.X >= this.chromatogramTicEicUI.LeftMargin)
                    {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramTicEicUI.RightButtonStartClickPoint.X - this.chromatogramTicEicUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.chromatogramTicEicUI.RightButtonStartClickPoint.Y > this.chromatogramTicEicUI.RightButtonEndClickPoint.Y)
            {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramTicEicUI.BottomMargin - this.chromatogramTicEicUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.chromatogramTicEicUI.LeftButtonStartClickPoint.X == -1 || this.chromatogramTicEicUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.chromatogramTicEicViewModel.DisplayRangeRtMin == null || this.chromatogramTicEicViewModel.DisplayRangeRtMax == null)
            {
                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
            }

            if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMin == null || this.chromatogramTicEicViewModel.DisplayRangeIntensityMax == null)
            {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
            }

            float durationX = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.chromatogramTicEicUI.LeftButtonStartClickPoint.X > this.chromatogramTicEicUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.chromatogramTicEicUI.LeftButtonStartClickPoint.X - this.chromatogramTicEicUI.LeftButtonEndClickPoint.X;

                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeRtMax > this.chromatogramTicEicViewModel.MaxRt)
                {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MaxRt - durationX;
                }
            }
            else
            {
                distanceX = this.chromatogramTicEicUI.LeftButtonEndClickPoint.X - this.chromatogramTicEicUI.LeftButtonStartClickPoint.X;

                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeRtMin < this.chromatogramTicEicViewModel.MinRt)
                {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MinRt + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin;
            if (this.chromatogramTicEicUI.LeftButtonStartClickPoint.Y < this.chromatogramTicEicUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.chromatogramTicEicUI.LeftButtonEndClickPoint.Y - this.chromatogramTicEicUI.LeftButtonStartClickPoint.Y;

                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMax > this.chromatogramTicEicViewModel.MaxIntensity)
                {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.chromatogramTicEicUI.LeftButtonStartClickPoint.Y - this.chromatogramTicEicUI.LeftButtonEndClickPoint.Y;

                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMin < this.chromatogramTicEicViewModel.MinIntensity)
                {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MinIntensity + durationY;
                }
            }
            ChromatogramDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramTicEicUI.RightButtonStartClickPoint.X, this.chromatogramTicEicUI.RightButtonStartClickPoint.Y), new Point(this.chromatogramTicEicUI.RightButtonEndClickPoint.X, this.chromatogramTicEicUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange()
        {
            this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
            this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
            this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
            this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;

            ChromatogramDraw();
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.chromatogramTicEicViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((mousePoint.X - this.chromatogramTicEicUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.chromatogramTicEicUI.BottomMargin) / this.yPacket);

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
