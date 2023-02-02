using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharp;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PdfExporter
{
    public class PdfExporter
    {
        public string Facename { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;
        public int SpaceSize { get; set; } = 12;
        public XPdfFontOptions Options { get; set; } = new XPdfFontOptions(PdfFontEncoding.WinAnsi);
        public XFont DefaultFont { get; set; }
        public XBrush DefaultBrush { get; set; } = XBrushes.Black;
        public PdfDocument Document { get; set; }
        public XGraphics XGraphics { get; set; }
        public string SavePath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int DpiX { get; set; }
        public int DpiY { get; set; }

        public PdfExporter(string path) { SavePath = path; DefaultFont = new XFont(Facename, FontSize, XFontStyle.Regular, Options); }
        public PdfExporter(string path, int width, int height, int dpiX, int dpiY, int size = 10, int spacesize = 10) {
            SavePath = path;
            FontSize = size; SpaceSize = spacesize;
            DefaultFont = new XFont(Facename, FontSize, XFontStyle.Regular, Options);
            Width = width; Height = height; DpiX = dpiX / 96; DpiY = dpiY / 96;
            StartExport();
        }

        public void StartExport() {
            this.Document = new PdfDocument();
        }

        public void SaveDocument() {
            this.Document.Save(SavePath);
            this.Document.Dispose();
            this.Document.Close();
        }

        public void OpenPdf() {
            Process.Start(SavePath);
        }

        public void AddPage() {
            if (XGraphics != null) { XGraphics.Dispose(); }
            var page = this.Document.AddPage();
            page.Width = Width;
            page.Height = Height;
            XGraphics = XGraphics.FromPdfPage(page);
        }

        public void AddPage(int width, int height) {
            if (XGraphics != null) { XGraphics.Dispose(); }
            var page = this.Document.AddPage();
            page.Width = width;
            page.Height = height;
            XGraphics = XGraphics.FromPdfPage(page);
        }

        public void DrawTextToPage(string text, int pointX, int pointY) {
            XGraphics.DrawString(text, DefaultFont, DefaultBrush, pointX, pointY);
        }

        public void DrawTextToPage(string text, int pointX, int pointY, int fontSize) {
            XGraphics.DrawString(text, new XFont(Facename, fontSize, XFontStyle.Regular, Options), DefaultBrush, pointX, pointY);
        }

/*
        public void DrawFigureFromChart(System.Windows.Forms.DataVisualization.Charting.Chart chart,
            int pointX, int pointY) {
            using (Bitmap bitmap = new Bitmap(WidthAct, HeightAct)) {
                chart.DrawToBitmap(bitmap, new Rectangle(0, 0, WidthAct, HeightAct));
                var ximage = XImage.FromStream(bitmap);
                XGraphics.DrawImage(ximage, pointX, pointY, Width, Height);
            }
        }
        */
        public void DrawFigureFromDrawVisual(System.Windows.Media.DrawingVisual dv, int pointX, int pointY,
            int width = 500, int height = 300, int dpix = 300, int dpiy = 300) {
            var bitmapTarget = new RenderTargetBitmap(width * dpix / 96, height * dpiy / 96, dpix, dpiy, System.Windows.Media.PixelFormats.Pbgra32);
            bitmapTarget.Render(dv); 

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapTarget));

            using (var stream = new MemoryStream()) {
                encoder.Save(stream);
                var ximage = XImage.FromStream(stream);
                XGraphics.DrawImage(ximage, pointX, pointY, width, height);
            }
        }


        public Bitmap BitmapFromSource(BitmapSource bitmapsource) {
            Bitmap bitmap;
            using (var outStream = new MemoryStream()) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }


        #region test methods
        /* public static void testChart() {
             var chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
             chart1.Series.Clear();
             chart1.ChartAreas.Clear();
             chart1.Legends.Clear();
             chart1.Titles.Clear();

             var num = new List<int[]>();
             num.Add(new int[2] { 1, 3 });
             num.Add(new int[2] { 2, 4 });
             num.Add(new int[2] { 3, 6 });
             num.Add(new int[2] { 4, 9 });
             num.Add(new int[2] { 5, 11 });

             ChartArea area1 = new ChartArea("Area1");
             area1.AxisX.MajorGrid.Enabled = false;
             area1.AxisY.MajorGrid.Enabled = false;
             area1.BorderColor = Color.Aqua;
             area1.BorderWidth = 1;
             chart1.Size = new System.Drawing.Size(1200, 1200);
             area1.AxisX.Minimum = 0;

             Series seriesLine = new Series();
             seriesLine.ChartType = SeriesChartType.Point;
             seriesLine.MarkerStyle = MarkerStyle.Circle;
             seriesLine.Color = Color.Blue;
             seriesLine.MarkerSize = 2;
             foreach (var n in num) {
                 seriesLine.Points.AddXY(n[0], n[1]);
             }
             // chartarea
             seriesLine.ChartArea = "Area1";

             chart1.ChartAreas.Add(area1);
             chart1.Series.Add(seriesLine);

             chart1.Width = 1200;
             chart1.Height = 1200;

             Bitmap bitmap = new Bitmap(1200, 1200);
             Graphics g = Graphics.FromImage(bitmap);
             g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

             chart1.DrawToBitmap(bitmap, chart1.ClientRectangle);
             g.DrawImage(bitmap, 0, 0, 1200, 1200);
             g.Dispose();
             string SavePath = @"D:\ImageTest2.pdf";
             using (PdfDocument Document = new PdfDocument()) {
                 PdfPage page = Document.AddPage();
                 page.Width = 1200;
                 page.Height = 1200;
                 var ximage = XImage.FromGdiPlusImage(bitmap);

                 using (XGraphics gfx = XGraphics.FromPdfPage(page)) {
                     gfx.DrawImage(ximage, 0, 0, 1200, 1200);
                 }
                 Document.Save(SavePath);

             }
             Process.Start(SavePath);

         }

         public static void DrawText(String text, XGraphics gfx, XBrush brush, int fontSize = 10, int x = 0, int y = 0) {
             //            const string facename = "Times New Roman";
             const string facename = "Helvetica";
             XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.WinAnsi, PdfFontEmbedding.Default);
             XFont fontRegular = new XFont(facename, fontSize, XFontStyle.Regular, options);
             gfx.DrawString(text, fontRegular, brush, x, y);

         }

         public static void DrawTextText(XGraphics gfx) {

             //            const string facename = "Times New Roman";
             const string facename = "Helvetica";

             //XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.Unicode, PdfFontEmbedding.Always);
             XPdfFontOptions options = new XPdfFontOptions(PdfFontEncoding.WinAnsi, PdfFontEmbedding.Default);

             XFont fontRegular = new XFont(facename, 20, XFontStyle.Regular, options);
             XFont fontBold = new XFont(facename, 20, XFontStyle.Bold, options);
             XFont fontItalic = new XFont(facename, 20, XFontStyle.Italic, options);
             XFont fontBoldItalic = new XFont(facename, 20, XFontStyle.BoldItalic, options);

             // The default alignment is baseline left (that differs from GDI+)
             gfx.DrawString("Times (regular)", fontRegular, XBrushes.DarkSlateGray, 0, 30);
             gfx.DrawString("Times (bold)", fontBold, XBrushes.DarkSlateGray, 0, 65);
             gfx.DrawString("Times (italic)", fontItalic, XBrushes.DarkSlateGray, 0, 100);
             gfx.DrawString("Times (bold italic)", fontBoldItalic, XBrushes.DarkSlateGray, 0, 135);

         }
         */
        #endregion
    }
}
