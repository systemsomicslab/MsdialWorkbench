using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramWithReferenceUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramWithReferenceUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramWithReferenceUI)target);

                }
                else if (target.GetType() == typeof(MassSpectrogramUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramUI)target);
                }
                else
                    return;

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
                System.Windows.Media.Drawing drawing = null;

                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                    drawing = BCDev.XamlToys.Utility.GetDrawingFromXaml((MassSpectrogramWithReferenceUI)target);
                else if (target.GetType() == typeof(MassSpectrogramUI))
                    drawing = BCDev.XamlToys.Utility.GetDrawingFromXaml((MassSpectrogramUI)target);
                else
                    return;

                using (var stream = File.Create(exportFilePath))
                {
                    using (var graphics = BCDev.XamlToys.Utility.CreateEmf(stream, drawing.Bounds))
                    {
                        BCDev.XamlToys.Utility.RenderDrawingToGraphics(drawing, graphics);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void CopyImageAsBitmap(string bitmapFormat, object target, double horizontalDpi, double verticalDpi)
        {
            try
            {
                RenderTargetBitmap renderTargetBitmap = null;

                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramWithReferenceUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramWithReferenceUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramWithReferenceUI)target);

                }
                else if (target.GetType() == typeof(MassSpectrogramUI))
                {
                    renderTargetBitmap = new RenderTargetBitmap((int)(((MassSpectrogramUI)target).ActualWidth * horizontalDpi / 96.0), (int)(((MassSpectrogramUI)target).ActualHeight * verticalDpi / 96.0), horizontalDpi, verticalDpi, PixelFormats.Pbgra32);
                    renderTargetBitmap.Render((MassSpectrogramUI)target);
                }
                else
                    return;

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

                if (target.GetType() == typeof(MassSpectrogramWithReferenceUI))
                    drawing = BCDev.XamlToys.Utility.GetDrawingFromXaml((MassSpectrogramWithReferenceUI)target);
                else if (target.GetType() == typeof(MassSpectrogramUI))
                    drawing = BCDev.XamlToys.Utility.GetDrawingFromXaml((MassSpectrogramUI)target);
                else
                    return;

                MemoryStream wmfStream = new MemoryStream();
                using (var graphics = BCDev.XamlToys.Utility.CreateEmf(wmfStream, drawing.Bounds))
                    BCDev.XamlToys.Utility.RenderDrawingToGraphics(drawing, graphics);
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

        public static void particular_settings_tada(object target, RawData query) {
            var date = DateTime.Now;
            var dateString = date.Year.ToString() + date.Month.ToString() + date.Day.ToString() + date.Hour.ToString() + date.Minute.ToString() + date.Second.ToString();

            //var export_dir = mainWindow.ProjectProperty.ProjectFolderPath + "\\" + "Exported_Figures";
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a save folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var export_dir = string.Empty;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                export_dir = fbd.SelectedPath;
            }
            if (export_dir == string.Empty) return;
            //var export_dir = mainWindow.ProjectProperty.ProjectFolderPath + "\\" + "Exported_Figures";
            if (!System.IO.Directory.Exists(export_dir))
                System.IO.Directory.CreateDirectory(export_dir);

            #region mainWindow deconvoluted MS/MS spectrum
            if (target.GetType() == typeof(MassSpectrogramUI)) {
                var massSpectrogramUI = (MassSpectrogramUI)((MassSpectrogramUI)target).Content;
                MassSpectrogramViewModel massSpectrogramViewModel = massSpectrogramUI.MassSpectrogramViewModel;
                var mesSpec = GetMeasuredPeakList_fromMassSpectrogramViewModel(massSpectrogramViewModel);
                if (mesSpec == null) return;

                // original
                // var dv = CreateDrawVisual_massSpectrum(mesSpec);

                // if you want to adjust the window size and min and max value to MainWindow
                var dv = CreateDrawVisual_massSpectrum(mesSpec, (float)massSpectrogramUI.ActualWidth, (float)massSpectrogramUI.ActualHeight);
                dv.MaxX = massSpectrogramViewModel.DisplayRangeMassMax ?? dv.MaxX;
                dv.MinX = massSpectrogramViewModel.DisplayRangeMassMin ?? dv.MinX;
                dv.MaxY = massSpectrogramViewModel.DisplayRangeIntensityMax ?? dv.MaxY;
                dv.MinY = massSpectrogramViewModel.DisplayRangeIntensityMin ?? dv.MinY;

                //var file_name = analysisFile.AnalysisFilePropertyBean.AnalysisFileId + "_" +
                //        analysisFile.AnalysisFilePropertyBean.AnalysisFileName + "_Peak" + mainWindow.FocusedPeakID.ToString("0000");

           
                var file_name = query.Name + "_" + query.PrecursorMz + "_" + dateString;
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var converted = string.Concat(
                          file_name.Select(c => invalidChars.Contains(c) ? '_' : c));
                SaveChartAsPng(dv, Path.Combine(export_dir, converted + ".png"));
                SaveChartAsEmf(dv, Path.Combine(export_dir, converted + ".emf"));
            }
            #endregion

            #region mainWindow reverse mass spectrum of each sample
            else if (target.GetType() == typeof(MassSpectrogramWithReferenceUI)) {
                var massSpectrogramUI = (MassSpectrogramWithReferenceUI)((MassSpectrogramWithReferenceUI)target).Content;
                MassSpectrogramViewModel massSpectrogramViewModel = massSpectrogramUI.MassSpectrogramViewModel;
                var mesSpec = GetMeasuredPeakList_fromMassSpectrogramViewModel(massSpectrogramViewModel);
                var refSpec = GetReferencePeakList_fromMassSpectrogramViewModel(massSpectrogramViewModel);
                // original
                // var dv = CreateDrawVisual_massSpectrumWithRef(mesSpec, refSpec);

                // if you want to adjust the window size and min and max value to MainWindow
                var dv = CreateDrawVisual_massSpectrumWithRef(mesSpec, refSpec, (float)massSpectrogramUI.ActualWidth, (float)massSpectrogramUI.ActualHeight);
                dv.MaxX = massSpectrogramViewModel.DisplayRangeMassMax ?? dv.MaxX;
                dv.MinX = massSpectrogramViewModel.DisplayRangeMassMin ?? dv.MinX;
                //dv.MaxY = massSpectrogramViewModel.DisplayRangeIntensityMax ?? dv.MaxY;
                //dv.MinY = massSpectrogramViewModel.DisplayRangeIntensityMin ?? dv.MinY;

                // var file_name = analysisFile.AnalysisFilePropertyBean.AnalysisFileId + "_" +
                //        analysisFile.AnalysisFilePropertyBean.AnalysisFileName + "_Peak" + mainWindow.FocusedPeakID.ToString("0000") + "_withRef";
                var file_name = "File " + query.Name + "-" + dateString;
                var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                var converted = string.Concat(
                  file_name.Select(c => invalidChars.Contains(c) ? '_' : c));
                SaveChartAsPng(dv, Path.Combine(export_dir, converted + ".png"));
                SaveChartAsEmf(dv, Path.Combine(export_dir, converted + ".emf"));
            }
            #endregion
        }

        public static List<float[]> Convert2RelativeIntensity(List<float[]> ms2list) {
            var maxInt = ms2list.Max(x => x[1]);
            var maxIntRate = 100.0f / maxInt;
            var newList = new List<float[]>();
            foreach (var ms in ms2list) {
                newList.Add(new float[] { ms[0], ms[1] * maxIntRate });
            }
            return newList;
        }

        // msList    = new List<double[]>() { new double[] { m/z, Intensity } };
        public static DrawVisual CreateDrawVisual_massSpectrum(List<float[]> msList, float width = 1000f, float height = 500f) {
            var area = new Area() {
                Height = height,
                Width = width,
                LabelSpace = new LabelSpace() { Top = 25, Bottom = 0 },
                Margin = new Margin(60, 0, 20, 40)
            };
            area.AxisX.IsItalicLabel = true;
            area.AxisX.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 1.5);
            area.AxisY.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 1.5);
            area.AxisX.FontSize = 17;
            area.AxisY.FontSize = 14;
            area.AxisX.AxisLabel = "m/z";
            area.AxisY.AxisLabel = "Intensity";
            area.AxisY.MinorScaleEnabled = false;
            area.AxisX.MinorScaleEnabled = false;
            //            area.AxisX.FontType = new Typeface("Arial");
            //            area.AxisY.FontType = new Typeface("Arial");

            var list = GetMSMS(msList);
            var title = new Title() { Label = "" };
            var drawing = new DrawVisual(area, title, list, true);
            var diff = (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * 0.1f;
            drawing.MaxX += diff;
            drawing.MinX -= diff;
            drawing.MinY = 0f;
            drawing.Initialize();
            return drawing;
        }

        // msList    = new List<double[]>() { new double[] { m/z, Intensity } };
        // msListRef = new List<double[]>() { new double[] { m/z, Intensity } };
        public static DrawVisual CreateDrawVisual_massSpectrumWithRef(List<float[]> msList, List<float[]> msListRef, float width = 1000f, float height = 500f) {
            var area = new Area() {
                Height = height,
                Width = width,
                LabelSpace = new LabelSpace() { Top = 25, Bottom = 25 },
                Margin = new Margin(60, 10, 20, 40)
            };
            area.AxisX.IsItalicLabel = true;
            area.AxisX.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 1.5);
            area.AxisY.Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 1.5);
            area.AxisX.FontSize = 17;
            area.AxisY.FontSize = 17;
            area.AxisX.AxisLabel = "m/z";
            area.AxisY.AxisLabel = "Intensity";
            area.AxisY.MinorScaleEnabled = false;
            area.AxisX.MinorScaleEnabled = false;
            area.AxisX.FontType = new Typeface("Arial");
            area.AxisY.FontType = new Typeface("Arial");

            var list = GetMSMSwithRef(Convert2RelativeIntensity(msList), Convert2RelativeIntensity(msListRef));
            var title = new Title() { Label = "" };
            var drawing = new DrawVisual(area, title, list, true);
            var diff = (drawing.SeriesList.MaxX - drawing.SeriesList.MinX) * 0.1f;
            drawing.MaxX += diff;
            drawing.MinX -= diff;
            drawing.MaxY = 100f;
            drawing.MinY = 0f;
            drawing.Initialize();
            return drawing;
        }

        public static void SaveChartAsEmf(DrawVisual drawing, string filePath) {
            var dv = drawing.GetChart();
            drawing.SaveDrawingAsEmf(dv, filePath);
        }
        public static void SaveChartAsPng(DrawVisual drawing, string filePath, int dpiX = 300, int dpiY = 300) {
            var height = drawing.Area.Height;
            var width = drawing.Area.Width;
            var dv = drawing.GetChart();
            drawing.SaveChart(dv, filePath, (int)width, (int)height, dpiX, dpiY);
        }
        // msList    = new List<double[]>() { new double[] { m/z, Intensity } };
        public static SeriesList GetMSMS(List<float[]> msList) {
            var slist = new SeriesList();
            var s = new Series() { ChartType = ChartType.MS, MarkerType = MarkerType.None, Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2), FontType = new Typeface("Arial") };
            var x1 = msList.Select(x => (float)(x[0])).ToArray();
            var y1 = msList.Select(x => (float)(x[1])).ToArray();
            for (var i = 0; i < x1.Length; i++) {
                s.AddPoint(x1[i], y1[i], x1[i].ToString("0.000"));
            }
            s.IsLabelVisible = true;
            slist.Series.Add(s);
            return slist;
        }

        // msList    = new List<double[]>() { new double[] { m/z, Intensity } };
        // msListRef = new List<double[]>() { new double[] { m/z, Intensity } };
        public static SeriesList GetMSMSwithRef(List<float[]> msList, List<float[]> msListRef) {
            var slist = new SeriesList();
            var s = new Series() { ChartType = ChartType.MSwithRef, MarkerType = MarkerType.None, Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Black, 2), FontType = new Typeface("Arial") };
            var s2 = new Series() { ChartType = ChartType.MSwithRef, MarkerType = MarkerType.None, Pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Red, 2), Brush = System.Windows.Media.Brushes.Red, FontType = new Typeface("Arial") };
            var x1 = msList.Select(x => (float)(x[0])).ToArray();
            var y1 = msList.Select(x => (float)(x[1])).ToArray();
            var x2 = msListRef.Select(x => (float)(x[0])).ToArray();
            var y2 = msListRef.Select(x => (float)(x[1])).ToArray();
            for (var i = 0; i < x1.Length; i++) {
                s.AddPoint(x1[i], y1[i], x1[i].ToString("0.000"));
            }
            for (var i = 0; i < x2.Length; i++) {
                s2.AddPoint(x2[i], y2[i], x2[i].ToString("0.000"));
            }
            s.IsLabelVisible = true;
            s2.IsLabelVisible = true;
            slist.Series.Add(s);
            slist.Series.Add(s2);
            return slist;
        }

        public static List<float[]> GetPeakList_fromMSP(List<MspFormatCompoundInformationBean> msps, int id) {
            var msList = new List<float[]>();
            if (id < 0 || id >= msps.Count) {
                msList.Add(new float[] { 0.0f, 0.0f });
            }
            else {
                var msp = msps[id];
                for (int i = 0; i < msp.MzIntensityCommentBeanList.Count; i++) {
                    msList.Add(new float[] { (float)msp.MzIntensityCommentBeanList[i].Mz, (float)msp.MzIntensityCommentBeanList[i].Intensity });
                }
            }
            return msList;
        }

        public static List<float[]> GetMeasuredPeakList_fromMassSpectrogramViewModel(MassSpectrogramViewModel massSpectrogramViewModel) {
            var msList = new List<float[]>();
            if (massSpectrogramViewModel == null || massSpectrogramViewModel.MeasuredMassSpectrogramBean == null ||
                massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null ||
                massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return null;
            for (int i = 0; i < massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count; i++) {
                msList.Add(new float[] { (float)massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection[i][0], (float)massSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection[i][1] });
            }
            return msList;
        }

        public static List<float[]> GetReferencePeakList_fromMassSpectrogramViewModel(MassSpectrogramViewModel massSpectrogramViewModel) {
            var msList = new List<float[]>();
            if (massSpectrogramViewModel == null || massSpectrogramViewModel.ReferenceMassSpectrogramBean == null ||
                massSpectrogramViewModel.ReferenceMassSpectrogramBean.MassSpectraCollection == null ||
                massSpectrogramViewModel.ReferenceMassSpectrogramBean.MassSpectraCollection.Count == 0) {
                msList.Add(new float[] { 0f, 0f });
                return msList;
            }
            for (int i = 0; i < massSpectrogramViewModel.ReferenceMassSpectrogramBean.MassSpectraCollection.Count; i++) {
                msList.Add(new float[] { (float)massSpectrogramViewModel.ReferenceMassSpectrogramBean.MassSpectraCollection[i][0], (float)massSpectrogramViewModel.ReferenceMassSpectrogramBean.MassSpectraCollection[i][1] });
            }
            return msList;

        }



    }
}
