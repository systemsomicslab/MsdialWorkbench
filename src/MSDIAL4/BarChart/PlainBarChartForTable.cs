using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace Common.BarChart
{
    public class PlainBarChartForTable {

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        private DrawingVisual drawingVisual;
        private DrawingContext drawingContext;

        private BarChartBean barChartBean;

        private Brush graphBackGround = Brushes.WhiteSmoke; // Graph Background
        private Pen graphBorder = new Pen(Brushes.LightGray, 1.0); // Graph Border

        // Drawing Packet
        private double yPacket;

        // Bar width
        private double leftMargin = 3;
        private double topMargin = 5;
        private double rightMargin = 5;
        private double bottomMargin = 3;
        private double topMarginForLabel = 1;
        private double barMargin = 5;

        private double drawSpace;
        private double barSpace;
        private double barWidth;
        private double elementNumber;
        private double errorLineLength;
        private double errorWidth;

        private int height;
        private int width;
        private int dpix;
        private int dpiy;

        public PlainBarChartForTable(int h, int w, int x, int y) {
            height = h; width = w; dpix = x; dpiy = y;
        }

        private void SetBarChartDrawingVisual(BarChartBean barChartBean, bool isAllRequiredElements = false) {
            this.barChartBean = barChartBean;
            if (isAllRequiredElements == false)
                this.drawingVisual = GetBarChartDrawingVisual((double)width, (double)height);
            else {
                var barchartUI = new Common.BarChart.BarChartUI(barChartBean);
                barchartUI.TopMargin = 15; // default 15, for temp, use 25
                barchartUI.TopMarginForLabel = 10;
                this.drawingVisual = new Common.BarChart.BarChartFE(barChartBean, barchartUI).GetBarChartDrawingVisual((double)width, (double)height);
            }
        }

        public DrawingImage GetDrawingImage(BarChartBean barChartBean, bool isAllRequiredElements = false) {
            SetBarChartDrawingVisual(barChartBean, isAllRequiredElements);
            var drawingImage = new DrawingImage(this.drawingVisual.Drawing);
            drawingImage.Freeze();
            return drawingImage;
        }

        public BitmapSource DrawBarChart2BitmapSource(BarChartBean barChartBean, bool isAllRequiredElements = false) {
            SetBarChartDrawingVisual(barChartBean, isAllRequiredElements);

            RenderTargetBitmap renderTargetBitmap = null;
            try {
                renderTargetBitmap = new RenderTargetBitmap(width * dpix / 96, height * dpiy / 96, dpix, dpiy, PixelFormats.Pbgra32);
                renderTargetBitmap.Render(this.drawingVisual);

                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
            }
            catch (System.Runtime.InteropServices.COMException ex) {
                Console.WriteLine(ex.Message);
                
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
                return null;
            }
            finally {

            }

            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            System.Drawing.Bitmap bmap;
            using (var mem = new System.IO.MemoryStream()) {
                encoder.Save(mem);
                bmap = new System.Drawing.Bitmap(mem);
            }

            var bitmapSource = ConvertBitmap(bmap);
            if (bitmapSource == null) return null;

            bitmapSource.Freeze();
            return bitmapSource;
        }

        public static BitmapSource ConvertBitmap(System.Drawing.Bitmap bmp) {
            IntPtr hBitmap = IntPtr.Zero;
            try {
                hBitmap = bmp.GetHbitmap();
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch (System.ArgumentException ex) {
                Console.WriteLine(ex.Message);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
                return null;
            }
            catch (System.Runtime.InteropServices.ExternalException ex) {
                Console.WriteLine(ex.Message);
                //GC.Collect();
                //GC.WaitForPendingFinalizers();
                //GC.Collect();
                return null;
            }
            finally {
                DeleteObject(hBitmap);
            }
        }

        public DrawingVisual GetBarChartDrawingVisual(double drawWidth, double drawHeight) {
            var drawingVisual = new DrawingVisual();

            // Check Drawing Size
            if (drawWidth < 2 * (this.leftMargin + this.rightMargin) || drawHeight < 1.5 * (this.bottomMargin + this.topMargin)) return drawingVisual;
            this.drawingContext = drawingVisual.RenderOpen();

            drawBackground(drawWidth, drawHeight);
            if (this.barChartBean == null) return null;
            this.yPacket = (drawHeight - this.topMargin - this.topMarginForLabel - this.bottomMargin) / (double)(this.barChartBean.MaxValue - this.barChartBean.MinValue);

            drawingSetting(drawWidth, drawHeight, this.barChartBean.DisplayedBarElements.Count);
            this.drawingContext.PushTransform(new TranslateTransform(0, drawHeight));
            this.drawingContext.PushTransform(new ScaleTransform(1, -1));
            this.drawingContext.PushClip(new RectangleGeometry(new Rect(this.leftMargin, this.bottomMargin, drawWidth - this.leftMargin - this.rightMargin, drawHeight - this.bottomMargin - this.topMargin)));

            // 5-1. Initialize Graph Plot Start
            #region
            // Graph Brush and Pen
            Pen errorPen;
            errorPen = new Pen(Brushes.Black, this.errorLineLength); // Set Graph Pen
            errorPen.Freeze();

            var graphBrush = combineAlphaAndColor(0.85, Brushes.Blue);
            #endregion

            var elements = this.barChartBean.DisplayedBarElements;
            for (int i = 0; i < elements.Count; i++) {
                if (elements[i].IsBoxPlot == false) {
                    var min = this.barChartBean.MinValue;
                    var barMax = elements[i].Value + elements[i].Error;
                    var barValue = elements[i].Value;
                    var barMin = elements[i].Value - elements[i].Error;
                    var yPointMax = this.bottomMargin + (barMax - min) * this.yPacket;
                    var yPointCenter = this.bottomMargin + (barValue - min) * this.yPacket;
                    var yPointMin = this.bottomMargin + (barMin - min) * this.yPacket;
                    var xPoint = this.barSpace * 0.5 + i * this.barSpace + this.leftMargin;

                    var barBrush = combineAlphaAndColor(0.75, elements[i].Brush);

                    this.drawingContext.DrawLine(new Pen(barBrush, this.barWidth), new Point(xPoint, yPointCenter), new Point(xPoint, this.bottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.barWidth * 0.5, yPointCenter), new Point(xPoint - this.barWidth * 0.5, this.bottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint + this.barWidth * 0.5, yPointCenter), new Point(xPoint + this.barWidth * 0.5, this.bottomMargin));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.barWidth * 0.5, yPointCenter), new Point(xPoint + this.barWidth * 0.5, yPointCenter));

                    if (elements[i].Error <= 0) continue;
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPointMin), new Point(xPoint, yPointMax));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.errorWidth, yPointMax), new Point(xPoint + this.errorWidth, yPointMax));
                    this.drawingContext.DrawLine(errorPen, new Point(xPoint - this.errorWidth, yPointMin), new Point(xPoint + this.errorWidth, yPointMin));
                } else {
                    var min = this.barChartBean.MinValue;
                    var boxMax = elements[i].MaxValue;
                    var box75 = elements[i].SeventyFiveValue;
                    var boxMedian = elements[i].Median;
                    var box25 = elements[i].TwentyFiveValue;
                    var boxMin = elements[i].MinValue;

                    var yPointMax = this.bottomMargin + (boxMax - min) * this.yPacket;
                    var yPoint75 = this.bottomMargin + (box75 - min) * this.yPacket;
                    var yPointMedian = this.bottomMargin + (boxMedian - min) * this.yPacket;
                    var yPoint25 = this.bottomMargin + (box25 - min) * this.yPacket;
                    var yPointMin = this.bottomMargin + (boxMin - min) * this.yPacket;
                    var xPoint = this.barSpace * 0.5 + i * this.barSpace + this.leftMargin;

                    var barBrush = combineAlphaAndColor(0.75, elements[i].Brush);

                    // draw box
                    this.drawingContext.DrawLine(new Pen(barBrush, this.barWidth),
                        new Point(xPoint, yPoint75),
                        new Point(xPoint, yPoint25));

                    // draw median
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPointMedian),
                        new Point(xPoint + this.barWidth * 0.5, yPointMedian));

                    // draw border
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint75),
                        new Point(xPoint - this.barWidth * 0.5, yPoint25));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint + this.barWidth * 0.5, yPoint75),
                        new Point(xPoint + this.barWidth * 0.5, yPoint25));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint75),
                        new Point(xPoint + this.barWidth * 0.5, yPoint75));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.barWidth * 0.5, yPoint25),
                        new Point(xPoint + this.barWidth * 0.5, yPoint25));

                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPoint75), new Point(xPoint, yPointMax));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.errorWidth, yPointMax),
                        new Point(xPoint + this.errorWidth, yPointMax));

                    this.drawingContext.DrawLine(errorPen, new Point(xPoint, yPoint25), new Point(xPoint, yPointMin));
                    this.drawingContext.DrawLine(errorPen,
                        new Point(xPoint - this.errorWidth, yPointMin),
                        new Point(xPoint + this.errorWidth, yPointMin));
                }
            }

            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Pop();// Reset Drawing Region
            this.drawingContext.Close();// Close DrawingContext

            return drawingVisual;
        }

        private void drawingSetting(double drawWidth, double drawHeight, int elementNum) {
            this.elementNumber = elementNum;
            this.drawSpace = drawWidth - this.rightMargin - this.leftMargin;
            this.barSpace = this.drawSpace / this.elementNumber;
            this.barWidth = this.barSpace - this.barMargin;
            this.errorWidth = this.barWidth * 0.25;
            this.errorLineLength = this.errorWidth * 0.5; if (this.errorLineLength > 2) this.errorLineLength = 2.0;
            if (this.barWidth < 0) return;
        }


        private void drawBackground(double drawWidth, double drawHeight) {
            // Draw background, graphRegion, x-axis, y-axis
            #region
            this.drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, drawWidth, drawHeight));
            this.drawingContext.DrawRectangle(this.graphBackGround, this.graphBorder, new Rect(new Point(this.leftMargin, this.topMargin), new Size(drawWidth - this.leftMargin - this.rightMargin, drawHeight - this.bottomMargin - this.topMargin)));
            #endregion
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
            } catch {
                returnSolidColorBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            }
            return returnSolidColorBrush;
        }
    }
}