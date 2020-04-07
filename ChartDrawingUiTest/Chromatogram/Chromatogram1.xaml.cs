using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Diagnostics;

using Rfx.Riken.OsakaUniv;
using Rfx.Riken.OsakaUniv.ManualPeakMod;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Chromatogram.ManualPeakModification;


namespace ChartDrawingUiTest.Chromatogram
{
    /// <summary>
    /// Chromatogram1.xaml の相互作用ロジック
    /// </summary>
    public partial class Chromatogram1 : Page
    {
        public ChromatogramPageVM VM { get; set; }
        public Chromatogram1()
        {
            InitializeComponent();
            loadChromatogram(@"C:\Users\ADMIN\Dropbox\testchrom.txt");
        }

        private void loadChromatogram(string path) {
            var peaklist = new List<double[]>();
            //read peak list
            using (var sr = new StreamReader(path, Encoding.ASCII)) {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == null || line == string.Empty) continue;
                    var linearray = line.Split('\t');
                    double rt, intensity;
                    if (linearray.Length > 3 && double.TryParse(linearray[1], out rt) && double.TryParse(linearray[3], out intensity)) {
                        peaklist.Add(new double[] { 0, rt, 0, intensity });
                    }
                }
            }

            //just test, no bug estimation, hypothesize the peaks must be detected.
            var param = new AnalysisParametersBean();
            //var detectedPeaks = PeakDetection.GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(
            //    param.MinimumDatapoints, param.MinimumAmplitude, param.AmplitudeNoiseFactor, param.SlopeNoiseFactor, param.PeaktopNoiseFactor, peaklist);

            var minDatapoints = param.MinimumDatapoints;
            var minAmps = param.MinimumAmplitude;
            var detectedPeaks = PeakDetection.PeakDetectionVS1(minDatapoints, minAmps, peaklist);
            var maxPeak = detectedPeaks.OrderByDescending(n => n.IntensityAtPeakTop).ToList()[0];

            //to use chartdrawing
            var drawVisual = getChromatogramDrawingVisual(peaklist, maxPeak.RtAtPeakTop, maxPeak.RtAtLeftPeakEdge, maxPeak.RtAtRightPeakEdge);
            drawVisual.PropertyChanged += DrawVisual_PropertyChanged;
            drawVisual.CalculateChromPeakProperties(drawVisual.SeriesList.Series[0]);

            var peakModUC = new PeakModUC(drawVisual, true);
            this.VM = new ChromatogramPageVM() { ChromUC = peakModUC };
            this.DataContext = VM;
        }

        // reflect if the peak property is changed
        private void DrawVisual_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            
            if (e.PropertyName == "IsPeakPropertyChanged") {

                var dv = (DrawVisualManualPeakModification)sender;
                if (dv != null) {
                    var prop = dv.ChromPeakProperty;
                    if (prop != null) {

                        this.Label_AreaFromBaseline.Content = "Area from baseline: " + Math.Round(prop.AreaFromBaseline, 0);
                        this.Label_AreaFromParallelBaseline.Content = "Area from parallel baseline: " + Math.Round(prop.AreaFromParallelBaseline, 0);
                        this.Label_AreaFromZero.Content = "Area from zero: " + Math.Round(prop.AreaFromZero, 0);

                        this.Label_HeightFromBaseline.Content = "Height from baseline: " + Math.Round(prop.HeightFromBaseline, 0);
                        this.Label_HeightFromParallelBaseline.Content = "Height from parallel baseline: " + Math.Round(prop.HeightFromParallelBaseline, 0);
                        this.Label_HeightFromZero.Content = "Height from zero: " + Math.Round(prop.HeightFromZero, 0);

                        this.Label_HeightLeftFromZero.Content = "Height left from zero: " + Math.Round(prop.HeightLeftFromZero, 0);
                        this.Label_HeightMinLeftFromZero.Content = "Height min left from zero: " + Math.Round(prop.HeightMinLeftFromZero, 0);

                        this.Label_HeightMinRightFromZero.Content = "Height min right from zero: " + Math.Round(prop.HeightMinRightFromZero, 0);
                        this.Label_HeightRightFromZero.Content = "Height right from zero: " + Math.Round(prop.HeightRightFromZero, 0);

                        this.Label_RtLeft.Content = "RT left: " + Math.Round(prop.RtLeft, 3);
                        this.Label_RtTop.Content = "RT top: " + Math.Round(prop.RtTop, 3);
                        this.Label_RtRight.Content = "RT right: " + Math.Round(prop.RtRight, 3);

                        this.Label_RtMinLeft.Content = "RT min left: " + Math.Round(prop.RtMinLeft, 3);
                        this.Label_RtMinRight.Content = "RT min right: " + Math.Round(prop.RtMinRight, 3);

                        this.Label_ScanLeft.Content = "Scan left: " + prop.ScanLeft;
                        this.Label_ScanTop.Content = "Scan top: " + prop.ScanTop;
                        this.Label_ScanRight.Content = "Scan right: " + prop.ScanRight;

                        this.Label_ScanMinLeft.Content = "Scan min left: " + prop.ScanMinLeft;
                        this.Label_ScanMinRight.Content = "Scan min right: " + prop.ScanMinRight;

                    }
                }
            }
        }

        private DrawVisualManualPeakModification getChromatogramDrawingVisual(List<double[]> peaklist, float rtTop, float rtLeft, float rtRight) {
            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = "Retention time (min)" },
                AxisY = new AxisY() { AxisLabel = "Intensity" },
                LabelSpace = new LabelSpace(0, 10, 0, 0)
            };
            var title = new Title() { Label = "Extracted ion chromatogram" };

            var slist = new SeriesList();
            var point = new Series() {
                ChartType = ChartType.Chromatogram,
                MarkerType = MarkerType.None,
                XaxisUnit = XaxisUnit.Minuites,
                MarkerSize = new Size(7,7),
                Pen = new Pen(Brushes.Blue, 1.0),
                Brush = Brushes.Blue
            };

            foreach (var p in peaklist) {
                point.AddPoint((float)p[1], (float)p[3]);
                Debug.WriteLine("X {0}, Y {1}", (float)p[1], (float)p[3]);
            }
            point.Accessory = new Accessory();
            point.Accessory.SetChromatogram(rtTop, rtLeft, rtRight); 
            slist.Series.Add(point);

            return new DrawVisualManualPeakModification(area, title, slist);
        }
    }

    public class ChromatogramPageVM : ViewModelBase {
        public PeakModUC ChromUC { get; set; }
    }
}
