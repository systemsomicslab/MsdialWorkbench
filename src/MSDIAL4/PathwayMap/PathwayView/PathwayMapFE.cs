using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.BarChart;
using System.Windows;
using System.Windows.Media;
using System.Globalization;
using System.Diagnostics;

namespace Riken.Metabolomics.Pathwaymap
{
    public class XY {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class NodeRect {
        public XY StartXY { get; set; }
        public XY EndXY { get; set; }
    }

    public class PathwayMapFE : FrameworkElement {
        private VisualCollection visualCollection;//
        private DrawingVisual drawingVisual;//
        private DrawingContext drawingContext;//
        //private List<DrawingVisual> graphVisuals;

        private PathwayMapUI pathwayUI;
        private PathwayMapObj pathwayObj;

        //Graph area format
        private double longScaleSize = 5; // Scale Size (Long)
        private double shortScaleSize = 2; // Scale Size (Short)
        private double axisFromGraphArea = 1; // Location of Axis from Graph Area
        private double labelYDistance = 10; // Label Distance From Peak Top

        // Constant for Graph Scale
        private decimal xMajorScale = -1; // X-Axis Major Scale (min.)
        private decimal xMinorScale = -1; // X-Axis Minor Scale (min.)
        private decimal yMajorScale = -1; // Y-Axis Major Scale (Intensity)
        private decimal yMinorScale = -1; // Y-Axis Minor Scale (Intensity)

        // Graph Color & Font Settings
        private FormattedText formattedText; // FormatText for Scale    
        private Brush graphBackGround = Brushes.WhiteSmoke; // Graph Background
        private Pen graphBorder = new Pen(Brushes.White, 1.0); // Graph Border
        private Pen graphAxis = new Pen(Brushes.Black, 0.5);
        private Brush graphFontBrush = Brushes.Black;

        // Rubber
        private SolidColorBrush rubberRectangleColor = Brushes.DarkGray;
        private Brush rubberRectangleBackGround; // Background for Zooming Regctangle
        private Pen rubberRectangleBorder; // Border for Zooming Rectangle  

        private CultureInfo cultureinfo = CultureInfo.GetCultureInfo("en-us");
        private Typeface typeface = new Typeface("Calibri");
        private int fontsize = 10;

        // Drawing Coordinates
        private double xs, ys, xt, yt, xe, ye;

        // Drawing Packet
        private double xPacket;
        private double yPacket;

        public bool IsNodeCaptured { get; set; }
        public bool IsLeftClicked { get; set; }
        public bool IsControlHold { get; set; }
        public bool IsShiftHold { get; set; }
        public bool IsLeftReleased { get; set; }

        public PathwayMapFE(PathwayMapObj pathwayObj, PathwayMapUI pathwayUI) {
            this.visualCollection = new VisualCollection(this);
            this.pathwayObj = pathwayObj;
            this.pathwayUI = pathwayUI;

            // Set RuberRectangle Colror
            rubberRectangleBorder = new Pen(rubberRectangleColor, 1.0);
            rubberRectangleBorder.Freeze();
            rubberRectangleBackGround = combineAlphaAndColor(0.25, rubberRectangleColor);
            rubberRectangleBackGround.Freeze();
        }

        public void PathwayMapDraw() {
            this.visualCollection.Clear();
            this.drawingVisual = getPathwayDrawingVisual();
            this.visualCollection.Add(this.drawingVisual);
        }

        private DrawingVisual getPathwayDrawingVisual() {
            this.drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (this.ActualWidth < this.pathwayUI.LeftMargin + this.pathwayUI.RightMargin ||
                this.ActualHeight < this.pathwayUI.BottomMargin + this.pathwayUI.TopMargin) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            if (this.pathwayObj != null && this.pathwayObj.Nodes != null && this.pathwayObj.Nodes.Count > 0) {
                this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, this.ActualWidth, this.ActualHeight));
                setPackets();
                drawNodes();
                drawEdges();
            }

            this.drawingContext.Close();
            return this.drawingVisual;
        }

        private void drawEdges() {
            var edges = this.pathwayObj.Edges;

            foreach (var edge in edges) {

                var edgeSourceXY = new Point();
                var edgeTargetXY = new Point();
                var sourceAngle = 0.0;
                var targetAngle = 0.0;
                //Console.WriteLine(edge.TargetArrow + "\t" + edge.SourceArrow);

                setEdgeProperties(edge, out edgeSourceXY, out edgeTargetXY, out sourceAngle, out targetAngle);
                edge.SourceX = (float)edgeSourceXY.X;
                edge.SourceY = (float)edgeSourceXY.Y;
                edge.TargetX = (float)edgeTargetXY.X;
                edge.TargetY = (float)edgeTargetXY.Y;

                drawEdge(edgeSourceXY, edgeTargetXY, sourceAngle, targetAngle, edge.SourceArrow, edge.TargetArrow);
            }
        }

