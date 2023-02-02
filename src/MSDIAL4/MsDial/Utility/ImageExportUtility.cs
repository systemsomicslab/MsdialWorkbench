using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;
using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Controls;
using Riken.Metabolomics.Pathwaymap;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class ImageExportUtility
    {
        private ImageExportUtility() { }

        public static void SaveImageAsBitmap(string bitmapFormat, string exportFilePath, object target, double horizontalDpi, double verticalDpi)
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap = null;
                renderTargetBitmap = new RenderTargetBitmap((int)(((UserControl)target).ActualWidth * horizontalDpi / 96.0), (int)(((UserControl)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                renderTargetBitmap.Render((UserControl)target);

                #region old converters 
                /*
                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramWithReferenceUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramWithReferenceUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramWithReferenceUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramMrmUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramMrmUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramMrmUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramMrmUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramTicEicUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramTicEicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramTicEicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramTicEicUI)target);
                }
                else if (target.GetType() == typeof(MassSpectrogramUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotPeakViewUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotPeakViewUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotPeakViewUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotPeakViewUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotAlignmentViewUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotAlignmentViewUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotAlignmentViewUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotAlignmentViewUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramXicUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramXicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramXicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramXicUI)target);
                }
                else if (target.GetType() == typeof(MassSpectrogramLeftRotateUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramLeftRotateUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramLeftRotateUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramLeftRotateUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramAlignedEicUI)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramAlignedEicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramAlignedEicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramAlignedEicUI)target);
                }
                else if (target.GetType() == typeof(Common.BarChart.BarChartUI)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((Common.BarChart.BarChartUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((Common.BarChart.BarChartUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((Common.BarChart.BarChartUI)target);
                }
                else if (target.GetType() == typeof(ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target);
                }
                else if (target.GetType() == typeof(PathwayMapUI)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PathwayMapUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PathwayMapUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PathwayMapUI)target);
                }

                else
                    return;
                */
                #endregion

                BitmapEncoder encoder = null;


                if (bitmapFormat.Equals(".jpg"))
                {
                    encoder = new JpegBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".bmp"))
                {
                    encoder = new BmpBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".png"))
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".gif"))
                {
                    encoder = new GifBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".tiff"))
                {
                    encoder = new TiffBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

                using (FileStream fs = File.Open(exportFilePath, FileMode.Create)) { encoder.Save(fs); }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void SaveImageAsEmf(string exportFilePath, object target)
        {
            try
            {
                System.Windows.Media.Drawing drawing = Utility.GetDrawingFromXaml((UserControl)target);

                #region old converters
                /*
                 *                 if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramWithReferenceUI)target);
                else if (target.GetType() == typeof(ChromatogramMrmUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramMrmUI)target);
                else if (target.GetType() == typeof(ChromatogramTicEicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramTicEicUI)target);
                else if (target.GetType() == typeof(MassSpectrogramUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramUI)target);
                else if (target.GetType() == typeof(PairwisePlotPeakViewUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotPeakViewUI)target);
                else if (target.GetType() == typeof(PairwisePlotAlignmentViewUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotAlignmentViewUI)target);
                else if (target.GetType() == typeof(PairwisePlotUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotUI)target);
                else if (target.GetType() == typeof(ChromatogramXicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramXicUI)target);
                else if (target.GetType() == typeof(MassSpectrogramLeftRotateUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramLeftRotateUI)target);
                else if (target.GetType() == typeof(ChromatogramAlignedEicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramAlignedEicUI)target);
                else if (target.GetType() == typeof(Common.BarChart.BarChartUI))
                    drawing = Utility.GetDrawingFromXaml((Common.BarChart.BarChartUI)target);
                else if (target.GetType() == typeof(ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>))
                    drawing = Utility.GetDrawingFromXaml((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target);
                else if (target.GetType() == typeof(ChartDrawing.DefaultUC))
                    drawing = Utility.GetDrawingFromXaml((ChartDrawing.DefaultUC)target);
                else if (target.GetType() == typeof(PathwayMapUI))
                    drawing = Utility.GetDrawingFromXaml((PathwayMapUI)target);
                else
                    return;
                 */
                #endregion

                using (var stream = File.Create(exportFilePath))
                {
                    using (var graphics = Utility.CreateEmf(stream, drawing.Bounds))
                    {
                        Utility.RenderDrawingToGraphics(drawing, graphics);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //MessageBox.Show(ex.Message);
            }
        }

        public static void CopyImageAsBitmap(string bitmapFormat, object target, double horizontalDpi, double verticalDpi)
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap = null;

                renderTargetBitmap = new RenderTargetBitmap((int)(((System.Windows.Controls.UserControl)target).ActualWidth * horizontalDpi / 96.0), (int)(((System.Windows.Controls.UserControl)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                renderTargetBitmap.Render((System.Windows.Controls.UserControl)target);

                #region old converters
                /*
                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramWithReferenceUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramWithReferenceUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramWithReferenceUI)target);

                }
                else if (target.GetType() == typeof(ChromatogramMrmUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramMrmUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramMrmUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramMrmUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramTicEicUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramTicEicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramTicEicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramTicEicUI)target);
                }
                else if (target.GetType() == typeof(MassSpectrogramUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotPeakViewUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotPeakViewUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotPeakViewUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotPeakViewUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotAlignmentViewUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotAlignmentViewUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotAlignmentViewUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotAlignmentViewUI)target);
                }
                else if (target.GetType() == typeof(PairwisePlotUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((PairwisePlotUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((PairwisePlotUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((PairwisePlotUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramXicUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramXicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramXicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramXicUI)target);
                }
                else if (target.GetType() == typeof(MassSpectrogramLeftRotateUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramLeftRotateUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramLeftRotateUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramLeftRotateUI)target);
                }
                else if (target.GetType() == typeof(ChromatogramAlignedEicUI)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChromatogramAlignedEicUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChromatogramAlignedEicUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChromatogramAlignedEicUI)target);
                }
                else if (target.GetType() == typeof(Common.BarChart.BarChartUI)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((Common.BarChart.BarChartUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((Common.BarChart.BarChartUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((Common.BarChart.BarChartUI)target);
                }
                else if (target.GetType() == typeof(ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)) {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target).ActualWidth * horizontalDpi / 96.0), (int)(((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target);
                }


                else
                    return;
                    */
                #endregion


                BitmapEncoder encoder = null;


                if (bitmapFormat.Equals(".jpg"))
                {
                    encoder = new JpegBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".bmp"))
                {
                    encoder = new BmpBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".png"))
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".gif"))
                {
                    encoder = new GifBitmapEncoder();
                }
                else if (bitmapFormat.Equals(".tiff"))
                {
                    encoder = new TiffBitmapEncoder();
                }

                encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                Bitmap gdiBitmap;
                using (MemoryStream ms = new MemoryStream()) { encoder.Save(ms); gdiBitmap = new Bitmap(ms); }

                var dataObj = new DataObject();
                dataObj.SetData(DataFormats.Bitmap, gdiBitmap);
                Clipboard.SetDataObject(dataObj, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void CopyImageAsEmf(string exportFilePath, object target, Window clipBoardOwnerwindow)
        {
            try
            {
                System.Windows.Media.Drawing drawing = null;

                drawing = Utility.GetDrawingFromXaml((MassSpectrogramWithReferenceUI)target);

                #region old converters
      /*          if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramWithReferenceUI)target);
                else if (target.GetType() == typeof(ChromatogramMrmUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramMrmUI)target);
                else if (target.GetType() == typeof(ChromatogramTicEicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramTicEicUI)target);
                else if (target.GetType() == typeof(MassSpectrogramUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramUI)target);
                else if (target.GetType() == typeof(PairwisePlotPeakViewUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotPeakViewUI)target);
                else if (target.GetType() == typeof(PairwisePlotAlignmentViewUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotAlignmentViewUI)target);
                else if (target.GetType() == typeof(PairwisePlotUI))
                    drawing = Utility.GetDrawingFromXaml((PairwisePlotUI)target);
                else if (target.GetType() == typeof(ChromatogramXicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramXicUI)target);
                else if (target.GetType() == typeof(MassSpectrogramLeftRotateUI))
                    drawing = Utility.GetDrawingFromXaml((MassSpectrogramLeftRotateUI)target);
                else if (target.GetType() == typeof(ChromatogramAlignedEicUI))
                    drawing = Utility.GetDrawingFromXaml((ChromatogramAlignedEicUI)target);
                else if (target.GetType() == typeof(Common.BarChart.BarChartUI))
                    drawing = Utility.GetDrawingFromXaml((Common.BarChart.BarChartUI)target);
                else if (target.GetType() == typeof(ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>))
                    drawing = Utility.GetDrawingFromXaml((ChartDrawing.DefaultUC<ChartDrawing.DrawVisual>)target);
                else
                    return;
                    */
                #endregion


                MemoryStream wmfStream = new MemoryStream();
                using (var graphics = Utility.CreateEmf(wmfStream, drawing.Bounds))
                    Utility.RenderDrawingToGraphics(drawing, graphics);
                wmfStream.Position = 0;
                System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(wmfStream);
                IntPtr hEMF, hEMF2;
                hEMF = metafile.GetHenhmetafile(); // invalidates mf

                if (!hEMF.Equals(new IntPtr(0)))
                {
                    hEMF2 = ExtensionMethods.CopyEnhMetaFile(hEMF, new IntPtr(0));
                    if (!hEMF2.Equals(new IntPtr(0)))
                    {
                        if (ExtensionMethods.OpenClipboard(((IWin32Window)clipBoardOwnerwindow.OwnerAsWin32()).Handle))
                        {
                            if (ExtensionMethods.EmptyClipboard())
                            {
                                ExtensionMethods.SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                                ExtensionMethods.CloseClipboard();
                            }
                        }
                    }
                    ExtensionMethods.DeleteEnhMetaFile(hEMF);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
