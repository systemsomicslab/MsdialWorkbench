using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
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
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    public sealed class AlignmentEicLoader : DisposableModelBase
    {
        private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSpotSerializer;
        private readonly string _eicFile;
        private readonly List<FileChromatogram> _fileChromatograms;

        public AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, string eicFile, AnalysisFileBeanModelCollection files, FilePropertiesModel projectParameter) {
            _chromatogramSpotSerializer = chromatogramSpotSerializer ?? throw new ArgumentNullException(nameof(chromatogramSpotSerializer));
            _eicFile = eicFile ?? throw new ArgumentNullException(nameof(eicFile));
            // var classToColor = projectParameter.ObserveProperty(p => p.ClassProperties).Select(props => props.ToDictionary(prop => prop.Name, prop => prop.ObserveProperty(p => p.Color))).ToReactiveProperty().AddTo(Disposables);
            var classToProp = projectParameter.ClassProperties.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default)
                .Select(_ => projectParameter.ClassProperties.ToDictionary(prop => prop.Name, prop => prop)).ToReactiveProperty().AddTo(Disposables);
            _fileChromatograms = files.AnalysisFiles.Select(file => new FileChromatogram(file, classToProp)).ToList();           
        }

        public IObservable<List<Chromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                var spotinfo = _chromatogramSpotSerializer.DeserializeAtFromFile(_eicFile, target.MasterAlignmentID);
                var ps = Enumerable.Range(0, _fileChromatograms.Count)
                    .Select(i => target.AlignedPeakPropertiesModelProperty.Select(peaks => peaks?[i]));
                return _fileChromatograms.Zip(ps, spotinfo.PeakInfos, (fileChromatogram, p, info) => fileChromatogram.GetChromatogram(target, p, info))
                    .CombineLatest()
                    .Throttle(TimeSpan.FromSeconds(.05d))
                    .Select(chromatograms => chromatograms.Where(chromatogram => !(chromatogram is null)).ToList());
            }
            else {
                return Observable.Return(new List<Chromatogram>(0));
            }
        }

        class FileChromatogram {
            private readonly IObservable<bool> _includes;
            private readonly IObservable<string> _clss;
            private readonly IObservable<string> _name;
            private readonly IObservable<Color> _color;

            public FileChromatogram(AnalysisFileBeanModel file, IObservable<Dictionary<string, FileClassPropertyModel>> class2Prop) {
                _includes = file.ObserveProperty(f => f.AnalysisFileIncluded).ToReactiveProperty();
                _clss = file.ObserveProperty(f => f.AnalysisFileClass).ToReactiveProperty();
                _name = file.ObserveProperty(f => f.AnalysisFileName).ToReactiveProperty();
                _color = class2Prop.CombineLatest(_clss, (c2p, cls) => c2p.TryGetValue(cls, out var prop) ? prop.ObserveProperty(p => p.Color) : Observable.Return(Colors.Blue)).Switch(); 
            }

            public IObservable<Chromatogram> GetChromatogram(AlignmentSpotPropertyModel _spot, IObservable<AlignmentChromPeakFeatureModel> _peak, ChromatogramPeakInfo _peakInfo) {
                var peaks = _peakInfo.Chromatogram.Select(peak => new PeakItem(peak)).ToList();
                var area = new[]
                {
                    _peak.SkipNull().Select(p => peaks.Where(item => p.ChromXsLeft.Value <= item.Time && item.Time <= p.ChromXsRight.Value).ToList()),
                    _peak.TakeNull().Select(_ => new List<PeakItem>(0)),
                }.Merge();

                return new[]
                {
                    _includes.Where(x => x).Select(_ => Observable.Return(Unit.Default).CombineLatest(_clss, _name, _color, area, (_2, clss, name, color, area_) => new Chromatogram(peaks, area_, clss, color, _spot.ChromXType, _spot.ChromXUnit, name))),
                    _includes.Where(x => !x).Select(_ => Observable.Return((Chromatogram)null)),
                }.Merge().Switch();
            }
        }
    }
}