using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class AlignmentEicLoader : DisposableModelBase
    {
        public AlignmentEicLoader(ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer, string eicFile, IObservable<IReadOnlyDictionary<int, string>> id2clsAsObservable, IObservable<IReadOnlyDictionary<string, Color>> cls2colorAsObservable) {

            _chromatogramSpotSerializer = chromatogramSpotSerializer;
            _eicFile = eicFile;
            _id2ClsAsObservable = id2clsAsObservable.ToReactiveProperty().AddTo(Disposables);
            _cls2ColorAsObservable = cls2colorAsObservable.ToReactiveProperty().AddTo(Disposables);
        }

        private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSpotSerializer;
        private readonly string _eicFile;
        private readonly IObservable<IReadOnlyDictionary<int, string>> _id2ClsAsObservable;
        private readonly IObservable<IReadOnlyDictionary<string, Color>> _cls2ColorAsObservable;

        public IObservable<List<Chromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                // maybe using file pointer is better
                var spotinfo = _chromatogramSpotSerializer.DeserializeAtFromFile(_eicFile, target.MasterAlignmentID);
                var itemss = spotinfo.PeakInfos.Select(peakinfo => (peakinfo.FileID, peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList()));

                var eicChromatograms = Observable.CombineLatest(
                    _id2ClsAsObservable,
                    _cls2ColorAsObservable,
                    target.AlignedPeakPropertiesModelAsObservable.Where(props => props != null),
                    (id2cls, cls2color, peakProperties) => itemss
                        .Zip(peakProperties, (pair, peakProperty) =>
                            peakProperty.ObserveProperty(p => p.ChromXsLeft).Merge(
                                peakProperty.ObserveProperty(p => p.ChromXsRight))
                            .Throttle(TimeSpan.FromMilliseconds(50))
                            .Select(_ => new Chromatogram(
                                peaks: pair.Item2,
                                peakArea: pair.Item2.Where(item => peakProperty.ChromXsLeft.Value <= item.Time && item.Time <= peakProperty.ChromXsRight.Value).ToList(),
                                class_: id2cls[pair.Item1],
                                color: cls2color.TryGetValue(id2cls[pair.Item1], out var color) ? color : Colors.Blue,
                                type: target.ChromXType,
                                unit: target.ChromXUnit)))
                        .CombineLatest()
                        .Select(xs => xs.ToList()))
                    .Switch();
                return eicChromatograms;
            }
            else {
                return Observable.Return(new List<Chromatogram>());
            }
        }
    }
}