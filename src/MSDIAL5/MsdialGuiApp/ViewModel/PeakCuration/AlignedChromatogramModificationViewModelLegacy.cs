using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Chromatogram.ManualPeakModification;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.PeakCuration
{
    internal sealed class AlignedChromatogramModificationViewModelLegacy : ViewModelBase {
        private readonly AlignedChromatogramModificationModelLegacy _model;

        public PeakModUCLegacy? OriginalChromUC {
            get => _originalChromUC;
            set => SetProperty(ref _originalChromUC, value);
        }
        private PeakModUCLegacy? _originalChromUC;
        public PeakModUCLegacy? AlignedChromUC {
            get => _alignedChromUC;
            set => SetProperty(ref _alignedChromUC, value);
        }
        private PeakModUCLegacy? _alignedChromUC;
        public PeakModUCLegacy? PickingUC {
            get => _pickingUC;
            set => SetProperty(ref _pickingUC, value);
        }
        private PeakModUCLegacy? _pickingUC;

        public ReadOnlyReactivePropertySlim<PeakPropertyLegacy[]> PeakPropertyList { get; }
        public bool IsRI => _model.IsRI.Value;

        public DelegateCommand UpdateChromsCommand => _updateChromsCommand ??= new DelegateCommand(UpdateChroms);
        private DelegateCommand? _updateChromsCommand = null;


        public AlignedChromatogramModificationViewModelLegacy(AlignedChromatogramModificationModelLegacy model) {
            _model = model;
            model.ObservablePeakProperties.ObserveOnDispatcher().Subscribe(peakProperties =>
            {
                if (peakProperties is null) {
                    OriginalChromUC = new PeakModUCLegacy();
                    AlignedChromUC = new PeakModUCLegacy();
                    PickingUC = new PeakModUCLegacy();
                    return;
                }
                PeakPropertyLegacy[] properties = peakProperties.Properties;
                var dv_ = UtilityLegacy.GetDrawingVisualUC(properties, PeakModType.Original);
                var dv2_ = UtilityLegacy.GetDrawingVisualUC(properties, PeakModType.Aligned);
                var dv3_ = UtilityLegacy.GetDrawingVisualUC(properties, PeakModType.Picking);
                var originalChromUC = new PeakModUCLegacy(this, dv_, new MouseActionSetting() { FixMinY = true }, PeakModType.Original, properties.ToList());
                originalChromUC.RefreshUI();
                OriginalChromUC = originalChromUC;
                var alignedChromUC = new PeakModUCLegacy(this, dv2_, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, properties.ToList());
                alignedChromUC.RefreshUI();
                AlignedChromUC = alignedChromUC;
                var pickingUC = new PeakModUCLegacy(this, dv3_, new MouseActionSetting() { CanMouseAction = false }, PeakModType.Picking);
                pickingUC.RefreshUI();
                PickingUC = pickingUC;
            }).AddTo(Disposables);

            PeakPropertyList = model.ObservablePeakProperties.Select(props => props?.Properties ?? []).ToReadOnlyReactivePropertySlim(initialValue: []).AddTo(Disposables);
        }

        public void UpdateAlignedChromUC() {
            var dv2 = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList.Value, PeakModType.Aligned, IsRI);
            var alignedChromUC = new PeakModUCLegacy(this, dv2, new MouseActionSetting() { FixMinY = true }, PeakModType.Aligned, PeakPropertyList.Value.ToList());
            alignedChromUC.RefreshUI();
            AlignedChromUC = alignedChromUC;
        }
        public void UpdatePickingChromUC() {
            var dv = UtilityLegacy.GetDrawingVisualUC(PeakPropertyList.Value, PeakModType.Picking, IsRI);
            var pickingUC = new PeakModUCLegacy(this, dv, new MouseActionSetting() { FixMinY = true }, PeakModType.Picking, PeakPropertyList.Value.ToList());
            pickingUC.RefreshUI();
            PickingUC = pickingUC;
        }

        public void UpdatePeakInfo() {
            _model.UpdatePeakInfo();
        }

        public void ClearRtAlignment() {
            _model.ClearRtAlignment();
            UpdateAlignedChromUC();
        }

        public void UpdateChroms() {
            UpdateAlignedChromUC();
            UpdatePickingChromUC();
        }
    }

    public sealed class PeakPropertiesLegacy {
        private readonly AlignmentSpotPropertyModel _spot;

        public PeakPropertiesLegacy(AlignmentSpotPropertyModel spot, PeakPropertyLegacy[] properties) {
            _spot = spot ?? throw new ArgumentNullException(nameof(spot));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public PeakPropertyLegacy[] Properties { get; }

        public void UpdatePeakInfo() {
            if (Properties.IsEmptyOrNull()) {
                return;
            }

            var rtList = new List<double>();
            foreach (var p in Properties) {
                if (p.Accessory is null) {
                    return;
                }

                p.Model.ChromXsLeft.SetChromX(ChromX.Convert(p.Accessory.Chromatogram.RtLeft, _spot.ChromXType, _spot.ChromXUnit));
                p.Model.ChromXsTop.SetChromX(ChromX.Convert(p.Accessory.Chromatogram.RtTop, _spot.ChromXType, _spot.ChromXUnit));
                p.Model.ChromXsRight.SetChromX(ChromX.Convert(p.Accessory.Chromatogram.RtRight, _spot.ChromXType, _spot.ChromXUnit));

                rtList.Add(p.Accessory.Chromatogram.RtTop);

                //p.PeakBean.PeakID = -3;
                p.Model.IsManuallyModifiedForQuant = true;
                p.Model.PeakAreaAboveZero = p.PeakAreaAboveZero;
                p.Model.PeakAreaAboveBaseline = p.PeakAreaAboveBaseline;
                p.Model.PeakHeightTop = p.PeakHeight;
                p.Model.SignalToNoise = p.Accessory.Chromatogram.SignalToNoise;
            }

            if (rtList.Max() > 0) {
                Properties[0].AverageRt = rtList.Average();
            }
            _spot.TimesCenter = _spot.AlignedPeakPropertiesModelProperty.Value?.DefaultIfEmpty().Average(p => p?.ChromXsTop.GetRepresentativeXAxis().Value) ?? 0d;
            _spot.IsManuallyModifiedForQuant = true;
        }

        public void ClearRtAlignment() {
            foreach (var p in Properties) {
                p.ClearAlignedPeakList();
            }
        }
    }

    public class PeakPropertyLegacy {
        private readonly RetentionIndexHandler? _riHandler;

        public AlignmentChromPeakFeatureModel Model { get; set; }
        //public ChromatogramPeakInfo PeakSpotInfo { get; set; }
        public Brush Brush { get; set; }
        public List<ChromatogramPeak> SmoothedPeakList { get; set; }
        public List<ChromatogramPeak> AlignedPeakList { get; set; }
        public double AlignOffset { get; set; }
        public double AverageRt { get; set; }
        public double PeakAreaAboveZero { get; set; }
        public double PeakAreaAboveBaseline { get; set; }
        public double PeakHeight { get; set; }
        public Accessory? Accessory { get; set; }
        public bool Include { get; set; } = true;

        public PeakPropertyLegacy(AlignmentChromPeakFeatureModel bean, Brush brush, List<ChromatogramPeak> speaks, RetentionIndexHandler? riHandler) {
            Model = bean;
            Brush = brush;
            SmoothedPeakList = speaks;
            _riHandler = riHandler;
            AlignedPeakList = new List<ChromatogramPeak>(0);
        }

        public void SetAlignOffSet(double val) {
            AlignOffset = val;
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                var nChromXs = new ChromXs(p.ChromXs.GetRepresentativeXAxis().Add(-AlignOffset));
                if (nChromXs.MainType == ChromXType.RI && _riHandler is { }) {
                    nChromXs.RT = _riHandler.ConvertBack(nChromXs.RI);
                }
                AlignedPeakList.Add(new ChromatogramPeak(p.ID, 0d, p.Intensity, nChromXs));
            }
        }

        public void ClearAlignedPeakList() {
            AlignOffset = 0d;
            AlignedPeakList = new List<ChromatogramPeak>();
            foreach (var p in SmoothedPeakList) {
                if (p.ChromXs.MainType == ChromXType.RI && _riHandler is { }) {
                    p.ChromXs.RT = _riHandler.ConvertBack(p.ChromXs.RI);
                }
                AlignedPeakList.Add(new ChromatogramPeak(p.ID, 0d, p.Intensity, p.ChromXs));
            }
        }

        public void ModifyPeakEdge(double minChromXValue, double maxChromXValue) {
            if (!Include) {
                return;
            }
            var maxInt = 0.0;
            var maxIntChromXValue = 0.0;
            var sPeaklist = this.AlignedPeakList;
            var peakAreaAboveZero = 0.0;
            var areatype = Model.ChromXsTop.MainType;
            if (areatype == ChromXType.RI && _riHandler is { }) {
                areatype = ChromXType.RT;
            }
            for (var i = 0; i < sPeaklist.Count; i++) {
                if (sPeaklist[i].ChromXs.GetRepresentativeXAxis().Value < minChromXValue) continue;
                if (sPeaklist[i].ChromXs.GetRepresentativeXAxis().Value > maxChromXValue) break;
                if (maxInt < sPeaklist[i].Intensity) {
                    maxInt = sPeaklist[i].Intensity;
                    maxIntChromXValue = sPeaklist[i].ChromXs.GetRepresentativeXAxis().Value;
                }
                if (i + 1 < sPeaklist.Count) {
                    peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) * (sPeaklist[i + 1].ChromXs.GetChromByType(areatype).Value - sPeaklist[i].ChromXs.GetChromByType(areatype).Value) * 0.5;
                }
            }
            if (maxIntChromXValue == 0.0) {
                maxIntChromXValue = (minChromXValue + maxChromXValue) * 0.5;
            }
            var peakHeightFromBaseline = maxInt - Math.Min(sPeaklist[0].Intensity, sPeaklist[sPeaklist.Count - 1].Intensity);
            var noise = this.Model.EstimatedNoise;
            var sn = peakHeightFromBaseline / noise;

            this.PeakAreaAboveZero = peakAreaAboveZero;
            var left = sPeaklist.FirstOrDefault(p => p.ChromXs.GetRepresentativeXAxis().Value >= minChromXValue);
            var right = sPeaklist.LastOrDefault(p => p.ChromXs.GetRepresentativeXAxis().Value <= maxChromXValue);
            this.PeakAreaAboveBaseline = peakAreaAboveZero - (right.ChromXs.GetRepresentativeXAxis().Value - left.ChromXs.GetRepresentativeXAxis().Value) * (right.Intensity + left.Intensity) / 2;
            if (areatype == ChromXType.RT || areatype == ChromXType.RI) {
                this.PeakAreaAboveZero *= 60d;
                this.PeakAreaAboveBaseline *= 60d;
            }
            this.PeakHeight = maxInt;
            this.Accessory = new Accessory();
            this.Accessory.SetChromatogram(
                maxIntChromXValue + this.AlignOffset,
                minChromXValue + this.AlignOffset,
                maxChromXValue + this.AlignOffset,
                noise, sn);
        }
    }

    public static class UtilityLegacy {
        private static readonly Brush _unincludedBrush;
        private static readonly Pen _unincludedPen;

        static UtilityLegacy() {
            _unincludedBrush = Brushes.LightGray.Clone();
            _unincludedBrush.Opacity = .5;
            _unincludedBrush.Freeze();
            _unincludedPen = new Pen(_unincludedBrush, 1.0);
            _unincludedPen.Freeze();
        }

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
            peakProperties[0].AverageRt = aveRt;
            for (var i = 0; i < peakProperties.Count; i++) {
                if (rtList[i] > 0)
                    peakProperties[i].SetAlignOffSet((rtList[i] - aveRt));
                else
                    peakProperties[i].SetAlignOffSet(0d);
            }
        }

        public static DrawVisualManualPeakModification GetDrawingVisualUC(IReadOnlyList<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false, bool isDrift = false) {
            System.Diagnostics.Debug.Assert(peakProperties != null && peakProperties.Count > 0);
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
            var slist = GetChromatogramFromAlignedData(peakProperties!, type, isRI);
            return new DrawVisualManualPeakModification(area, title, slist);
        }

        private static SeriesList GetChromatogramFromAlignedData(IReadOnlyList<PeakPropertyLegacy> peakProperties, PeakModType type, bool isRI = false) {
            var slist = new SeriesList();
            for (var i = 0; i < peakProperties.Count; i++) {
                var prop = peakProperties[i];
                Brush brush;
                Pen pen;
                if (prop.Include) {
                    brush = prop.Brush;
                    pen = new Pen(brush, 1.0);
                    pen.Freeze();
                }
                else {
                    brush = _unincludedBrush;
                    pen = _unincludedPen;
                }
                var point = new Series() {
                    ChartType = ChartType.Chromatogram,
                    MarkerType = MarkerType.None,
                    Pen = pen,
                    Brush = brush,
                };

                if (type == PeakModType.Original) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    point.Accessory.SetChromatogram(
                        (float)prop.Model.ChromXsTop.Value, 
                        (float)prop.Model.ChromXsLeft.Value, 
                        (float)prop.Model.ChromXsRight.Value,
                        prop.Model.EstimatedNoise, 
                        prop.Model.SignalToNoise);
                }
                else if (type == PeakModType.Aligned) {
                    foreach (var p in prop.AlignedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.Model.ChromXsTop.Value - prop.AlignOffset,
                            (float)prop.Model.ChromXsLeft.Value - prop.AlignOffset,
                            (float)prop.Model.ChromXsRight.Value - prop.AlignOffset,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
                    }
                    else {
                        point.Accessory.SetChromatogram(prop.Accessory.Chromatogram.RtTop - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtLeft - prop.AlignOffset,
                            prop.Accessory.Chromatogram.RtRight - prop.AlignOffset,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
                    }
                }
                else if (type == PeakModType.Picking) {
                    foreach (var p in prop.SmoothedPeakList) {
                        point.AddPoint((float)p.ChromXs.Value, (float)p.Intensity);
                    }
                    point.Accessory = new Accessory();
                    if (prop.Accessory == null) {
                        point.Accessory.SetChromatogram(
                            (float)prop.Model.ChromXsTop.Value,
                            (float)prop.Model.ChromXsLeft.Value,
                            (float)prop.Model.ChromXsRight.Value,
                            prop.Model.EstimatedNoise,
                            prop.Model.SignalToNoise);
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
