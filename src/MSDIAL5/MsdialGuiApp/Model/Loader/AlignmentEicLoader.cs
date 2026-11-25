using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    public sealed class AlignmentEicLoader : DisposableModelBase
    {
        private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSpotSerializer;
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly List<FileChromatogram> _fileChromatograms;
        private readonly AlignmentRtAxis _rtAxis;
        private readonly object _spotInfoLock = new object();
        private (int Id, ChromatogramSpotInfo Info)? _lastSpotInfo;

        public AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, AlignmentFileBeanModel alignmentFile, AnalysisFileBeanModelCollection files, FilePropertiesModel projectParameter, bool useOriginalRtPeakBounds = false)
            : this(chromatogramSpotSerializer, alignmentFile, files, projectParameter, useOriginalRtPeakBounds, AlignmentRtAxis.NotCorrected) {
        }

        /// <param name="rtAxis">
        /// The RT axes of the alignment result. When its aligned RTs are on another axis than the
        /// chromatograms (the automatic alignment RT correction ran), each file's peak area is taken at
        /// the measured-axis bounds: through the saved map when there is one, else at the stored bounds.
        /// </param>
        public AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, AlignmentFileBeanModel alignmentFile, AnalysisFileBeanModelCollection files, FilePropertiesModel projectParameter, AlignmentRtAxis rtAxis)
            : this(chromatogramSpotSerializer, alignmentFile, files, projectParameter, rtAxis?.IsCorrected ?? false, rtAxis ?? AlignmentRtAxis.NotCorrected) {
        }

        private AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, AlignmentFileBeanModel alignmentFile, AnalysisFileBeanModelCollection files, FilePropertiesModel projectParameter, bool useOriginalRtPeakBounds, AlignmentRtAxis rtAxis) {
            _chromatogramSpotSerializer = chromatogramSpotSerializer ?? throw new ArgumentNullException(nameof(chromatogramSpotSerializer));
            _alignmentFile = alignmentFile ?? throw new ArgumentNullException(nameof(alignmentFile));
            _rtAxis = rtAxis;
            var classToProp = projectParameter.ClassProperties.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default)
                .Select(_ => projectParameter.ClassProperties.ToDictionary(prop => prop.Name, prop => prop)).ToReactiveProperty().AddTo(Disposables);
            _fileChromatograms = files.AnalysisFiles.Select(file => new FileChromatogram(file, classToProp, useOriginalRtPeakBounds)).ToList();
            foreach (var fileChromatogram in _fileChromatograms) {
                Disposables.Add(fileChromatogram);
            }
        }

        /// <summary>The RT axes of the alignment result this loader reads.</summary>
        public AlignmentRtAxis RtAxis => _rtAxis;

        private ChromatogramSpotInfo LoadSpotInfo(AlignmentSpotPropertyModel target) {
            lock (_spotInfoLock) {
                if (_lastSpotInfo is { } last && last.Id == target.MasterAlignmentID) {
                    return last.Info;
                }
            }
            var spotinfo = _chromatogramSpotSerializer.DeserializeAtFromFile(_alignmentFile.EicFilePath, target.MasterAlignmentID);
            lock (_spotInfoLock) {
                _lastSpotInfo = (target.MasterAlignmentID, spotinfo);
            }
            return spotinfo;
        }

        /// <summary>
        /// How each of the spot's aligned peaks moves between its file's measured RT axis and the
        /// aligned RT axis; for a result without the correction, the identity for every peak.
        /// </summary>
        public IObservable<AlignedSpotRtAxes> LoadRtAxesAsObservable(AlignmentSpotPropertyModel? target) {
            if (target is null || !_rtAxis.IsCorrected) {
                return Observable.Return(AlignedSpotRtAxes.NotCorrected);
            }
            if (_rtAxis.Correction is not null) {
                // The saved map does not depend on the peaks.
                return Observable.Return(_rtAxis.ForSpot(null, target.AlignedPeakPropertiesModelProperty.Value?.Select(peak => (
                    peak.FileID, double.NaN, double.NaN, double.NaN, false)) ?? Enumerable.Empty<(int, double, double, double, bool)>()));
            }
            var spotinfo = LoadSpotInfo(target);
            // The peaks' bounds as they are when the spot is loaded: the bounds the alignment wrote,
            // unless a person has edited the peak, which ForSpot then refuses.
            return target.AlignedPeakPropertiesModelProperty
                .Where(peaks => peaks is not null)
                .Take(1)
                .Select(peaks => _rtAxis.ForSpot(spotinfo.PeakInfos, peaks!.Select(peak => (
                    peak.FileID,
                    peak.ChromXsLeft?.RT?.Value ?? double.NaN,
                    peak.ChromXsTop?.RT?.Value ?? double.NaN,
                    peak.ChromXsRight?.RT?.Value ?? double.NaN,
                    peak.IsManuallyModifiedForQuant))))
                .Replay(1).RefCount();
        }

        public IObservable<List<PeakChromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                var spotinfo = LoadSpotInfo(target);
                var ps = Enumerable.Range(0, _fileChromatograms.Count)
                    .Select(i => target.AlignedPeakPropertiesModelProperty.Select(peaks => peaks?[i]));
                return _fileChromatograms.Zip(ps, spotinfo.PeakInfos, (fileChromatogram, p, info) => fileChromatogram.GetChromatogram(target, p, info, _rtAxis))
                    .CombineLatest()
                    .Throttle(TimeSpan.FromSeconds(.05d))
                    .Select(chromatograms => chromatograms.OfType<PeakChromatogram>().ToList())
                    .Replay(1).RefCount();
            }
            else {
                return Observable.Return(new List<PeakChromatogram>(0));
            }
        }

        internal static PeakOfChromatogram? SelectPeakArea(
            Chromatogram chromatogram, ChromatogramPeakInfo peakInfo,
            double alignedLeft, double alignedRight, bool useOriginalRtPeakBounds) {
            if (useOriginalRtPeakBounds) {
                const double serializedRtTolerance = 1e-5d;
                return chromatogram.AsPeak(
                    peakInfo.ChromXsLeft.Value - serializedRtTolerance,
                    peakInfo.ChromXsRight.Value + serializedRtTolerance);
            }
            return chromatogram.AsPeak(alignedLeft, alignedRight);
        }

        class FileChromatogram : IDisposable {
            private readonly IObservable<bool> _includes;
            private readonly IObservable<string> _clss;
            private readonly IObservable<string> _name;
            private readonly IObservable<Color> _color;
            private readonly bool _useOriginalRtPeakBounds;
            private CompositeDisposable? _disposables = new();

            public FileChromatogram(AnalysisFileBeanModel file, IObservable<Dictionary<string, FileClassPropertyModel>> class2Prop, bool useOriginalRtPeakBounds) {
                _includes = file.ObserveProperty(f => f.AnalysisFileIncluded).ToReactiveProperty().AddTo(_disposables);
                _name = file.ObserveProperty(f => f.AnalysisFileName).ToReactiveProperty(string.Empty).AddTo(_disposables);
                _clss = file.ObserveProperty(f => f.AnalysisFileClass).ToReactiveProperty(string.Empty).AddTo(_disposables);
                _color = class2Prop.CombineLatest(_clss, (c2p, cls) => c2p.TryGetValue(cls, out var prop) ? prop.ObserveProperty(p => p.Color) : Observable.Return(Colors.Blue)).Switch(); 
                _useOriginalRtPeakBounds = useOriginalRtPeakBounds;
            }

            public IObservable<PeakChromatogram?> GetChromatogram(AlignmentSpotPropertyModel _spot, IObservable<AlignmentChromPeakFeatureModel?> _peak, ChromatogramPeakInfo _peakInfo, AlignmentRtAxis rtAxis) {
                var chromatogram = new Chromatogram(_peakInfo.Chromatogram, _spot.ChromXType, _spot.ChromXUnit);
                // Automatic alignment correction keeps the EIC on raw RT; aligned peak bounds are on another axis.
                // With the alignment's saved map they are moved onto the EIC's axis, so a peak edited
                // after alignment is shown where it now is; without it the stored bounds are shown.
                var chromatogramArea = rtAxis.Correction is { } correction
                    ? _peak.DefaultIfNull(p => {
                        var map = AlignedPeakRtAxisMap.FromCorrection(correction, _peakInfo.FileID);
                        return SelectPeakArea(chromatogram, _peakInfo, map.ToOriginal(p.ChromXsLeft.Value), map.ToOriginal(p.ChromXsRight.Value), false);
                    })
                    : _useOriginalRtPeakBounds
                    ? Observable.Return(SelectPeakArea(chromatogram, _peakInfo, 0d, 0d, true))
                    : _peak.DefaultIfNull(p => SelectPeakArea(chromatogram, _peakInfo, p.ChromXsLeft.Value, p.ChromXsRight.Value, false));

                return new IObservable<IObservable<PeakChromatogram?>>[]
                {
                    _includes.Where(x => x).Select(_ => Observable.CombineLatest(_clss, _name, _color, chromatogramArea, (clss, name, color, area_) => new PeakChromatogram(chromatogram, area_, clss, color, name))),
                    _includes.Where(x => !x).Select(_ => Observable.Return((PeakChromatogram?)null)),
                }.Merge().Switch();
            }

            public void Dispose() {
                _disposables?.Dispose();
                _disposables = null;
            }
        }
    }
}
