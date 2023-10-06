using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.Legacy {
    public class PlainChromatogramXicForTableViewerLegacy {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        //ViewModel
        private ChromatogramXicViewModelLegacy chromatogramXicViewModel;

        // Scale
        private double axisFromGraphArea = 1; // Location of Axis from Graph Area

        // Graph Color & Font Settings
        private Brush graphBackGround = Brushes.WhiteSmoke; // Graph Background
        private Pen graphBorder = new Pen(Brushes.LightGray, 1.0); // Graph Border
        private Pen graphAxis = new Pen(Brushes.Black, 0.5);

        // Drawing Coordinates
        private double xs, ys;

        // Drawing Packet
        private double xPacket;
        private double yPacket;

        private double leftMargin = 3;
        private double topMargin = 5;
        private double rightMargin = 5;
        private double bottomMargin = 3;
        private double topMarginForLabel = 1;

        private int height;
        private int width;
        private int dpix;
        private int dpiy;

        public PlainChromatogramXicForTableViewerLegacy(int h, int w, int x, int y) {
            height = h; width = w; dpix = x; dpiy = y;
            graphBorder.Freeze();
            graphAxis.Freeze();
        }

        public Drawing GetChromatogramDrawing(ChromatogramXicViewModelLegacy chromatogramXicViewModel) {
            this.chromatogramXicViewModel = chromatogramXicViewModel;
            var drawingVisual = chromatogramDrawingVisual((double)width, (double)height);
            var drawing = drawingVisual.Drawing;
            drawing.Freeze();
            return drawing;
        }

        public DrawingImage GetChromatogramDrawingImage(ChromatogramXicViewModelLegacy chromatogramXicViewModel) {
            this.chromatogramXicViewModel = chromatogramXicViewModel;
            var drawingVisual = chromatogramDrawingVisual((double)width, (double)height);
            var image = new DrawingImage(drawingVisual.Drawing);
            image.Freeze();
            return image;
        }

        public BitmapSource DrawChromatogramXic2BitmapSource(ChromatogramXicViewModelLegacy chromatogramXicViewModel) {
            this.chromatogramXicViewModel = chromatogramXicViewModel;

            var drawingVisual = chromatogramDrawingVisual((double)width, (double)height);
            var renderTargetBitmap = new RenderTargetBitmap(width * dpix / 96, height * dpiy / 96, dpix, dpiy, PixelFormats.Pbgra32);
            renderTargetBitmap.Render(drawingVisual);
            renderTargetBitmap.Freeze();
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            System.Drawing.Bitmap bmap;
            using (var mem = new System.IO.MemoryStream()) {
                encoder.Save(mem);
                bmap = new System.Drawing.Bitmap(mem);
            }

            var bitmapSource = ConvertBitmap(bmap);
            bitmapSource.Freeze();
            return bitmapSource;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bmp) {
            IntPtr hBitmap = bmp.GetHbitmap();
            try {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            finally {
                DeleteObject(hBitmap);
            }
        }

        private DrawingVisual chromatogramDrawingVisual(double drawWidth, double drawHeight) {
            var drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < this.leftMargin + this.rightMargin || drawHeight < this.bottomMargin + this.topMargin) return drawingVisual;
            var drawingContext = drawingVisual.RenderOpen();

            // Graph Brush and Pen
            SolidColorBrush graphBrush;
            Pen graphPen;
            Pen graphPenPeakEdge;

            //Bean
            ChromatogramBeanLegacy chromatogramBean;

            // Graph Line and Area Draw
            PathFigure pathFigure;
            PathFigure areaPathFigure;
            PathGeometry pathGeometry;
            PathGeometry areaPathGeometry;
            //data point
            int scanNumber;
            float retentionTime;
            float intensity;
            float mzValue;

            // 1. Draw background, graphRegion, x-axis, y-axis
            #region
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.leftMargin, this.topMargin), new Size(drawWidth - this.leftMargin - this.rightMargin, drawHeight - this.bottomMargin - this.topMargin)));
            drawingContext.DrawLine(this.graphAxis, new Point(this.leftMargin - this.axisFromGraphArea, drawHeight - this.bottomMargin), new Point(drawWidth - this.rightMargin, drawHeight - this.bottomMargin));
            drawingContext.DrawLine(this.graphAxis, new Point(this.leftMargin - this.axisFromGraphArea, drawHeight - this.bottomMargin), new Point(this.leftMargin - this.axisFromGraphArea, this.topMargin));
            #endregion

            // 2. Check null of chromatogramMrmBean
            #region
            if (this.chromatogramXicViewModel == null) {
                // Close DrawingContext
                drawingContext.Close();
                return drawingVisual;
            }
            #endregion

            // 3. Calculate packet size
            #region
            this.xPacket = (drawWidth - this.leftMargin - this.rightMargin) / (double)(this.chromatogramXicViewModel.DisplayRangeRtMax - this.chromatogramXicViewModel.DisplayRangeRtMin);
            this.yPacket = (drawHeight - this.topMargin - this.bottomMargin - this.topMarginForLabel) / (double)(this.chromatogramXicViewModel.DisplayRangeIntensityMax - this.chromatogramXicViewModel.DisplayRangeIntensityMin);
            #endregion

            drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            drawingContext.PushTransform(new ScaleTransform(1, -1));
            drawingContext.PushClip(new RectangleGeometry(new Rect(this.leftMargin, this.bottomMargin, drawWidth - this.leftMargin - this.rightMargin, drawHeight - this.bottomMargin - this.topMargin)));

            // 5. Reference chromatogram
            #region
            chromatogramBean = this.chromatogramXicViewModel.ChromatogramBean;

            // 5-1. Initialize Graph Plot Start
            #region
            pathFigure = new PathFigure() { StartPoint = new Point(0.0, 0.0) }; // PathFigure for GraphLine
            areaPathFigure = new PathFigure(); // PathFigure for GraphLine
            graphBrush = combineAlphaAndColor(0.25, chromatogramBean.DisplayBrush);// Set Graph Brush
            graphPen = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness); // Set Graph Pen
            graphPenPeakEdge = new Pen(chromatogramBean.DisplayBrush, chromatogramBean.LineTickness * 1.5); // Set Graph Pen
            graphPenPeakEdge.Freeze();
            graphBrush.Freeze();
            graphPen.Freeze();

            var flagLeft = true;
            var flagRight = true;
            var flagFill = false;
            #endregion

            // 5-2. Draw datapoints
            var pathPoints = new List<Point>();
            var areaPathPoints = new List<Point>();
            #region
            if (this.chromatogramXicViewModel.FillPeakArea && this.chromatogramXicViewModel.TargetRightRt - this.chromatogramXicViewModel.TargetLeftRt > 0.0001) {
                for (int i = 0; i < chromatogramBean.ChromatogramDataPointCollection.Count; i++) {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[i].ID;
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[i].ChromXs.Value;
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[i].Intensity;
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[i].Mass;
                    if (retentionTime < this.chromatogramXicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.leftMargin + (retentionTime - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.bottomMargin + (intensity - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathPoints.Add(new Point(this.xs, this.ys));

                    if (flagFill) {
                        areaPathPoints.Add(new Point(this.xs, this.ys));
                    }
                    if (flagLeft && retentionTime >= this.chromatogramXicViewModel.TargetLeftRt) {
                        areaPathFigure.StartPoint = new Point(this.xs, this.bottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket); // PathFigure for GraphLine
                        areaPathPoints.Add(new Point(this.xs, this.ys));
                        flagFill = true; flagLeft = false;
                    }
                    else if (flagRight && retentionTime > this.chromatogramXicViewModel.TargetRightRt) {
                        areaPathPoints.Add(new Point(this.xs, this.bottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                        flagFill = false; flagRight = false;
                    }

                    //if (Math.Abs(retentionTime - this.chromatogramXicViewModel.TargetRt) < 0.0001)
                    //    this.drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xs, this.ys),
                    //        new Point(this.xs, this.bottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    if (i == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramXicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond
                }
                areaPathPoints.Add(new Point(this.xs, 0));
                areaPathFigure.Segments.Add(new PolyLineSegment(areaPathPoints, isStroked: false));
                areaPathFigure.Freeze();
                areaPathGeometry = new PathGeometry(new PathFigure[] { areaPathFigure });
                areaPathGeometry.Freeze();

                drawingContext.DrawGeometry(graphBrush, graphPenPeakEdge, areaPathGeometry);
            }
            else {
                for (int i = 0; i < chromatogramBean.ChromatogramDataPointCollection.Count; i++) {
                    scanNumber = (int)chromatogramBean.ChromatogramDataPointCollection[i].ID;
                    retentionTime = (float)chromatogramBean.ChromatogramDataPointCollection[i].ChromXs.Value;
                    intensity = (float)chromatogramBean.ChromatogramDataPointCollection[i].Intensity;
                    mzValue = (float)chromatogramBean.ChromatogramDataPointCollection[i].Mass;
                    if (retentionTime < this.chromatogramXicViewModel.DisplayRangeRtMin - 5) continue; // Use Data -5 second beyond

                    this.xs = this.leftMargin + (retentionTime - (float)this.chromatogramXicViewModel.DisplayRangeRtMin) * this.xPacket;// Calculate x Plot Coordinate
                    this.ys = this.bottomMargin + (intensity - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket;// Calculate y Plot Coordinate

                    if (this.xs < double.MinValue || this.xs > double.MaxValue || this.ys < double.MinValue || this.ys > double.MaxValue) continue;// Avoid Calculation Error
                    pathPoints.Add(new Point(this.xs, this.ys));

                    if (Math.Abs(retentionTime - this.chromatogramXicViewModel.TargetRt) < 0.0001) drawingContext.DrawLine(new Pen(Brushes.Red, 1.0), new Point(this.xs, this.ys), new Point(this.xs, this.bottomMargin + (0 - (float)this.chromatogramXicViewModel.DisplayRangeIntensityMin) * this.yPacket));
                    if (i == -1 + chromatogramBean.ChromatogramDataPointCollection.Count || retentionTime > this.chromatogramXicViewModel.DisplayRangeRtMax + 5) break;// Use Data till +5 second beyond
                }
            }
            #endregion

            // 5-3. Close Graph Path (When Loop Finish or Display range exceeded)
            #region
            pathPoints.Add(new Point(drawWidth, 0.0));
            pathFigure.Segments.Add(new PolyLineSegment(pathPoints, isStroked: true));
            pathFigure.Freeze();
            pathGeometry = new PathGeometry(new PathFigure[] { pathFigure });
            pathGeometry.Freeze();
            #endregion

            drawingContext.DrawGeometry(null, graphPen, pathGeometry); // Draw Chromatogram Graph Line
            #endregion

            drawingContext.Pop();// Reset Drawing Region
            drawingContext.Pop();// Reset Drawing Region
            drawingContext.Pop();// Reset Drawing Region
            drawingContext.Close();// Close DrawingContext

            return drawingVisual;
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
    }
}