        private void drawEdge(Point edgeSourceXY, Point edgeTargetXY, double sourceAngle, double targetAngle, string sourceArrowType, string targetArrowType) {

            //var color = (Color)ColorConverter.ConvertFromString("#000000");

            this.drawingContext.DrawLine(new Pen(Brushes.Black, 1.0), edgeSourceXY, edgeTargetXY);
            //this.drawingContext.DrawLine(new Pen(new SolidColorBrush(color), 1.0), edgeSourceXY, edgeTargetXY);

            var sourceArrow = getPathFigure(edgeSourceXY, sourceArrowType);
            var targetArrow = getPathFigure(edgeTargetXY, targetArrowType);

            var areaPathGeometry = new PathGeometry(new PathFigure[] { sourceArrow });
            areaPathGeometry.Freeze();

            this.drawingContext.PushTransform(new RotateTransform(sourceAngle) { CenterX = edgeSourceXY.X, CenterY = edgeSourceXY.Y });
            this.drawingContext.DrawGeometry(Brushes.Black, null, areaPathGeometry);
            this.drawingContext.Pop();

            areaPathGeometry = new PathGeometry(new PathFigure[] { targetArrow });
            areaPathGeometry.Freeze();

            this.drawingContext.PushTransform(new RotateTransform(targetAngle) { CenterX = edgeTargetXY.X, CenterY = edgeTargetXY.Y });
            this.drawingContext.DrawGeometry(Brushes.Black, null, areaPathGeometry);
            this.drawingContext.Pop();
        }

        private PathFigure getPathFigure(Point startPoint, string arrowType) {
            
            var arrow = new PathFigure();
            if (arrowType == "TBar") {
                arrow.StartPoint = new Point(startPoint.X, startPoint.Y + 7 * this.yPacket); // PathFigure for GraphLine 
                arrow.Segments.Add(new LineSegment() { Point = new Point(startPoint.X, startPoint.Y - 7 * this.yPacket) });
                arrow.Segments.Add(new LineSegment() { Point = new Point(startPoint.X + 1 * this.xPacket, startPoint.Y - 7 * this.yPacket) });
                arrow.Segments.Add(new LineSegment() { Point = new Point(startPoint.X + 1 * this.xPacket, startPoint.Y + 7 * this.yPacket) });
                arrow.Freeze();
            } else if (arrowType == null || arrowType == string.Empty) {
                arrow.StartPoint = startPoint; // PathFigure for GraphLine 
                arrow.Freeze();
            }
            else {
                arrow.StartPoint = startPoint; // PathFigure for GraphLine 
                arrow.Segments.Add(new LineSegment() { Point = new Point(startPoint.X + 10 * this.xPacket, startPoint.Y + 5 * this.yPacket) });
                arrow.Segments.Add(new LineSegment() { Point = new Point(startPoint.X + 10 * this.xPacket, startPoint.Y - 5 * this.yPacket) });
                arrow.Freeze();
            }
            return arrow;
        }

