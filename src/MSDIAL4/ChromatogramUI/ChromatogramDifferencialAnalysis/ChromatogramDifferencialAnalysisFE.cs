using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class ChromatogramDifferencialAnalysisFE : FrameworkElement
    {
        //ViewModel
        private ChromatogramDifferencialAnalysisViewModel chromatogramDifferencialAnalysisViewModel;

        //UI
        private ChromatogramDifferencialAnalysisUI chromatogramDifferencialAnalysisUI;

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

        public ChromatogramDifferencialAnalysisFE(ChromatogramDifferencialAnalysisViewModel chromatogramDifferencialAnalysisBean, ChromatogramDifferencialAnalysisUI chromatogramDifferencialAnalysisUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.chromatogramDifferencialAnalysisViewModel = chromatogramDifferencialAnalysisBean;
            this.chromatogramDifferencialAnalysisUI = chromatogramDifferencialAnalysisUI;

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
            if (drawWidth < this.chromatogramDifferencialAnalysisUI.LeftMargin + this.chromatogramDifferencialAnalysisUI.RightMargin || drawHeight < this.chromatogramDifferencialAnalysisUI.BottomMargin + this.chromatogramDifferencialAnalysisUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;

            //Bean
            ChromatogramBean chromatogramBean;
            PeakAreaBean peakAreaBean;
            PeakLabelBean peakLabelBean;

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
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin, this.chromatogramDifferencialAnalysisUI.TopMargin), new Size(drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(drawWidth - this.chromatogramDifferencialAnalysisUI.RightMargin, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.axisFromGraphArea, this.chromatogramDifferencialAnalysisUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramDifferencialAnalysisViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin) / 10;
                yPacket = (drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("No Info.");
                drawCaptionOnAxis(drawWidth, drawHeight, ChromatogramIntensityMode.Absolute, 0, 100);
                drawScaleOnYAxis(0, 100, drawWidth, drawHeight, ChromatogramIntensityMode.Absolute, 0, 100); // Draw Y-Axis Scale
                drawScaleOnXAxis(0, 10, drawWidth, drawHeight);

                // Close DrawingContext
                this.drawingContext.Close();

                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin) / (double)(this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax - this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel) / (double)(this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax - this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.chromatogramDifferencialAnalysisViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, this.chromatogramDifferencialAnalysisViewModel.IntensityMode, (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin, (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin, (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.chromatogramDifferencialAnalysisViewModel.IntensityMode, this.chromatogramDifferencialAnalysisViewModel.MinIntensity, this.chromatogramDifferencialAnalysisViewModel.MaxIntensity);                              
            drawScaleOnXAxis((float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin, (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.chromatogramDifferencialAnalysisUI.LeftMargin, this.chromatogramDifferencialAnalysisUI.BottomMargin, drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMargin)));

            // 5. Reference chromatogram
            #region
            chromatogramBean = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean;

            // 5-1. Initialize Graph Plot Start
            #region
            pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine                    
            graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
            graphBrush.Freeze();
            graphPen.Freeze();
            #endregion

            // 5-2. Draw datapoints
            #region
            for (int i = 0; i < chromatogramBean.ChromatogramDataPointCollection.Count; i++)
            {
                scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[i][0];
                retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[i][1];
                intensity = (float)chromatogramBean.ChromatogramDataPointCollection[i][3];
                mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[i][2];

                if (retentionTime < this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (retentionTime - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (intensity - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                if (i == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
            }
            #endregion

            // 5-3. Close Graph Path (When Loop Finish or Display range exceeded)
            #region
            pathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, 0.0) });
            pathFigure.Freeze();
            pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
            pathGeometry.Freeze();
            #endregion

            // 5-4. Draw area information
            #region
            areaPathFigure = new PathFigure() { StartPoint = new Point(0, drawHeight) }; //Draw peak area
            if (chromatogramBean.PeakAreaBeanCollection != null)
            {
                peakLabelBeanCollection = new ObservableCollection<PeakLabelBean>();
                for (int i = 0; i < chromatogramBean.PeakAreaBeanCollection.Count; i++)
                {
                    if (chromatogramBean.PeakAreaBeanCollection[i].RtAtRightPeakEdge < this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) continue;
                    if (chromatogramBean.PeakAreaBeanCollection[i].RtAtLeftPeakEdge > this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax) break;

                    peakAreaBean = chromatogramBean.PeakAreaBeanCollection[i];

                    // Set Top point
                    this.xt = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtPeakTop - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                    this.yt = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtPeakTop - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                    areaTriangleFigure = new PathFigure() { StartPoint = new Point(this.xt, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TriangleSize * 2) };
                    areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt - this.chromatogramDifferencialAnalysisUI.TriangleSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin) });
                    areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt + this.chromatogramDifferencialAnalysisUI.TriangleSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin) });
                    areaTriangleGeometry = new PathGeometry(new PathFigure[] { areaTriangleFigure });

                    if (i == this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId) this.drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);
                    else this.drawingContext.DrawGeometry(Brushes.Blue, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);

                    //Set Labels
                    peakLabelBean = null;
                    if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel != ChromatogramDisplayLabel.ReferenceRt)
                        peakLabelBean = getPeakLabel(this.chromatogramDifferencialAnalysisViewModel.DisplayLabel, peakAreaBean, this.xt, drawHeight - this.yt - this.labelYDistance, chromatogramBean.DisplayBrush);
                    if (peakLabelBean != null) peakLabelBeanCollection.Add(peakLabelBean);

                    if (i != this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId) continue;

                    // Set Start Point on top
                    this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                    this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                    // Set End Point on Bottom
                    this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                    this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                    this.drawingContext.DrawLine(new Pen(Brushes.Red, 2.0), new Point(this.xs, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel), new Point(this.xe, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel));
                    this.drawingContext.DrawRectangle(Brushes.Red, new Pen(Brushes.Gray, 2), new Rect(new Point(this.xs - this.edgeBoxSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel - this.edgeBoxSize), new Point(this.xs + this.edgeBoxSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel + this.edgeBoxSize)));
                    this.drawingContext.DrawRectangle(Brushes.Red, new Pen(Brushes.Gray, 2), new Rect(new Point(this.xe - this.edgeBoxSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel - this.edgeBoxSize), new Point(this.xe + this.edgeBoxSize, drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel + this.edgeBoxSize)));

                    if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.Height)
                    {
                        this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xt, this.yt), new Point(this.xt, this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    }
                    else
                    {
                        if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveZero)
                        {
                            // Set Start Point on top
                            this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                            this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                            // Set End Point on Bottom
                            this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                            this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;
                        }
                        else if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveBaseline)
                        {
                            // Set Start Point on top
                            this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                            this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                            // Set End Point on Bottom
                            this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                            this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;
                        }

                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, drawHeight) });// Set Start Point on Top
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });// Set Start Point on Bottom
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, this.ye) });// Set End Point on Bottom
                        areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, drawHeight) });// Set End Point on Top
                    }
                }

                if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.ReferenceRt)
                {
                    double xrt = this.chromatogramDifferencialAnalysisUI.LeftMargin + (this.chromatogramDifferencialAnalysisViewModel.ReferenceRetentionTime - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                    this.drawingContext.DrawLine(new Pen(Brushes.Green, 1.0), new Point(xrt, this.chromatogramDifferencialAnalysisUI.BottomMargin + ((float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket), new Point(xrt, this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket));
                }

                if (peakLabelBeanCollection.Count != 0)
                {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));
                    FormattedText formattedText;
                    graphBrush = peakLabelBeanCollection[0].LabelBrush;
                    graphBrush.Freeze();

                    for (int k = 0; k < peakLabelBeanCollection.Count; k++)
                    {
                        formattedText = new FormattedText(peakLabelBeanCollection[k].Label, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, graphBrush);
                        formattedText.TextAlignment = TextAlignment.Center;
                        this.drawingContext.DrawText(formattedText, new Point(peakLabelBeanCollection[k].LabelCoordinateX, peakLabelBeanCollection[k].LabelCoordinateY));
                    }

                    peakLabelBeanCollection = new ObservableCollection<PeakLabelBean>();
                    graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
                    graphBrush.Freeze();
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
            #endregion

            // 5-5. Close Area Path
            #region
            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, drawHeight) });
            areaPathFigure.Freeze();
            areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
            areaPathGeometry.Freeze();
            #endregion

            // 5-6. Combine graph path and area path
            #region
            combinedGeometry = new CombinedGeometry(pathGeometry.GetFlattenedPathGeometry(), areaPathGeometry.GetFlattenedPathGeometry());  // CombinedGeometry is SLOW
            combinedGeometry.GeometryCombineMode = GeometryCombineMode.Intersect;
            combinedGeometry.Freeze();
            #endregion

            // Draw Chromatogram & Area
            this.drawingContext.DrawGeometry(graphBrush, graphPen, combinedGeometry); // Draw Chromatogram Graph Area   
            this.drawingContext.DrawGeometry(null, graphPen, pathGeometry); // Draw Chromatogram Graph Line  
            #endregion

            // 6. Sample chromatogram draw
            # region
            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; i++)
            {
                if (this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].IsVisible == false) continue;
                chromatogramBean = this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i];

                // Initialize Graph Plot Start
                pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine                    
                graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
                graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
                graphBrush.Freeze();
                graphPen.Freeze();

                // 6-1. Plot DataPoint by DataPoint
                #region
                for (int j = 0; j < chromatogramBean.ChromatogramDataPointCollection.Count; j++)
                {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[j][0];
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[j][1];
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[j][3];
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[j][2];

                    if (retentionTime < this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (retentionTime - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (intensity - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });
                    if (j == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
                }
                #endregion

                // 6-2. Close Graph Path (When Loop Finish or Display range exceeded)
                #region
                pathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, 0.0) });
                pathFigure.Freeze();
                pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
                pathGeometry.Freeze();
                #endregion

                // 6-3. Set Peak Areas and Labels
                #region
                areaPathFigure = new PathFigure() { StartPoint = new Point(0, drawHeight) }; //Draw peak area
                if (chromatogramBean.PeakAreaBeanCollection != null)
                {
                    for (int j = 0; j < chromatogramBean.PeakAreaBeanCollection.Count; j++)
                    {
                        if (chromatogramBean.PeakAreaBeanCollection[j].RtAtRightPeakEdge < this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) continue;
                        if (chromatogramBean.PeakAreaBeanCollection[j].RtAtLeftPeakEdge > this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax) break;

                        peakAreaBean = chromatogramBean.PeakAreaBeanCollection[j];

                        // Set Top point
                        this.xt = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtPeakTop - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.yt = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtPeakTop - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        if (j != this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId) continue;

                        // Set Start Point on top
                        this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        // Set End Point on Bottom
                        this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.Height)
                        {
                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xt, this.yt), new Point(this.xt, this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket));
                        }
                        else
                        {
                            if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveZero)
                            {
                                // Set Start Point on top
                                this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                                // Set End Point on Bottom
                                this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (0 - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;
                            }
                            else if (this.chromatogramDifferencialAnalysisViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveBaseline)
                            {
                                // Set Start Point on top
                                this.xs = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ys = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;

                                // Set End Point on Bottom
                                this.xe = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ye = this.chromatogramDifferencialAnalysisUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin) * this.yPacket;
                            }

                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, drawHeight) });// Set Start Point on Top
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });// Set Start Point on Bottom
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, this.ye) });// Set End Point on Bottom
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, drawHeight) });// Set End Point on Top
                        }
                    }
                }
                #endregion

                // 6-4. Close Area Path
                #region
                areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(drawWidth, drawHeight) });
                areaPathFigure.Freeze();
                areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
                areaPathGeometry.Freeze();
                #endregion

                // 6-5. Combine graph path and area path
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
            if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.None)
                peakLabelBean = null;
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakID)
                peakLabelBean = new PeakLabelBean(peakAreaBean.PeakID.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtLeftPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtLeftPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtLeftPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtRightPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtRightPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtRightPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtPeakTop)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtPeakTop.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtPeakTop, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtPeakTop, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveZero)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveZero, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveBaseline)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveBaseline, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakPureValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakPureValue, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.ShapenessValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.ShapenessValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.GauusianSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.GaussianSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.IdealSlopeValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IdealSlopeValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.BasePeakValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.BasePeakValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.SymmetryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.SymmetryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeScoreValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeScoreValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeOrderValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeOrderValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeRatioSimilatiryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeRatioSimilatiryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.RtSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakShapeSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakShapeSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakTopDifferencialValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakTopDifferencialValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.TotalScore)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.TotalScore, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramDifferencialAnalysisViewModel.DisplayLabel == ChromatogramDisplayLabel.AnnotatedMetabolite)
                peakLabelBean = new PeakLabelBean(peakAreaBean.MetaboliteName, x, y, solidColorBrush);
            return peakLabelBean;
        }

        private void drawGraphTitle(string graphTitle)
        {
            if (this.chromatogramDifferencialAnalysisViewModel == null) return;

            this.formattedText = new FormattedText("Name: " + graphTitle + "    Transitions: " + this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PrecursorMz.ToString() + " -> " + this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ProductMz.ToString() + "     Reference Chromatogram: " + this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.FileName, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin, this.chromatogramDifferencialAnalysisUI.TopMargin - 17));

            if (this.chromatogramDifferencialAnalysisViewModel.ClassId_SolidColorBrush_Dictionary == null) return;

            double textHeight = 0;
            foreach (var idColor in this.chromatogramDifferencialAnalysisViewModel.ClassId_SolidColorBrush_Dictionary)
            {
                this.formattedText = new FormattedText("Class ID: " + idColor.Key, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, idColor.Value);
                this.formattedText.TextAlignment = TextAlignment.Right;
                this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.chromatogramDifferencialAnalysisUI.RightMargin - 10, this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TopMarginForLabel + textHeight));
                textHeight += this.formattedText.Height + 2;
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
                xPixelValue = this.chromatogramDifferencialAnalysisUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.chromatogramDifferencialAnalysisUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.chromatogramDifferencialAnalysisUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin + this.shortScaleSize));
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
            int xAxisPixelRange = (int)(drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin);
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

            double yspacket = (float)(((double)(drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yAxisMinValue / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yAxisMaxValue / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - (yAxisValue - yAxisMinValue) * yspacket;
                if (yPixelValue > drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin) continue;
                if (yPixelValue < this.chromatogramDifferencialAnalysisUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3 || foldChange < -1) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else
                    {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.chromatogramDifferencialAnalysisUI.TopMargin - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMarginForLabel);
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
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin + 0.5 * (drawWidth - this.chromatogramDifferencialAnalysisUI.LeftMargin - this.chromatogramDifferencialAnalysisUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.chromatogramDifferencialAnalysisUI.TopMargin + 0.5 * (drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMargin)));
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
                    formattedText = new FormattedText("Absolute Abundance (1e+" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else if (figure < -1)
                {
                    formattedText = new FormattedText("Absolute Abundance (1e" + figure.ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else
                {
                    formattedText = new FormattedText("Absolute Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
            }
            else
            {
                formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.chromatogramDifferencialAnalysisUI.TopMargin + 0.5 * (drawHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.TopMargin))));
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
            if (Math.Abs(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X - this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.Y - this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X - this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X > this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X)
            {
                if (this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X > this.chromatogramDifferencialAnalysisUI.LeftMargin)
                {
                    if (this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.chromatogramDifferencialAnalysisUI.RightMargin)
                    {
                        this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin + (float)((this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X >= this.chromatogramDifferencialAnalysisUI.LeftMargin)
                    {
                        this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin + (float)((this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X > this.chromatogramDifferencialAnalysisUI.LeftMargin)
                {
                    if (this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.chromatogramDifferencialAnalysisUI.RightMargin)
                    {
                        this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin + (float)((this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X >= this.chromatogramDifferencialAnalysisUI.LeftMargin)
                    {
                        this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin + (float)((this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.Y > this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.Y)
            {
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin - this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X == -1 || this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin == null || this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax == null)
            {
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.MinRt;
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.MaxRt;
            }

            if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin == null || this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax == null)
            {
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.MinIntensity;
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.MaxIntensity;
            }

            float durationX = (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X > this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.X;

                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax > this.chromatogramDifferencialAnalysisViewModel.MaxRt)
                {
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.MaxRt;
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.MaxRt - durationX;
                }
            }
            else
            {
                distanceX = this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.X - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X;

                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin < this.chromatogramDifferencialAnalysisViewModel.MinRt)
                {
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.MinRt;
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.MinRt + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin;
            if (this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y < this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.Y - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y;

                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax > this.chromatogramDifferencialAnalysisViewModel.MaxIntensity)
                {
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.MaxIntensity;
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y - this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.Y;

                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin < this.chromatogramDifferencialAnalysisViewModel.MinIntensity)
                {
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.MinIntensity;
                    this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.MinIntensity + durationY;
                }
            }
            ChromatogramDraw();
        }

        public void PeakLeftEdgeClickCheck()
        {
            //int targetTransitionIndex = this.chromatogramDifferencialAnalysisViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float leftEdgeRt = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].RtAtLeftPeakEdge;
            //int leftEdgeInt = this.chromatogramDifferencialAnalysisViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtLeftPeakEdge;

            Point leftEdgePoint = new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin + (leftEdgeRt - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket, this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TopMarginForLabel);

            if (Math.Abs(leftEdgePoint.X - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(leftEdgePoint.Y - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y) < 5) this.chromatogramDifferencialAnalysisUI.LeftMouseButtonLeftEdgeCapture = true;
            else this.chromatogramDifferencialAnalysisUI.LeftMouseButtonLeftEdgeCapture = false;
        }

        public void PeakRightEdgeClickCheck()
        {
            //int targetTransitionIndex = this.chromatogramDifferencialAnalysisViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float rightEdgeRt = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].RtAtRightPeakEdge;
            //int rightEdgeInt = this.chromatogramDifferencialAnalysisViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtRightPeakEdge;

            Point rightEdgePoint = new Point(this.chromatogramDifferencialAnalysisUI.LeftMargin + (rightEdgeRt - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket, this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TopMarginForLabel);

            if (Math.Abs(rightEdgePoint.X - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(rightEdgePoint.Y - this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.Y) < 5) this.chromatogramDifferencialAnalysisUI.LeftMouseButtonRightEdgeCapture = true;
            else this.chromatogramDifferencialAnalysisUI.LeftMouseButtonRightEdgeCapture = false;
        }

        public void PeakLeftEdgeEdit()
        {
            this.chromatogramDifferencialAnalysisUI.LeftMouseButtonLeftEdgeCapture = false;

            //int targetTransitionIndex = this.chromatogramDifferencialAnalysisViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId;

            float newLeftEdgeRt = getDataPositionOnMousePoint(this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint)[1];

            int minimumRtDeviIndex = -1;
            float minimumRtDevi = float.MaxValue, rtDevi;
            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi = Math.Abs((float)this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1] - newLeftEdgeRt);
                if (minimumRtDevi > rtDevi) { minimumRtDevi = rtDevi; minimumRtDeviIndex = i; }
            }

            if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].ScanNumberAtRightPeakEdge < minimumRtDeviIndex) { return; }

            int leftPeakEdgeScanNumber = minimumRtDeviIndex;
            int rightPeakEdgeScanNumber = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].ScanNumberAtRightPeakEdge;

            List<double[]> datapoints = new List<double[]>();
            double maxintensity = double.MinValue;
            int maxIndex = -1;
            for (int i = leftPeakEdgeScanNumber; i <= rightPeakEdgeScanNumber; i++)
            {
                if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][0], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][2], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            PeakDetectionResult detectedPeakInformationBean = PeakDetection.GetPeakDetectionResult(datapoints, maxIndex - (int)datapoints[0][0]);

            if (detectedPeakInformationBean == null) return;

            PeakAreaBean peakAreaBean = getPeakAreaBean(detectedPeakInformationBean);
            this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
            this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
            setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection);

            PeakDetectionResult sampleDetectedPeakInformationBean;
            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; i++)
            {
                sampleDetectedPeakInformationBean = PeakDetection.DataDependendPeakDetection(new ObservableCollection<PeakDetectionResult>() { detectedPeakInformationBean }, this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].ChromatogramDataPointCollection.ToList())[0];
                peakAreaBean = getPeakAreaBean(sampleDetectedPeakInformationBean);
                this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
                this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
                setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection);
            }

            Mouse.OverrideCursor = null;
        }

        public void PeakRightEdgeEdit()
        {
            this.chromatogramDifferencialAnalysisUI.LeftMouseButtonRightEdgeCapture = false;

            //int targetTransitionIndex = this.chromatogramDifferencialAnalysisViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId;

            float newRightEdgeRt = getDataPositionOnMousePoint(this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint)[1];

            int minimumRtDeviIndex = -1;
            float minimumRtDevi = float.MaxValue, rtDevi;
            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi = Math.Abs((float)this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1] - newRightEdgeRt);
                if (minimumRtDevi > rtDevi) { minimumRtDevi = rtDevi; minimumRtDeviIndex = i; }
            }

            if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].ScanNumberAtLeftPeakEdge > minimumRtDeviIndex) { return; }

            int leftPeakEdgeScanNumber = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[selectedPeakId].ScanNumberAtLeftPeakEdge;
            int rightPeakEdgeScanNumber = minimumRtDeviIndex;

            List<double[]> datapoints = new List<double[]>();
            double maxintensity = double.MinValue;
            int maxIndex = -1;
            for (int i = leftPeakEdgeScanNumber; i <= rightPeakEdgeScanNumber; i++)
            {
                if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][0], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][2], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            PeakDetectionResult detectedPeakInformationBean = PeakDetection.GetPeakDetectionResult(datapoints, maxIndex - (int)datapoints[0][0]);

            if (detectedPeakInformationBean == null) return;

            PeakAreaBean peakAreaBean = getPeakAreaBean(detectedPeakInformationBean);
            this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
            this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
            setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection);

            PeakDetectionResult sampleDetectedPeakInformationBean;
            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; i++)
            {
                sampleDetectedPeakInformationBean = PeakDetection.DataDependendPeakDetection(new ObservableCollection<PeakDetectionResult>() { detectedPeakInformationBean }, this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].ChromatogramDataPointCollection.ToList())[0];
                peakAreaBean = getPeakAreaBean(sampleDetectedPeakInformationBean);
                this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
                this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
                setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection);
            }
            Mouse.OverrideCursor = null;
        }

        public void PeakNewEdit()
        {
            //int targetTransitionIndex = this.chromatogramDifferencialAnalysisViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId;

            float rt1 = getDataPositionOnMousePoint(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint)[1];
            float rt2 = getDataPositionOnMousePoint(this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint)[1];

            int minScan1 = -1, minScan2 = -1;
            float minimumRtDevi1 = float.MaxValue, minimumRtDevi2 = float.MaxValue, rtDevi1, rtDevi2;

            for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi1 = Math.Abs((float)this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1] - rt1);
                rtDevi2 = Math.Abs((float)this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1] - rt2);
                if (minimumRtDevi1 > rtDevi1) { minimumRtDevi1 = rtDevi1; minScan1 = i; }
                if (minimumRtDevi2 > rtDevi2) { minimumRtDevi2 = rtDevi2; minScan2 = i; }
            }

            int leftPeakEdgeScanNumber = Math.Min(minScan1, minScan2);
            int rightPeakEdgeScanNumber = Math.Max(minScan1, minScan2);

            List<double[]> datapoints = new List<double[]>();
            double maxintensity = double.MinValue;
            int maxIndex = -1;
            for (int i = leftPeakEdgeScanNumber; i <= rightPeakEdgeScanNumber; i++)
            {
                if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][0], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][1], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][2], this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;

            //already included check
            bool deleteCheck = false;
            if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection != null)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Count; i++)
                {
                    if ((int)datapoints[0][0] < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[i].ScanNumberAtPeakTop && this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[i].ScanNumberAtPeakTop < datapoints[datapoints.Count - 1][0])
                    {
                        deleteCheck = true;
                        peakDelete(this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection, i);
                        for (int j = 0; j < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; j++)
                        {
                            peakDelete(this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[j].PeakAreaBeanCollection, i);
                        }
                        i--;
                        if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Count == 0) break;
                    }
                }
                Mouse.OverrideCursor = null;
            }
            if (deleteCheck) return;

            PeakDetectionResult detectedPeakInformationBean = PeakDetection.GetPeakDetectionResult(datapoints, maxIndex - (int)datapoints[0][0]);

            if (detectedPeakInformationBean == null) return;

            PeakAreaBean peakAreaBean = getPeakAreaBean(detectedPeakInformationBean);
            if (this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection == null || this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Count == 0)
            {
                this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
                this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Add(peakAreaBean);
                setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection);

                PeakDetectionResult sampleDetectedPeakInformationBean;
                for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; i++)
                {
                    sampleDetectedPeakInformationBean = PeakDetection.DataDependendPeakDetection(new ObservableCollection<PeakDetectionResult>() { detectedPeakInformationBean }, this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].ChromatogramDataPointCollection.ToList())[0];
                    peakAreaBean = getPeakAreaBean(sampleDetectedPeakInformationBean);
                    this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
                    this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.Add(peakAreaBean);
                    setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection);

                }
            }
            else
            {
                int rtOrder = 0;
                for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Count; i++)
                    if (peakAreaBean.ScanNumberAtPeakTop > this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection[i].ScanNumberAtPeakTop) rtOrder++;
                this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection.Insert(rtOrder, peakAreaBean);
                setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean.PeakAreaBeanCollection);

                PeakDetectionResult sampleDetectedPeakInformationBean;
                for (int i = 0; i < this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection.Count; i++)
                {
                    sampleDetectedPeakInformationBean = PeakDetection.DataDependendPeakDetection(new ObservableCollection<PeakDetectionResult>() { detectedPeakInformationBean }, this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].ChromatogramDataPointCollection.ToList())[0];
                    peakAreaBean = getPeakAreaBean(sampleDetectedPeakInformationBean);
                    this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection.Insert(rtOrder, peakAreaBean);
                    setPeakIdAmplitudeScoreOfPeakAreaBeanCollection(this.chromatogramDifferencialAnalysisViewModel.SampleChromatogramBeanCollection[i].PeakAreaBeanCollection);
                }
            }
        }

        public void SelectedPeakChange()
        {
            if (this.chromatogramDifferencialAnalysisUI.CurrentMousePoint.Y > this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TriangleSize * 2) return;

            //Bean
            ChromatogramBean chromatogramBean;
            PeakAreaBean peakAreaBean;

            bool doubleClickChecker = false;

            chromatogramBean = this.chromatogramDifferencialAnalysisViewModel.ReferenceChromatogramBean;
            if (chromatogramBean.PeakAreaBeanCollection == null) return;

            for (int j = 0; j < chromatogramBean.PeakAreaBeanCollection.Count; j++)
            {
                if (chromatogramBean.PeakAreaBeanCollection[j].RtAtRightPeakEdge < this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) continue;
                if (chromatogramBean.PeakAreaBeanCollection[j].RtAtLeftPeakEdge > this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax) break;

                peakAreaBean = chromatogramBean.PeakAreaBeanCollection[j];

                // Set Top point
                this.xt = this.chromatogramDifferencialAnalysisUI.LeftMargin + (peakAreaBean.RtAtPeakTop - (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin) * this.xPacket;

                if (this.xt - this.chromatogramDifferencialAnalysisUI.TriangleSize < this.chromatogramDifferencialAnalysisUI.CurrentMousePoint.X && this.xt + this.chromatogramDifferencialAnalysisUI.TriangleSize > this.chromatogramDifferencialAnalysisUI.CurrentMousePoint.X)
                {
                    if (j != this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId)
                    {
                        this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId = j;
                        doubleClickChecker = true;
                        break;
                    }
                }
            }
            if (doubleClickChecker) ChromatogramDraw();
        }

        public void NotDetectedEdit()
        {
            this.chromatogramDifferencialAnalysisViewModel.SelectedPeakId = -1;
            ChromatogramDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X, this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.Y), new Point(this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X, this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void NewPeakGenerateRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramDifferencialAnalysisUI.RightButtonStartClickPoint.X, this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(this.chromatogramDifferencialAnalysisUI.RightButtonEndClickPoint.X, this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void PeakEdgeEditRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramDifferencialAnalysisUI.LeftButtonStartClickPoint.X, this.ActualHeight - this.chromatogramDifferencialAnalysisUI.BottomMargin), new Point(this.chromatogramDifferencialAnalysisUI.LeftButtonEndClickPoint.X, this.chromatogramDifferencialAnalysisUI.TopMargin + this.chromatogramDifferencialAnalysisUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange()
        {
            this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin = this.chromatogramDifferencialAnalysisViewModel.MinIntensity;
            this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMax = this.chromatogramDifferencialAnalysisViewModel.MaxIntensity;
            this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin = this.chromatogramDifferencialAnalysisViewModel.MinRt;
            this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMax = this.chromatogramDifferencialAnalysisViewModel.MaxRt;

            ChromatogramDraw();
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.chromatogramDifferencialAnalysisViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeRtMin + (float)((mousePoint.X - this.chromatogramDifferencialAnalysisUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.chromatogramDifferencialAnalysisViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.chromatogramDifferencialAnalysisUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { scanNumber, retentionTime, mzValue, intensity };

            return peakInformation;
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

        private float getPosteriorOneTransition(float intensityScore, float rtScore)
        {
            double disScore = this.oneTransitionCoefficient[0] + this.oneTransitionCoefficient[1] * intensityScore + this.oneTransitionCoefficient[2] * rtScore;
            return getPosterior(disScore);
        }

        private float getPosteriorMultiTransitionWithoutRaio(float intensityScore, float rtScore, float shapeScore, float coeluteScore)
        {
            double disScore;
            disScore = this.multiTransitionCoefficientWithoutRatio[0]
                + this.multiTransitionCoefficientWithoutRatio[1] * intensityScore
                + this.multiTransitionCoefficientWithoutRatio[2] * rtScore
                + this.multiTransitionCoefficientWithoutRatio[3] * shapeScore
                + this.multiTransitionCoefficientWithoutRatio[4] * Math.Exp(-0.5 * Math.Pow(coeluteScore / 0.05, 2));
            return getPosterior(disScore);
        }

        private float getPosteriorMultiTransitionWithRatio(float intensityScore, float rtScore, float ratioScore, float shapeScore, float coeluteScore)
        {
            double disScore;
            disScore = this.multiTransitionCoefficientWithRatio[0]
                + this.multiTransitionCoefficientWithRatio[1] * intensityScore
                + this.multiTransitionCoefficientWithRatio[2] * rtScore
                + this.multiTransitionCoefficientWithRatio[3] * ratioScore
                + this.multiTransitionCoefficientWithRatio[4] * shapeScore
                + this.multiTransitionCoefficientWithRatio[5] * Math.Exp(-0.5 * Math.Pow(coeluteScore / 0.05, 2));
            return getPosterior(disScore);
        }

        private float getPosterior(double discriminantScore)
        {
            double posterior = 100 * (1 - 1 / (1 + Math.Exp(-1 * discriminantScore)));
            return (float)posterior;
        }

        private PeakAreaBean getPeakAreaBean(PeakDetectionResult detectedPeakInformationBean)
        {
            PeakAreaBean peakAreaBean = new PeakAreaBean()
            {
                AmplitudeOrderValue = detectedPeakInformationBean.AmplitudeOrderValue,
                AmplitudeRatioSimilatiryValue = -1,
                AmplitudeScoreValue = detectedPeakInformationBean.AmplitudeScoreValue,
                AreaAboveBaseline = detectedPeakInformationBean.AreaAboveBaseline,
                AreaAboveZero = detectedPeakInformationBean.AreaAboveZero,
                BasePeakValue = detectedPeakInformationBean.BasePeakValue,
                GaussianSimilarityValue = detectedPeakInformationBean.GaussianSimilarityValue,
                IdealSlopeValue = detectedPeakInformationBean.IdealSlopeValue,
                IntensityAtLeftPeakEdge = detectedPeakInformationBean.IntensityAtLeftPeakEdge,
                IntensityAtPeakTop = detectedPeakInformationBean.IntensityAtPeakTop,
                IntensityAtRightPeakEdge = detectedPeakInformationBean.IntensityAtRightPeakEdge,
                PeakID = detectedPeakInformationBean.PeakID,
                PeakPureValue = detectedPeakInformationBean.PeakPureValue,
                PeakShapeSimilarityValue = -1,
                PeakTopDifferencialValue = -1,
                RtAtLeftPeakEdge = detectedPeakInformationBean.RtAtLeftPeakEdge,
                RtAtPeakTop = detectedPeakInformationBean.RtAtPeakTop,
                RtAtRightPeakEdge = detectedPeakInformationBean.RtAtRightPeakEdge,
                RtSimilarityValue = -1,
                ScanNumberAtLeftPeakEdge = detectedPeakInformationBean.ScanNumAtLeftPeakEdge,
                ScanNumberAtPeakTop = detectedPeakInformationBean.ScanNumAtPeakTop,
                ScanNumberAtRightPeakEdge = detectedPeakInformationBean.ScanNumAtRightPeakEdge,
                ShapenessValue = -1,
                SymmetryValue = detectedPeakInformationBean.SymmetryValue,
                TotalScore = -1
            };
            return peakAreaBean;
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
