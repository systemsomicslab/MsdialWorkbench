using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfExporter
{
    public static class ExporterUtility
    {
        public enum PdfSize { A0, A1, A2, A3, A4, Square300, Square500 }
        public enum Orientation { Landscape, Portrait }

        public static PdfExporter InitialSetting(string path, PdfSize pdfSize = PdfSize.A4, Orientation orientation = Orientation.Landscape, int dpiX = 300, int dpiY = 300) {
            PdfSizeConverter(pdfSize, orientation, out var width, out var height);
            return new PdfExporter(path, width, height, 300, 300);
        }

        public static PdfExporter InitialSetting(string path, int width, int height, int dpiX = 300, int dpiY = 300) {
            return new PdfExporter(path, width, height, dpiX, dpiY);
        }

        public static void ExportSingleDrawingVisual(PdfExporter exporter, System.Windows.Media.DrawingVisual dv,
            string title, int width = 380, int height = 380, int dpiX = 300, int dpiY = 300) {
            exporter.AddPage();
            exporter.DrawTextToPage(title, 20, 25, 18);
            exporter.DrawFigureFromDrawVisual(dv, 20, 50, width, height, dpiX, dpiY);
        }

        public static void ExportDoubleDrawingVisual(PdfExporter exporter, System.Windows.Media.DrawingVisual dv, System.Windows.Media.DrawingVisual dv2,
            string title, int width = 380, int height = 380, int dpiX = 300, int dpiY = 300) {
            exporter.AddPage();
            exporter.DrawTextToPage(title, 20, 25, 18);
            exporter.DrawFigureFromDrawVisual(dv, 20, 50, width, height, dpiX, dpiY);
            exporter.DrawFigureFromDrawVisual(dv2, 20 + width/2, 50, width, height, dpiX, dpiY);
        }

        #region private methods
        private static void PdfSizeConverter(PdfSize size, Orientation orientation, out int width, out int height) {
            width = 500; height = 500;
            switch (size) {
                case PdfSize.A0: height = 3370; width = 2384; break;
                case PdfSize.A1: height = 2384; width = 1684; break;
                case PdfSize.A2: height = 1684; width = 1191; break;
                case PdfSize.A3: height = 1191; width = 842; break;
                case PdfSize.A4: height = 842; width = 595; break;
                case PdfSize.Square300: height = 300; width = 300; break;
                case PdfSize.Square500: height = 500; width = 500; break;
            }
            if (orientation == Orientation.Landscape) {
                var tmp = width;
                width = height;
                height = tmp;
            }
        }

        #endregion
    }
}