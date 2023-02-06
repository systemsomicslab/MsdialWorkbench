using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rfx.Riken.OsakaUniv {
	public class PairwisePlotPeakViewFE : FrameworkElement
    {
        private VisualCollection visualCollection;//絵を描くための画用紙みたいなもの
        private DrawingVisual drawingVisual;//絵を描くための筆とかパレットみたいなもの
        private DrawingContext drawingContext;//絵を描く人

        private PairwisePlotPeakViewUI pairwisePlotPeakViewUI;
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
        private double xs, ys, xt, yt, xe, ye;

        // Drawing Packet
        private double xPacket;
        private double yPacket;

		public PairwisePlotBean PairwisePlotBean { get; set; }

        public PairwisePlotPeakViewFE(PairwisePlotBean pairwisePlotBean, PairwisePlotPeakViewUI pairwisePlotPeakViewUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.pairwisePlotBean = pairwisePlotBean;
            this.pairwisePlotPeakViewUI = pairwisePlotPeakViewUI;

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
            if (drawWidth < this.pairwisePlotPeakViewUI.LeftMargin + this.pairwisePlotPeakViewUI.RightMargin || drawHeight < this.pairwisePlotPeakViewUI.BottomMargin + this.pairwisePlotPeakViewUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            //FormattedText formattedText;

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.pairwisePlotPeakViewUI.LeftMargin, this.pairwisePlotPeakViewUI.TopMargin), new Size(drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin), new Point(drawWidth - this.pairwisePlotPeakViewUI.RightMargin, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin), new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.axisFromGraphArea, this.pairwisePlotPeakViewUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.pairwisePlotBean.XAxisDatapointCollection == null)
            {
                // Calculate Packet Size
                this.xPacket = (drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin) / 100;
                this.yPacket = (drawHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.BottomMargin) / 100;

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
            this.xPacket = (drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxX - this.pairwisePlotBean.DisplayRangeMinX);
            this.yPacket = (drawHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.BottomMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxY - this.pairwisePlotBean.DisplayRangeMinY);
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
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.pairwisePlotPeakViewUI.LeftMargin, this.pairwisePlotPeakViewUI.BottomMargin, drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.TopMargin)));

            // 5. Draw plot
            #region
            drawPairwisePlots();
			if (this.pairwisePlotBean.SelectedPlotId >= 0) {
				drawFocusedPlot();
			}

			if (this.pairwisePlotBean.Ms1DecResults != null && this.pairwisePlotBean.Ms1DecResults.Count > 0)
            {
                drawMS1DecResults();
				if (this.pairwisePlotBean.SelectedMs1DecID >= 0) {
					drawFocusedMS1Dec();
				}
            }
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

		// draws the blue line for focused GC triangle
        private void drawFocusedMS1Dec()
        {
            this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.Ms1DecResults[this.pairwisePlotBean.SelectedMs1DecID].RetentionTime - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
            this.drawingContext.DrawLine(new Pen(Brushes.Blue, 1.0), new Point(this.xt, this.pairwisePlotPeakViewUI.BottomMargin), new Point(this.xt, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin));
        }

		// draws gray triangles for Deconvoluted GC features and in blue the one where the mouse is over
        private void drawMS1DecResults()
        {
            PathFigure areaTriangleFigure;
            PathGeometry areaTriangleGeometry;
            for (int i = 0; i < this.pairwisePlotBean.Ms1DecResults.Count; i++)
            {
                if (!ms1DecDisplayFilter(this.pairwisePlotBean, i)) continue;
                this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.Ms1DecResults[i].RetentionTime - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.TriangleSize;

                areaTriangleFigure = new PathFigure() { StartPoint = new Point(this.xt, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.TriangleSize * 2) };
                areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt - this.pairwisePlotPeakViewUI.TriangleSize, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin) });
                areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt + this.pairwisePlotPeakViewUI.TriangleSize, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin) });
                areaTriangleGeometry = new PathGeometry(new PathFigure[] { areaTriangleFigure });

                if (i == this.pairwisePlotPeakViewUI.PairwisePlotBean.SelectedMs1DecID) this.drawingContext.DrawGeometry(Brushes.Red, new Pen(Brushes.Red, 1.0), areaTriangleGeometry);
                else this.drawingContext.DrawGeometry(Brushes.Gray, new Pen(Brushes.Gray, 1.0), areaTriangleGeometry);

                if (Math.Abs(this.xt - this.pairwisePlotPeakViewUI.CurrentMousePoint.X) < this.pairwisePlotPeakViewUI.PlotSize
                    && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotPeakViewUI.CurrentMousePoint.Y)) < this.pairwisePlotPeakViewUI.PlotSize)
                {
                    areaTriangleFigure = new PathFigure() { StartPoint = new Point(this.xt, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.TriangleSize * 2 - 2) };
                    areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt - this.pairwisePlotPeakViewUI.TriangleSize - 2, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin + 2) });
                    areaTriangleFigure.Segments.Add(new LineSegment() { Point = new Point(this.xt + this.pairwisePlotPeakViewUI.TriangleSize + 2, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin + 2) });
                    areaTriangleGeometry = new PathGeometry(new PathFigure[] { areaTriangleFigure });
                    this.drawingContext.DrawGeometry(Brushes.Blue, new Pen(Brushes.Blue, 1.0), areaTriangleGeometry);
                    
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

					formattedText = new FormattedText("Spot ID: " + i.ToString() + "; Scan #: " + this.pairwisePlotBean.Ms1DecResults[i].ScanNumber + "; RT[min]: " + Math.Round(this.pairwisePlotBean.Ms1DecResults[i].RetentionTime, 3).ToString()
						+ "; RI: " + Math.Round(this.pairwisePlotBean.Ms1DecResults[i].RetentionIndex, 0).ToString()
						+ "; Quant mass: " + Math.Round(this.pairwisePlotBean.Ms1DecResults[i].BasepeakMz, 4).ToString(), CultureInfo.GetCultureInfo("en-us"),
						FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);

					formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, this.pairwisePlotPeakViewUI.TopMargin + this.pairwisePlotPeakViewUI.TriangleSize + this.labelYDistance));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
        }

		//draws the red linse that mark a selected LC spot and the circle if the mouse is over it
        private void drawFocusedPlot()
        {
            #region
            if (this.pairwisePlotBean.PeakAreaBeanCollection != null && this.pairwisePlotBean.PeakAreaBeanCollection.Count > 0) {
                var peakCollection = this.pairwisePlotBean.PeakAreaBeanCollection;
                var spotID = this.pairwisePlotBean.SelectedPlotId;
                var spot = peakCollection[spotID];


                this.xt = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.XAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.YAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0),
                    new Point(this.pairwisePlotPeakViewUI.LeftMargin, this.yt),
                    new Point(this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin, this.yt));

                this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0),
                    new Point(this.xt, this.pairwisePlotPeakViewUI.BottomMargin),
                    new Point(this.xt, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin));

                this.xs = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.RectangleRangeXmin - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.xe = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.RectangleRangeXmax - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.ys = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.RectangleRangeYmin - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                this.ye = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.RectangleRangeYmax - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                this.drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(50, 200, 255, 255)), new Pen(Brushes.Cyan, 1), new Rect(new Point(this.xs, this.ys), new Point(this.xe, this.ye)));

                if (spot.PeakLinks != null) { // this means new version of ms-dail

                    var donelist = new List<string>();
                    var lineDoneList = new List<string>();
                    var yLabelPositions = new List<double>();
                    var peakID = spot.PeakID;
                    foreach (var sameGroupPeak in peakCollection.Where(n => n.PeakGroupID == spot.PeakGroupID)) {
                        if (sameGroupPeak.PeakID != peakID) continue; // now peaks connected to the target peak are shown
                        this.xs = this.pairwisePlotPeakViewUI.LeftMargin +
                                (this.pairwisePlotBean.XAxisDatapointCollection[sameGroupPeak.PeakID] -
                                (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        this.ys = this.pairwisePlotPeakViewUI.BottomMargin +
                                (this.pairwisePlotBean.YAxisDatapointCollection[sameGroupPeak.PeakID] -
                                (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                        // showing isotope adduct information
                        #region
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                        if (sameGroupPeak.IsotopeWeightNumber == 0) {
                            formattedText = new FormattedText(sameGroupPeak.AdductIonName,
                                          CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                            formattedText.TextAlignment = TextAlignment.Center;
                            this.drawingContext.DrawText(formattedText, new Point(this.xs, this.ActualHeight - this.ys + this.labelYDistance * 0.5));

                            if (sameGroupPeak.AdductFromAmalgamation != null && sameGroupPeak.AdductFromAmalgamation.FormatCheck) {
                                formattedText = new FormattedText(sameGroupPeak.AdductFromAmalgamation.AdductIonName + " (Amal.)",
                                          CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Red);
                                formattedText.TextAlignment = TextAlignment.Center;
                                this.drawingContext.DrawText(formattedText, new Point(this.xs, this.ActualHeight - this.ys + this.labelYDistance * 0.5 * 2.0));
                            }
                        }

                        formattedText = new FormattedText("M + " + sameGroupPeak.IsotopeWeightNumber.ToString(),
                                      CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                        formattedText.TextAlignment = TextAlignment.Left;
                        this.drawingContext.DrawText(formattedText, new Point(this.xs + 30 * 0.333, this.ActualHeight - this.ys));

                        this.drawingContext.Pop();
                        this.drawingContext.Pop();
                        #endregion

                        foreach (var linkedPeakCharacter in sameGroupPeak.PeakLinks) {

                            var character = linkedPeakCharacter.Character;
                            var linkedPeak = peakCollection[linkedPeakCharacter.LinkedPeakID];

                            this.xe = this.pairwisePlotPeakViewUI.LeftMargin +
                            (this.pairwisePlotBean.XAxisDatapointCollection[linkedPeak.PeakID] -
                            (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                            this.ye = this.pairwisePlotPeakViewUI.BottomMargin +
                                (this.pairwisePlotBean.YAxisDatapointCollection[linkedPeak.PeakID] -
                                (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                            var blushColor = Brushes.Blue;
                            var xOffset = 30;
                            var yTextOffset = 30;

                            var characterString = string.Empty;
                            switch (character) {
                                case PeakLinkFeatureEnum.SameFeature:
                                    blushColor = Brushes.Gray;
                                    characterString = "Same metabolite name";
                                    break;
                                case PeakLinkFeatureEnum.Isotope:
                                    blushColor = Brushes.Red;
                                    characterString = "M + " + linkedPeak.IsotopeWeightNumber.ToString();
                                    break;
                                case PeakLinkFeatureEnum.Adduct:
                                    blushColor = Brushes.Blue;
                                    characterString = linkedPeak.AdductIonName;
                                    yTextOffset = -30;
                                    break;
                                case PeakLinkFeatureEnum.ChromSimilar:
                                    blushColor = Brushes.Green;
                                    characterString = "Chromatogram similar";
                                    xOffset = -30;
                                    break;
                                case PeakLinkFeatureEnum.FoundInUpperMsMs:
                                    blushColor = Brushes.Pink;
                                    characterString = "Found in upper MS/MS";
                                    xOffset = -30;
                                    yTextOffset = -30;
                                    break;
                            }

                            var index = Math.Min(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                                Math.Max(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                                characterString;
                            if (donelist.Contains(index)) continue;
                            else donelist.Add(index);

                            var lineIndex = Math.Min(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                                Math.Max(linkedPeak.PeakID, sameGroupPeak.PeakID) + "_" +
                                blushColor.ToString();

                            if (Math.Abs(this.ye - this.ys) >= 20) {

                                var yOffset = this.ye - this.ys > 0 ? -10 : 10;

                                if (!lineDoneList.Contains(lineIndex)) {
                                    this.drawingContext.DrawLine(new Pen(blushColor, 1),
                                        new Point(this.xs - xOffset - (this.xs - this.xe), this.ys - yOffset),
                                        new Point(this.xe - xOffset, this.ye + yOffset));
                                    this.drawingContext.DrawLine(new Pen(blushColor, 1),
                                        new Point(this.xs, this.ys),
                                        new Point(this.xs - xOffset - (this.xs - this.xe), this.ys - yOffset));
                                    this.drawingContext.DrawLine(new Pen(blushColor, 1),
                                        new Point(this.xe, this.ye),
                                        new Point(this.xe - xOffset, this.ye + yOffset));

                                  
                                    lineDoneList.Add(lineIndex);
                                }

                                this.drawingContext.DrawEllipse(null,
                                              new Pen(blushColor, 1.0),
                                              new Point(this.xe, this.ye),
                                              this.pairwisePlotPeakViewUI.PlotSize,
                                              this.pairwisePlotPeakViewUI.PlotSize);

                                this.drawingContext.DrawEllipse(null,
                                              new Pen(blushColor, 1.0),
                                              new Point(this.xs, this.ys),
                                              this.pairwisePlotPeakViewUI.PlotSize,
                                              this.pairwisePlotPeakViewUI.PlotSize);

                                formattedText = new FormattedText(characterString,
                                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                                formattedText.TextAlignment = TextAlignment.Left;

                                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                                this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                                var xPoint = 0.0;
                                var yPoint = 0.0;
                                if (characterString == "Found in upper MS/MS" || characterString == "Chromatogram similar") {
                                    xPoint = this.xe - xOffset + 5;
                                    yPoint = this.ActualHeight + yTextOffset * 0.333 + 2.5 - (this.ye + this.ys) * 0.5;
                                }
                                else if (character == PeakLinkFeatureEnum.Adduct) {
                                    formattedText.TextAlignment = TextAlignment.Center;
                                    xPoint = this.xe;
                                    yPoint = this.ActualHeight - this.ye + this.labelYDistance * 0.5;
                                }
                                else {
                                    xPoint = this.xe + xOffset * 0.333;
                                    yPoint = this.ActualHeight + yTextOffset * 0.2 - this.ye;
                                }

                                var isLabeledOverlaped = false;
                                foreach (var yLabeledPos in yLabelPositions) {
                                    if (Math.Abs(yLabeledPos - yPoint) < 30) {
                                        isLabeledOverlaped = true;
                                        break;
                                    }
                                }

                                if (isLabeledOverlaped == false || character == PeakLinkFeatureEnum.Adduct) {
                                    this.drawingContext.DrawText(formattedText, new Point(xPoint, yPoint));
                                    yLabelPositions.Add(yPoint);
                                }
                                this.drawingContext.Pop();
                                this.drawingContext.Pop();

                            }
                        }
                    }
                }

                //if (spot.IsotopeParentPeakID >= 0) {
                //    var parentID = spot.IsotopeParentPeakID;

                //    this.xs = this.pairwisePlotPeakViewUI.LeftMargin +
                //        (this.pairwisePlotBean.XAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                //    this.ys = this.pairwisePlotPeakViewUI.BottomMargin +
                //        (this.pairwisePlotBean.YAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                //    foreach (var sameParent in peakCollection.Where(n => n.IsotopeParentPeakID == parentID)) {

                //        this.xe = this.pairwisePlotPeakViewUI.LeftMargin +
                //                (this.pairwisePlotBean.XAxisDatapointCollection[sameParent.PeakID] -
                //                (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                //        this.ye = this.pairwisePlotPeakViewUI.BottomMargin +
                //            (this.pairwisePlotBean.YAxisDatapointCollection[sameParent.PeakID] -
                //            (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                //        if (Math.Abs(this.ye - this.ys) >= 20) {

                //            var offset = this.ye - this.ys > 0 ? -10 : 10;

                //            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1),
                //                new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset),
                //                new Point(this.xe - 20, this.ye + offset));
                //            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1),
                //                new Point(this.xs, this.ys),
                //                new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset));
                //            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1),
                //                new Point(this.xe, this.ye),
                //                new Point(this.xe - 20, this.ye + offset));

                //            this.drawingContext.DrawEllipse(null,
                //                        new Pen(Brushes.Red, 1.0),
                //                        new Point(this.xe, this.ye),
                //                        this.pairwisePlotPeakViewUI.PlotSize,
                //                        this.pairwisePlotPeakViewUI.PlotSize);

                //            formattedText = new FormattedText("M + " + sameParent.IsotopeWeightNumber,
                //                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                //            formattedText.TextAlignment = TextAlignment.Center;

                //            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                //            this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                //            this.drawingContext.DrawText(formattedText, new Point(this.xe + 30, this.ActualHeight - this.ye - 8));

                //            this.drawingContext.Pop();
                //            this.drawingContext.Pop();

                //        }
                //    }
                //}

                //if (this.pairwisePlotBean.PeakAreaBeanCollection[this.pairwisePlotBean.SelectedPlotId].AdductIonName != string.Empty
                //    && this.pairwisePlotBean.PeakAreaBeanCollection[this.pairwisePlotBean.SelectedPlotId].AdductParent >= 0
                //    && this.pairwisePlotBean.PeakAreaBeanCollection[this.pairwisePlotBean.SelectedPlotId].IsotopeWeightNumber == 0) {
                //    var adductParent = this.pairwisePlotBean.PeakAreaBeanCollection[this.pairwisePlotBean.SelectedPlotId].AdductParent;

                //    this.xs = this.pairwisePlotPeakViewUI.LeftMargin +
                //        (this.pairwisePlotBean.XAxisDatapointCollection[this.pairwisePlotBean.SelectedPlotId] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                //    this.ys = this.pairwisePlotPeakViewUI.BottomMargin +
                //            (this.pairwisePlotBean.YAxisDatapointCollection[this.pairwisePlotBean.SelectedPlotId] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                //    foreach (var sameParentPeak in this.pairwisePlotBean.PeakAreaBeanCollection.Where(n => n.AdductParent == adductParent)) {

                //        if (sameParentPeak.IsotopeWeightNumber != 0)
                //            continue;

                //        this.xe = this.pairwisePlotPeakViewUI.LeftMargin +
                //            (this.pairwisePlotBean.XAxisDatapointCollection[sameParentPeak.PeakID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                //        this.ye = this.pairwisePlotPeakViewUI.BottomMargin +
                //            (this.pairwisePlotBean.YAxisDatapointCollection[sameParentPeak.PeakID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                //        if (Math.Abs(this.ye - this.ys) >= 20) {

                //            var offset = this.ye - this.ys > 0 ? -10 : 10;

                //            this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset), new Point(this.xe - 20, this.ye + offset));
                //            this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xs, this.ys), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset));
                //            this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xe, this.ye), new Point(this.xe - 20, this.ye + offset));

                //            this.drawingContext.DrawEllipse(null,
                //                        new Pen(Brushes.Orange, 1.0),
                //                        new Point(this.xe, this.ye),
                //                        this.pairwisePlotPeakViewUI.PlotSize,
                //                        this.pairwisePlotPeakViewUI.PlotSize);

                //            formattedText = new FormattedText(sameParentPeak.AdductIonName,
                //                       CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                //            formattedText.TextAlignment = TextAlignment.Center;

                //            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                //            this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                //            this.drawingContext.DrawText(formattedText, new Point(this.xe, this.ActualHeight - this.ye - 25));

                //            this.drawingContext.Pop();
                //            this.drawingContext.Pop();
                //        }
                //    }
                //}

            }
            #endregion
            else if (this.pairwisePlotBean.DriftSpots != null && this.pairwisePlotBean.DriftSpots.Count > 0) {

                var driftSpots = this.pairwisePlotBean.DriftSpots;
                var spotID = this.pairwisePlotBean.SelectedPlotId;
                var spot = driftSpots[spotID];

                this.xt = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.XAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.YAxisDatapointCollection[spotID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0),
                    new Point(this.pairwisePlotPeakViewUI.LeftMargin, this.yt),
                    new Point(this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin, this.yt));

                this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0),
                    new Point(this.xt, this.pairwisePlotPeakViewUI.BottomMargin),
                    new Point(this.xt, this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin));

                this.xs = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.RectangleRangeXmin - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.xe = this.pairwisePlotPeakViewUI.LeftMargin +
                    (this.pairwisePlotBean.RectangleRangeXmax - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.ys = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.RectangleRangeYmin - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                this.ye = this.pairwisePlotPeakViewUI.BottomMargin +
                    (this.pairwisePlotBean.RectangleRangeYmax - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                this.drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(50, 200, 255, 255)), new Pen(Brushes.Cyan, 1), new Rect(new Point(this.xs, this.ys), new Point(this.xe, this.ye)));

            }
        }

		// draws the spots representing the features found on LC data
        private void drawPairwisePlots()
        {
            var displayedSpotCount = 0;
            for (int i = 0; i < this.pairwisePlotBean.XAxisDatapointCollection.Count; i++)
            {
                
                if (!peakAreaBeanDisplayFilter(this.pairwisePlotBean, i)) continue;
                if (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX < 0) continue;
                if (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMaxX > 0) continue;
                if (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY < 0) continue;
                if (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMaxY > 0) continue;

                this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotPeakViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                this.drawingContext.DrawEllipse(this.pairwisePlotBean.PlotBrushCollection[i], new Pen(this.pairwisePlotBean.PlotBrushCollection[i], 0.1),
                    new Point(this.xt, this.yt), this.pairwisePlotPeakViewUI.PlotSize, this.pairwisePlotPeakViewUI.PlotSize);


                drawDisplayLabels(this.pairwisePlotBean, i, this.ActualHeight);
                displayedSpotCount++;

                if (Math.Abs(this.xt - this.pairwisePlotPeakViewUI.CurrentMousePoint.X) < this.pairwisePlotPeakViewUI.PlotSize
                    && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotPeakViewUI.CurrentMousePoint.Y)) < this.pairwisePlotPeakViewUI.PlotSize)
                {
                    this.drawingContext.DrawEllipse(null, new Pen(this.pairwisePlotBean.PlotBrushCollection[i], 1), new Point(this.xt, this.yt),
                        this.pairwisePlotPeakViewUI.PlotSize + 2, this.pairwisePlotPeakViewUI.PlotSize + 2);

                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                    if (this.pairwisePlotBean.PeakAreaBeanCollection != null && this.pairwisePlotBean.PeakAreaBeanCollection.Count > 0) {
                        formattedText = new FormattedText("Spot ID: " + i.ToString() + "; Scan #: " + this.pairwisePlotBean.PeakAreaBeanCollection[i].ScanNumberAtPeakTop +
                            "; RT[min]: " + Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 3).ToString() +
                            "; Mass[Da]: " + Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 4).ToString(), CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);

                    } else if (this.pairwisePlotBean.DriftSpots != null && this.pairwisePlotBean.DriftSpots.Count > 0) {
                        formattedText = new FormattedText("Drift ID: " + this.pairwisePlotBean.DriftSpots[i].PeakID + 
                            "; Parent scan #: " + this.pairwisePlotBean.DriftSpots[i].PeakAreaBeanID +
                            "; Drift time[millisecond]: " + Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 3).ToString() +
                            "; Mass[Da]: " + Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 4).ToString(), CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                    }
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt - this.labelYDistance));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }

            this.pairwisePlotBean.DisplayedSpotCount = displayedSpotCount;
        }

		// checks if the current spot should be painted or not
        private bool ms1DecDisplayFilter(PairwisePlotBean plot, int i)
        {
            if (plot.Ms1DecResults[i].AmplitudeScore < plot.AmplitudeDisplayLowerFilter * 0.01) return false;
            if (plot.Ms1DecResults[i].AmplitudeScore > plot.AmplitudeDisplayUpperFilter * 0.01) return false;

            if (plot.Ms1DecResults[i].ScanNumber != plot.ScanNumber && plot.ScanNumber > -1) return false;

            if (plot.DisplayRangeMinX - 1 > plot.Ms1DecResults[i].RetentionTime || plot.Ms1DecResults[i].RetentionTime > plot.DisplayRangeMaxX + 1) return false;

            var isDisplayed = false;
            if (!plot.AnnotatedOnlyDisplayFilter && !plot.IdentifiedOnlyDisplayFilter && !plot.UnknownFilter)
                isDisplayed = true;

            if (plot.AnnotatedOnlyDisplayFilter || plot.IdentifiedOnlyDisplayFilter) {
                if (plot.Ms1DecResults[i].MspDbID >= 0) isDisplayed = true;
            }

            if (plot.UnknownFilter) {
                if (plot.Ms1DecResults[i].MspDbID < 0) isDisplayed = true;
            }
            return isDisplayed;

            //if (plot.AnnotatedOnlyDisplayFilter || plot.IdentifiedOnlyDisplayFilter)
            //{
            //    if (plot.Ms1DecResults[i].MspDbID < 0) return false;
            //}
            //else if (plot.UnknownFilter)
            //{
            //    if (plot.Ms1DecResults[i].MspDbID >= 0) return false;
            //}
            //return true;

        }

        private void drawDisplayLabels(PairwisePlotBean plot, int i, double drawHeight)
        {
            if (plot.DisplayLabel == PairwisePlotDisplayLabel.None)
            {

            }
            else if (plot.DisplayLabel == PairwisePlotDisplayLabel.Metabolite)
            {
                if (plot.Ms1DecResults != null && plot.Ms1DecResults.Count > 0) return;

                if (plot.PeakAreaBeanCollection != null && plot.PeakAreaBeanCollection.Count > 0) {
                    if (plot.PeakAreaBeanCollection[i].MetaboliteName != string.Empty && plot.PeakAreaBeanCollection[i].MetaboliteName != null) {
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                        formattedText = new FormattedText(this.pairwisePlotBean.PeakAreaBeanCollection[i].MetaboliteName,
                            CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                        formattedText.TextAlignment = TextAlignment.Center;

                        this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                        this.drawingContext.Pop();
                        this.drawingContext.Pop();
                    }
                } else if (plot.DriftSpots != null && plot.DriftSpots.Count > 0) {
                    if (plot.DriftSpots[i].MetaboliteName != string.Empty && plot.DriftSpots[i].MetaboliteName != null) {
                        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                        formattedText = new FormattedText(this.pairwisePlotBean.DriftSpots[i].MetaboliteName,
                            CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                        formattedText.TextAlignment = TextAlignment.Center;

                        this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                        this.drawingContext.Pop();
                        this.drawingContext.Pop();
                    }
                }
            }
            else if (plot.DisplayLabel == PairwisePlotDisplayLabel.X)
            {
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                formattedText = new FormattedText(Math.Round(plot.XAxisDatapointCollection[i], 3).ToString(), CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                formattedText.TextAlignment = TextAlignment.Center;

                this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                this.drawingContext.Pop();
                this.drawingContext.Pop();
            }
            else if (plot.DisplayLabel == PairwisePlotDisplayLabel.Y)
            {
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                formattedText = new FormattedText(Math.Round(plot.YAxisDatapointCollection[i], 5).ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new Typeface("Calibri"), 13, Brushes.Gray);
                formattedText.TextAlignment = TextAlignment.Center;

                this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                this.drawingContext.Pop();
                this.drawingContext.Pop();
            }
            else if (plot.DisplayLabel == PairwisePlotDisplayLabel.Isotope)
            {
                if (plot.PeakAreaBeanCollection != null && plot.PeakAreaBeanCollection.Count > 0) {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText("M + " + plot.PeakAreaBeanCollection[i].IsotopeWeightNumber.ToString(),
                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                } else if (plot.DriftSpots != null && plot.DriftSpots.Count > 0) {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText("M + " + plot.DriftSpots[i].IsotopeWeightNumber.ToString(),
                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Adduct)
            {
                if (plot.Ms1DecResults != null && plot.Ms1DecResults.Count > 0) return;
                if (plot.PeakAreaBeanCollection != null && plot.PeakAreaBeanCollection.Count > 0) {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText(plot.PeakAreaBeanCollection[i].AdductIonName, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    if (plot.PeakAreaBeanCollection[i].AdductFromAmalgamation != null && plot.PeakAreaBeanCollection[i].AdductFromAmalgamation.FormatCheck == true) {
                        formattedText = new FormattedText(plot.PeakAreaBeanCollection[i].AdductFromAmalgamation.AdductIonName + " (Amal.)", CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Red);
                        formattedText.TextAlignment = TextAlignment.Center;

                        this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5 * 2.0));
                    }

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                } else if (plot.DriftSpots != null && plot.DriftSpots.Count > 0) {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -drawHeight));

                    formattedText = new FormattedText(plot.DriftSpots[i].AdductIonName, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, drawHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
        }

		// checks if a spot should be painted on the plot or not
        private bool peakAreaBeanDisplayFilter(PairwisePlotBean plot, int i)
        {
            if (plot.PeakAreaBeanCollection != null && plot.PeakAreaBeanCollection.Count > 0) {

                if (plot.PeakAreaBeanCollection[i].AmplitudeScoreValue < plot.AmplitudeDisplayLowerFilter * 0.01) return false;
                if (plot.PeakAreaBeanCollection[i].AmplitudeScoreValue > plot.AmplitudeDisplayUpperFilter * 0.01) return false;
                if (plot.MolcularIonFilter && plot.PeakAreaBeanCollection[i].IsotopeWeightNumber > 0) return false;
                if (plot.DisplayRangeMinX - 1 > plot.XAxisDatapointCollection[i] || plot.XAxisDatapointCollection[i] > plot.DisplayRangeMaxX + 1) return false;
                if (plot.DisplayRangeMinY - 5 > plot.YAxisDatapointCollection[i] || plot.YAxisDatapointCollection[i] > plot.DisplayRangeMaxY + 5) return false;

                // added scan# filter
                if (plot.ScanNumber != plot.PeakAreaBeanCollection[i].ScanNumberAtPeakTop && plot.ScanNumber > -1) return false;

                if (plot.Ms1DecResults == null || plot.Ms1DecResults.Count == 0) {
                    return isDisplayed(plot, i, plot.PeakAreaBeanCollection);

                    //if (plot.AnnotatedOnlyDisplayFilter && plot.IdentifiedOnlyDisplayFilter) {
                    //    if (plot.PeakAreaBeanCollection[i].MetaboliteName == string.Empty) return false;
                    //}
                    //else if (plot.AnnotatedOnlyDisplayFilter) {
                    //    if (!plot.PeakAreaBeanCollection[i].MetaboliteName.Contains("w/o")) return false;
                    //}
                    //else if (plot.IdentifiedOnlyDisplayFilter) {
                    //    if (plot.PeakAreaBeanCollection[i].MetaboliteName == string.Empty ||
                    //        plot.PeakAreaBeanCollection[i].MetaboliteName == "Unknown" ||
                    //        this.pairwisePlotBean.PeakAreaBeanCollection[i].MetaboliteName.Contains("w/o")) return false;
                    //}
                    //else if (plot.UnknownFilter) {
                    //    if (plot.PeakAreaBeanCollection[i].LibraryID >= 0 || plot.PeakAreaBeanCollection[i].PostIdentificationLibraryId >= 0) return false;
                    //}

                    //if (plot.MsmsOnlyDisplayFilter && plot.PeakAreaBeanCollection[i].Ms2LevelDatapointNumber < 0) return false;
                    //if (plot.UniqueionFilter && !plot.PeakAreaBeanCollection[i].IsFragmentQueryExist) return false;
                }
            } else if (plot.DriftSpots != null && plot.DriftSpots.Count > 0) {
                if (plot.DriftSpots[i].AmplitudeScoreValue < plot.AmplitudeDisplayLowerFilter * 0.01) return false;
                if (plot.DriftSpots[i].AmplitudeScoreValue > plot.AmplitudeDisplayUpperFilter * 0.01) return false;
                if (plot.MolcularIonFilter && plot.DriftSpots[i].IsotopeWeightNumber > 0) return false;
                if (plot.DisplayRangeMinX - 1 > plot.XAxisDatapointCollection[i] || plot.XAxisDatapointCollection[i] > plot.DisplayRangeMaxX + 1) return false;
                if (plot.DisplayRangeMinY - 5 > plot.YAxisDatapointCollection[i] || plot.YAxisDatapointCollection[i] > plot.DisplayRangeMaxY + 5) return false;

                if (plot.ScanNumber != plot.DriftSpots[i].DriftScanAtPeakTop && plot.ScanNumber > -1) return false;

                if (plot.Ms1DecResults == null || plot.Ms1DecResults.Count == 0) {
                    return isDisplayed(plot, i, plot.DriftSpots);
                }
            }

            return true;
        }

        private bool isDisplayed(PairwisePlotBean plot, int id, ObservableCollection<PeakAreaBean> peakSpots) {
            var isDriftData = peakSpots[id].DriftSpots != null && peakSpots[id].DriftSpots.Count > 0;

            var metabolite = peakSpots[id].MetaboliteName;
            var isDisplayed = false;

            if (!plot.AnnotatedOnlyDisplayFilter && !plot.IdentifiedOnlyDisplayFilter && !plot.UnknownFilter && !plot.CcsFilter)
                isDisplayed = true;

            if (plot.IdentifiedOnlyDisplayFilter &&
                metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                isDisplayed = true;
            if (plot.AnnotatedOnlyDisplayFilter &&
                metabolite != string.Empty && metabolite.Contains("w/o"))
                isDisplayed = true;
            if (plot.CcsFilter && peakSpots[id].IsCcsMatch)
                isDisplayed = true;
            if (plot.UnknownFilter &&
                (metabolite == string.Empty || metabolite.Contains("Unknown")))
                isDisplayed = true;

            if (plot.UniqueionFilter &&
                !peakSpots[id].IsFragmentQueryExist)
                isDisplayed = false;
            if (isDriftData) {
                if (plot.MsmsOnlyDisplayFilter &&
                    peakSpots[id].DriftSpots.Count(n => n.Ms2LevelDatapointNumber >= 0) == 0)
                    isDisplayed = false;
            }
            else {
                if (plot.MsmsOnlyDisplayFilter &&
                    peakSpots[id].Ms2LevelDatapointNumber < 0)
                    isDisplayed = false;
            }

            return isDisplayed;
        }

        private bool isDisplayed(PairwisePlotBean plot, int id, ObservableCollection<DriftSpotBean> driftSpots) {

            var metabolite = driftSpots[id].MetaboliteName;
            var isDisplayed = false;

            if (!plot.AnnotatedOnlyDisplayFilter && !plot.IdentifiedOnlyDisplayFilter && !plot.UnknownFilter && !plot.CcsFilter)
                isDisplayed = true;

            if (plot.IdentifiedOnlyDisplayFilter &&
                metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                isDisplayed = true;
            if (plot.AnnotatedOnlyDisplayFilter &&
                metabolite != string.Empty && metabolite.Contains("w/o"))
                isDisplayed = true;
            if (plot.CcsFilter && driftSpots[id].IsCcsMatch)
                isDisplayed = true;
            if (plot.UnknownFilter &&
                (metabolite == string.Empty || metabolite.Contains("Unknown")))
                isDisplayed = true;

            if (plot.UniqueionFilter &&
                !driftSpots[id].IsFragmentQueryExist)
                isDisplayed = false;
            if (plot.MsmsOnlyDisplayFilter &&
                driftSpots[id].Ms2LevelDatapointNumber < 0)
                isDisplayed = false;

            return isDisplayed;
        }

        // writes text to the (left,top) of the plot
        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotPeakViewUI.LeftMargin, this.pairwisePlotPeakViewUI.TopMargin - 17));
        }

		// draws the X axis' tic marks
        private void drawScaleOnXAxis(float xAxisMinValue, float xAxisMaxValue, double drawWidth, double drawHeight)
        {
            getXaxisScaleInterval((double)xAxisMinValue, (double)xAxisMaxValue, drawWidth);
            int xStart = (int)(xAxisMinValue / (double)this.xMinorScale) - 1;
            int xEnd = (int)(xAxisMaxValue / (double)this.xMinorScale) + 1;

            double xAxisValue, xPixelValue;
            for (int i = xStart; i <= xEnd; i++)
            {
                xAxisValue = i * (double)this.xMinorScale;
                xPixelValue = this.pairwisePlotPeakViewUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.pairwisePlotPeakViewUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.pairwisePlotPeakViewUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotPeakViewUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

		// draws the tics on the X axis
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
            int xAxisPixelRange = (int)(drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin);
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


		// draws the tics on the X axis
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
                yPixelValue = drawHeight - this.pairwisePlotPeakViewUI.BottomMargin - (yAxisValue - yAxisMinValue) * this.yPacket;
                if (yPixelValue > drawHeight - this.pairwisePlotPeakViewUI.BottomMargin) continue;
                if (yPixelValue < this.pairwisePlotPeakViewUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    if (this.yMajorScale < 1)
                        this.formattedText = new FormattedText(yAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(yAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                    this.drawingContext.PushTransform(new RotateTransform(270.0));
                    this.drawingContext.DrawText(formattedText, new Point(drawHeight - yPixelValue, this.pairwisePlotPeakViewUI.LeftMargin - this.formattedText.Height - this.longScaleSize - this.axisFromGraphArea - 1));
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotPeakViewUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
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
            int yAxisPixelRange = (int)(drawHeight - this.pairwisePlotPeakViewUI.TopMargin - this.pairwisePlotPeakViewUI.BottomMargin);
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
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotPeakViewUI.LeftMargin + 0.5 * (drawWidth - this.pairwisePlotPeakViewUI.LeftMargin - this.pairwisePlotPeakViewUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.pairwisePlotPeakViewUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.TopMargin)));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            formattedText = new FormattedText(this.pairwisePlotBean.YAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.pairwisePlotPeakViewUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.TopMargin))));
        }

        private double toRoundUp(double dValue, int iDigits)
        {
            double dCoef = Math.Pow(10, iDigits);

            return dValue > 0 ? Math.Ceiling(dValue * dCoef) / dCoef :
                                Math.Floor(dValue * dCoef) / dCoef;
        }

        private double toRoundDown(double dValue, int iDigits)
        {
            double dCoef = Math.Pow(10, iDigits);

            return dValue > 0 ? Math.Floor(dValue * dCoef) / dCoef :
                                Math.Ceiling(dValue * dCoef) / dCoef;
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



        public void PlotFocus()
        {
            if (Math.Abs(this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.X) > this.pairwisePlotPeakViewUI.PlotSize 
                || Math.Abs(this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.Y - this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.Y) > this.pairwisePlotPeakViewUI.PlotSize) return;

            if (this.pairwisePlotBean == null || this.pairwisePlotBean.XAxisDatapointCollection == null || this.pairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (this.pairwisePlotPeakViewUI.CurrentMousePoint.Y >= this.pairwisePlotPeakViewUI.TopMargin
                && this.pairwisePlotPeakViewUI.CurrentMousePoint.Y <= this.pairwisePlotPeakViewUI.TopMargin + this.pairwisePlotPeakViewUI.TriangleSize * 2)
            {
                if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) return;
                for (int i = 0; i < this.pairwisePlotBean.Ms1DecResults.Count; i++)
                {
                    if (this.pairwisePlotBean.Ms1DecResults[i].AmplitudeScore < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                    if (this.pairwisePlotBean.Ms1DecResults[i].AmplitudeScore > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) continue;
                   
                    if (this.pairwisePlotBean.AnnotatedOnlyDisplayFilter || this.pairwisePlotBean.IdentifiedOnlyDisplayFilter)
                    {
                        if (this.pairwisePlotBean.Ms1DecResults[i].MspDbID < 0) continue;
                    }
                    else if (this.pairwisePlotBean.UnknownFilter == true)
                    {
                        if (this.pairwisePlotBean.Ms1DecResults[i].MspDbID >= 0) continue;
                    }

                    this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.Ms1DecResults[i].RetentionTime - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                    if (Math.Abs(this.xt - this.pairwisePlotPeakViewUI.CurrentMousePoint.X) < this.pairwisePlotPeakViewUI.TriangleSize)
                    {
                        this.pairwisePlotBean.SelectedMs1DecID = i;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < this.pairwisePlotBean.XAxisDatapointCollection.Count; i++)
                {
                    if (this.pairwisePlotBean.PeakAreaBeanCollection != null && this.pairwisePlotBean.PeakAreaBeanCollection.Count > 0) {
                        var spot = this.pairwisePlotBean.PeakAreaBeanCollection[i];
                        var isDriftData = spot.DriftSpots != null && spot.DriftSpots.Count > 0 ? true : false;
                        if (this.pairwisePlotBean.PeakAreaBeanCollection[i].AmplitudeScoreValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                        if (this.pairwisePlotBean.PeakAreaBeanCollection[i].AmplitudeScoreValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) continue;
                        if (this.pairwisePlotBean.PeakAreaBeanCollection[i].AmplitudeScoreValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                        if (isDriftData) {
                            if ((this.pairwisePlotBean.MsmsOnlyDisplayFilter &&
                                spot.DriftSpots.Count(n => n.Ms2LevelDatapointNumber >= 0) == 0)) continue;
                        }
                        else {
                            if (this.pairwisePlotBean.MsmsOnlyDisplayFilter && 
                                this.pairwisePlotBean.PeakAreaBeanCollection[i].Ms2LevelDatapointNumber < 0) continue;
                        }
                        if (this.pairwisePlotBean.MolcularIonFilter && this.pairwisePlotBean.PeakAreaBeanCollection[i].IsotopeWeightNumber > 0) continue;
                        if (this.pairwisePlotBean.UniqueionFilter && this.pairwisePlotBean.PeakAreaBeanCollection[i].IsFragmentQueryExist == false) continue;

                        if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) {

                            var displayed = isDisplayed(this.pairwisePlotBean, i, this.pairwisePlotBean.PeakAreaBeanCollection);
                            if (displayed == false) continue;
                            
                        }

                        this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        this.yt = this.pairwisePlotPeakViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                        if (this.xt < this.pairwisePlotPeakViewUI.LeftMargin || this.xt > this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin) continue;
                        if (this.yt < this.pairwisePlotPeakViewUI.BottomMargin || this.yt > this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin) continue;

                        if (Math.Abs(this.xt - this.pairwisePlotPeakViewUI.CurrentMousePoint.X) < this.pairwisePlotPeakViewUI.PlotSize && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotPeakViewUI.CurrentMousePoint.Y)) < this.pairwisePlotPeakViewUI.PlotSize) {
                            this.pairwisePlotBean.SelectedPlotId = i;
                            break;
                        }
                    } else if (this.pairwisePlotBean.DriftSpots != null && this.pairwisePlotBean.DriftSpots.Count > 0) {

                        if (this.pairwisePlotBean.DriftSpots[i].AmplitudeScoreValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                        if (this.pairwisePlotBean.DriftSpots[i].AmplitudeScoreValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) continue;
                        if (this.pairwisePlotBean.DriftSpots[i].AmplitudeScoreValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                        if (this.pairwisePlotBean.MsmsOnlyDisplayFilter && this.pairwisePlotBean.DriftSpots[i].Ms2LevelDatapointNumber < 0) continue;
                        if (this.pairwisePlotBean.MolcularIonFilter && this.pairwisePlotBean.DriftSpots[i].IsotopeWeightNumber > 0) continue;
                        if (this.pairwisePlotBean.UniqueionFilter && this.pairwisePlotBean.DriftSpots[i].IsFragmentQueryExist == false) continue;

                        if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) {

                            var displayed = isDisplayed(this.pairwisePlotBean, i, this.pairwisePlotBean.DriftSpots);
                            if (displayed == false) continue;
                        }

                        this.xt = this.pairwisePlotPeakViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        this.yt = this.pairwisePlotPeakViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                        if (this.xt < this.pairwisePlotPeakViewUI.LeftMargin || this.xt > this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin) continue;
                        if (this.yt < this.pairwisePlotPeakViewUI.BottomMargin || this.yt > this.ActualHeight - this.pairwisePlotPeakViewUI.TopMargin) continue;

                        if (Math.Abs(this.xt - this.pairwisePlotPeakViewUI.CurrentMousePoint.X) < this.pairwisePlotPeakViewUI.PlotSize 
                            && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotPeakViewUI.CurrentMousePoint.Y)) < this.pairwisePlotPeakViewUI.PlotSize) {
                            this.pairwisePlotBean.SelectedPlotId = i;
                            break;
                        }
                    }
                }
            }
		}

		public void GraphZoom()
        {
            // Avoid Miss Double Click Operation (if clicks separated by less than 5 units -- pixels?)
            if (Math.Abs(this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.Y - this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
                return;

            // Zoom X-Coordinate        
            if (this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X > this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X)
            {
                if (this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X > this.pairwisePlotPeakViewUI.LeftMargin)
                {
                    if (this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X >= this.pairwisePlotPeakViewUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X - this.pairwisePlotPeakViewUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X > this.pairwisePlotPeakViewUI.LeftMargin)
                {
                    if (this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.pairwisePlotPeakViewUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X - this.pairwisePlotPeakViewUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X >= this.pairwisePlotPeakViewUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.Y > this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.Y)
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotPeakViewUI.BottomMargin - this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.X == -1 || this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.Y == -1)
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
            if (this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.X > this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.X - this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.X;

                newMinX = this.pairwisePlotPeakViewUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotPeakViewUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

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
                distanceX = this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.X - this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.X;

                newMinX = this.pairwisePlotPeakViewUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotPeakViewUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

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
            if (this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.Y < this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.Y - this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.Y;

                newMinY = this.pairwisePlotPeakViewUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotPeakViewUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

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
                distanceY = this.pairwisePlotPeakViewUI.LeftButtonStartClickPoint.Y - this.pairwisePlotPeakViewUI.LeftButtonEndClickPoint.Y;

                newMinY = this.pairwisePlotPeakViewUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotPeakViewUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

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
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.X, this.pairwisePlotPeakViewUI.RightButtonStartClickPoint.Y), new Point(this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.X, this.pairwisePlotPeakViewUI.RightButtonEndClickPoint.Y)));
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

            x_Value = (float)this.pairwisePlotBean.DisplayRangeMinX + (float)((mousePoint.X - this.pairwisePlotPeakViewUI.LeftMargin) / this.xPacket);
            y_Value = (float)this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - mousePoint.Y - this.pairwisePlotPeakViewUI.BottomMargin) / this.yPacket);

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
