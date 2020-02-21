using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class PairwisePlotAlignmentViewFE: FrameworkElement
    {
        private VisualCollection visualCollection;//絵を描くための画用紙みたいなもの
        private DrawingVisual drawingVisual;//絵を描くための筆とかパレットみたいなもの
        private DrawingContext drawingContext;//絵を描く人

        private PairwisePlotAlignmentViewUI pairwisePlotAlignmentViewUI;
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

        public PairwisePlotAlignmentViewFE(PairwisePlotBean pairwisePlotBean, PairwisePlotAlignmentViewUI pairwisePlotAlignmentViewUI)
        {
            this.visualCollection = new VisualCollection(this);
            this.pairwisePlotBean = pairwisePlotBean;
            this.pairwisePlotAlignmentViewUI = pairwisePlotAlignmentViewUI;

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
            if (drawWidth < this.pairwisePlotAlignmentViewUI.LeftMargin + this.pairwisePlotAlignmentViewUI.RightMargin || drawHeight < this.pairwisePlotAlignmentViewUI.BottomMargin + this.pairwisePlotAlignmentViewUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            //FormattedText formattedText;
            
            SolidColorBrush graphGreenBrush = combineAlphaAndColor(0.25, Brushes.Green);;
            Pen graphGreenPen = new Pen(Brushes.Green, 1);
            graphGreenBrush.Freeze();
            graphGreenPen.Freeze();
            
            SolidColorBrush graphRedBrush = combineAlphaAndColor(0.25, Brushes.Red); ;
            Pen graphRedPen = new Pen(Brushes.Red, 1);
            graphRedBrush.Freeze();
            graphRedPen.Freeze();
            
            SolidColorBrush graphBlackBrush = combineAlphaAndColor(0.8, Brushes.Black); ;
            Pen graphBlackPen = new Pen(Brushes.Black, 1);
            graphBlackBrush.Freeze();
            graphBlackPen.Freeze();
            
            SolidColorBrush graphOrangeBrush = combineAlphaAndColor(0.8, Brushes.Orange); ;
            Pen graphOrangePen = new Pen(Brushes.Orange, 1);
            graphBlackBrush.Freeze();
            graphBlackPen.Freeze();

            // 1. Draw background, graphRegion, x-axis, y-axis 
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.pairwisePlotAlignmentViewUI.LeftMargin, this.pairwisePlotAlignmentViewUI.TopMargin), new Size(drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.TopMargin)));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin), new Point(drawWidth - this.pairwisePlotAlignmentViewUI.RightMargin, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin));
            this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.axisFromGraphArea, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin), new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.axisFromGraphArea, this.pairwisePlotAlignmentViewUI.TopMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.pairwisePlotBean.XAxisDatapointCollection == null)
            {
                // Calculate Packet Size
                this.xPacket = (drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin) / 100;
                this.yPacket = (drawHeight - this.pairwisePlotAlignmentViewUI.TopMargin - this.pairwisePlotAlignmentViewUI.BottomMargin) / 100;

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
            this.xPacket = (drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxX - this.pairwisePlotBean.DisplayRangeMinX);
            this.yPacket = (drawHeight - this.pairwisePlotAlignmentViewUI.TopMargin - this.pairwisePlotAlignmentViewUI.BottomMargin) / (double)(this.pairwisePlotBean.DisplayRangeMaxY - this.pairwisePlotBean.DisplayRangeMinY);
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
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.pairwisePlotAlignmentViewUI.LeftMargin, this.pairwisePlotAlignmentViewUI.BottomMargin, drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.TopMargin)));

            // 5. Draw plot
            #region
            drawPairwisePlots(graphGreenBrush, graphGreenPen, graphRedBrush, graphRedPen, graphBlackBrush, graphBlackPen, graphOrangeBrush, graphOrangePen);
            
            #endregion

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return this.drawingVisual;
        }

        private void drawPairwisePlots(SolidColorBrush graphGreenBrush, Pen graphGreenPen, SolidColorBrush graphRedBrush, Pen graphRedPen, SolidColorBrush graphBlackBrush, Pen graphBlackPen, SolidColorBrush graphOrangeBrush, Pen graphOrangePen)
        {
            var displayedSpotCount = 0;
            for (int i = 0; i < this.pairwisePlotBean.XAxisDatapointCollection.Count; i++)
            {
                if (!displayFiltering(i)) continue;
                
                this.xt = this.pairwisePlotAlignmentViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotAlignmentViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                this.drawingContext.DrawEllipse(this.pairwisePlotBean.PlotBrushCollection[i], new Pen(this.pairwisePlotBean.PlotBrushCollection[i], 0.1), new Point(this.xt, this.yt), this.pairwisePlotAlignmentViewUI.PlotSize, this.pairwisePlotAlignmentViewUI.PlotSize);

                drawDisplayLables(i);
                displayedSpotCount++;

                if (Math.Abs(this.xt - this.pairwisePlotAlignmentViewUI.CurrentMousePoint.X) < this.pairwisePlotAlignmentViewUI.PlotSize 
                    && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotAlignmentViewUI.CurrentMousePoint.Y)) < this.pairwisePlotAlignmentViewUI.PlotSize)
                {
                    mouseFocused(i);
                }

                if (this.pairwisePlotBean.SelectedPlotId >= 0 && i == this.pairwisePlotBean.SelectedPlotId)
                    drawFocusedSpots(i, graphGreenBrush, graphGreenPen, graphRedBrush, graphRedPen, graphBlackBrush, graphBlackPen, graphOrangeBrush, graphOrangePen);
            }
            this.pairwisePlotBean.DisplayedSpotCount = displayedSpotCount;
        }

        private void drawFocusedSpots(int i, SolidColorBrush graphGreenBrush, Pen graphGreenPen, SolidColorBrush graphRedBrush, Pen graphRedPen, SolidColorBrush graphBlackBrush, Pen graphBlackPen, SolidColorBrush graphOrangeBrush, Pen graphOrangePen)
        {
            this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
            this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

            if (this.pairwisePlotBean.AlignmentPropertyBeanCollection != null && this.pairwisePlotBean.AlignmentPropertyBeanCollection.Count > 0) {
                var maxIntensity = this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].MaxValiable;
                var minIntensity = this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].MinValiable;
                var maxPeaks = this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection.Count;

                var alignedSpots = this.pairwisePlotBean.AlignmentPropertyBeanCollection;
                var alignSpot = alignedSpots[i];

                var plotSize = this.pairwisePlotAlignmentViewUI.PlotSize;
                this.drawingContext.DrawEllipse(null, new Pen(Brushes.Red, 1),
                    new Point(this.xs, this.ys), this.pairwisePlotAlignmentViewUI.PlotSize + 1, this.pairwisePlotAlignmentViewUI.PlotSize + 1);

                for (int j = 0; j < alignSpot.AlignedPeakPropertyBeanCollection.Count; j++) {
                    if (Math.Abs((double)this.pairwisePlotBean.DisplayRangeMaxX - (double)this.pairwisePlotBean.DisplayRangeMinX) > 1.0) break;
                    if (Math.Abs((double)this.pairwisePlotBean.DisplayRangeMaxY - (double)this.pairwisePlotBean.DisplayRangeMinY) > 2.5) break;

                    #region
                    if (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].PeakID == -1) continue;

                    if (this.pairwisePlotBean.RetentionUnit == RetentionUnit.RT)
                        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin + (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].RetentionTime
                            - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                    else
                        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin + (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].RetentionIndex
                            - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;

                    if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) {
                        this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin + (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].AccurateMass
                            - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                    }
                    else {
                        double zoomPacket = 1.0;
                        if (this.pairwisePlotBean.DisplayRangeMaxY > this.pairwisePlotBean.DisplayRangeMinY)
                            zoomPacket = (double)(this.pairwisePlotBean.MaxY - this.pairwisePlotBean.MinY) / (double)(this.pairwisePlotBean.DisplayRangeMaxY - this.pairwisePlotBean.DisplayRangeMinY);
                        else zoomPacket = 6.0;
                        if (zoomPacket > 6.0) zoomPacket = 6.0;
                        this.ye = this.ys + 20 * zoomPacket * Math.Cos(Math.PI / (double)maxPeaks * (double)j);
                    }



                    if (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignedPeakPropertyBeanCollection[j].PeakID >= 0) {
                        this.drawingContext.DrawLine(graphGreenPen, new Point(this.xs, this.ys), new Point(this.xe, this.ye));
                        this.drawingContext.DrawEllipse(graphGreenBrush, graphGreenPen, new Point(this.xe, this.ye), plotSize, plotSize);
                    }
                    else {
                        this.drawingContext.DrawLine(graphRedPen, new Point(this.xs, this.ys), new Point(this.xe, this.ye));
                        this.drawingContext.DrawEllipse(graphRedBrush, graphRedPen, new Point(this.xe, this.ye), plotSize, plotSize);
                    }
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                    formattedText = new FormattedText(this.pairwisePlotBean.AlignmentPropertyBeanCollection[i]
                        .AlignedPeakPropertyBeanCollection[j].FileName,
                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, graphBlackBrush);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xe, this.ActualHeight - this.ye - plotSize + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                    #endregion
                }

                if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) {

                    if (alignSpot.PeakLinks != null) { // this means new version of ms-dail

                        this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin +
                                (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin +
                                (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                        var donelist = new List<string>();
                        var lineDoneList = new List<string>();
                        var yLabelPositions = new List<double>();
                        var spotID = alignSpot.AlignmentID;

                        foreach (var sameGroupPeak in alignedSpots.Where(n => n.PeakGroupID == alignSpot.PeakGroupID)) {
                            if (sameGroupPeak.AlignmentID != spotID) continue; // now peaks connected to the target peak are shown

                            this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin +
                                    (this.pairwisePlotBean.XAxisDatapointCollection[sameGroupPeak.AlignmentID] -
                                    (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                            this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin +
                                    (this.pairwisePlotBean.YAxisDatapointCollection[sameGroupPeak.AlignmentID] -
                                    (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                            // showing adduct information
                            #region
                            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                            this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                            formattedText = new FormattedText(sameGroupPeak.AdductIonName,
                                           CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                            formattedText.TextAlignment = TextAlignment.Center;
                            this.drawingContext.DrawText(formattedText, new Point(this.xs, this.ActualHeight - this.ys + this.labelYDistance * 0.5));

                            if (sameGroupPeak.AdductIonNameFromAmalgamation != null && sameGroupPeak.AdductIonNameFromAmalgamation != string.Empty) {
                                formattedText = new FormattedText(sameGroupPeak.AdductIonNameFromAmalgamation + " (Amal.)", CultureInfo.GetCultureInfo("en-us"),
                                FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Red);
                                formattedText.TextAlignment = TextAlignment.Center;

                                this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt + this.labelYDistance * 0.5 * 2.0));
                            }

                            this.drawingContext.Pop();
                            this.drawingContext.Pop();
                            #endregion

                            foreach (var linkedPeakCharacter in sameGroupPeak.PeakLinks) {

                                var character = linkedPeakCharacter.Character;
                                var linkedPeak = alignedSpots[linkedPeakCharacter.LinkedPeakID];

                                this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin +
                                (this.pairwisePlotBean.XAxisDatapointCollection[linkedPeak.AlignmentID] -
                                (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                                this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin +
                                    (this.pairwisePlotBean.YAxisDatapointCollection[linkedPeak.AlignmentID] -
                                    (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                                var blushColor = Brushes.Blue;
                                var xOffset = 30;
                                var yTextOffset = 30;

                                var characterString = string.Empty;
                                switch (character) {
                                    case PeakLinkFeatureEnum.CorrelSimilar:
                                        blushColor = Brushes.Red;
                                        characterString = "Ion correlated in samples";
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

                                var index = Math.Min(linkedPeak.AlignmentID, sameGroupPeak.AlignmentID) + "_" +
                                    Math.Max(linkedPeak.AlignmentID, sameGroupPeak.AlignmentID) + "_" +
                                    characterString;
                                if (donelist.Contains(index)) continue;
                                else donelist.Add(index);

                                var lineIndex = Math.Min(linkedPeak.AlignmentID, sameGroupPeak.AlignmentID) + "_" +
                                   Math.Max(linkedPeak.AlignmentID, sameGroupPeak.AlignmentID) + "_" +
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
                                                this.pairwisePlotAlignmentViewUI.PlotSize,
                                                this.pairwisePlotAlignmentViewUI.PlotSize);

                                    this.drawingContext.DrawEllipse(null,
                                                new Pen(blushColor, 1.0),
                                                new Point(this.xs, this.ys),
                                                this.pairwisePlotAlignmentViewUI.PlotSize,
                                                this.pairwisePlotAlignmentViewUI.PlotSize);

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
                                    else if (characterString == "Ion correlated in samples") {
                                        formattedText.TextAlignment = TextAlignment.Right;
                                        xPoint = this.xe - xOffset + 5;
                                        yPoint = this.ActualHeight + yTextOffset * 0.333 - (this.ye + this.ys) * 0.5;
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


                    if (alignSpot.PostDefinedAdductParentID >= 0 && alignSpot.AlignmentSpotVariableCorrelations != null) {

                        //this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin +
                        //(this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        //this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin +
                        //        (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;




                        //foreach (var sameParentSpot in 
                        //    this.pairwisePlotBean.AlignmentPropertyBeanCollection
                        //    .Where(n => n.PostDefinedAdductParentID == alignSpot.PostDefinedAdductParentID)) {

                        //    if (sameParentSpot.IsotopeTrackingWeightNumber > 0 || sameParentSpot.PostDefinedIsotopeWeightNumber > 0)
                        //        continue;
                        //    this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin +
                        //        (this.pairwisePlotBean.XAxisDatapointCollection[sameParentSpot.AlignmentID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        //    this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin +
                        //        (this.pairwisePlotBean.YAxisDatapointCollection[sameParentSpot.AlignmentID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                        //    if (Math.Abs(this.ye - this.ys) >= 20) {

                        //        var offset = this.ye - this.ys > 0 ? -10 : 10;

                        //        this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset), new Point(this.xe - 20, this.ye + offset));
                        //        this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xs, this.ys), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset));
                        //        this.drawingContext.DrawLine(new Pen(Brushes.Orange, 1), new Point(this.xe, this.ye), new Point(this.xe - 20, this.ye + offset));

                        //        this.drawingContext.DrawEllipse(null,
                        //                new Pen(Brushes.Orange, 1.0),
                        //                new Point(this.xe, this.ye),
                        //                this.pairwisePlotAlignmentViewUI.PlotSize,
                        //                this.pairwisePlotAlignmentViewUI.PlotSize);

                        //        formattedText = new FormattedText(sameParentSpot.AdductIonName,
                        //                CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                        //                formattedText.TextAlignment = TextAlignment.Center;

                        //        this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                        //        this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                        //        this.drawingContext.DrawText(formattedText, new Point(this.xe, this.ActualHeight - this.ye + 15));

                        //        this.drawingContext.Pop();
                        //        this.drawingContext.Pop();
                        //    }
                        //}





                        //if (alignSpot.AlignmentSpotVariableCorrelations != null && alignSpot.AlignmentSpotVariableCorrelations.Count > 0) {
                        //    foreach (var pairSpot in alignSpot.AlignmentSpotVariableCorrelations) {
                        //        var pairID = pairSpot.CorrelateAlignmentID;

                        //        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin +
                        //        (this.pairwisePlotBean.XAxisDatapointCollection[pairID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        //        this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin +
                        //            (this.pairwisePlotBean.YAxisDatapointCollection[pairID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                        //        if (Math.Abs(this.ye - this.ys) >= 20) {

                        //            var offset = this.ye - this.ys > 0 ? -10 : 10;

                        //            this.drawingContext.DrawLine(new Pen(Brushes.Black, 1), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset), new Point(this.xe - 20, this.ye + offset));
                        //            this.drawingContext.DrawLine(new Pen(Brushes.Black, 1), new Point(this.xs, this.ys), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset));
                        //            this.drawingContext.DrawLine(new Pen(Brushes.Black, 1), new Point(this.xe, this.ye), new Point(this.xe - 20, this.ye + offset));

                        //            this.drawingContext.DrawEllipse(null,
                        //                new Pen(Brushes.Black, 1.0),
                        //                new Point(this.xe, this.ye), 
                        //                this.pairwisePlotAlignmentViewUI.PlotSize, 
                        //                this.pairwisePlotAlignmentViewUI.PlotSize);

                        //            formattedText = new FormattedText("Corr. score: " + Math.Round(pairSpot.CorrelationScore * 100, 2) + "%",
                        //               CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
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
                }

                if (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].IsotopeTrackingParentID >= 0 &&
                    (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0)) {
                    var parentID = alignSpot.IsotopeTrackingParentID;

                    this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin +
                        (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                    this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin +
                            (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                    foreach (var spot in this.pairwisePlotBean.AlignmentPropertyBeanCollection.Where(n => n.IsotopeTrackingParentID == parentID)) {
                        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin +
                                (this.pairwisePlotBean.XAxisDatapointCollection[spot.AlignmentID] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                        this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin +
                            (this.pairwisePlotBean.YAxisDatapointCollection[spot.AlignmentID] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                        if (Math.Abs(this.ye - this.ys) >= 20) {

                            var offset = this.ye - this.ys > 0 ? -10 : 10;

                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset), new Point(this.xe - 20, this.ye + offset));
                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(this.xs, this.ys), new Point(this.xs - 20 - (this.xs - this.xe), this.ys - offset));
                            this.drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(this.xe, this.ye), new Point(this.xe - 20, this.ye + offset));

                            this.drawingContext.DrawEllipse(null,
                                        new Pen(Brushes.Red, 1.0),
                                        new Point(this.xe, this.ye),
                                        this.pairwisePlotAlignmentViewUI.PlotSize,
                                        this.pairwisePlotAlignmentViewUI.PlotSize);

                            var isotopeLabel = "M + " + spot.IsotopeTrackingWeightNumber;
                            if (spot.IsotopeTrackingWeightNumber >= 1000)
                                isotopeLabel = "Isotopes of other elements";
                            formattedText = new FormattedText(isotopeLabel,
                                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
                            formattedText.TextAlignment = TextAlignment.Left;

                            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                            this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                            this.drawingContext.DrawText(formattedText, new Point(this.xe + 10, this.ActualHeight - this.ye - 8));

                            this.drawingContext.Pop();
                            this.drawingContext.Pop();

                        }
                    }
                }
            }
            else if (this.pairwisePlotBean.AlignmentDriftSpotBean != null && this.pairwisePlotBean.AlignmentDriftSpotBean.Count > 0) {
                var driftSpots = this.pairwisePlotBean.AlignmentDriftSpotBean;
                var maxIntensity = driftSpots[i].MaxValiable;
                var minIntensity = driftSpots[i].MinValiable;
                var maxPeaks = driftSpots[i].AlignedPeakPropertyBeanCollection.Count;

                var plotSize = this.pairwisePlotAlignmentViewUI.PlotSize;
                this.drawingContext.DrawEllipse(null, new Pen(Brushes.Red, 1),
                    new Point(this.xs, this.ys), this.pairwisePlotAlignmentViewUI.PlotSize + 1, this.pairwisePlotAlignmentViewUI.PlotSize + 1);

                for (int j = 0; j < driftSpots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                    if (Math.Abs((double)this.pairwisePlotBean.DisplayRangeMaxX - (double)this.pairwisePlotBean.DisplayRangeMinX) > 1.0) break;
                    if (Math.Abs((double)this.pairwisePlotBean.DisplayRangeMaxY - (double)this.pairwisePlotBean.DisplayRangeMinY) > 2.5) break;

                    #region
                    if (driftSpots[i].AlignedPeakPropertyBeanCollection[j].PeakID == -1) continue;

                    if (this.pairwisePlotBean.RetentionUnit == RetentionUnit.RT)
                        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin + (driftSpots[i].AlignedPeakPropertyBeanCollection[j].RetentionTime
                            - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                    else
                        this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin + (driftSpots[i].AlignedPeakPropertyBeanCollection[j].RetentionIndex
                            - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;

                    if (this.pairwisePlotBean.Ms1DecResults == null || this.pairwisePlotBean.Ms1DecResults.Count == 0) {
                        this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin + (driftSpots[i].AlignedPeakPropertyBeanCollection[j].AccurateMass
                            - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
                    }
                    else {
                        double zoomPacket = 1.0;
                        if (this.pairwisePlotBean.DisplayRangeMaxY > this.pairwisePlotBean.DisplayRangeMinY)
                            zoomPacket = (double)(this.pairwisePlotBean.MaxY - this.pairwisePlotBean.MinY) / (double)(this.pairwisePlotBean.DisplayRangeMaxY - this.pairwisePlotBean.DisplayRangeMinY);
                        else zoomPacket = 6.0;
                        if (zoomPacket > 6.0) zoomPacket = 6.0;
                        this.ye = this.ys + 20 * zoomPacket * Math.Cos(Math.PI / (double)maxPeaks * (double)j);
                    }



                    if (driftSpots[i].AlignedPeakPropertyBeanCollection[j].PeakID >= 0) {
                        this.drawingContext.DrawLine(graphGreenPen, new Point(this.xs, this.ys), new Point(this.xe, this.ye));
                        this.drawingContext.DrawEllipse(graphGreenBrush, graphGreenPen, new Point(this.xe, this.ye), plotSize, plotSize);
                    }
                    else {
                        this.drawingContext.DrawLine(graphRedPen, new Point(this.xs, this.ys), new Point(this.xe, this.ye));
                        this.drawingContext.DrawEllipse(graphRedBrush, graphRedPen, new Point(this.xe, this.ye), plotSize, plotSize);
                    }
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                    formattedText = new FormattedText(driftSpots[i]
                        .AlignedPeakPropertyBeanCollection[j].FileName,
                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, graphBlackBrush);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xe, this.ActualHeight - this.ye - plotSize + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                    #endregion
                }


            }

            this.xs = this.pairwisePlotAlignmentViewUI.LeftMargin +
                   (this.pairwisePlotBean.RectangleRangeXmin - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
            this.xe = this.pairwisePlotAlignmentViewUI.LeftMargin +
                (this.pairwisePlotBean.RectangleRangeXmax - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
            this.ys = this.pairwisePlotAlignmentViewUI.BottomMargin +
                (this.pairwisePlotBean.RectangleRangeYmin - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;
            this.ye = this.pairwisePlotAlignmentViewUI.BottomMargin +
                (this.pairwisePlotBean.RectangleRangeYmax - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

            this.drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(50, 200, 255, 255)), new Pen(Brushes.Cyan, 1), new Rect(new Point(this.xs, this.ys), new Point(this.xe, this.ye)));
        }

        private void mouseFocused(int i)
        {
            this.drawingContext.DrawEllipse(null, new Pen(this.pairwisePlotBean.PlotBrushCollection[i], 1), new Point(this.xt, this.yt), this.pairwisePlotAlignmentViewUI.PlotSize + 2, this.pairwisePlotAlignmentViewUI.PlotSize + 2);

            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

            var isOnDrift = this.pairwisePlotBean.AlignmentDriftSpotBean != null && this.pairwisePlotBean.AlignmentDriftSpotBean.Count > 0;

            var id = isOnDrift ? this.pairwisePlotBean.AlignmentDriftSpotBean[i].MasterID : this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AlignmentID;

            var rt = Math.Round(this.pairwisePlotBean.XAxisDatapointCollection[i], 2).ToString();
            var mz = Math.Round(this.pairwisePlotBean.YAxisDatapointCollection[i], 4).ToString();

            var rtTitle = "; RT(min): ";
            if (this.pairwisePlotBean.RetentionUnit == RetentionUnit.RI) rtTitle = "; RI: ";
            if (isOnDrift) rtTitle = "; Mobility: ";

            formattedText = new FormattedText("ID #: " + id + rtTitle + rt + "; m/z: " + mz, 
                CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Gray);
            formattedText.TextAlignment = TextAlignment.Center;

            this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt - this.labelYDistance));

            this.drawingContext.Pop();
            this.drawingContext.Pop();
        }

        private void drawDisplayLables(int i)
        {
            var isOnDrift = this.pairwisePlotBean.AlignmentDriftSpotBean != null && this.pairwisePlotBean.AlignmentDriftSpotBean.Count > 0;
            if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.None)
            {

            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Metabolite)
            {
                var metaboliteName
                    = isOnDrift
                    ? this.pairwisePlotBean.AlignmentDriftSpotBean[i].MetaboliteName
                    : this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].MetaboliteName;

                if (metaboliteName != string.Empty && metaboliteName != null)
                {
                    this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                    this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                    formattedText = new FormattedText(metaboliteName, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt + this.labelYDistance * 0.5));

                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.X)
            {
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                formattedText = new FormattedText(this.pairwisePlotBean.XAxisDatapointCollection[i].ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                formattedText.TextAlignment = TextAlignment.Center;

                this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt + this.labelYDistance * 0.5));

                this.drawingContext.Pop();
                this.drawingContext.Pop();
            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Y)
            {
                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                formattedText = new FormattedText(this.pairwisePlotBean.YAxisDatapointCollection[i].ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                formattedText.TextAlignment = TextAlignment.Center;

                this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt + this.labelYDistance * 0.5));

                this.drawingContext.Pop();
                this.drawingContext.Pop();
            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Isotope)
            {

            }
            else if (this.pairwisePlotBean.DisplayLabel == PairwisePlotDisplayLabel.Adduct)
            {
                var adductIonName
                   = isOnDrift
                   ? this.pairwisePlotBean.AlignmentDriftSpotBean[i].AdductIonName
                   : this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AdductIonName;

                this.drawingContext.PushTransform(new ScaleTransform(1, -1));
                this.drawingContext.PushTransform(new TranslateTransform(0, -this.ActualHeight));

                formattedText = new FormattedText(adductIonName, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, this.pairwisePlotBean.PlotBrushCollection[i]);
                formattedText.TextAlignment = TextAlignment.Center;

                this.drawingContext.DrawText(formattedText, new Point(this.xt, this.ActualHeight - this.yt + this.labelYDistance * 0.5));

                if (!isOnDrift && this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AdductIonNameFromAmalgamation != null && this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AdductIonNameFromAmalgamation != string.Empty) {
                    formattedText = new FormattedText(this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].AdductIonNameFromAmalgamation + " (Amal.)", CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Red);
                    formattedText.TextAlignment = TextAlignment.Center;

                    this.drawingContext.DrawText(formattedText, new Point(this.xt, ActualHeight - this.yt + this.labelYDistance * 0.5 * 2.0));
                }

                this.drawingContext.Pop();
                this.drawingContext.Pop();
            }
        }

        private bool displayFiltering(int i)
        {
            var isOnDrift = this.pairwisePlotBean.AlignmentDriftSpotBean != null && this.pairwisePlotBean.AlignmentDriftSpotBean.Count > 0;
            var plot = this.pairwisePlotBean;

            if (isOnDrift) {
                var spot = this.pairwisePlotBean.AlignmentDriftSpotBean[i];
                if (spot.RelativeAmplitudeValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) return false;
                if (spot.RelativeAmplitudeValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) return false;
                if (this.pairwisePlotBean.MolcularIonFilter && spot.PostDefinedIsotopeWeightNumber > 0) return false;

                if (this.pairwisePlotBean.DisplayRangeMinX - 1 > this.pairwisePlotBean.XAxisDatapointCollection[i] ||
                    this.pairwisePlotBean.XAxisDatapointCollection[i] > this.pairwisePlotBean.DisplayRangeMaxX + 1) return false;


                return isDisplayed(plot, spot, i);
            }
            else {
                var spot = this.pairwisePlotBean.AlignmentPropertyBeanCollection[i];
                if (spot.RelativeAmplitudeValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) return false;
                if (spot.RelativeAmplitudeValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) return false;
                if (this.pairwisePlotBean.MolcularIonFilter && spot.IsotopeTrackingWeightNumber > 0) return false;
                if (this.pairwisePlotBean.MolcularIonFilter && spot.PostDefinedIsotopeWeightNumber > 0) return false;

                if (this.pairwisePlotBean.DisplayRangeMinX - 1 > this.pairwisePlotBean.XAxisDatapointCollection[i] ||
                    this.pairwisePlotBean.XAxisDatapointCollection[i] > this.pairwisePlotBean.DisplayRangeMaxX + 1) return false;


                return isDisplayed(plot, spot, i);
            }
        }

        private bool isDisplayed(PairwisePlotBean plot, AlignmentPropertyBean spot, int id) {
            var metabolite = spot.MetaboliteName;
            var isDisplayed = false;

            if (!plot.AnnotatedOnlyDisplayFilter && !plot.IdentifiedOnlyDisplayFilter && !plot.UnknownFilter && !plot.CcsFilter)
                isDisplayed = true;

            if (plot.IdentifiedOnlyDisplayFilter &&
                metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                isDisplayed = true;
            if (plot.AnnotatedOnlyDisplayFilter &&
                metabolite != string.Empty && metabolite.Contains("w/o"))
                isDisplayed = true;
            if (plot.CcsFilter && spot.IsCcsMatch)
                isDisplayed = true;
            if (plot.UnknownFilter &&
                (metabolite == string.Empty || metabolite.Contains("Unknown")))
                isDisplayed = true;
            if (plot.UniqueionFilter &&
                !spot.IsFragmentQueryExist)
                isDisplayed = false;
            if (plot.MsmsOnlyDisplayFilter &&
                spot.MsmsIncluded == false)
                isDisplayed = false;
            if (plot.BlankFilter &&
               spot.IsBlankFiltered == true)
                isDisplayed = false;
            return isDisplayed;
        }

        private bool isDisplayed(PairwisePlotBean plot, AlignedDriftSpotPropertyBean spot, int id) {
            var metabolite = spot.MetaboliteName;
            var isDisplayed = false;

            if (!plot.AnnotatedOnlyDisplayFilter && !plot.IdentifiedOnlyDisplayFilter && !plot.UnknownFilter && !plot.CcsFilter)
                isDisplayed = true;

            if (plot.IdentifiedOnlyDisplayFilter &&
                metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                isDisplayed = true;
            if (plot.AnnotatedOnlyDisplayFilter &&
                metabolite != string.Empty && metabolite.Contains("w/o"))
                isDisplayed = true;
            if (plot.CcsFilter && spot.IsCcsMatch)
                isDisplayed = true;
            if (plot.UnknownFilter &&
                (metabolite == string.Empty || metabolite.Contains("Unknown")))
                isDisplayed = true;
            if (plot.UniqueionFilter &&
                !spot.IsFragmentQueryExist)
                isDisplayed = false;
            if (plot.MsmsOnlyDisplayFilter &&
                spot.MsmsIncluded == false)
                isDisplayed = false;
            if (plot.BlankFilter &&
               spot.IsBlankFiltered == true)
                isDisplayed = false;
            return isDisplayed;
        }


        private void drawGraphTitle(string graphTitle)
        {
            this.formattedText = new FormattedText(graphTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 15, Brushes.Black);
            this.formattedText.TextAlignment = TextAlignment.Left;
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin, this.pairwisePlotAlignmentViewUI.TopMargin - 17));
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
                xPixelValue = this.pairwisePlotAlignmentViewUI.LeftMargin + (xAxisValue - xAxisMinValue) * this.xPacket;
                if (xPixelValue < this.pairwisePlotAlignmentViewUI.LeftMargin) continue;
                if (xPixelValue > drawWidth - this.pairwisePlotAlignmentViewUI.RightMargin) break;

                if ((decimal)xAxisValue - ((decimal)((int)((decimal)xAxisValue / this.xMajorScale)) * this.xMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin + this.longScaleSize));
                    if (this.xMajorScale < 1)
                        this.formattedText = new FormattedText(xAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(xAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.DrawText(formattedText, new Point(xPixelValue, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin + this.longScaleSize));
                }
                else//Minor scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(xPixelValue, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin), new Point(xPixelValue, drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin + this.shortScaleSize));
                }
            }
        }

        private void getXaxisScaleInterval(double min, double max, double drawWidth)
        {
            if (max == min) { max += 0.9; }
            if (min > max) {
                max = min + 1;
            }
            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double xScale;

            if (eff >= 5) { xScale = sft * 0.5; } else if (eff >= 2) { xScale = sft * 0.5 * 0.5; } else { xScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int xAxisPixelRange = (int)(drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin);
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
                yPixelValue = drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - (yAxisValue - yAxisMinValue) * this.yPacket;
                if (yPixelValue > drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin) continue;
                if (yPixelValue < this.pairwisePlotAlignmentViewUI.TopMargin) break;

                if ((decimal)yAxisValue - ((decimal)((int)((decimal)yAxisValue / this.yMajorScale)) * this.yMajorScale) == 0)//Major scale
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.longScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                    if (this.yMajorScale < 1)
                        this.formattedText = new FormattedText(yAxisValue.ToString("f3"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    else
                        this.formattedText = new FormattedText(yAxisValue.ToString("f0"), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
                    this.formattedText.TextAlignment = TextAlignment.Center;
                    this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
                    this.drawingContext.PushTransform(new RotateTransform(270.0));
                    this.drawingContext.DrawText(formattedText, new Point(drawHeight - yPixelValue, this.pairwisePlotAlignmentViewUI.LeftMargin - this.formattedText.Height - this.longScaleSize - this.axisFromGraphArea - 1));
                    this.drawingContext.Pop();
                    this.drawingContext.Pop();
                }
                else
                {
                    this.drawingContext.DrawLine(this.graphAxis, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.shortScaleSize - this.axisFromGraphArea, yPixelValue), new Point(this.pairwisePlotAlignmentViewUI.LeftMargin - this.axisFromGraphArea, yPixelValue));
                }

            }
        }

        private void getYaxisScaleInterval(double min, double max, double drawHeight)
        {
            if (min > max) { min = max - 0.9; }
            if (max == min) { max += 0.9; }

            double eff = max - min;
            double sft = 1;
            while (eff >= 10) { eff /= 10; sft *= 10; }
            while (eff < 1) { eff *= 10; sft /= 10; }

            double yScale;

            if (eff >= 5) { yScale = sft * 0.5; } else if (eff >= 2) { yScale = sft * 0.5 * 0.5; } else { yScale = sft * 0.2 * 0.5; }

            FormattedText formattedText;
            int yAxisPixelRange = (int)(drawHeight - this.pairwisePlotAlignmentViewUI.TopMargin - this.pairwisePlotAlignmentViewUI.BottomMargin);
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
            this.drawingContext.DrawText(formattedText, new Point(this.pairwisePlotAlignmentViewUI.LeftMargin + 0.5 * (drawWidth - this.pairwisePlotAlignmentViewUI.LeftMargin - this.pairwisePlotAlignmentViewUI.RightMargin), drawHeight - 20));

            // Set Caption to Y-Axis                                                
            this.drawingContext.PushTransform(new TranslateTransform(7, this.pairwisePlotAlignmentViewUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.TopMargin)));
            this.drawingContext.PushTransform(new RotateTransform(270.0));

            formattedText = new FormattedText(this.pairwisePlotBean.YAxisTitle, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Calibri"), 13, Brushes.Black);
            formattedText.SetFontStyle(FontStyles.Italic);
            formattedText.TextAlignment = TextAlignment.Center;
            this.drawingContext.DrawText(formattedText, new Point(0, 0));

            this.drawingContext.PushTransform(new RotateTransform(-270.0));
            this.drawingContext.PushTransform(new TranslateTransform(-7, -(this.pairwisePlotAlignmentViewUI.TopMargin + 0.5 * (drawHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.TopMargin))));
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

        public void PlotFocus()
        {
            if (Math.Abs(this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.X) > this.pairwisePlotAlignmentViewUI.PlotSize ||
                Math.Abs(this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.Y - this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.Y) > this.pairwisePlotAlignmentViewUI.PlotSize) return;

            if (this.pairwisePlotBean == null || this.pairwisePlotBean.XAxisDatapointCollection == null || this.pairwisePlotBean.XAxisDatapointCollection.Count == 0) return;
            var isOnDrift = this.pairwisePlotBean.AlignmentDriftSpotBean != null && this.pairwisePlotBean.AlignmentDriftSpotBean.Count > 0;

            for (int i = 0; i < this.pairwisePlotBean.XAxisDatapointCollection.Count; i++)
            {
                if (isOnDrift) {
                    if (this.pairwisePlotBean.AlignmentDriftSpotBean[i].RelativeAmplitudeValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                    if (this.pairwisePlotBean.AlignmentDriftSpotBean[i].RelativeAmplitudeValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) continue;
                    if (this.pairwisePlotBean.MsmsOnlyDisplayFilter && this.pairwisePlotBean.AlignmentDriftSpotBean[i].MsmsIncluded == false) continue;
                    if (this.pairwisePlotBean.UniqueionFilter && this.pairwisePlotBean.AlignmentDriftSpotBean[i].IsFragmentQueryExist == false) continue;

                    var displayed = isDisplayed(this.pairwisePlotBean, this.pairwisePlotBean.AlignmentDriftSpotBean[i], i);
                    if (displayed == false) continue;
                }
                else {
                    if (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue < this.pairwisePlotBean.AmplitudeDisplayLowerFilter * 0.01) continue;
                    if (this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].RelativeAmplitudeValue > this.pairwisePlotBean.AmplitudeDisplayUpperFilter * 0.01) continue;
                    if (this.pairwisePlotBean.MsmsOnlyDisplayFilter && this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].MsmsIncluded == false) continue;
                    if (this.pairwisePlotBean.UniqueionFilter && this.pairwisePlotBean.AlignmentPropertyBeanCollection[i].IsFragmentQueryExist == false) continue;

                    var displayed = isDisplayed(this.pairwisePlotBean, this.pairwisePlotBean.AlignmentPropertyBeanCollection[i], i);
                    if (displayed == false) continue;
                }
            
                this.xt = this.pairwisePlotAlignmentViewUI.LeftMargin + (this.pairwisePlotBean.XAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinX) * this.xPacket;
                this.yt = this.pairwisePlotAlignmentViewUI.BottomMargin + (this.pairwisePlotBean.YAxisDatapointCollection[i] - (double)this.pairwisePlotBean.DisplayRangeMinY) * this.yPacket;

                if (this.xt < this.pairwisePlotAlignmentViewUI.LeftMargin || this.xt > this.ActualWidth - this.pairwisePlotAlignmentViewUI.RightMargin) continue;
                if (this.yt < this.pairwisePlotAlignmentViewUI.BottomMargin || this.yt > this.ActualHeight - this.pairwisePlotAlignmentViewUI.TopMargin) continue;

                if (Math.Abs(this.xt - this.pairwisePlotAlignmentViewUI.CurrentMousePoint.X) < this.pairwisePlotAlignmentViewUI.PlotSize && Math.Abs(this.yt - (this.ActualHeight - this.pairwisePlotAlignmentViewUI.CurrentMousePoint.Y)) < this.pairwisePlotAlignmentViewUI.PlotSize)
                {
                    this.pairwisePlotBean.SelectedPlotId = i;
                }
            }
        }

        public void GraphZoom()
        {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X) < 5 
                && Math.Abs(this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.Y - this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X) / xPacket < 0.01)
            {
                return;
            }

            // Zoom X-Coordinate        
            if (this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X > this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X)
            {
                if (this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X > this.pairwisePlotAlignmentViewUI.LeftMargin)
                {
                    if (this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X <= this.ActualWidth - this.pairwisePlotAlignmentViewUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X >= this.pairwisePlotAlignmentViewUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftMargin) / this.xPacket);
                    }
                }

            }
            else
            {
                if (this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X > this.pairwisePlotAlignmentViewUI.LeftMargin)
                {
                    if (this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X <= this.ActualWidth - this.pairwisePlotAlignmentViewUI.RightMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMaxX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftMargin) / this.xPacket);
                    }
                    if (this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X >= this.pairwisePlotAlignmentViewUI.LeftMargin)
                    {
                        this.pairwisePlotBean.DisplayRangeMinX = this.pairwisePlotBean.DisplayRangeMinX + (float)((this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftMargin) / this.xPacket);
                    }
                }
            }

            // Zoom Y-Coordinate               
            if (this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.Y > this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.Y)
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.Y) / this.yPacket);

            }
            else
            {
                this.pairwisePlotBean.DisplayRangeMaxY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.Y) / this.yPacket);
                this.pairwisePlotBean.DisplayRangeMinY = this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - this.pairwisePlotAlignmentViewUI.BottomMargin - this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.Y) / this.yPacket);
            }
        }

        public void GraphScroll()
        {
            if (this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.X == -1 || this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.Y == -1)
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
            if (this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.X > this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.X)
            {
                distanceX = this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.X;

                newMinX = this.pairwisePlotAlignmentViewUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotAlignmentViewUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

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
                distanceX = this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.X - this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.X;

                newMinX = this.pairwisePlotAlignmentViewUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                newMaxX = this.pairwisePlotAlignmentViewUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

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
            if (this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.Y < this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.Y)
            {
                distanceY = this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.Y - this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.Y;

                newMinY = this.pairwisePlotAlignmentViewUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotAlignmentViewUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

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
                distanceY = this.pairwisePlotAlignmentViewUI.LeftButtonStartClickPoint.Y - this.pairwisePlotAlignmentViewUI.LeftButtonEndClickPoint.Y;

                newMinY = this.pairwisePlotAlignmentViewUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                newMaxY = this.pairwisePlotAlignmentViewUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

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
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, new Rect(new Point(this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.X,
                this.pairwisePlotAlignmentViewUI.RightButtonStartClickPoint.Y), new Point(this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.X, this.pairwisePlotAlignmentViewUI.RightButtonEndClickPoint.Y)));
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

            x_Value = (float)this.pairwisePlotBean.DisplayRangeMinX + (float)((mousePoint.X - this.pairwisePlotAlignmentViewUI.LeftMargin) / this.xPacket);
            y_Value = (float)this.pairwisePlotBean.DisplayRangeMinY + (float)((this.ActualHeight - mousePoint.Y - this.pairwisePlotAlignmentViewUI.BottomMargin) / this.yPacket);

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
