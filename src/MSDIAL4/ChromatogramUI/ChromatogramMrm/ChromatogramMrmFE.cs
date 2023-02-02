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
    public class ChromatogramMrmFE : FrameworkElement
    {
        //ViewModel
        private ChromatogramMrmViewModel chromatogramMrmViewModel;

        //UI
        private ChromatogramMrmUI chromatogramMrmUI;

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

        public ChromatogramMrmFE(ChromatogramMrmViewModel chromatogramMrmBean, ChromatogramMrmUI chromatogramMrmUI) 
        {
            this.visualCollection = new VisualCollection(this);
            this.chromatogramMrmViewModel = chromatogramMrmBean;
            this.chromatogramMrmUI = chromatogramMrmUI;

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
            if (drawWidth < this.chromatogramMrmUI.LeftMargin + this.chromatogramMrmUI.RightMargin || drawHeight < this.chromatogramMrmUI.BottomMargin + this.chromatogramMrmUI.TopMargin) return drawingVisual;
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
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.chromatogramMrmUI.LeftMargin, this.chromatogramMrmUI.TopMargin), new Size(drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin, drawHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramMrmUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramMrmUI.BottomMargin), new Point(drawWidth - this.chromatogramMrmUI.RightMargin, drawHeight - this.chromatogramMrmUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramMrmUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.chromatogramMrmUI.BottomMargin), new Point(this.chromatogramMrmUI.LeftMargin - this.axisFromGraphArea, this.chromatogramMrmUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramMrmViewModel == null)
            {
                // Calculate Packet Size
                xPacket = (drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin) / 10;
                yPacket = (drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMarginForLabel) / 100;

                // Draw Graph Title, Y scale, X scale
                drawGraphTitle("MS2 chromatograms: ");
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
            this.xPacket = (drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin) / (double)(this.chromatogramMrmViewModel.DisplayRangeRtMax - this.chromatogramMrmViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMarginForLabel) / (double)(this.chromatogramMrmViewModel.DisplayRangeIntensityMax - this.chromatogramMrmViewModel.DisplayRangeIntensityMin);
            #endregion

            // 4. Draw graph title, x axis, y axis, and its captions
            #region
            drawGraphTitle(this.chromatogramMrmViewModel.GraphTitle);
            drawCaptionOnAxis(drawWidth, drawHeight, this.chromatogramMrmViewModel.IntensityMode, (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin, (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMax);
            drawScaleOnYAxis((float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin, (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMax, drawWidth, drawHeight, this.chromatogramMrmViewModel.IntensityMode, this.chromatogramMrmViewModel.MinIntensity, this.chromatogramMrmViewModel.MaxIntensity);                              
            drawScaleOnXAxis((float)this.chromatogramMrmViewModel.DisplayRangeRtMin, (float)this.chromatogramMrmViewModel.DisplayRangeRtMax, drawWidth, drawHeight);
            #endregion

            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.chromatogramMrmUI.LeftMargin, this.chromatogramMrmUI.BottomMargin, drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin, drawHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMargin)));

            // 5. Draw Chromatograms
            # region
            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count; i++)
            {
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;
                chromatogramBean = this.chromatogramMrmViewModel.ChromatogramBeanCollection[i];

                // Initialize Graph Plot Start
                pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0)}; // PathFigure for GraphLine                    
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

                    if (retentionTime < this.chromatogramMrmViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.chromatogramMrmUI.LeftMargin + (retentionTime - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.chromatogramMrmUI.BottomMargin + (intensity - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate
                    
                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys)} );
                    if (j == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramMrmViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond    
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
                if (chromatogramBean.PeakAreaBeanCollection != null && this.chromatogramMrmViewModel.TargetTransitionIndex == i)
                {
                    peakLabelBeanCollection = new ObservableCollection<PeakLabelBean>();
                    for (int j = 0; j < chromatogramBean.PeakAreaBeanCollection.Count; j++)
                    {
                        if (chromatogramBean.PeakAreaBeanCollection[j].RtAtRightPeakEdge < this.chromatogramMrmViewModel.DisplayRangeRtMin) continue;
                        if (chromatogramBean.PeakAreaBeanCollection[j].RtAtLeftPeakEdge > this.chromatogramMrmViewModel.DisplayRangeRtMax) break;

                        peakAreaBean = chromatogramBean.PeakAreaBeanCollection[j];

                        // Set Top point
                        this.xt = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtPeakTop - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.yt = this.chromatogramMrmUI.BottomMargin + (peakAreaBean.IntensityAtPeakTop - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        areaTriangleFigure = new PathFigure() { StartPoint = new Point(this.xt, drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.TriangleSize * 2) };
                        areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt - this.chromatogramMrmUI.TriangleSize, drawHeight - this.chromatogramMrmUI.TopMargin) });
                        areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt + this.chromatogramMrmUI.TriangleSize, drawHeight - this.chromatogramMrmUI.TopMargin) });
                        areaTriangleGeometry = new PathGeometry(new PathFigure[] { areaTriangleFigure });

                        if (j == this.chromatogramMrmViewModel.SelectedPeakId) this.drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);
                        else this.drawingContext.DrawGeometry(Brushes.Blue, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);

                        //Set Labels
                        peakLabelBean = null;
                        if(this.chromatogramMrmViewModel.DisplayLabel != ChromatogramDisplayLabel.ReferenceRt)
                            peakLabelBean = getPeakLabel(this.chromatogramMrmViewModel.DisplayLabel, peakAreaBean, this.xt, drawHeight - this.yt - this.labelYDistance, chromatogramBean.DisplayBrush);
                        if (peakLabelBean != null) peakLabelBeanCollection.Add(peakLabelBean);

                        if (j != this.chromatogramMrmViewModel.SelectedPeakId) continue;

                        // Set Start Point on top
                        this.xs = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.ys = this.chromatogramMrmUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        // Set End Point on Bottom
                        this.xe = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.ye = this.chromatogramMrmUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;

                        this.drawingContext.DrawRectangle(Brushes.Red, new Pen(Brushes.Gray, 2), new Rect(new Point(this.xs - this.edgeBoxSize, this.ys - this.edgeBoxSize), new Point(this.xs + this.edgeBoxSize, this.ys + this.edgeBoxSize)));
                        this.drawingContext.DrawRectangle(Brushes.Red, new Pen(Brushes.Gray, 2), new Rect(new Point(this.xe - this.edgeBoxSize, this.ye - this.edgeBoxSize), new Point(this.xe + this.edgeBoxSize, this.ye + this.edgeBoxSize)));

                        if (this.chromatogramMrmViewModel.QuantitativeMode == ChromatogramQuantitativeMode.Height)
                        {
                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xt, this.yt), new Point(this.xt, this.chromatogramMrmUI.BottomMargin + (0 - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket));
                        }
                        else
                        {
                            if (this.chromatogramMrmViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveZero)
                            {
                                // Set Start Point on top
                                this.xs = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ys = this.chromatogramMrmUI.BottomMargin + (0 - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;

                                // Set End Point on Bottom
                                this.xe = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ye = this.chromatogramMrmUI.BottomMargin + (0 - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;
                            }
                            else if (this.chromatogramMrmViewModel.QuantitativeMode == ChromatogramQuantitativeMode.AreaAboveBaseline)
                            {
                                // Set Start Point on top
                                this.xs = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtLeftPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ys = this.chromatogramMrmUI.BottomMargin + (peakAreaBean.IntensityAtLeftPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;

                                // Set End Point on Bottom
                                this.xe = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtRightPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                                this.ye = this.chromatogramMrmUI.BottomMargin + (peakAreaBean.IntensityAtRightPeakEdge - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket;
                            }

                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, drawHeight) });// Set Start Point on Top
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xs, this.ys) });// Set Start Point on Bottom
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, this.ye) });// Set End Point on Bottom
                            areaPathFigure.Segments.Add(new LineSegment() { Point = new Point(this.xe, drawHeight) });// Set End Point on Top
                        }
                    }

                    if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.ReferenceRt)
                    {
                        double xrt = this.chromatogramMrmUI.LeftMargin + (this.chromatogramMrmViewModel.ReferenceRetentionTime - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                        this.drawingContext.DrawLine(new Pen(Brushes.Green, 1.0), new Point(xrt, this.chromatogramMrmUI.BottomMargin + ((float)this.chromatogramMrmViewModel.DisplayRangeIntensityMax - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket), new Point(xrt, this.chromatogramMrmUI.BottomMargin + (0 - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket));
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

                // Set Top point
                this.xt = this.chromatogramMrmUI.LeftMargin + (this.chromatogramMrmViewModel.ReferenceRetentionTime - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;
                this.yt = drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.TopMarginForLabel;

                areaTriangleFigure = new PathFigure() { StartPoint = new Point(this.xt, drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.TriangleSize * 2) };
                areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt - this.chromatogramMrmUI.TriangleSize, drawHeight - this.chromatogramMrmUI.TopMargin) });
                areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt + this.chromatogramMrmUI.TriangleSize, drawHeight - this.chromatogramMrmUI.TopMargin) });
                areaTriangleGeometry = new PathGeometry(new PathFigure[] { areaTriangleFigure });

                this.drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);

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
            if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.None)
                peakLabelBean = null;
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakID)
                peakLabelBean = new PeakLabelBean(peakAreaBean.PeakID.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtLeftPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtLeftPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtLeftPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtLeftPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtRightPeakEdge.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtRightPeakEdge, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtRightPeakEdge)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtRightPeakEdge, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.ScanNumAtPeakTop)
                peakLabelBean = new PeakLabelBean(peakAreaBean.ScanNumberAtPeakTop.ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.RtAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtAtPeakTop, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.IntensityAtPeakTop)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IntensityAtPeakTop, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveZero)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveZero, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AreaAboveBaseline)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AreaAboveBaseline, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakPureValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakPureValue, 0).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.ShapenessValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.ShapenessValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.GauusianSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.GaussianSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.IdealSlopeValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.IdealSlopeValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.BasePeakValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.BasePeakValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.SymmetryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.SymmetryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeScoreValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeScoreValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeOrderValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeOrderValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AmplitudeRatioSimilatiryValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AmplitudeRatioSimilatiryValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.RtSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.RtSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakShapeSimilarityValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakShapeSimilarityValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.PeakTopDifferencialValue)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.PeakTopDifferencialValue, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.TotalScore)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.TotalScore, 2).ToString(), x, y, solidColorBrush);
            else if (this.chromatogramMrmViewModel.DisplayLabel == ChromatogramDisplayLabel.AlignedRetentionTime)
                peakLabelBean = new PeakLabelBean(Math.Round((double)peakAreaBean.AlignedRetentionTime, 2).ToString(), x, y, solidColorBrush);
            return peakLabelBean;
        }

        private void drawGraphTitle(string graphTitle)
        {
            double stringLength = 0;
            if (this.chromatogramMrmViewModel == null || this.chromatogramMrmViewModel.ChromatogramBeanCollection == null || this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count == 0) return;

            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[0].PrecursorMz < 0)
                this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            else
                this.formattedText = new FormattedText(graphTitle + "Precursor: ", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin, this.chromatogramMrmUI.TopMargin - 18));


            stringLength += this.formattedText.Width + 6;

            //target transition
            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[0].PrecursorMz > 0)
            {
                this.formattedText = new FormattedText(this.chromatogramMrmViewModel.ChromatogramBeanCollection[0].PrecursorMz.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, this.chromatogramMrmViewModel.ChromatogramBeanCollection[0].DisplayBrush);
                this.formattedText.TextAlignment = TextAlignment.Left;
                this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin + stringLength, this.chromatogramMrmUI.TopMargin - 17));
            }
            stringLength += this.formattedText.Width + 6;

            double textHeight = 0;
            bool checker = false;

            this.formattedText = new FormattedText("Raw chrom.", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Right;
            this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.chromatogramMrmUI.RightMargin, this.chromatogramMrmUI.TopMargin + textHeight));

            textHeight += this.formattedText.Height;

            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count; i++)
            {
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].IsVisible == false) continue;
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].LineTickness > 1 && checker == false)
                {
                    textHeight = 0;

                    this.formattedText = new FormattedText("Deconvoluted chrom.", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Left;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin, this.chromatogramMrmUI.TopMargin + textHeight));

                    textHeight += this.formattedText.Height;

                    checker = true;
                }

                this.formattedText = new FormattedText("-> " + this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].ProductMz.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].DisplayBrush);

                if (checker == false)
                {
                    this.formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.ActualWidth - this.chromatogramMrmUI.RightMargin, this.chromatogramMrmUI.TopMargin + textHeight));
                }
                else
                {
                    this.formattedText.TextAlignment = TextAlignment.Left;
                    this.formattedText.SetFontWeight(FontWeights.Bold);
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin, this.chromatogramMrmUI.TopMargin + textHeight));
                }

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
                xPixelValue = this.chromatogramMrmUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.chromatogramMrmUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.chromatogramMrmUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramMrmUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramMrmUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.chromatogramMrmUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.chromatogramMrmUI.BottomMargin), new Point(xPixelValue, drawHeight - this.chromatogramMrmUI.BottomMargin + this.shortScaleSize));
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
            int xAxisPixelRange = (int)(drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin);
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

            double yspacket = (float)(((double)(drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMarginForLabel)) / (yscale_max - yscale_min)); // Packet for Y-Scale For Zooming

            getYaxisScaleInterval(yscale_min, yscale_max, drawHeight);
            int yStart = (int)(yscale_min / (double)this.yMinorScale) - 1;
            int yEnd = (int)(yscale_max / (double)this.yMinorScale) + 1;

            double yAxisValue, yPixelValue;

            for (int i = yStart; i <= yEnd; i++)
            {
                yAxisValue = i * (double)this.yMinorScale;
                yPixelValue = drawHeight - this.chromatogramMrmUI.BottomMargin - (yAxisValue - yscale_min) * yspacket;
                if (yPixelValue > drawHeight - this.chromatogramMrmUI.BottomMargin) continue;
                if (yPixelValue < this.chromatogramMrmUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    if (foldChange > 3) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f2"); }
                    else if (foldChange <= 0) { yString = (yAxisValue / Math.Pow(10, foldChange)).ToString("f1"); }
                    else {
                        if (this.yMajorScale >= 1) yString = yAxisValue.ToString("f0");
                        else yString = yAxisValue.ToString("f3");
                    }
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramMrmUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramMrmUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    formattedText = new FormattedText(yString, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    formattedText.TextAlignment = TextAlignment.Right;
                    this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea - 1, yPixelValue - formattedText.Height * 0.5));
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.chromatogramMrmUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.chromatogramMrmUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.chromatogramMrmUI.TopMargin - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMarginForLabel);
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
            this.formattedText = new FormattedText("Retention time [min]", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Center;
            this.formattedText.SetFontStyle(FontStyles.Italic);
            this.drawingContext.DrawText(formattedText, new Point(this.chromatogramMrmUI.LeftMargin + 0.5 * (drawWidth - this.chromatogramMrmUI.LeftMargin - this.chromatogramMrmUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.chromatogramMrmUI.TopMargin + 0.5 * (drawHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMargin)));
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
                if (figure > 0) {
                    formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
                else {
                    formattedText = new FormattedText("Relative Abundance (1e" + (figure - 1).ToString() + ")", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                }
               // formattedText = new FormattedText("Relative Abundance", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            }
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.chromatogramMrmUI.TopMargin + 0.5 * (drawHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.TopMargin))));
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
            if (Math.Abs(this.chromatogramMrmUI.RightButtonStartClickPoint.X - this.chromatogramMrmUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.chromatogramMrmUI.RightButtonStartClickPoint.Y - this.chromatogramMrmUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.chromatogramMrmUI.RightButtonStartClickPoint.X - this.chromatogramMrmUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.chromatogramMrmUI.RightButtonStartClickPoint.X > this.chromatogramMrmUI.RightButtonEndClickPoint.X)
            {
                if (this.chromatogramMrmUI.RightButtonStartClickPoint.X > this.chromatogramMrmUI.LeftMargin)
                {
                    if (this.chromatogramMrmUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.chromatogramMrmUI.RightMargin)
                    {
                        this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.DisplayRangeRtMin + (float)((this.chromatogramMrmUI.RightButtonStartClickPoint.X - this.chromatogramMrmUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramMrmUI.RightButtonEndClickPoint.X >= this.chromatogramMrmUI.LeftMargin)
                    {
                        this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.DisplayRangeRtMin + (float)((this.chromatogramMrmUI.RightButtonEndClickPoint.X - this.chromatogramMrmUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.chromatogramMrmUI.RightButtonEndClickPoint.X > this.chromatogramMrmUI.LeftMargin)
                {
                    if (this.chromatogramMrmUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.chromatogramMrmUI.RightMargin)
                    {
                        this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.DisplayRangeRtMin + (float)((this.chromatogramMrmUI.RightButtonEndClickPoint.X - this.chromatogramMrmUI.LeftMargin) / this.xPacket);
                    }
                    if (this.chromatogramMrmUI.RightButtonStartClickPoint.X >= this.chromatogramMrmUI.LeftMargin)
                    {
                        this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.DisplayRangeRtMin + (float)((this.chromatogramMrmUI.RightButtonStartClickPoint.X - this.chromatogramMrmUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.chromatogramMrmUI.RightButtonStartClickPoint.Y > this.chromatogramMrmUI.RightButtonEndClickPoint.Y)
            {
                this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - this.chromatogramMrmUI.BottomMargin - this.chromatogramMrmUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.chromatogramMrmUI.LeftButtonStartClickPoint.X == -1 || this.chromatogramMrmUI.LeftButtonStartClickPoint.Y == -1)
                return;

            if (this.chromatogramMrmViewModel.DisplayRangeRtMin == null || this.chromatogramMrmViewModel.DisplayRangeRtMax == null)
            {
                this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.MinRt;
                this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.MaxRt;
            }

            if (this.chromatogramMrmViewModel.DisplayRangeIntensityMin == null || this.chromatogramMrmViewModel.DisplayRangeIntensityMax == null)
            {
                this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.MinIntensity;
                this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.MaxIntensity;
            }

            float durationX = (float)this.chromatogramMrmViewModel.DisplayRangeRtMax - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin;
            double distanceX = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.chromatogramMrmUI.LeftButtonStartClickPoint.X > this.chromatogramMrmUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.chromatogramMrmUI.LeftButtonStartClickPoint.X - this.chromatogramMrmUI.LeftButtonEndClickPoint.X;

                this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (this.chromatogramMrmViewModel.DisplayRangeRtMax > this.chromatogramMrmViewModel.MaxRt)
                {
                    this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.MaxRt;
                    this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.MaxRt - durationX;
                }
            }
            else
            {
                distanceX = this.chromatogramMrmUI.LeftButtonEndClickPoint.X - this.chromatogramMrmUI.LeftButtonStartClickPoint.X;

                this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (this.chromatogramMrmViewModel.DisplayRangeRtMin < this.chromatogramMrmViewModel.MinRt)
                {
                    this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.MinRt;
                    this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.MinRt + durationX;
                }
            }

            // Y-Direction
            durationY = (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMax - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin;
            if (this.chromatogramMrmUI.LeftButtonStartClickPoint.Y < this.chromatogramMrmUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.chromatogramMrmUI.LeftButtonEndClickPoint.Y - this.chromatogramMrmUI.LeftButtonStartClickPoint.Y;

                this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (this.chromatogramMrmViewModel.DisplayRangeIntensityMax > this.chromatogramMrmViewModel.MaxIntensity)
                {
                    this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.MaxIntensity;
                    this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.MaxIntensity - durationY;
                }
            }
            else
            {
                distanceY = this.chromatogramMrmUI.LeftButtonStartClickPoint.Y - this.chromatogramMrmUI.LeftButtonEndClickPoint.Y;

                this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (this.chromatogramMrmViewModel.DisplayRangeIntensityMin < this.chromatogramMrmViewModel.MinIntensity)
                {
                    this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.MinIntensity;
                    this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.MinIntensity + durationY;
                }
            }
            ChromatogramDraw();
        }

        public void PeakLeftEdgeClickCheck()
        {
            int targetTransitionIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramMrmViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float leftEdgeRt = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].RtAtLeftPeakEdge;
            float leftEdgeInt = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtLeftPeakEdge;

            Point leftEdgePoint = new Point(this.chromatogramMrmUI.LeftMargin + (leftEdgeRt - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket, this.ActualHeight - this.chromatogramMrmUI.BottomMargin - (leftEdgeInt - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket);

            if (Math.Abs(leftEdgePoint.X - this.chromatogramMrmUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(leftEdgePoint.Y - this.chromatogramMrmUI.LeftButtonStartClickPoint.Y) < 5)this.chromatogramMrmUI.LeftMouseButtonLeftEdgeCapture = true;
            else this.chromatogramMrmUI.LeftMouseButtonLeftEdgeCapture = false;
        }

        public void PeakRightEdgeClickCheck()
        {
            int targetTransitionIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramMrmViewModel.SelectedPeakId;
            if (selectedPeakId == -1) return;

            float rightEdgeRt = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].RtAtRightPeakEdge;
            float rightEdgeInt = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].IntensityAtRightPeakEdge;

            Point rightEdgePoint = new Point(this.chromatogramMrmUI.LeftMargin + (rightEdgeRt - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket, this.ActualHeight - this.chromatogramMrmUI.BottomMargin - (rightEdgeInt - (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin) * this.yPacket);

            if (Math.Abs(rightEdgePoint.X - this.chromatogramMrmUI.LeftButtonStartClickPoint.X) < 5 && Math.Abs(rightEdgePoint.Y - this.chromatogramMrmUI.LeftButtonStartClickPoint.Y) < 5) this.chromatogramMrmUI.LeftMouseButtonRightEdgeCapture = true;
            else this.chromatogramMrmUI.LeftMouseButtonRightEdgeCapture = false;
        }

        public void PeakLeftEdgeEdit()
        {
            this.chromatogramMrmUI.LeftMouseButtonLeftEdgeCapture = false;

            int targetTransitionIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramMrmViewModel.SelectedPeakId;

            float newLeftEdgeRt = getDataPositionOnMousePoint(this.chromatogramMrmUI.LeftButtonEndClickPoint)[1];

            int minimumRtDeviIndex = -1;
            float minimumRtDevi = float.MaxValue, rtDevi;
            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi = Math.Abs((float)this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1] - newLeftEdgeRt);
                if (minimumRtDevi > rtDevi) { minimumRtDevi = rtDevi; minimumRtDeviIndex = i; }
            }

            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].ScanNumberAtRightPeakEdge < minimumRtDeviIndex) { return; }

            int leftPeakEdgeScanNumber = minimumRtDeviIndex;
            int rightPeakEdgeScanNumber = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].ScanNumberAtRightPeakEdge;

            List<double[]> datapoints = new List<double[]>();
            double maxintensity = double.MinValue;
            int maxIndex = -1;
            for (int i = leftPeakEdgeScanNumber; i <= rightPeakEdgeScanNumber; i++)
            {
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][0], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][2], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;
            double[] detectedPeaks = getNewDetectedPeakInformation(datapoints, maxIndex - (int)datapoints[0][0]);
            if (detectedPeaks == null) return;

            PeakAreaBean peakAreaBean = new PeakAreaBean() { AmplitudeOrderValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeOrderValue, IdealSlopeValue = (float)detectedPeaks[15], GaussianSimilarityValue = (float)detectedPeaks[14], BasePeakValue = (float)detectedPeaks[16], AreaAboveZero = (int)detectedPeaks[10], AreaAboveBaseline = (int)detectedPeaks[11], AmplitudeScoreValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeScoreValue, AmplitudeRatioSimilatiryValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeRatioSimilatiryValue, PeakID = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakID, IntensityAtLeftPeakEdge = (int)detectedPeaks[3], IntensityAtPeakTop = (int)detectedPeaks[9], IntensityAtRightPeakEdge = (int)detectedPeaks[6], PeakPureValue = (float)detectedPeaks[12], PeakShapeSimilarityValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakShapeSimilarityValue, PeakTopDifferencialValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakTopDifferencialValue, RtAtLeftPeakEdge = (float)detectedPeaks[2], RtAtPeakTop = (float)detectedPeaks[8], RtAtRightPeakEdge = (float)detectedPeaks[5], RtSimilarityValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].RtSimilarityValue, ScanNumberAtLeftPeakEdge = (int)detectedPeaks[1], ScanNumberAtPeakTop = (int)detectedPeaks[7], ScanNumberAtRightPeakEdge = (int)detectedPeaks[4], ShapenessValue = (float)detectedPeaks[13], SymmetryValue = (float)detectedPeaks[17], TotalScore = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].TotalScore };
            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
        }

        public void PeakRightEdgeEdit()
        {
            this.chromatogramMrmUI.LeftMouseButtonRightEdgeCapture = false;

            int targetTransitionIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramMrmViewModel.SelectedPeakId;

            float newRightEdgeRt = getDataPositionOnMousePoint(this.chromatogramMrmUI.LeftButtonEndClickPoint)[1];

            int minimumRtDeviIndex = -1;
            float minimumRtDevi = float.MaxValue, rtDevi;
            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi = Math.Abs((float)this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1] - newRightEdgeRt);
                if (minimumRtDevi > rtDevi) { minimumRtDevi = rtDevi; minimumRtDeviIndex = i; }
            }

            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].ScanNumberAtLeftPeakEdge > minimumRtDeviIndex) { return; }

            int leftPeakEdgeScanNumber = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].ScanNumberAtLeftPeakEdge;
            int rightPeakEdgeScanNumber = minimumRtDeviIndex;

            List<double[]> datapoints = new List<double[]>();
            double maxintensity = double.MinValue;
            int maxIndex = -1;
            for (int i = leftPeakEdgeScanNumber; i <= rightPeakEdgeScanNumber; i++)
            {
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][0], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][2], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;

            double[] detectedPeaks = getNewDetectedPeakInformation(datapoints, maxIndex - (int)datapoints[0][0]);
            if (detectedPeaks == null) return;

            PeakAreaBean peakAreaBean = new PeakAreaBean() { AmplitudeOrderValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeOrderValue, IdealSlopeValue = (float)detectedPeaks[15], GaussianSimilarityValue = (float)detectedPeaks[14], BasePeakValue = (float)detectedPeaks[16], AreaAboveZero = (int)detectedPeaks[10], AreaAboveBaseline = (int)detectedPeaks[11], AmplitudeScoreValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeScoreValue, AmplitudeRatioSimilatiryValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].AmplitudeRatioSimilatiryValue, PeakID = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakID, IntensityAtLeftPeakEdge = (int)detectedPeaks[3], IntensityAtPeakTop = (int)detectedPeaks[9], IntensityAtRightPeakEdge = (int)detectedPeaks[6], PeakPureValue = (float)detectedPeaks[12], PeakShapeSimilarityValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakShapeSimilarityValue, PeakTopDifferencialValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].PeakTopDifferencialValue, RtAtLeftPeakEdge = (float)detectedPeaks[2], RtAtPeakTop = (float)detectedPeaks[8], RtAtRightPeakEdge = (float)detectedPeaks[5], RtSimilarityValue = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].RtSimilarityValue, ScanNumberAtLeftPeakEdge = (int)detectedPeaks[1], ScanNumberAtPeakTop = (int)detectedPeaks[7], ScanNumberAtRightPeakEdge = (int)detectedPeaks[4], ShapenessValue = (float)detectedPeaks[13], SymmetryValue = (float)detectedPeaks[17], TotalScore = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[selectedPeakId].TotalScore };
            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Insert(selectedPeakId, peakAreaBean);
            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.RemoveAt(selectedPeakId + 1);
        }

        public void PeakNewEdit()
        {
            int targetTransitionIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            int selectedPeakId = this.chromatogramMrmViewModel.SelectedPeakId;

            float rt1 = getDataPositionOnMousePoint(this.chromatogramMrmUI.RightButtonStartClickPoint)[1];
            float rt2 = getDataPositionOnMousePoint(this.chromatogramMrmUI.RightButtonEndClickPoint)[1];

            int minScan1 = -1, minScan2 = -1;
            float minimumRtDevi1 = float.MaxValue, minimumRtDevi2 = float.MaxValue, rtDevi1, rtDevi2;

            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection.Count; i++)
            {
                rtDevi1 = Math.Abs((float)this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1] - rt1);
                rtDevi2 = Math.Abs((float)this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1] - rt2);
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
                if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] > maxintensity)
                {
                    maxintensity = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3];
                    maxIndex = i;
                }
                datapoints.Add(new double[] { this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][0], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][1], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][2], this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].ChromatogramDataPointCollection[i][3] });
            }
            if (maxintensity <= 0) return;

            //already included check
            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection != null)
                for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Count; i++)
                    if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[i].ScanNumberAtPeakTop == maxIndex)
                    {
                        //Kumazawa
                        peakDelete(i);
                        if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Count == 0) this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].SelectedPeakId = -1;
                        else
                        {
                            float totalMax = float.MinValue;
                            int totalMaxId = -1;
                            for (int j = 0; j < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Count; j++)
                            {
                                if (totalMax < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[j].TotalScore)
                                {
                                    totalMax = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[j].TotalScore;
                                    totalMaxId = j;
                                }
                            }
                            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].SelectedPeakId = totalMaxId;
                        }
                        return;
                    }

            double[] detectedPeaks = getNewDetectedPeakInformation(datapoints, maxIndex - (int)datapoints[0][0]);
            if (detectedPeaks == null) return;

            bool sameDatapointsNumber = true;
            //Check same datapoints number
            if (this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count != 1)
                for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count - 1; i++)
                    if (this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].ChromatogramDataPointCollection.Count != this.chromatogramMrmViewModel.ChromatogramBeanCollection[i + 1].ChromatogramDataPointCollection.Count) sameDatapointsNumber = false;

            int moveLevel = 10;
            List<double> datapointList = new List<double>();
            List<List<double>> examinedDatapointsList = new List<List<double>>();
            List<double> examinedDatapoints = new List<double>();
            List<double> peaktopIntensitiyList = new List<double>();
            if (sameDatapointsNumber == true)
            {
                for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count; i++)
                {
                    peaktopIntensitiyList.Add(this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].ChromatogramDataPointCollection[maxIndex][3]);
                    if (i != this.chromatogramMrmViewModel.TargetTransitionIndex)
                    {
                        examinedDatapoints = new List<double>();
                        for (int j = -moveLevel + leftPeakEdgeScanNumber; j <= moveLevel + rightPeakEdgeScanNumber; j++)
                        {
                            if (j < 0) examinedDatapoints.Add(0);
                            else if (j > this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].ChromatogramDataPointCollection.Count - 1) examinedDatapoints.Add(0);
                            else examinedDatapoints.Add(this.chromatogramMrmViewModel.ChromatogramBeanCollection[i].ChromatogramDataPointCollection[j][3]);
                        }
                        examinedDatapointsList.Add(examinedDatapoints);
                    }
                    else { for (int j = 0; j < datapoints.Count; j++) { datapointList.Add(datapoints[j][3]); } }
                }
            }

            this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection = getNewPeakAreaBeanCollection(detectedPeaks, this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection, this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].MetaboliteId, this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].FileId, targetTransitionIndex, moveLevel, datapointList, examinedDatapointsList, peaktopIntensitiyList, sameDatapointsNumber);
            this.chromatogramMrmViewModel.DetectedPeakNumber = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Count;
            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection.Count; i++)
                if (maxIndex == this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetTransitionIndex].PeakAreaBeanCollection[i].ScanNumberAtPeakTop) this.chromatogramMrmViewModel.SelectedPeakId = i;
        }

        public void SelectedPeakChange()
        {
            if (this.chromatogramMrmUI.CurrentMousePoint.Y > this.chromatogramMrmUI.TopMargin + this.chromatogramMrmUI.TriangleSize * 2) return;

            //Bean
            ChromatogramBean chromatogramBean;
            PeakAreaBean peakAreaBean;

            bool doubleClickChecker = false;

            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count; i++)
            {
                chromatogramBean = this.chromatogramMrmViewModel.ChromatogramBeanCollection[i];
                if (chromatogramBean.PeakAreaBeanCollection == null) continue;

                for (int j = 0; j < chromatogramBean.PeakAreaBeanCollection.Count; j++)
                {
                    if (chromatogramBean.PeakAreaBeanCollection[j].RtAtRightPeakEdge < this.chromatogramMrmViewModel.DisplayRangeRtMin) continue;
                    if (chromatogramBean.PeakAreaBeanCollection[j].RtAtLeftPeakEdge > this.chromatogramMrmViewModel.DisplayRangeRtMax) break;

                    peakAreaBean = chromatogramBean.PeakAreaBeanCollection[j];

                    // Set Top point
                    this.xt = this.chromatogramMrmUI.LeftMargin + (peakAreaBean.RtAtPeakTop - (float)this.chromatogramMrmViewModel.DisplayRangeRtMin) * this.xPacket;

                    if (this.xt - this.chromatogramMrmUI.TriangleSize < this.chromatogramMrmUI.CurrentMousePoint.X && this.xt + this.chromatogramMrmUI.TriangleSize > this.chromatogramMrmUI.CurrentMousePoint.X)
                    {
                        if (j != this.chromatogramMrmViewModel.SelectedPeakId)
                        {
                            this.chromatogramMrmViewModel.SelectedPeakId = j;
                            doubleClickChecker = true;
                            break;
                        }
                    }
                }
                if (doubleClickChecker) ChromatogramDraw();
            }
        }

        public void NotDetectedEdit()
        {
            for (int i = 0; i < this.chromatogramMrmViewModel.ChromatogramBeanCollection.Count; i++)
                this.chromatogramMrmViewModel.SelectedPeakId = -1;
            ChromatogramDraw();
        }

        public void ZoomRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramMrmUI.RightButtonStartClickPoint.X, this.chromatogramMrmUI.RightButtonStartClickPoint.Y), new Point(this.chromatogramMrmUI.RightButtonEndClickPoint.X, this.chromatogramMrmUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void NewPeakGenerateRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramMrmUI.RightButtonStartClickPoint.X, this.ActualHeight - this.chromatogramMrmUI.BottomMargin), new Point(this.chromatogramMrmUI.RightButtonEndClickPoint.X, this.chromatogramMrmUI.TopMargin + this.chromatogramMrmUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void PeakEdgeEditRubberDraw()
        {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.chromatogramMrmUI.LeftButtonStartClickPoint.X, this.ActualHeight - this.chromatogramMrmUI.BottomMargin), new Point(this.chromatogramMrmUI.LeftButtonEndClickPoint.X, this.chromatogramMrmUI.TopMargin + this.chromatogramMrmUI.TopMarginForLabel)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ResetGraphDisplayRange()
        {
            this.chromatogramMrmViewModel.DisplayRangeIntensityMin = this.chromatogramMrmViewModel.MinIntensity;
            this.chromatogramMrmViewModel.DisplayRangeIntensityMax = this.chromatogramMrmViewModel.MaxIntensity;
            this.chromatogramMrmViewModel.DisplayRangeRtMin = this.chromatogramMrmViewModel.MinRt;
            this.chromatogramMrmViewModel.DisplayRangeRtMax = this.chromatogramMrmViewModel.MaxRt;

            ChromatogramDraw();
        }

        public float[] getDataPositionOnMousePoint(Point mousePoint)
        {
            if (this.chromatogramMrmViewModel == null)
                return null;

            float[] peakInformation;
            float scanNumber, retentionTime, mzValue, intensity;

            scanNumber = -1;
            retentionTime = (float)this.chromatogramMrmViewModel.DisplayRangeRtMin + (float)((mousePoint.X - this.chromatogramMrmUI.LeftMargin) / this.xPacket);
            mzValue = 0;
            intensity = (float)this.chromatogramMrmViewModel.DisplayRangeIntensityMin + (float)((this.ActualHeight - mousePoint.Y - this.chromatogramMrmUI.BottomMargin) / this.yPacket);

            peakInformation = new float[] { scanNumber, retentionTime, mzValue, intensity };

            return peakInformation;
        }

        private double[] getNewDetectedPeakInformation(List<double[]> datapoints, int peakTopId)
        {
            double[] detectedPeakInformation;
            double peakHwhm, peakHalfDiff, peakFivePercentDiff, leftShapenessValue, rightShapenessValue
                , gaussianSigma, gaussianNormalize, gaussianArea, gaussinaSimilarityValue, gaussianSimilarityLeftValue, gaussianSimilarityRightValue
                , realAreaAboveZero, realAreaAboveBaseline, leftPeakArea, rightPeakArea, idealSlopeValue, nonIdealSlopeValue, symmetryValue, basePeakValue, peakPureValue;
            int peakHalfId = -1, leftPeakFivePercentId = -1, rightPeakFivePercentId = -1, leftPeakHalfId = -1, rightPeakHalfId = -1;

            //1. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
            #region
            if (datapoints.Count <= 3) return null;
            if (datapoints[peakTopId][3] - datapoints[0][3] < 0 && datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < 0) return null;
            idealSlopeValue = 0;
            nonIdealSlopeValue = 0;
            peakHalfDiff = double.MaxValue;
            peakFivePercentDiff = double.MaxValue;
            leftShapenessValue = double.MinValue;

            for (int j = peakTopId; j >= 0; j--)
            {
                if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3])))
                {
                    peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]));
                    leftPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 20 - (datapoints[j][3] - datapoints[0][3])))
                {
                    peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 20 - (datapoints[j][3] - datapoints[0][3]));
                    leftPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (leftShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]))
                    leftShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]);
            }
            peakHalfDiff = double.MaxValue;
            peakFivePercentDiff = double.MaxValue;
            rightShapenessValue = double.MinValue;
            for (int j = peakTopId; j <= datapoints.Count - 1; j++)
            {
                if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3])))
                {
                    peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                    rightPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 20 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3])))
                {
                    peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 20 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                    rightPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (rightShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]))
                    rightShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]);
            }

            if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3])
            {
                gaussianNormalize = datapoints[peakTopId][3] - datapoints[0][3];
                peakHalfId = leftPeakHalfId;
                basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / (datapoints[peakTopId][3] - datapoints[0][3]));
            }
            else
            {
                gaussianNormalize = datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3];
                peakHalfId = rightPeakHalfId;
                basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / (datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]));
            }

            if (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) <= Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]))
                symmetryValue = (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) + Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1])) / (2 * Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]));
            else
                symmetryValue = (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) + Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1])) / (2 * Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]));

            peakHwhm = Math.Abs(datapoints[peakHalfId][1] - datapoints[peakTopId][1]);
            #endregion

            //2. Calculate peak pure value (from gaussian area and real area)
            #region
            gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
            gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

            realAreaAboveZero = 0;
            leftPeakArea = 0;
            rightPeakArea = 0;
            for (int j = 0; j < datapoints.Count - 1; j++)
            {
                realAreaAboveZero += (datapoints[j][3] + datapoints[j + 1][3]) * (datapoints[j + 1][1] - datapoints[j][1]) * 0.5;
                if (j == peakTopId - 1)
                    leftPeakArea = realAreaAboveZero;
                else if (j == datapoints.Count - 2)
                    rightPeakArea = realAreaAboveZero - leftPeakArea;
            }
            realAreaAboveBaseline = realAreaAboveZero - (datapoints[0][3] + datapoints[datapoints.Count - 1][3]) * (datapoints[datapoints.Count - 1][1] - datapoints[0][1]) / 2;

            if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3])
            {
                leftPeakArea = leftPeakArea - datapoints[0][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                rightPeakArea = rightPeakArea - datapoints[0][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
            }
            else
            {
                leftPeakArea = leftPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                rightPeakArea = rightPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
            }

            if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
            else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

            if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
            else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

            gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;
            if (idealSlopeValue < 0) idealSlopeValue = 0;

            peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue) / 3 * 100;
            if (peakPureValue > 100) peakPureValue = 100;
            if (peakPureValue < 0) peakPureValue = 0;
            #endregion

            //3. Set area information
            #region

            //[0]peakID[1]scanNumAtLeftPeakEdge[2]rtAtLeftPeakEdge[3]intensityAtLeftPeakEdge
            //[4]scanNumAtRightPeakEdge[5]rtAtRightPeakEdge[6]intensityAtRightPeakEdge
            //[7]scanNumAtPeakTop[8]rtAtPeakTop[9]intensityAtPeakTop
            //[10]areaAboveZero[11]areaAboveBaseline[12]peakPureValue[13]shapnessValue
            //[14]gaussianSimilarityValue[15]idealSlopeValue[16]basePeakValue[17]symmetryValue
            //[18]amplitudeScoreValue[19]amplitudeOrderValue

            detectedPeakInformation = new double[] { -1, datapoints[0][0], datapoints[0][1], datapoints[0][3]
                    , datapoints[datapoints.Count - 1][0], datapoints[datapoints.Count - 1][1], datapoints[datapoints.Count - 1][3]
                    , datapoints[peakTopId][0], datapoints[peakTopId][1], datapoints[peakTopId][3]
                    , realAreaAboveZero, realAreaAboveBaseline, peakPureValue, (leftShapenessValue + rightShapenessValue)/2
                    , gaussinaSimilarityValue, idealSlopeValue, basePeakValue, symmetryValue, -1, -1 };
            #endregion
            return detectedPeakInformation;
        }

        private void peakDelete(int deleteId)
        {
            int targetMrmIndex = this.chromatogramMrmViewModel.TargetTransitionIndex;
            ObservableCollection<PeakAreaBean> peakAreaBeanCollection = this.chromatogramMrmViewModel.ChromatogramBeanCollection[targetMrmIndex].PeakAreaBeanCollection;
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

        private ObservableCollection<PeakAreaBean> getNewPeakAreaBeanCollection(double[] detectedPeakInformation, ObservableCollection<PeakAreaBean> peakAreaBeanCollection, int metaboliteID, int fileID, int targetMrmTransitionIndex, int moveLevel, List<double> detectedPeakDatapoints, List<List<double>> examinedDatapointsList, List<double> peaktopIntensityList, bool sameDatapointsNumber)
        {
            double retentionTimeReference = this.chromatogramMrmViewModel.ReferenceRetentionTime;
            double retentionTimeTolerance = this.chromatogramMrmViewModel.RtTolerance;
            ObservableCollection<float> amplitudeRatioCollection = this.chromatogramMrmViewModel.ReferenceAmplitudeRatioCollection;

            PeakAreaBean newPeakAreaBean = new PeakAreaBean();
            newPeakAreaBean.ScanNumberAtLeftPeakEdge = (int)detectedPeakInformation[1];
            newPeakAreaBean.RtAtLeftPeakEdge = (float)detectedPeakInformation[2];
            newPeakAreaBean.IntensityAtLeftPeakEdge = (int)detectedPeakInformation[3];
            newPeakAreaBean.ScanNumberAtRightPeakEdge = (int)detectedPeakInformation[4];
            newPeakAreaBean.RtAtRightPeakEdge = (float)detectedPeakInformation[5];
            newPeakAreaBean.IntensityAtRightPeakEdge = (int)detectedPeakInformation[6];
            newPeakAreaBean.ScanNumberAtPeakTop = (int)detectedPeakInformation[7];
            newPeakAreaBean.RtAtPeakTop = (float)detectedPeakInformation[8];
            newPeakAreaBean.IntensityAtPeakTop = (int)detectedPeakInformation[9];
            newPeakAreaBean.AreaAboveZero = (int)detectedPeakInformation[10];
            newPeakAreaBean.AreaAboveBaseline = (int)detectedPeakInformation[11];
            newPeakAreaBean.PeakPureValue = (float)detectedPeakInformation[12];
            newPeakAreaBean.ShapenessValue = (float)detectedPeakInformation[13];
            newPeakAreaBean.GaussianSimilarityValue = (float)detectedPeakInformation[14];
            newPeakAreaBean.IdealSlopeValue = (float)detectedPeakInformation[15];
            newPeakAreaBean.BasePeakValue = (float)detectedPeakInformation[16];
            newPeakAreaBean.SymmetryValue = (float)detectedPeakInformation[17];

            if (peakAreaBeanCollection == null || peakAreaBeanCollection.Count == 0 || sameDatapointsNumber == false)
            {
                if (peakAreaBeanCollection == null) peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();
                newPeakAreaBean.PeakID = 0;
                newPeakAreaBean.AmplitudeScoreValue = 1F;
                newPeakAreaBean.AmplitudeOrderValue = 1;
                newPeakAreaBean.RtSimilarityValue = (float)Math.Exp(-0.5 * Math.Pow((retentionTimeReference - newPeakAreaBean.RtAtPeakTop) / retentionTimeTolerance, 2));

                if (examinedDatapointsList == null || examinedDatapointsList.Count == 0)
                {
                    newPeakAreaBean.AmplitudeRatioSimilatiryValue = -1000;
                    newPeakAreaBean.PeakTopDifferencialValue = -1000;
                    newPeakAreaBean.PeakShapeSimilarityValue = -1000;
                    newPeakAreaBean.TotalScore = getPosteriorOneTransition(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue);

                }
                else
                {
                    float[] threeExaminedValueArray = getRatioDifferentialShapeSimilarityValues(targetMrmTransitionIndex, moveLevel, detectedPeakDatapoints, examinedDatapointsList, peaktopIntensityList, amplitudeRatioCollection);

                    newPeakAreaBean.AmplitudeRatioSimilatiryValue = threeExaminedValueArray[0];
                    newPeakAreaBean.PeakTopDifferencialValue = (newPeakAreaBean.RtAtPeakTop - newPeakAreaBean.RtAtLeftPeakEdge) / (newPeakAreaBean.ScanNumberAtPeakTop - newPeakAreaBean.ScanNumberAtLeftPeakEdge) * threeExaminedValueArray[1];
                    newPeakAreaBean.PeakShapeSimilarityValue = threeExaminedValueArray[2];
                    if (newPeakAreaBean.AmplitudeRatioSimilatiryValue < 0)
                        newPeakAreaBean.TotalScore = getPosteriorMultiTransitionWithoutRaio(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue, newPeakAreaBean.PeakShapeSimilarityValue, newPeakAreaBean.PeakTopDifferencialValue);
                    else
                        newPeakAreaBean.TotalScore = getPosteriorMultiTransitionWithRatio(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue, newPeakAreaBean.AmplitudeRatioSimilatiryValue, newPeakAreaBean.PeakShapeSimilarityValue, newPeakAreaBean.PeakTopDifferencialValue);

                }
                peakAreaBeanCollection.Add(newPeakAreaBean);
            }
            else
            {
                int rtOrder = 0;
                int amplitudeOrder = 1;
                double amplitudeMax = double.MinValue;

                for (int i = 0; i < peakAreaBeanCollection.Count; i++)
                {
                    if (newPeakAreaBean.ScanNumberAtPeakTop > peakAreaBeanCollection[i].ScanNumberAtPeakTop) rtOrder++;
                    if (newPeakAreaBean.IntensityAtPeakTop < peakAreaBeanCollection[i].IntensityAtPeakTop) amplitudeOrder++;
                    if (peakAreaBeanCollection[i].IntensityAtPeakTop > amplitudeMax) amplitudeMax = peakAreaBeanCollection[i].IntensityAtPeakTop;
                }
                newPeakAreaBean.PeakID = rtOrder;
                newPeakAreaBean.AmplitudeOrderValue = amplitudeOrder;
                newPeakAreaBean.AmplitudeScoreValue = (float)(newPeakAreaBean.IntensityAtPeakTop / amplitudeMax);

                if (examinedDatapointsList == null || examinedDatapointsList.Count == 0)
                {
                    newPeakAreaBean.AmplitudeRatioSimilatiryValue = -1000;
                    newPeakAreaBean.PeakTopDifferencialValue = -1000;
                    newPeakAreaBean.PeakShapeSimilarityValue = -1000;
                    newPeakAreaBean.TotalScore = getPosteriorOneTransition(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue);
                }
                else
                {
                    float[] threeExaminedValueArray = getRatioDifferentialShapeSimilarityValues(targetMrmTransitionIndex, moveLevel, detectedPeakDatapoints, examinedDatapointsList, peaktopIntensityList, amplitudeRatioCollection);

                    newPeakAreaBean.AmplitudeRatioSimilatiryValue = threeExaminedValueArray[0];
                    newPeakAreaBean.PeakTopDifferencialValue = (newPeakAreaBean.RtAtPeakTop - newPeakAreaBean.RtAtLeftPeakEdge) / (newPeakAreaBean.ScanNumberAtPeakTop - newPeakAreaBean.ScanNumberAtLeftPeakEdge) * threeExaminedValueArray[1];
                    newPeakAreaBean.PeakShapeSimilarityValue = threeExaminedValueArray[2];

                    if (newPeakAreaBean.AmplitudeRatioSimilatiryValue < 0)
                        newPeakAreaBean.TotalScore = getPosteriorMultiTransitionWithoutRaio(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue, newPeakAreaBean.PeakShapeSimilarityValue, newPeakAreaBean.PeakTopDifferencialValue);
                    else
                        newPeakAreaBean.TotalScore = getPosteriorMultiTransitionWithRatio(newPeakAreaBean.AmplitudeScoreValue, newPeakAreaBean.RtSimilarityValue, newPeakAreaBean.AmplitudeRatioSimilatiryValue, newPeakAreaBean.PeakShapeSimilarityValue, newPeakAreaBean.PeakTopDifferencialValue);
                }
                peakAreaBeanCollection.Insert(rtOrder, newPeakAreaBean);

                //Reorder
                for (int i = 0; i < peakAreaBeanCollection.Count; i++)
                {
                    if (i > rtOrder) peakAreaBeanCollection[i].PeakID++;
                    if (peakAreaBeanCollection[i].IntensityAtPeakTop < newPeakAreaBean.IntensityAtPeakTop) peakAreaBeanCollection[i].AmplitudeOrderValue++;
                }
            }

            return peakAreaBeanCollection;
        }

        private float[] getRatioDifferentialShapeSimilarityValues(int targetMrmTransitionIndex, int moveLevel, List<double> detectedPeaksDatapoints, List<List<double>> examinedDatapointsList, List<double> peakTopIntensityList, ObservableCollection<float> amplitudeRatioReferenceList)
        {
            double peaktopDifferential, amplitudeRatioSimilarity, peakShapeSimilarity;
            double minDiff = double.MaxValue, maxCorr = double.MinValue;
            double sum, corr, cov, sca1, sca2;

            sum = 0;
            bool lessZero = false;
            for (int i = 0; i < peakTopIntensityList.Count; i++)
            {
                if (amplitudeRatioReferenceList[i] < 0) { lessZero = true; break; };
                sum += Math.Exp(-0.5 * Math.Pow((peakTopIntensityList[i] / peakTopIntensityList[targetMrmTransitionIndex] * 100 - amplitudeRatioReferenceList[i]) / this.chromatogramMrmViewModel.AmplitudeTolerance, 2));
            }
            if (lessZero) amplitudeRatioSimilarity = -1000;
            else amplitudeRatioSimilarity = sum / peakTopIntensityList.Count;

            for (int i = -moveLevel; i <= moveLevel; i++)
            {
                corr = 0;
                for (int j = 0; j < examinedDatapointsList.Count; j++)
                {
                    cov = sca1 = sca2 = 0;
                    for (int k = 0; k < detectedPeaksDatapoints.Count; k++)
                    {
                        cov += detectedPeaksDatapoints[k] * examinedDatapointsList[j][k + moveLevel + i];
                        sca1 += detectedPeaksDatapoints[k] * detectedPeaksDatapoints[k];
                        sca2 += examinedDatapointsList[j][k + moveLevel + i] * examinedDatapointsList[j][k + moveLevel + i];
                    }
                    if (sca1 == 0 || sca2 == 0) corr += 0;
                    else corr += cov / Math.Sqrt(sca1) / Math.Sqrt(sca2);
                }
                corr = corr / examinedDatapointsList.Count;
                if (maxCorr < corr) { maxCorr = corr; minDiff = i; }
            }

            peaktopDifferential = minDiff;
            peakShapeSimilarity = maxCorr;

            return new float[] { (float)amplitudeRatioSimilarity, (float)peaktopDifferential, (float)peakShapeSimilarity };
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
