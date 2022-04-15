using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    class AlignmentEicLoader
    {
        public AlignmentEicLoader(
            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer,
            string eicFile,
            IObservable<IReadOnlyDictionary<int, string>> id2clsAsObservable,
            IObservable<IReadOnlyDictionary<string, Color>> cls2colorAsObservable) {

            this.chromatogramSpotSerializer = chromatogramSpotSerializer;
            this.eicFile = eicFile;
            id2ClsAsObservable = id2clsAsObservable;
            cls2ColorAsObservable = cls2colorAsObservable;
        }

        private readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly string eicFile;
        private readonly IObservable<IReadOnlyDictionary<int, string>> id2ClsAsObservable;
        private readonly IObservable<IReadOnlyDictionary<string, Color>> cls2ColorAsObservable;

        public IObservable<List<Chromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                // maybe using file pointer is better
                var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                var itemss = spotinfo.PeakInfos.Select(peakinfo => (peakinfo.FileID, peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList()));

                var eicChromatograms = Observable.CombineLatest(
                    id2ClsAsObservable,
                    cls2ColorAsObservable,
                    (id2cls, cls2color) => itemss
                        .Zip(target.AlignedPeakPropertiesModel,
                            (pair, peakProperty) =>
                            peakProperty.ObserveProperty(p => p.ChromXsLeft).Merge(
                                peakProperty.ObserveProperty(p => p.ChromXsRight))
                            .Throttle(TimeSpan.FromMilliseconds(50))
                            .Select(_ => new Chromatogram(
                                peaks: pair.Item2,
                                peakArea: pair.Item2.Where(item => peakProperty.ChromXsLeft.Value <= item.Time && item.Time <= peakProperty.ChromXsRight.Value).ToList(),
                                class_: id2cls[pair.Item1],
                                color: cls2color[id2cls[pair.Item1]])))
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