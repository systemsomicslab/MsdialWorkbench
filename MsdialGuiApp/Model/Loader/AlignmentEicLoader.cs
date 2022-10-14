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
                var itemss = spotinfo.PeakInfos.Select(peakinfo => (peakinfo.FileID, Peaks:peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList()));

                var eicChromatograms = target.AlignedPeakPropertiesModelAsObservable.Select(peakProperties => {
                    var chromatogramsAsObservable = new List<IObservable<Chromatogram>>();
                    foreach (var (pair, i) in itemss.WithIndex()) {
                        var clsAsObservable = GetClass(pair.FileID);
                        var clsColorAsObservable = GetClassColor(clsAsObservable);

                        var peakAreaAsObservable = Observable.Return(new List<PeakItem>(0));
                        if (peakProperties != null && i < peakProperties.Count) {
                            peakAreaAsObservable = new[]
                            {
                                peakProperties[i].ObserveProperty(p => p.ChromXsLeft),
                                peakProperties[i].ObserveProperty(p => p.ChromXsRight),
                            }.Merge()
                            .Select(_ => pair.Peaks.Where(item => peakProperties[i].ChromXsLeft.Value <= item.Time && item.Time <= peakProperties[i].ChromXsRight.Value).ToList());
                        }

                        var chromatogramAsObservable = Observable.CombineLatest(
                            peakAreaAsObservable,
                            clsAsObservable,
                            clsColorAsObservable,
                            (peakArea, cls_, color) => new Chromatogram(
                                peaks: pair.Peaks,
                                peakArea: peakArea,
                                class_: cls_,
                                color: color,
                                type: target.ChromXType,
                                unit: target.ChromXUnit));
                        chromatogramsAsObservable.Add(chromatogramAsObservable);
                    }
                    return chromatogramsAsObservable.CombineLatest().Select(xs => xs.ToList());
                }).Switch();
                return eicChromatograms;
            }
            else {
                return Observable.Return(new List<Chromatogram>());
            }
        }

        private IObservable<string> GetClass(int fileId) {
            return _id2ClsAsObservable.Select(id2Cls => id2Cls.TryGetValue(fileId, out var cls) ? cls : string.Empty);
        }

        private IObservable<Color> GetClassColor(IObservable<string> clsAsObservable) {
            return Observable.CombineLatest(clsAsObservable, _cls2ColorAsObservable, (cls, cls2Color) => cls2Color.TryGetValue(cls, out var color) ? color : Colors.Blue);
        }
    }
}