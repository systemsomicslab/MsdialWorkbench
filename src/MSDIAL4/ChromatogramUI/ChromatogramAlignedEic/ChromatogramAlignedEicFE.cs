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
    public class ChromatogramAlignedEicFE : FrameworkElement
    {
        //ViewModel
        private ChromatogramTicEicViewModel chromatogramTicEicViewModel;

        //UI
        private ChromatogramAlignedEicUI chromatogramAlignedEicUI;

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

        public ChromatogramAlignedEicFE(ChromatogramTicEicViewModel chromatogramMrmBean, ChromatogramAlignedEicUI ChromatogramAlignedEicUI) {
            this.visualCollection = new VisualCollection(this);
            this.chromatogramTicEicViewModel = chromatogramMrmBean;
            this.chromatogramAlignedEicUI = ChromatogramAlignedEicUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void ChromatogramDraw() {
            this.visualCollection.Clear();
            this.drawingVisual = chromatogramDrawingVisual(this.ActualWidth, this.ActualHeight);
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual chromatogramDrawingVisual(double drawWidth, double drawHeight) {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.chromatogramAlignedEicUI.LeftMargin + this.chromatogramAlignedEicUI.RightMargin || drawHeight < this.chromatogramAlignedEicUI.BottomMargin + this.chromatogramAlignedEicUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;
            Pen graphPenPeakEdge;

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
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.chromatogramAlignedEicUI.LeftMargin, this.chromatogramAlignedEicUI.TopMargin), new Size(drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin, drawHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramAlignedEicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramAlignedEicUI.BottomMargin), new Point(drawWidth - this.chromatogramAlignedEicUI.RightMargin, drawHeight - this.chromatogramAlignedEicUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramAlignedEicUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramAlignedEicUI.BottomMargin), new Point(this.chromatogramAlignedEicUI.LeftMargin - this.axisFromGraphArea, this.chromatogramAlignedEicUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramTicEicViewModel == null) {
                // Calculate Packet Size
                xPacket = (drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin) / 10;
                yPacket = (drawHeight - this.chromatogramAlignedEicUI.TopMargin - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMarginForLabel) / 100;

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
            this.xPacket = (drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin) / (double)(this.chromatogramTicEicViewModel.DisplayRangeRtMax - this.chromatogramTicEicViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.chromatogramAlignedEicUI.TopMargin - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMarginForLabel) / (double)(this.chromatogramTicEicViewModel.DisplayRangeIntensityMax - this.chromatogramTicEicViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.chromatogramTicEicViewModel.GraphTitle + "; Exact mass: " + this.chromatogramTicEicViewModel.ChromatogramBeanCollection[0].Mz + "; Tolerance: " + this.chromatogramTicEicViewModel.ChromatogramBeanCollection[0].MassTolerance);
            drawCaptionOnAxis(drawWidth, drawHeight, this.chromatogramTicEicViewModel.IntensityMode, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin, (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.chromatogramTicEicViewModel.IntensityMode, this.chromatogramTicEicViewModel.MinIntensity, this.chromatogramTicEicViewModel.MaxIntensity);
            drawScaleOnXAxis((float)this.chromatogramTicEicViewModel.DisplayRangeRtMin, (float)this.chromatogramTicEicViewModel.DisplayRangeRtMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.chromatogramAlignedEicUI.LeftMargin, this.chromatogramAlignedEicUI.BottomMargin, drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin, drawHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMargin)));

            // 5. Draw Chromatograms
            # region
            for (int i = 0; i < this.chromatogramTicEicViewModel.ChromatogramBeanCollection.Count; i++) {
                if (this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;
                chromatogramBean = this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i];

                // Initialize Graph Plot Start
                pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine                    
                areaPathFigure = new PathFigure(); // PathFigure for GraphArea 
                graphBrush = combineAlphaAndColor(0.1, chromatogramBean.DisplayBrush);// Set Graph Brush
                graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
                graphPenPeakEdge = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness * 1.5); // Set Graph Pen
                graphPenPeakEdge.Freeze();
                graphBrush.Freeze();
                graphPen.Freeze();

                var flagLeft = true;
                var flagRight = true;
                var flagFill = false;

                var isDetected = chromatogramBean.RtPeakRight - chromatogramBean.RtPeakLeft < 0.0001 ? false : true;

                // 6. Plot DataPoint by DataPoint
                #region
                //if (chromatogramTicEicViewModel.ChromatogramBeanCollection[i].GapFilled) {
                for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++) {

                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[j][0];
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[j][1];
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[j][3];
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[j][2];

                    if (retentionTime < this.chromatogramTicEicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramAlignedEicUI.LeftMargin + (retentionTime - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramAlignedEicUI.BottomMargin + (intensity - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });

                    if (Math.Abs(this.xs - this.chromatogramAlignedEicUI.CurrentMousePoint.X) < 3 && Math.Abs(drawHeight  - this.ys - this.chromatogramAlignedEicUI.CurrentMousePoint.Y) < 3) {
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));
                        var filenameText =
                            chromatogramTicEicViewModel.ChromatogramBeanCollection[i].GapFilled == false
                            ? chromatogramBean.FileName + " RT=" + Math.Round(retentionTime, 5)
                            : chromatogramBean.FileName + " RT=" + Math.Round(retentionTime, 5) + " (gap-filled)";

                        formattedText = new FormattedText(filenameText, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, chromatogramBean.DisplayBrush);
                        formattedText.TextAlignment = TextAlignment.Center;

                        this.drawingContext.DrawText(formattedText, new Point(this.xs, drawHeight - this.ys - 20));

                        this.drawingContext.Pop();
                        this.drawingContext.Pop();
                    }

                    if (isDetected) {
                        if (flagFill) {
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                        }
                        if (flagLeft && retentionTime >= chromatogramBean.RtPeakLeft) {
                            areaPathFigure.StartPoint = new Point(this.xs, this.chromatogramAlignedEicUI.BottomMargin + (0 - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket); // PathFigure for GraphLine 
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                            flagFill = true; flagLeft = false;
                        }
                        else if (flagRight && retentionTime >= chromatogramBean.RtPeakRight) {
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.chromatogramAlignedEicUI.BottomMargin + (0 - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket) }); // PathFigure for GraphLine 
                            flagFill = false; flagRight = false;
                        }
                    }
                //    if (Math.Abs(retentionTime - chromatogramBean.RtPeakTop) < 0.0001) this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.5), new Point(this.xs, this.ys), new Point(this.xs, this.chromatogramAlignedEicUI.BottomMargin + (0 - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    if (j == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramTicEicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
                }
                if (isDetected) {
                    areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, 0) }); // PathFigure for GraphLine 
                    areaPathFigure.Freeze();
                    areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
                    areaPathGeometry.Freeze();

                    this.drawingContext.DrawGeometry(graphBrush, graphPenPeakEdge, areaPathGeometry);
                }
                //}
                //else {
                //    for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++) {

                //        scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[j][0];
                //        retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[j][1];
                //        intensity = (float)chromatogramBean.ChromatogramDataPointCollection[j][3];
                //        mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[j][2];

                //        if (retentionTime < this.chromatogramTicEicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                //        this.xs = this.chromatogramAlignedEicUI.LeftMargin + (retentionTime - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                //        this.ys = this.chromatogramAlignedEicUI.BottomMargin + (intensity - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                //        if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                //        pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });

                //        if (Math.Abs(this.xs - this.chromatogramAlignedEicUI.CurrentMousePoint.X) < 3 && Math.Abs(drawHeight - this.ys - this.chromatogramAlignedEicUI.CurrentMousePoint.Y) < 3) {
                //            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                //            this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                //            formattedText = new FormattedText("ND:" + chromatogramBean.FileName, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, chromatogramBean.DisplayBrush);
                //            formattedText.TextAlignment = TextAlignment.Center;

                //            this.drawingContext.DrawText(formattedText, new Point(this.xs, drawHeight - this.ys - 20));

                //            this.drawingContext.Pop();
                //            this.drawingContext.Pop();
                //        }

                //        if (j == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramTicEicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond 
                //    }
                //}
                #endregion

                // 7. Close Graph Path (When Loop Finish or Display range exceeded)
                #region
                //Final set of chromatogram lines
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, 0.0) });
                pathFigure.Freeze();
                pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
                pathGeometry.Freeze();
                #endregion

                // Draw Chromatogram & Area
                this.drawingContext.DrawGeometry(null, graphPen, pathGeometry); // Draw Chromatogram Graph Line  
            }
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

        private PeakLabelBean getPeakLabel(ChromatogramDisplayLabel chromatogramDisplayLabel, PeakAreaBean peakAreaBean, double x, double y, SolidColorBrush solidColorBrush) {
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

        private void drawGraphTitle(string graphTitle) {
            double stringLength = 0;

            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramAlignedEicUI.LeftMargin, this.chromatogramAlignedEicUI.TopMargin - 18));

            if (this.chromatogramTicEicViewModel == null) return;
            if (this.chromatogramTicEicViewModel.GraphTitle == "TIC") return;

            stringLength += this.formattedText.Width + 6;
            /*
            double textHeight = 0;
            if (chromatogramTicEicViewModel.ChromatogramBeanCollection.Count * 15 < this.ActualHeight - this.chromatogramAlignedEicUI.TopMargin - this.chromatogramAlignedEicUI.BottomMargin)
                for (int i = 0; i < this.chromatogramTicEicViewModel.ChromatogramBeanCollection.Count; i++) {
                    if (this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;

                    this.formattedText = new FormattedText(this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].MetaboliteName, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, this.chromatogramTicEicViewModel.ChromatogramBeanCollection[i].DisplayBrush);

                    this.formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.chromatogramAlignedEicUI.RightMargin, this.chromatogramAlignedEicUI.TopMargin + textHeight));

                    textHeight += this.formattedText.Height;
                }
            */
        }

        private void drawScaleOnXAxis(float xAxisMinValue, float xAxisMaxValue, double drawWidth, double drawHeight) {
            getXaxisScaleInterval((double)xAxisMinValue, (double)xAxisMaxValue, drawWidth);
            int xStart = (int)(xAxisMinValue / (double)this.xMinorScale) - 1;
            int xEnd = (int)(xAxisMaxValue / (double)this.xMinorScale) + 1;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++) {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = this.chromatogramAlignedEicUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.chromatogramAlignedEicUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.chromatogramAlignedEicUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramAlignedEicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramAlignedEicUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.chromatogramAlignedEicUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramAlignedEicUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramAlignedEicUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth) {
            if (max < min) { max = min + 0.9; }
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin);
            int xStart, xEnd;
            double xScaleWidth, totalPixelWidth;

            do {
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

        private void drawScaleOnYAxis(float yAxisMinValue, float yAxisMaxValue, double drawWidth, double drawHeight, ChromatogramIntensityMode chromatogramIntensityMode, float lowestIntensity, float highestIntensity) {
            string yString = ""; // String for Y-Scale Value
            int foldChange = -1;
            double yscale_max;
            double yscale_min;

            if (chromatogramIntensityMode == ChromatogramIntensityMode.Absolute) {
                yscale_max = yAxisMaxValue; // Absolute Abundunce
                yscale_min = yAxisMinValue; // Absolute Abundunce
            }
            else {
                yscale_max = (double)(((yAxisMaxValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
                yscale_min = (double)(((yAxisMinValue - lowestIntensity) * 100) / (highestIntensity - lowestIntensity));  // Relative Abundance
            }
            if (yscale_max == yscale_min) yscale_max += 0.9;


            // Check Figure of Displayed Max Intensity
            if (yscale_max < 1) {
                foldChange = (int)toRoundUp(Math.Log10(yscale_max), 0);
            }
            else {
                foldChange = (int)toRoundDown(Math.Log10(yscale_max), 0);
            }

            double yspacket = (float)(((double)(drawHeight - this.chromatogramAlignedEicUI.TopMargin - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++) {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.chromatogramAlignedEicUI.BottomMargin - (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.chromatogramAlignedEicUI.BottomMargin) continue;
                if (yPixelValue < this.chromatogramAlignedEicUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange <= 0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramAlignedEicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramAlignedEicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramAlignedEicUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramAlignedEicUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramAlignedEicUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }
            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight) {
            if (max == min) max += 0.9;
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.chromatogramAlignedEicUI.TopMargin - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMarginForLabel);
            int yStart, yEnd;
            double yScaleHeight, totalPixelWidth;

            do {
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

        private void drawCaptionOnAxis(double drawWidth, double drawHeight, ChromatogramIntensityMode chromatogramIntensityMode, float yAxisMinValue, float yAxisMaxValue) {
            // Set Caption "Min." to X-Axis 
            var xTitle = "Retention time [min]";
            if (this.chromatogramTicEicViewModel != null && this.chromatogramTicEicViewModel.XAxisTitle != null && this.chromatogramTicEicViewModel.XAxisTitle != string.Empty)
                xTitle = this.chromatogramTicEicViewModel.XAxisTitle;

            this.formattedText = new FormattedText(xTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramAlignedEicUI.LeftMargin + 0.5 * (drawWidth - this.chromatogramAlignedEicUI.LeftMargin - this.chromatogramAlignedEicUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.chromatogramAlignedEicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMargin)));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            int figure = -1;

            if (yAxisMinValue >= 0) {
                if (yAxisMaxValue < 1)
                    figure = (int)toRoundUp(Math.Log10(yAxisMaxValue), 0);
                else
                    figure = (int)toRoundDown(Math.Log10(yAxisMaxValue), 0);
            }
            else {
                figure = 0;
            }

            if (chromatogramIntensityMode == ChromatogramIntensityMode.Absolute) {
                if (figure > 3) {
                    formattedText = new FormattedText("Absolute abundance (1e+" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else if (figure < -1) {
                    formattedText = new FormattedText("Absolute abundance (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else {
                    formattedText = new FormattedText("Absolute abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
            }
            else {
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
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.chromatogramAlignedEicUI.TopMargin + 0.5 * (drawHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.TopMargin))));
        }

        private double toRoundUp(double dValue, int iDigits) {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Ceiling(dValue * dCoef) / dCoef :
                                System.Math.Floor(dValue * dCoef) / dCoef;
        }

        private double toRoundDown(double dValue, int iDigits) {
            double dCoef = System.Math.Pow(10, iDigits);

            return dValue > 0 ? System.Math.Floor(dValue * dCoef) / dCoef :
                                System.Math.Ceiling(dValue * dCoef) / dCoef;
        }

        protected static SolidColorBrush combineAlphaAndColor(double opacity, SolidColorBrush baseBrush) {
            Color color = baseBrush.Color;
            SolidColorBrush returnSolidColorBrush;

            // Deal with )pacity
            if (opacity > 1.0)
                opacity = 1.0;

            if (opacity < 0.0)
                opacity = 0.0;

            // Get the Hex value of the Alpha Chanel (Opacity)
            byte a = (byte)(Convert.ToInt32(255 * opacity));

            try {
                byte r = color.R;
                byte g = color.G;
                byte b = color.B;

                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            catch {
                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            return returnSolidColorBrush;
        }

        public void GraphZoom() {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X - this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.chromatogramAlignedEicUI.RightButtonStartClickPoint.Y - this.chromatogramAlignedEicUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X - this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X) / xPacket < 0.01) {
                return;
            }

            // Zoom X-Coordinate        
            if (this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X > this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X) {
                if (this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X > this.chromatogramAlignedEicUI.LeftMargin) {
                    if (this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.chromatogramAlignedEicUI.RightMargin) {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X - this.chromatogramAlignedEicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X >= this.chromatogramAlignedEicUI.LeftMargin) {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X - this.chromatogramAlignedEicUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else {
                if (this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X > this.chromatogramAlignedEicUI.LeftMargin) {
                    if (this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.chromatogramAlignedEicUI.RightMargin) {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X - this.chromatogramAlignedEicUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X >= this.chromatogramAlignedEicUI.LeftMargin) {
                        this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X - this.chromatogramAlignedEicUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.chromatogramAlignedEicUI.RightButtonStartClickPoint.Y > this.chromatogramAlignedEicUI.RightButtonEndClickPoint.Y) {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramAlignedEicUI.BottomMargin - this.chromatogramAlignedEicUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll() {
            if (this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.X == -1 || this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.chromatogramTicEicViewModel.DisplayRangeRtMin == null || this.chromatogramTicEicViewModel.DisplayRangeRtMax == null) {
                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
            }

            if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMin == null || this.chromatogramTicEicViewModel.DisplayRangeIntensityMax == null) {
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
            }

            float durationX = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMax - (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.X > this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.X) {
                distanceX = this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.X - this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.X;

                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramAlignedEicUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramAlignedEicUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeRtMax > this.chromatogramTicEicViewModel.MaxRt) {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MaxRt - durationX;
                }
            }
            else {
                distanceX = this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.X - this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.X;

                this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramAlignedEicUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramAlignedEicUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeRtMin < this.chromatogramTicEicViewModel.MinRt) {
                    this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
                    this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MinRt + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMax - (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin;
            if (this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.Y < this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.Y) {
                distanceY = this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.Y - this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.Y;

                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramAlignedEicUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramAlignedEicUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMax > this.chromatogramTicEicViewModel.MaxIntensity) {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MaxIntensity - durationY;
                }
            }
            else {
                distanceY = this.chromatogramAlignedEicUI.LeftButtonStartClickPoint.Y - this.chromatogramAlignedEicUI.LeftButtonEndClickPoint.Y;

                this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramAlignedEicUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramAlignedEicUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.chromatogramTicEicViewModel.DisplayRangeIntensityMin < this.chromatogramTicEicViewModel.MinIntensity) {
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
                    this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MinIntensity + durationY;
                }
            }
            ChromatogramDraw();
        }

        public void ZoomRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramAlignedEicUI.RightButtonStartClickPoint.X, this.chromatogramAlignedEicUI.RightButtonStartClickPoint.Y), new Point(this.chromatogramAlignedEicUI.RightButtonEndClickPoint.X, this.chromatogramAlignedEicUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange() {
            this.chromatogramTicEicViewModel.DisplayRangeIntensityMin = this.chromatogramTicEicViewModel.MinIntensity;
            this.chromatogramTicEicViewModel.DisplayRangeIntensityMax = this.chromatogramTicEicViewModel.MaxIntensity;
            this.chromatogramTicEicViewModel.DisplayRangeRtMin = this.chromatogramTicEicViewModel.MinRt;
            this.chromatogramTicEicViewModel.DisplayRangeRtMax = this.chromatogramTicEicViewModel.MaxRt;

            ChromatogramDraw();
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint) {
            if (this.chromatogramTicEicViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.chromatogramTicEicViewModel.DisplayRangeRtMin + (float)((mousePoint.X - this.chromatogramAlignedEicUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.chromatogramTicEicViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.chromatogramAlignedEicUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { scanNumber, retentionTime, mzValue, intensity };

            return peakInformation;
        }
        /*
        private void LabelShow(string label, Pen graphPen) {
            while (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(this.visualCollection.Count - 1);

            var xmargine = 12;
            var ymargine = 3;
            var ymargine2 = 5;
            var ymargine3 = 10;
            var fontsize = 30;
            var fontsize2 = 20;
            var fontsize3 = 15;
            var space = 5;

            if (halfDrawHeight - 5 < fontsize * 2 + ymargine * 2 + ymargine2 * 3 + fontsize3 * 2) {
                xmargine = 5;
                ymargine = 3;
                ymargine2 = 3;
                ymargine3 = 5;
                fontsize = 15;
                fontsize2 = 10;
                fontsize3 = 7;
                space = 3;
            }

            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();
            var back = combineAlphaAndColor(0.9, Brushes.White);

            var text = new FormattedText(label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), fontsize, graphPen.Brush);

            var ystart = halfDrawHeight + this.massSpectrogramUI.TopMargin + space;
            var xstart = 0.0;
            if (this.xs + this.labelXDistance + this.massSpectrogramUI.RightMargin + textWidth + xmargine * 2 < this.ActualWidth)
                xstart = this.xs + this.labelXDistance - xmargine;
            else
                xstart = this.xs - this.labelXDistance - textWidth - xmargine;

            drawingContext.DrawRectangle(back, new Pen(Brushes.Red, 1.0), new Rect(xstart, ystart, textWidth + xmargine * 2, fontsize * 2 + fontsize3 * 2 + ymargine * 2 + ymargine2 * 5));
            drawingContext.DrawText(mzFormatted, new Point(xstart + xmargine, ystart + ymargine + ymargine3));
            drawingContext.DrawText(intFormatted, new Point(xstart + xmargine, ystart + fontsize + ymargine + ymargine2 + ymargine3));
            drawingContext.DrawText(mz, new Point(xstart + xmargine + mzWidth + space, ystart + ymargine));
            drawingContext.DrawText(Int, new Point(xstart + xmargine + intWidth + space, ystart + fontsize + ymargine + ymargine2));
            drawingContext.DrawText(ThirdLine, new Point(xstart + xmargine, ystart + fontsize * 2 + ymargine + ymargine2 * 2));
            drawingContext.DrawText(FourthLine, new Point(xstart + xmargine, ystart + fontsize * 2 + fontsize3 + ymargine + ymargine2 * 3));

            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }
        */
        #region // Required Methods for VisualCollection Object
        protected override int VisualChildrenCount {
            get { return visualCollection.Count; }
        }

        protected override Visual GetVisualChild(int index) {
            if (index < 0 || index >= visualCollection.Count) {
                throw new ArgumentOutOfRangeException();
            }
            return visualCollection[index];
        }
        #endregion // Required Methods for VisualCollection Object
    }
}
