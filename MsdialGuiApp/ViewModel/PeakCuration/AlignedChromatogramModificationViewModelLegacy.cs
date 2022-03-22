using CompMs.App.Msdial.View.PeakCuration;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.PeakCuration {
    public class AlignedChromatogramModificationViewModelLegacy : ViewModelBase {
        public PeakModUCLegacy OriginalChromUC { get; set; }
        public PeakModUCLegacy AlignedChromUC { get; set; }
        public PeakModUCLegacy PickingUC { get; set; }
        public AlignmentSpotProperty AlignmentPropertyBean { get; set; }
        public List<PeakPropertyLegacy> PeakPropertyList { get; set; }
        public ParameterBase Param { get; set; }
        public bool IsRI { get; set; } = false;
        public bool IsDrift { get; set; } = false;
        public bool UpdateTrigger { get; set; }

        // LC
        public AlignedChromatogramModificationViewModelLegacy(
            AlignmentSpotProperty bean,
            List<PeakPropertyLegacy> peakPropertyList, 
            ParameterBase param, 
            bool isRI = false) {
            AlignmentPropertyBean = bean;
            Param = param;
            PeakPropertyList = peakPropertyList;
            IsRI = isRI;
            IsDrift = bean.TimesCenter.Type == ChromXType.Drift ? true : false;
            var dv = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Original);
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned);
            var dv3 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking);
            OriginalChromUC = new PeakModUCLegacy(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, PeakPropertyList);
            AlignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            PickingUC = new PeakModUCLegacy(this, dv3, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
        }

        public void UpdateAlignedChromUC() {
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            AlignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            AlignedChromUC.RefreshUI();
            OnPropertyChanged("AlignedChromUC");
        }
        public void UpdatePickingChromUC() {
            var dv = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Picking, IsRI);
            PickingUC = new PeakModUCLegacy(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Picking, PeakPropertyList);
            PickingUC.RefreshUI();
            OnPropertyChanged("PickingUC");
        }

        public void UpdatePeakInfo() {

            var rtList = new List<double>();
            foreach (var p in PeakPropertyList) {
                if (p.Accessory == null) return;
                var chromtype = ChromXType.RT;
                if (IsDrift) chromtype = ChromXType.Drift;
                if (IsRI) chromtype = ChromXType.RI;

                var chromunit = ChromXUnit.Min;
                if (IsDrift) chromunit = ChromXUnit.Msec;
                if (IsRI) chromunit = ChromXUnit.None;

                p.PeakBean.ChromXsLeft = new ChromXs(p.Accessory.Chromatogram.RtLeft, chromtype, chromunit);
                p.PeakBean.ChromXsTop = new ChromXs(p.Accessory.Chromatogram.RtTop, chromtype, chromunit);
                p.PeakBean.ChromXsRight = new ChromXs(p.Accessory.Chromatogram.RtRight, chromtype, chromunit);

                rtList.Add(p.Accessory.Chromatogram.RtTop);

                //p.PeakBean.PeakID = -3;
                p.PeakBean.IsManuallyModifiedForQuant = true;
                p.PeakBean.PeakAreaAboveZero = p.PeakAreaAboveZero;
                p.PeakBean.PeakHeightTop = p.PeakHeight;
                p.PeakBean.PeakShape.SignalToNoise = p.Accessory.Chromatogram.SignalToNoise;
            }

            if (rtList.Max() > 0) {
                PeakPropertyList[0].AverageRt = (float)rtList.Average();
            }
            
            if (IsDrift) {
                AlignmentPropertyBean.TimesCenter = new ChromXs(new DriftTime(PeakPropertyList[0].AverageRt));
                AlignmentPropertyBean.IsManuallyModifiedForQuant = true;
            }
            else if (IsRI) {
                AlignmentPropertyBean.TimesCenter = new ChromXs(new RetentionIndex(PeakPropertyList[0].AverageRt));
                AlignmentPropertyBean.IsManuallyModifiedForQuant = true;
            }
            else {
                AlignmentPropertyBean.TimesCenter = new ChromXs(new RetentionTime(PeakPropertyList[0].AverageRt));
                AlignmentPropertyBean.IsManuallyModifiedForQuant = true;
            }
            OnPropertyChanged("UpdateTrigger");
        }

        public void ClearRtAlignment() {
            foreach (var p in PeakPropertyList) p.ClearAlignedPeakList();
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList, PeakModType.Aligned, IsRI);
            AlignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList);
            OnPropertyChanged("AlignedChromUC");
        }
    }

    public class PeakPropertyLegacy {
        public AlignmentChromPeakFeature PeakBean { get; set; }
        public ChromatogramPeakInfo PeakSpotInfo { get; set; }
        public Brush Brush { get; set; }
        public List<ChromatogramPeak> SmoothedPeakList { get; set; }
        public List<ChromatogramPeak> AlignedPeakList { get; set; }
        public float AlignOffset { get; set; }
        public float AverageRt { get; set; }
        public float PeakAreaAboveZero { get; set; }
        public float PeakHeight { get; set; }
        public Accessory Accessory { get; set; }

        public PeakPropertyLegacy(AlignmentChromPeakFeature bean, ChromatogramPeakInfo info, Brush brush, List<ChromatogramPeak> speaks) {
            PeakBean = bean;
            PeakSpotInfo = info;
            Brush = brush;
            SmoothedPeakList = speaks;
        }

        public void SetAlignOffSet(float val) {
            AlignOffset = val;
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                var nChromXs = new ChromXs(p.ChromXs.Value - AlignOffset, p.ChromXs.Type, p.ChromXs.Unit);
                AlignedPeakList.Add(new ChromatogramPeak() { ChromXs = nChromXs, Intensity = p.Intensity });
            }
        }

        public void ClearAlignedPeakList() {
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                AlignedPeakList.Add(new ChromatogramPeak { ChromXs = p.ChromXs, Intensity = p.Intensity });
            }
        }

    }

    public class UtilityLegacy {
        public static void ChangeAlignedRtProperty(List<PeakPropertyLegacy> peakProperties, double minX, double maxX) {
            var rtList = new List<double>();
            if (maxX - minX < 0.01) return;
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                foreach (var p in sample.SmoothedPeakList) {
                    if (p.ChromXs.Value < minX) continue;
                    if (p.ChromXs.Value > maxX) break;
                    if (maxInt < p.Intensity) {
                        maxInt = p.Intensity;
                        maxRt = p.ChromXs.Value;
                    }
                }
                rtList.Add(maxRt);
            }
            if (rtList.Max() <= 0) return;
            var aveRt = rtList.Where(x => x > 0).Average();
            peakProperties[0].AverageRt = (float)aveRt;
            for (var i = 0; i < peakProperties.Count; i++) {
                if (rtList[i] > 0)
                    peakProperties[i].SetAlignOffSet((float)(rtList[i] - aveRt));
                else
                    peakProperties[i].SetAlignOffSet(0f);
            }
        }

        public static void ModifyPeakEdge(List<PeakPropertyLegacy> peakProperties, double minX, double maxX) {
            foreach (var sample in peakProperties) {
                var maxInt = 0.0;
                var maxRt = 0.0;
                var sPeaklist = sample.AlignedPeakList;
                var peakAreaAboveZero = 0.0;
                for (var i = 0; i < sPeaklist.Count; i++) {
                    if (sPeaklist[i].ChromXs.Value < minX) continue;
                    if (sPeaklist[i].ChromXs.Value > maxX) break;
                    if (maxInt < sPeaklist[i].Intensity) {
                        maxInt = sPeaklist[i].Intensity;
                        maxRt = sPeaklist[i].ChromXs.Value;
                    }
                    if (i + 1 < sPeaklist.Count)
                        peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].ChromXs.Value - sPeaklist[i].ChromXs.Value) * 0.5;
                }
                if (maxRt == 0.0) {
                    maxRt = (minX + maxX) * 0.5;
                }
                var peakHeightFromBaseline = maxInt - Math.Min(sPeaklist[0].Intensity, sPeaklist[sPeaklist.Count - 1].Intensity);
                var noise = sample.PeakBean.PeakShape.EstimatedNoise;
                var sn = peakHeightFromBaseline / noise;

                sample.PeakAreaAboveZero = (float)peakAreaAboveZero * 60.0f;
                sample.PeakHeight = (float)maxInt;
                sample.Accessory = new Accessory();
                sample.Accessory.SetChromatogram((float)(maxRt + sample.AlignOffset), (float)(minX + sample.AlignOffset), (float)(maxX + sample.AlignOffset),
                    noise, (float)sn);
            }
        }

        public static DrawVisualManualPeakModification GetDrawingVisualUC(List<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false, bool isDrift = false) {
            if (peakProperties == null || peakProperties.Count == 0) return null;
            var xtitle = "Retention time (min)";
            if (isRI)
                xtitle = "Retention index";
            else if (isDrift)
                xtitle = "Mobility";


            var area = new Area() {
                AxisX = new AxisX() { AxisLabel = xtitle },
                AxisY = new AxisY() { AxisLabel = "Intensity" },
                LabelSpace = new LabelSpace(0, 10, 0, 0)
            };
            var title = new Title();
            if (type == PeakModType.Original)
                title.Label = "Original extracted ion chromatograms";
            else if (type == PeakModType.Aligned)
                title.Label = "Aligned chromatograms";
            else
                title.Label = "Manually modified chromatograms";
            var slist = GetChromatogramFromAlignedData(peakProperties, type, isRI);
            return new DrawVisualManualPeakModification(area, title, slist);
        }

        private static SeriesList GetChromatogramFromAlignedData(List<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false) {
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
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    point.Accessory.SetChromatogram(
                        (float)prop.PeakBean.ChromXsTop.Value, 
                        (float)prop.PeakBean.ChromXsLeft.Value, 
                        (float)prop.PeakBean.ChromXsRight.Value,
                        prop.PeakBean.PeakShape.EstimatedNoise, 
                        prop.PeakBean.PeakShape.SignalToNoise);
                }
                else if (type == PeakModType.Aligned) {
                    foreach (var p in prop.AlignedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.PeakBean.ChromXsTop.Value - prop.AlignOffset,
                            (float)prop.PeakBean.ChromXsLeft.Value - prop.AlignOffset,
                            (float)prop.PeakBean.ChromXsRight.Value - prop.AlignOffset,
                            prop.PeakBean.PeakShape.EstimatedNoise,
                            prop.PeakBean.PeakShape.SignalToNoise);
                    }
                    else {
                        point.Accessory.SetChromatogram(prop.Accessory.Chromatogram.RtTop - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtLeft - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtRight - prop.AlignOffset,
                            prop.PeakBean.PeakShape.EstimatedNoise,
                            prop.PeakBean.PeakShape.SignalToNoise);
                    }
                }
                else if (type == PeakModType.Picking) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.PeakBean.ChromXsTop.Value,
                            (float)prop.PeakBean.ChromXsLeft.Value,
                            (float)prop.PeakBean.ChromXsRight.Value,
                            prop.PeakBean.PeakShape.EstimatedNoise,
                            prop.PeakBean.PeakShape.SignalToNoise);
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
