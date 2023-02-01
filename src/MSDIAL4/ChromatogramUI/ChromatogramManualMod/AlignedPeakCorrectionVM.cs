using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.RawDataHandler.Core;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Chromatogram.ManualPeakModification;

namespace Rfx.Riken.OsakaUniv.ManualPeakMod
{
    public class AlignedChromatogramModificationVM : ViewModelBase
    {
        public PeakModUC OriginalChromUC { get; set; }
        public PeakModUC AlignedChromUC { get; set; }
        public PeakModUC PickingUC { get; set; }
        public AlignmentPropertyBean AlignmentPropertyBean { get; set; }
        public AlignedDriftSpotPropertyBean AlignedDriftSpotPropertyBean { get; set; }
        public List<PeakProperty> PeakPropertyList { get; set; }
        public AnalysisParametersBean Param { get; set; }
        public AnalysisParamOfMsdialGcms GcParam { get; set; }
        public ProjectPropertyBean ProjectPropety { get; set; }
        public bool IsRI { get; set; } = false;
        public bool UpdateTrigger { get; set; }

        // LC
        public AlignedChromatogramModificationVM(AlignmentPropertyBean bean, List<PeakProperty> peakPropertyList,
            ProjectPropertyBean projectProperty, AnalysisParametersBean param) {
            AlignmentPropertyBean = bean;
            ProjectPropety = projectProperty;
            Param = param;
            PeakPropertyList = peakPropertyList;
            var dv = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Original);
            var dv2 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned);
            var dv3 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking);
            OriginalChromUC = new PeakModUC(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, PeakPropertyList);
            AlignedChromUC = new PeakModUC(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            PickingUC = new PeakModUC(this, dv3, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
        }

        //IonMobility
        public AlignedChromatogramModificationVM(AlignedDriftSpotPropertyBean bean, List<PeakProperty> peakPropertyList,
            ProjectPropertyBean projectProperty, AnalysisParametersBean param) {
            AlignedDriftSpotPropertyBean = bean;
            ProjectPropety = projectProperty;
            Param = param;
            PeakPropertyList = peakPropertyList;
            var dv = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Original);
            var dv2 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned);
            var dv3 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking);
            OriginalChromUC = new PeakModUC(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, PeakPropertyList);
            AlignedChromUC = new PeakModUC(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            PickingUC = new PeakModUC(this, dv3, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
        }


        //GC
        public AlignedChromatogramModificationVM(AlignmentPropertyBean bean, List<PeakProperty> peakPropertyList,
            ProjectPropertyBean projectProperty, AnalysisParamOfMsdialGcms param) {
            AlignmentPropertyBean = bean;
            ProjectPropety = projectProperty;
            GcParam = param;
            IsRI = GcParam.AlignmentIndexType == AlignmentIndexType.RI && bean.CentralRetentionIndex > 0;
            PeakPropertyList = peakPropertyList;
            var dv = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Original, IsRI);
            var dv2 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            var dv3 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking, IsRI);
            OriginalChromUC = new PeakModUC(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, PeakPropertyList);
            AlignedChromUC = new PeakModUC(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            PickingUC = new PeakModUC(this, dv3, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
        }

        public void UpdateAlignedChromUC() {
            var dv2 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            AlignedChromUC = new PeakModUC(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            AlignedChromUC.RefreshUI();
            OnPropertyChanged("AlignedChromUC");
        }
        public void UpdatePickingChromUC() {
            var dv = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking, IsRI);
            PickingUC = new PeakModUC(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Picking, PeakPropertyList);
            PickingUC.RefreshUI();
            OnPropertyChanged("PickingUC");
        }

        public void UpdatePeakInfo() {

            var rtList = new List<double>();
            foreach (var p in PeakPropertyList) {
                if (p.Accessory == null) return;

                if (p.PeakBean.RetentionIndex > 0) {
                    p.PeakBean.RetentionIndexLeft = p.Accessory.Chromatogram.RtLeft;
                    p.PeakBean.RetentionIndex = p.Accessory.Chromatogram.RtTop;
                    p.PeakBean.RetentionIndexRight = p.Accessory.Chromatogram.RtRight;
                }
                else if (p.PeakBean.DriftTime > 0) {
                    p.PeakBean.DriftTimeLeft = p.Accessory.Chromatogram.RtLeft;
                    p.PeakBean.DriftTime = p.Accessory.Chromatogram.RtTop;
                    p.PeakBean.DriftTimeRight = p.Accessory.Chromatogram.RtRight;
                }
                else {
                    p.PeakBean.RetentionTimeLeft = p.Accessory.Chromatogram.RtLeft;
                    p.PeakBean.RetentionTime = p.Accessory.Chromatogram.RtTop;
                    p.PeakBean.RetentionTimeRight = p.Accessory.Chromatogram.RtRight;
                }
                rtList.Add(p.Accessory.Chromatogram.RtTop);

                //p.PeakBean.PeakID = -3;
                p.PeakBean.IsManuallyModified = true;
                p.PeakBean.Area = p.PeakAreaAboveZero;
                p.PeakBean.Variable = p.PeakHeight;
                p.PeakBean.SignalToNoise = p.Accessory.Chromatogram.SignalToNoise;
            }

            if (rtList.Max() > 0) {
                PeakPropertyList[0].AverageRt = (float)rtList.Average();
            }
            if (AlignedDriftSpotPropertyBean != null) {
                AlignedDriftSpotPropertyBean.CentralDriftTime = PeakPropertyList[0].AverageRt;
                AlignedDriftSpotPropertyBean.IsManuallyModified = true;
            }
            else if (IsRI) {
                AlignmentPropertyBean.CentralRetentionIndex = PeakPropertyList[0].AverageRt;
                AlignmentPropertyBean.IsManuallyModified = true;
            }
            else {
                AlignmentPropertyBean.CentralRetentionTime = PeakPropertyList[0].AverageRt;
                AlignmentPropertyBean.IsManuallyModified = true;
            }
            OnPropertyChanged("UpdateTrigger");
        }

        public void ClearRtAlignment() {
            foreach (var p in PeakPropertyList) p.ClearAlignedPeakList();
            var dv2 = Utility.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            AlignedChromUC = new PeakModUC(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            OnPropertyChanged("AlignedChromUC");
        }
    }

    public class PeakProperty
    {
        public AlignedPeakPropertyBean PeakBean { get; set; }
        public AlignedPeakSpotInfo PeakSpotInfo { get; set; }
        public Brush Brush { get; set; }
        public List<double[]> SmoothedPeakList { get; set; }
        public List<double[]> AlignedPeakList { get; set; }
        public float AlignOffset { get; set; }
        public float AverageRt { get; set; }
        public float PeakAreaAboveZero { get; set; }
        public float PeakHeight { get; set; }
        public Accessory Accessory { get; set; }

        public PeakProperty(AlignedPeakPropertyBean bean, AlignedPeakSpotInfo info, Brush brush, List<double[]> speaks) {
            PeakBean = bean;
            PeakSpotInfo = info;
            Brush = brush;
            SmoothedPeakList = speaks;
        }

        public void SetAlignOffSet(float val) {
            AlignOffset = val;
            AlignedPeakList = new List<double[]>();
            foreach (var p in SmoothedPeakList) {
                AlignedPeakList.Add(new double[] { p[0] - AlignOffset, p[1] });
            }
        }

        public void ClearAlignedPeakList() {
            AlignedPeakList = new List<double[]>();
            foreach (var p in SmoothedPeakList) {
                AlignedPeakList.Add(new double[] { p[0], p[1] });
            }
        }

    }

    public class Utility
    {
        public static void ChangeAlignedRtProperty(List<PeakProperty> peakProperties, double minX, double maxX) {
            var rtList = new List<double>();
            if (maxX - minX < 0.01) return;
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                foreach (var p in sample.SmoothedPeakList) {
                    if (p[0] < minX) continue;
                    if (p[0] > maxX) break;
                    if (maxInt < p[1]) {
                        maxInt = p[1];
                        maxRt = p[0];
                    }
                }
                rtList.Add(maxRt);
            }
            if (rtList.Max() <= 0) return;
            var aveRt = rtList.Where(x => x > 0).Average();
            peakProperties[0].AverageRt = (float)aveRt;
            for (var i = 0; i < peakProperties.Count; i++) {
                if(rtList[i] > 0)
                    peakProperties[i].SetAlignOffSet((float)(rtList[i] - aveRt));
                else
                    peakProperties[i].SetAlignOffSet(0f);
            }
        }

        public static void ModifyPeakEdge(List<PeakProperty> peakProperties, double minX, double maxX) {
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                var sPeaklist = sample.AlignedPeakList;
                var peakAreaAboveZero = 0.0;
                for (var i = 0; i < sPeaklist.Count; i++){
                    if (sPeaklist[i][0] < minX) continue;
                    if (sPeaklist[i][0] > maxX) break;
                    if (maxInt < sPeaklist[i][1]) {
                        maxInt = sPeaklist[i][1];
                        maxRt = sPeaklist[i][0];
                    }
                    if(i + 1 < sPeaklist.Count)
                        peakAreaAboveZero += (sPeaklist[i][1] + sPeaklist[i + 1][1]) * (sPeaklist[i + 1][0] - sPeaklist[i][0]) * 0.5;
                }
                if (maxRt == 0.0) {
                    maxRt = (minX + maxX) * 0.5;
                }
                var peakHeightFromBaseline = maxInt - Math.Min(sPeaklist[0][1], sPeaklist[sPeaklist.Count - 1][1]);
                var noise = sample.PeakBean.EstimatedNoise;
                var sn = peakHeightFromBaseline / noise;

                sample.PeakAreaAboveZero = (float)peakAreaAboveZero * 60.0f;
                sample.PeakHeight = (float)maxInt;
                sample.Accessory = new Accessory();
                sample.Accessory.SetChromatogram((float)(maxRt + sample.AlignOffset), (float)(minX + sample.AlignOffset), (float)(maxX + sample.AlignOffset), 
                    noise, (float)sn);
            }        
        }

        public static DrawVisualManualPeakModification GetDrawingVisualUC(List<PeakProperty> peakProperties, PeakModType type, bool isRI = false) {
            if (peakProperties == null || peakProperties.Count == 0) return null;
            var xtitle = "Retention time (min)";
            if (isRI)
                xtitle = "Retention index";
            else if (peakProperties[0].PeakBean.DriftTime > 0)
                xtitle = "Mobility";


            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xtitle },
                AxisY = new AxisY() { AxisLabel = "Intensity" },
                LabelSpace = new LabelSpace(0, 10, 0, 0)
            };
            var title = new Title();
            if(type == PeakModType.Original)
                title.Label = "Original extracted ion chromatograms";
            else if (type == PeakModType.Aligned)
                title.Label = "Aligned chromatograms";
            else
                title.Label = "Manually modified chromatograms";
            var slist = GetChromatogramFromAlignedData(peakProperties, type, isRI);
            return new DrawVisualManualPeakModification(area, title, slist);
        }

        private static SeriesList GetChromatogramFromAlignedData(List<PeakProperty> peakProperties, PeakModType type, bool isRI = false) {
            var slist = new SeriesList();
            for (var i = 0; i < peakProperties.Count; i++) {
                var prop = peakProperties[i];
                var point = new Series() {
                    ChartType = ChartType.Chromatogram,
                    MarkerType = MarkerType.None,
                    Pen = new Pen(prop.Brush, 1.0),
                    Brush = prop.Brush
                };

                if (type == PeakModType.Original) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p[0], (float)p[1]);
                    }
                    point.Accessory = new Accessory();
                    if (isRI) {
                        point.Accessory.SetChromatogram(prop.PeakBean.RetentionIndex, prop.PeakBean.RetentionIndexLeft, 
                            prop.PeakBean.RetentionIndexRight, prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                    } 
                    else if (prop.PeakBean.DriftTime > 0) {
                        point.Accessory.SetChromatogram(prop.PeakBean.DriftTime, 
                            prop.PeakBean.DriftTimeLeft, prop.PeakBean.DriftTimeRight,
                            prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                    }
                    else {
                        point.Accessory.SetChromatogram(prop.PeakBean.RetentionTime, prop.PeakBean.RetentionTimeLeft, prop.PeakBean.RetentionTimeRight,
                            prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                    }
                }
                else if (type == PeakModType.Aligned) {
                    foreach (var p in prop.AlignedPeakList) {
                        point.AddPoint((float)p[0], (float)p[1]);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        if (isRI) {
                            point.Accessory.SetChromatogram(prop.PeakBean.RetentionIndex - prop.AlignOffset, 
                                prop.PeakBean.RetentionIndexLeft - prop.AlignOffset, 
                                prop.PeakBean.RetentionIndexRight - prop.AlignOffset,
                                prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        } 
                        else if (prop.PeakBean.DriftTime > 0) {
                            point.Accessory.SetChromatogram(prop.PeakBean.DriftTime - prop.AlignOffset,
                                prop.PeakBean.DriftTimeLeft - prop.AlignOffset,
                                prop.PeakBean.DriftTimeRight - prop.AlignOffset,
                                prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        }
                        else {
                            point.Accessory.SetChromatogram(prop.PeakBean.RetentionTime - prop.AlignOffset,
                                prop.PeakBean.RetentionTimeLeft - prop.AlignOffset,
                                prop.PeakBean.RetentionTimeRight - prop.AlignOffset,
                                prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        }
                    }
                    else {
                        point.Accessory.SetChromatogram(prop.Accessory.Chromatogram.RtTop - prop.AlignOffset, 
                            prop.Accessory.Chromatogram.RtLeft - prop.AlignOffset, 
                            prop.Accessory.Chromatogram.RtRight - prop.AlignOffset,
                            prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                    }
                }
                else if (type == PeakModType.Picking) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p[0], (float)p[1]);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        if (isRI) {
                            point.Accessory.SetChromatogram(prop.PeakBean.RetentionIndex,
                                prop.PeakBean.RetentionIndexLeft,
                                prop.PeakBean.RetentionIndexRight,
                                prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        }
                        else if (prop.PeakBean.DriftTime > 0) {
                            point.Accessory.SetChromatogram(prop.PeakBean.DriftTime,
                               prop.PeakBean.DriftTimeLeft,
                               prop.PeakBean.DriftTimeRight,
                               prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        }
                        else {
                            point.Accessory.SetChromatogram(prop.PeakBean.RetentionTime, prop.PeakBean.RetentionTimeLeft, prop.PeakBean.RetentionTimeRight, 
                                prop.PeakBean.EstimatedNoise, prop.PeakBean.SignalToNoise);
                        }
                    }
                    else
                        point.Accessory = prop.Accessory;
                }

                if (point.Points.Count > 0)
                    slist.Series.Add(point);
            }
            return slist;
        }
    }
}