using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
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

        public AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, AlignmentFileBeanModel alignmentFile, AnalysisFileBeanModelCollection files, FilePropertiesModel projectParameter, bool useOriginalRtPeakBounds = false) {
            _chromatogramSpotSerializer = chromatogramSpotSerializer ?? throw new ArgumentNullException(nameof(chromatogramSpotSerializer));
            _alignmentFile = alignmentFile ?? throw new ArgumentNullException(nameof(alignmentFile));
            var classToProp = projectParameter.ClassProperties.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default)
                .Select(_ => projectParameter.ClassProperties.ToDictionary(prop => prop.Name, prop => prop)).ToReactiveProperty().AddTo(Disposables);
            _fileChromatograms = files.AnalysisFiles.Select(file => new FileChromatogram(file, classToProp, useOriginalRtPeakBounds)).ToList();
            foreach (var fileChromatogram in _fileChromatograms) {
                Disposables.Add(fileChromatogram);
            }
        }

        public IObservable<List<PeakChromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                var spotinfo = _chromatogramSpotSerializer.DeserializeAtFromFile(_alignmentFile.EicFilePath, target.MasterAlignmentID);
                var ps = Enumerable.Range(0, _fileChromatograms.Count)
                    .Select(i => target.AlignedPeakPropertiesModelProperty.Select(peaks => peaks?[i]));
                return _fileChromatograms.Zip(ps, spotinfo.PeakInfos, (fileChromatogram, p, info) => fileChromatogram.GetChromatogram(target, p, info))
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

            public IObservable<PeakChromatogram?> GetChromatogram(AlignmentSpotPropertyModel _spot, IObservable<AlignmentChromPeakFeatureModel?> _peak, ChromatogramPeakInfo _peakInfo) {
                var chromatogram = new Chromatogram(_peakInfo.Chromatogram, _spot.ChromXType, _spot.ChromXUnit);
                // Automatic alignment correction keeps the EIC on raw RT; aligned peak bounds are on another axis.
                var chromatogramArea = _useOriginalRtPeakBounds
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