        private void setEdgeProperties(Edge edge, out Point edgeSourceXY, 
            out Point edgeTargetXY, out double sourceAngle, out double targetAngle) {
            var sourceID = edge.SourceNodeID;
            var targetID = edge.TargetNodeID;

            sourceAngle = 0.0;
            targetAngle = 0.0;
            edgeSourceXY = new Point();
            edgeTargetXY = new Point();

            if (!this.pathwayObj.Id2Node.ContainsKey(sourceID) || !this.pathwayObj.Id2Node.ContainsKey(targetID)) return;

            var sourceNode = this.pathwayObj.Id2Node[sourceID];
            var targetNode = this.pathwayObj.Id2Node[targetID];

            var sourceNodeCenter = xyValuesToPoints(sourceNode.CopyX, sourceNode.CopyY);
            var targetNodeCenter = xyValuesToPoints(targetNode.CopyX, targetNode.CopyY);

            var distanceST = getManhattanDistance(sourceNodeCenter, targetNodeCenter);

            var sourceNodeRect = getNodeRectanglePoints(sourceNode.Width, sourceNode.Height, sourceNodeCenter.X, sourceNodeCenter.Y);
            var targetNodeRect = getNodeRectanglePoints(targetNode.Width, targetNode.Height, targetNodeCenter.X, targetNodeCenter.Y);

        

            //isOverlaped = false;
            //var euclideanST = getEuclideanDistance(sourceNodeCenter, targetNodeCenter);
            //var requiredEuclidean = Math.Sqrt(
            //    Math.Pow(sourceNode.Width * 0.5 * this.xPacket + targetNode.Width * 0.5 * this.xPacket, 2) +
            //    Math.Pow(sourceNode.Height * 0.5 * this.yPacket + targetNode.Height * 0.5 * this.yPacket, 2)
            //    );
            //if (requiredEuclidean >= euclideanST) {
            //    isOverlaped = true;
            //    return;
            //}
            //else
            //    isOverlaped = false;

            if (Math.Abs(targetNodeCenter.X - sourceNodeCenter.X) < 0.0001) {

                // x = b for connecting nodes
                // checking intersection point for source
                var sTopIntersect = new XY() { X = sourceNodeCenter.X, Y = sourceNodeRect.StartXY.Y };
                var sBottomIntersect = new XY() { X = sourceNodeCenter.X, Y = sourceNodeRect.EndXY.Y };

                // checking intersection point for target
                var tTopIntersect = new XY() { X = targetNodeCenter.X, Y = targetNodeRect.StartXY.Y };
                var tBottomIntersect = new XY() { X = targetNodeCenter.X, Y = targetNodeRect.EndXY.Y };

                var distanceSTopT = getManhattanDistance(sTopIntersect, targetNodeCenter);
                var distanceSBottomT = getManhattanDistance(sBottomIntersect, targetNodeCenter);

                if (distanceSTopT < distanceSBottomT) {
                    // the edge should be intersected at top of source panel and bottom of target panel
                    edgeSourceXY = new Point() { X = sTopIntersect.X, Y = sTopIntersect.Y };
                    edgeTargetXY = new Point() { X = tBottomIntersect.X, Y = tBottomIntersect.Y };
                }
                else {
                    // the edge should be intersected at bottom of source panel and top of target panel
                    edgeSourceXY = new Point() { X = sBottomIntersect.X, Y = sBottomIntersect.Y };
                    edgeTargetXY = new Point() { X = tTopIntersect.X, Y = tTopIntersect.Y };
                }

                if (edgeSourceXY.Y > edgeTargetXY.Y) {
                    sourceAngle = -1 * 90.0;
                    targetAngle = 90.0;
                    //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "1", sourceNode.Label, targetNode.Label, sourceAngle);
                }
                else {
                    sourceAngle = 90.0;
                    targetAngle = -1 * 90.0;
                    //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "2", sourceNode.Label, targetNode.Label, sourceAngle);
                }
            }
            else {

                // y = ax + b  for connecting nodes
                var a = (targetNodeCenter.Y - sourceNodeCenter.Y) / (targetNodeCenter.X - sourceNodeCenter.X);
                var b = sourceNodeCenter.Y - sourceNodeCenter.X * a;

                // checking intersection point for source
                var sLeftIntersect = new XY() { X = sourceNodeRect.StartXY.X, Y = a * sourceNodeRect.StartXY.X + b };
                var sTopIntersect = new XY() { X = (sourceNodeRect.StartXY.Y - b) / a, Y = sourceNodeRect.StartXY.Y };
                var sRightIntersect = new XY() { X = sourceNodeRect.EndXY.X, Y = a * sourceNodeRect.EndXY.X + b };
                var sBottomIntersect = new XY() { X = (sourceNodeRect.EndXY.Y - b) / a, Y = sourceNodeRect.EndXY.Y };

                // checking intersection point for target
                var tLeftIntersect = new XY() { X = targetNodeRect.StartXY.X, Y = a * targetNodeRect.StartXY.X + b };
                var tTopIntersect = new XY() { X = (targetNodeRect.StartXY.Y - b) / a, Y = targetNodeRect.StartXY.Y };
                var tRightIntersect = new XY() { X = targetNodeRect.EndXY.X, Y = a * targetNodeRect.EndXY.X + b };
                var tBottomIntersect = new XY() { X = (targetNodeRect.EndXY.Y - b) / a, Y = targetNodeRect.EndXY.Y };

                // checking source arrow angle and focused position
                if (sLeftIntersect.Y >= sourceNodeRect.StartXY.Y && sLeftIntersect.Y <= sourceNodeRect.EndXY.Y) {
                    // meaning the edge should be intersected at left or right of node panel
                    var distanceSLeftT = getManhattanDistance(sLeftIntersect, targetNodeCenter);
                    var distanceSRightT = getManhattanDistance(sRightIntersect, targetNodeCenter);

                    if (distanceSLeftT < distanceSRightT) {
                        // the edge should be intersected at left of source panel and right of target panel
                        edgeSourceXY = new Point() { X = sLeftIntersect.X, Y = sLeftIntersect.Y };

                    }
                    else {
                        // the edge should be intersected at right of source panel and left of target panel
                        edgeSourceXY = new Point() { X = sRightIntersect.X, Y = sRightIntersect.Y };
                    }

                    if (sourceNodeCenter.X > targetNodeCenter.X) {
                        if (Math.Abs(a) < 0.0001) {
                            sourceAngle = 180;
                            //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "3", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                        else {
                            sourceAngle = Math.Atan(a) / Math.PI * 180 - 180;
                            //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "4", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                    }
                    else {
                        if (Math.Abs(a) < 0.0001) {
                            sourceAngle = 0;
                            //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "5", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                        else {
                            sourceAngle = Math.Atan(a) / Math.PI * 180;
                            //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "6", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                    }
                }
                else {
                    var distanceSTopT = getManhattanDistance(sTopIntersect, targetNodeCenter);
                    var distanceSBottomT = getManhattanDistance(sBottomIntersect, targetNodeCenter);

                    if (distanceSTopT < distanceSBottomT) {
                        // the edge should be intersected at top of source panel and bottom of target panel
                        edgeSourceXY = new Point() { X = sTopIntersect.X, Y = sTopIntersect.Y };
                    }
                    else {
                        // the edge should be intersected at bottom of source panel and top of target panel
                        edgeSourceXY = new Point() { X = sBottomIntersect.X, Y = sBottomIntersect.Y };
                    }

                    if (sourceNodeCenter.Y > targetNodeCenter.Y) {
                        sourceAngle = Math.Atan(a) / Math.PI * 180;
                        if (sourceAngle > 0) {
                            sourceAngle = Math.Atan(a) / Math.PI * 180 - 180;
                        }
                        //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "7", sourceNode.Label, targetNode.Label, sourceAngle);
                    }
                    else {
                        sourceAngle = Math.Atan(a) / Math.PI * 180;
                        if (sourceAngle < 0) {
                            sourceAngle = Math.Atan(a) / Math.PI * 180 - 180;
                        }
                        //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "8", sourceNode.Label, targetNode.Label, sourceAngle);
                    }
                }

                // checking target arrow angle and focused position
                if (tLeftIntersect.Y >= targetNodeRect.StartXY.Y && tLeftIntersect.Y <= targetNodeRect.EndXY.Y) {
                    // meaning the edge should be intersected at left or right of node panel
                    var distanceSLeftT = getManhattanDistance(tLeftIntersect, sourceNodeCenter);
                    var distanceSRightT = getManhattanDistance(tRightIntersect, sourceNodeCenter);

                    if (distanceSLeftT < distanceSRightT) {
                        // the edge should be intersected at left of source panel and right of target panel
                        edgeTargetXY = new Point() { X = tLeftIntersect.X, Y = tLeftIntersect.Y };
                    }
                    else {
                        // the edge should be intersected at right of source panel and left of target panel
                        edgeTargetXY = new Point() { X = tRightIntersect.X, Y = tRightIntersect.Y };
                    }

                    if (sourceNodeCenter.X > targetNodeCenter.X) {
                        if (Math.Abs(a) < 0.0001) {
                            targetAngle = 0;
                            // Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "3", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                        else {
                            targetAngle = Math.Atan(a) / Math.PI * 180;
                            // Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "4", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                    }
                    else {
                        if (Math.Abs(a) < 0.0001) {
                            targetAngle = 180;
                            // Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "5", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                        else {
                            targetAngle = Math.Atan(a) / Math.PI * 180 - 180;
                            // Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "6", sourceNode.Label, targetNode.Label, sourceAngle);
                        }
                    }
                }
                else {
                    var distanceSTopT = getManhattanDistance(tTopIntersect, sourceNodeCenter);
                    var distanceSBottomT = getManhattanDistance(tBottomIntersect, sourceNodeCenter);

                    if (distanceSTopT < distanceSBottomT) {
                        // the edge should be intersected at top of source panel and bottom of target panel
                        edgeTargetXY = new Point() { X = tTopIntersect.X, Y = tTopIntersect.Y };
                    }
                    else {
                        // the edge should be intersected at bottom of source panel and top of target panel
                        edgeTargetXY = new Point() { X = tBottomIntersect.X, Y = tBottomIntersect.Y };
                    }

                    if (sourceNodeCenter.Y > targetNodeCenter.Y) {
                        //angle = Math.Atan(a) / Math.PI * 180 - 180;
                        targetAngle = Math.Atan(a) / Math.PI * 180;
                        if (targetAngle < 0) {
                            targetAngle = Math.Atan(a) / Math.PI * 180 + 180;
                        }
                        //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "7", sourceNode.Label, targetNode.Label, targetAngle);
                    }
                    else {
                        //sourceAngle = Math.Atan(a) / Math.PI * 180;
                        targetAngle = Math.Atan(a) / Math.PI * 180;
                        if (targetAngle > 0) {
                            //sourceAngle = Math.Atan(a) / Math.PI * 180 - 180;
                            targetAngle = Math.Atan(a) / Math.PI * 180 - 180;
                        }
                        //Debug.WriteLine("Type {0}, Source {1}, Target {2}, Angle {3}", "8", sourceNode.Label, targetNode.Label, targetAngle);
                    }
                }
            }
        }

        private double getManhattanDistance(XY source, XY target) {
            return Math.Abs(source.X - target.X) + Math.Abs(source.Y - target.Y);
        }

        private double getEuclideanDistance(XY source, XY target) {
            return Math.Sqrt(Math.Pow((source.X - target.X), 2) + Math.Pow((source.Y - target.Y), 2));
        }


        public void ResetNodeCaptures() {
            foreach (var node in this.pathwayObj.Nodes) {
                node.IsSelected = false;
            }
        }

        public void MultipleNodeCapture() {

            var startX = this.pathwayUI.CurrentMousePoint.X < this.pathwayUI.LeftButtonStartClickPoint.X
                ? this.pathwayUI.CurrentMousePoint.X : this.pathwayUI.LeftButtonStartClickPoint.X;
            var endX = this.pathwayUI.CurrentMousePoint.X < this.pathwayUI.LeftButtonStartClickPoint.X
                ? this.pathwayUI.LeftButtonStartClickPoint.X : this.pathwayUI.CurrentMousePoint.X;
            var startY = this.pathwayUI.CurrentMousePoint.Y < this.pathwayUI.LeftButtonStartClickPoint.Y
              ? this.pathwayUI.CurrentMousePoint.Y : this.pathwayUI.LeftButtonStartClickPoint.Y;
            var endY = this.pathwayUI.CurrentMousePoint.Y < this.pathwayUI.LeftButtonStartClickPoint.Y
                ? this.pathwayUI.LeftButtonStartClickPoint.Y : this.pathwayUI.CurrentMousePoint.Y;

            var nodes = this.pathwayObj.Nodes;
            foreach (var node in nodes) {
                var centerXY = xyValuesToPoints(node.X, node.Y);
                if (startX <= centerXY.X && centerXY.X <= endX &&
                    startY <= centerXY.Y && centerXY.Y <= endY) {
                    if (node.IsSelected == true) node.IsSelected = false;
                    else node.IsSelected = true;
                }
            }
        }

        private void drawNodes() {

            var nodes = this.pathwayObj.Nodes;
            var multipleNodeSelected = nodes.Count(n => n.IsSelected) > 1 ? true : false;
            foreach (var node in nodes) {
                var centerXY = xyValuesToPoints(node.CopyX, node.CopyY);
                // check xs, ys, xe, ye
                var nodeRect = getNodeRectanglePoints(node.Width, node.Height, centerXY.X, centerXY.Y);

                if (isMouseOnNode(nodeRect)) {
                    if (this.IsControlHold || this.IsShiftHold) {
                        if (node.IsSelected == true) node.IsSelected = false;
                        else node.IsSelected = true;
                        this.IsLeftClicked = false;
                    }
                    else {
                        if (this.IsNodeCaptured == false) {
                            node.IsSelected = true;
                            this.pathwayObj.SelectedPlotID = node.ID;
                            this.IsNodeCaptured = true;
                        }
                    }
                }

                if (node.IsSelected && !this.IsControlHold && !this.IsShiftHold) { // if yes, update node points
                    // node points are set to the current mouse point
                    var endPoint = this.pathwayUI.CurrentMousePoint;
                    var startPoint = this.pathwayUI.LeftButtonStartClickPoint;
                    var xPointDiff = endPoint.X - startPoint.X;
                    var yPoindDiff = endPoint.Y - startPoint.Y;

                    var centerOriginalXY = xyValuesToPoints(node.X, node.Y);

                    var updateX = centerOriginalXY.X + xPointDiff;
                    var updateY = centerOriginalXY.Y + yPoindDiff;

                    nodeRect = getNodeRectanglePoints(node.Width, node.Height, updateX, updateY);
                    var newNodeXY = xyPointsToValues(updateX, updateY);
                    node.CopyX = (float)newNodeXY.X;
                    node.CopyY = (float)newNodeXY.Y;

                    if (this.IsLeftReleased) {
                        newNodeXY = xyPointsToValues(updateX, updateY);
                        node.X = (float)newNodeXY.X;
                        node.Y = (float)newNodeXY.Y;
                    }      
                }

                this.xs = nodeRect.StartXY.X;
                this.xe = nodeRect.EndXY.X;
                this.ys = nodeRect.StartXY.Y;
                this.ye = nodeRect.EndXY.Y;

                var sPoint = new Point(this.xs, this.ys);
                var ePoint = new Point(this.xe, this.ye);
                var rect = new Rect(sPoint, ePoint);

                if (node.BarChart != null) {
                    this.drawingContext.DrawRectangle(graphBorder.Brush, new Pen(Brushes.Black, 1.0), rect);
                    this.drawingContext.DrawImage(node.BarImageSource, rect);
                }
                else {
                    var nodeFontSize = node.Label == "All others" ? fontsize * this.xPacket * 2.0 : fontsize * this.xPacket;
                    var brush = node.Label == "All others" ? Brushes.LightBlue : graphBorder.Brush;
                    this.drawingContext.DrawRectangle(brush, new Pen(Brushes.Black, 1.0), rect);
                    this.formattedText = new FormattedText(node.Label, cultureinfo,
                        FlowDirection.LeftToRight, typeface, nodeFontSize, graphFontBrush) {
                        TextAlignment = TextAlignment.Center
                    };
                    this.drawingContext.DrawText(this.formattedText, new Point((this.xs + this.xe) * 0.5, (this.ys + this.ye - this.formattedText.Height) * 0.5 ));
                }

                if (node.IsSelected) {
                    this.drawingContext.DrawRectangle(null, new Pen(Brushes.Red, 1.0), rect);
                }
            }
        }

        private bool isMouseOnNode(NodeRect nodeRect) {
            if (this.IsLeftClicked == false) return false;

            var leftMousePoint = this.pathwayUI.CurrentMousePoint;
            var leftX = leftMousePoint.X;
            var leftY = leftMousePoint.Y;

            if (nodeRect.StartXY.X <= leftX && leftX <= nodeRect.EndXY.X && 
                nodeRect.StartXY.Y <= leftY && leftY <= nodeRect.EndXY.Y) {
                return true;
            }
            else {
                return false;
            }
        }

        // x and y should be the center of targeted node
        private NodeRect getNodeRectanglePoints(double width, double height, double x, double y) {
            var startX = x - width * 0.5 * this.xPacket;
            var endX = x + width * 0.5 * this.xPacket;
            var startY = y - height * 0.5 * this.yPacket;
            var endY = y + height * 0.5 * this.yPacket;

            return new NodeRect() {
                StartXY = new XY() { X = startX, Y = startY },
                EndXY = new XY() { X = endX, Y = endY }
            };
        }

        private XY xyValuesToPoints(double x, double y) {
            var xpoint = xvalueToPoint(x);
            var ypoint = yvalueToPoint(y);
            return new XY() { X = xpoint, Y = ypoint };
        }

        private XY xyPointsToValues(double x, double y) {
            var xpoint = xpointToValue(x);
            var ypoint = ypointToValue(y);
            return new XY() { X = xpoint, Y = ypoint };
        }

        private double yvalueToPoint(double y) {
            return this.pathwayUI.BottomMargin + (y - (double)this.pathwayObj.DisplayRangeMinY) * this.yPacket;
        }

        private double xvalueToPoint(double x) {
            return this.pathwayUI.LeftMargin + (x - (double)this.pathwayObj.DisplayRangeMinX) * this.xPacket;
        }

        private double ypointToValue(double y) {
            return (double)this.pathwayObj.DisplayRangeMinY + (y - this.pathwayUI.BottomMargin) / this.yPacket;
        }

        private double xpointToValue(double x) {
            return (double)this.pathwayObj.DisplayRangeMinX + (x - this.pathwayUI.LeftMargin) / this.xPacket;
        }

        private void setPackets() {
            #region
            this.xPacket = (this.ActualWidth - this.pathwayUI.LeftMargin - this.pathwayUI.RightMargin) / 
                (double)(this.pathwayObj.DisplayRangeMaxX - this.pathwayObj.DisplayRangeMinX);
            this.yPacket = (this.ActualHeight - this.pathwayUI.TopMargin - this.pathwayUI.BottomMargin) / 
                (double)(this.pathwayObj.DisplayRangeMaxY - this.pathwayObj.DisplayRangeMinY);

            // test
            this.yPacket = this.xPacket;
            #endregion
        }

        public Point GetDataPositionOnMousePoint(Point mousePoint) {
            if (this.pathwayObj == null) return new Point();

            float x_Value, y_Value;

            x_Value = (float)this.pathwayObj.DisplayRangeMinX + (float)((mousePoint.X - this.pathwayUI.LeftMargin) / this.xPacket);
            y_Value = (float)this.pathwayObj.DisplayRangeMinY + (float)((mousePoint.Y - this.pathwayUI.BottomMargin) / this.yPacket);

            return new Point(x_Value, y_Value);
        }

        public void ZoomShiftedRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, 
                new Rect(new Point(this.pathwayUI.LeftButtonStartClickPoint.X, 
                this.pathwayUI.LeftButtonStartClickPoint.Y), 
                new Point(this.pathwayUI.CurrentMousePoint.X, 
                this.pathwayUI.CurrentMousePoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void ZoomRubberDraw() {
            if (this.visualCollection.Count > 1)
                this.visualCollection.RemoveAt(1);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();
            drawingContext.DrawRectangle(rubberRectangleBackGround, rubberRectangleBorder, 
                new Rect(
                    new Point(this.pathwayUI.RightButtonStartClickPoint.X,
                        this.pathwayUI.RightButtonStartClickPoint.Y), 
                    new Point(this.pathwayUI.CurrentMousePoint.X, 
                        this.pathwayUI.RightButtonEndClickPoint.Y)));
            drawingContext.Close();
            this.visualCollection.Add(drawingVisual);
        }

        public void GraphScroll() {
            if (this.pathwayUI.LeftButtonStartClickPoint.X == -1 || this.pathwayUI.LeftButtonStartClickPoint.Y == -1)
                return;

            float durationX = (float)this.pathwayObj.DisplayRangeMaxX - (float)this.pathwayObj.DisplayRangeMinX;
            double distanceX = 0;

            float newMinX = 0, newMaxX = 0, newMinY = 0, newMaxY = 0;

            float durationY;
            double distanceY = 0;

            // X-Direction
            if (this.pathwayUI.LeftButtonStartClickPoint.X > this.pathwayUI.LeftButtonEndClickPoint.X) {
                distanceX = this.pathwayUI.LeftButtonStartClickPoint.X - this.pathwayUI.LeftButtonEndClickPoint.X;

                newMinX = this.pathwayUI.GraphScrollInitialRtMin + (float)(distanceX / this.xPacket);
                newMaxX = this.pathwayUI.GraphScrollInitialRtMax + (float)(distanceX / this.xPacket);

                if (newMaxX > this.pathwayObj.MaxX) {
                }
                else {
                    this.pathwayObj.DisplayRangeMinX = newMinX;
                    this.pathwayObj.DisplayRangeMaxX = newMaxX;
                }
            }
            else {
                distanceX = this.pathwayUI.LeftButtonEndClickPoint.X - this.pathwayUI.LeftButtonStartClickPoint.X;

                newMinX = this.pathwayUI.GraphScrollInitialRtMin - (float)(distanceX / this.xPacket);
                newMaxX = this.pathwayUI.GraphScrollInitialRtMax - (float)(distanceX / this.xPacket);

                if (newMinX < this.pathwayObj.MinX) {
                }
                else {
                    this.pathwayObj.DisplayRangeMinX = newMinX;
                    this.pathwayObj.DisplayRangeMaxX = newMaxX;
                }
            }

            // Y-Direction
            durationY = (float)this.pathwayObj.DisplayRangeMaxY - (float)this.pathwayObj.DisplayRangeMinY;
            if (this.pathwayUI.LeftButtonStartClickPoint.Y < this.pathwayUI.LeftButtonEndClickPoint.Y) {
                distanceY = this.pathwayUI.LeftButtonEndClickPoint.Y - this.pathwayUI.LeftButtonStartClickPoint.Y;

                newMinY = this.pathwayUI.GraphScrollInitialIntensityMin - (float)(distanceY / this.yPacket);
                newMaxY = this.pathwayUI.GraphScrollInitialIntensityMax - (float)(distanceY / this.yPacket);

                if (newMaxY > this.pathwayObj.MaxY) {
                }
                else {
                    this.pathwayObj.DisplayRangeMinY = newMinY;
                    this.pathwayObj.DisplayRangeMaxY = newMaxY;
                }
            }
            else {
                distanceY = this.pathwayUI.LeftButtonStartClickPoint.Y - this.pathwayUI.LeftButtonEndClickPoint.Y;

                newMinY = this.pathwayUI.GraphScrollInitialIntensityMin + (float)(distanceY / this.yPacket);
                newMaxY = this.pathwayUI.GraphScrollInitialIntensityMax + (float)(distanceY / this.yPacket);

                if (newMinY < this.pathwayObj.MinY) {
                }
                else {
                    this.pathwayObj.DisplayRangeMinY = newMinY;
                    this.pathwayObj.DisplayRangeMaxY = newMaxY;
                }
            }
            PathwayMapDraw();
        }

        public void GraphZoom() {
            // Avoid Miss Double Click Operation
            if (Math.Abs(this.pathwayUI.RightButtonStartClickPoint.X - this.pathwayUI.RightButtonEndClickPoint.X) < 5 && Math.Abs(this.pathwayUI.RightButtonStartClickPoint.Y - this.pathwayUI.RightButtonEndClickPoint.Y) < 5)
                return;

            // Avoid Focus exceeding data point resolution            
            if (Math.Abs(this.pathwayUI.RightButtonStartClickPoint.X - this.pathwayUI.RightButtonEndClickPoint.X) / xPacket < 0.01) {
                return;
            }

            var HoverV = (this.pathwayObj.MaxX - this.pathwayObj.MinX) / (this.pathwayObj.MaxY - this.pathwayObj.MinY);
            var VoverH = (this.pathwayObj.MaxY - this.pathwayObj.MinY) / (this.pathwayObj.MaxX - this.pathwayObj.MinX);

            var startX = this.pathwayUI.CurrentMousePoint.X < this.pathwayUI.RightButtonStartClickPoint.X
                ? this.pathwayUI.CurrentMousePoint.X : this.pathwayUI.RightButtonStartClickPoint.X;
            var endX = this.pathwayUI.CurrentMousePoint.X < this.pathwayUI.RightButtonStartClickPoint.X
                ? this.pathwayUI.RightButtonStartClickPoint.X : this.pathwayUI.CurrentMousePoint.X;
            var startY = this.pathwayUI.CurrentMousePoint.Y < this.pathwayUI.RightButtonStartClickPoint.Y
              ? this.pathwayUI.CurrentMousePoint.Y : this.pathwayUI.RightButtonStartClickPoint.Y;
            var endY = this.pathwayUI.CurrentMousePoint.Y < this.pathwayUI.RightButtonStartClickPoint.Y
                ? this.pathwayUI.RightButtonStartClickPoint.Y : this.pathwayUI.CurrentMousePoint.Y;

            var rubberWidth = endX - startX;
            var rubberHeight = endY - startY;

            if (rubberWidth > rubberHeight) { // meaning horizontal axis is bigger than vertical
                var centerY = (startY + endY) * 0.5;
                var yHeight = rubberWidth * VoverH;
                startY = centerY - yHeight * 0.5;
                endY = centerY + yHeight * 0.5;
            }
            else { // meaning vertical axis is bigger than hori
                var centerX = (startX + endX) * 0.5;
                var xWidth = rubberHeight * HoverV;
                startX = centerX - xWidth * 0.5;
                endX = centerX + xWidth * 0.5;
            }

            this.pathwayObj.DisplayRangeMaxX = this.pathwayObj.DisplayRangeMinX + (float)((endX - this.pathwayUI.LeftMargin) / this.xPacket);
            this.pathwayObj.DisplayRangeMinX = this.pathwayObj.DisplayRangeMinX + (float)((startX - this.pathwayUI.LeftMargin) / this.xPacket);

            this.pathwayObj.DisplayRangeMaxY = this.pathwayObj.DisplayRangeMinY + (float)((endY - this.pathwayUI.BottomMargin) / this.yPacket);
            this.pathwayObj.DisplayRangeMinY = this.pathwayObj.DisplayRangeMinY + (float)((startY - this.pathwayUI.BottomMargin) / this.yPacket);

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
