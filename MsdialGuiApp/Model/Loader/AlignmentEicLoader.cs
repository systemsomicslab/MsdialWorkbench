using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    class AlignmentEicLoader
    {
        public AlignmentEicLoader(
            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer,
            string eicFile,
            IReadOnlyDictionary<int, string> id2cls) {

            this.chromatogramSpotSerializer = chromatogramSpotSerializer;
            this.eicFile = eicFile;
            this.id2cls = id2cls;
        }

        private readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly string eicFile;
        private readonly IReadOnlyDictionary<int, string> id2cls;

        public IObservable<List<Chromatogram>> LoadEicAsObservable(AlignmentSpotPropertyModel target) {
            if (target != null) {
                // maybe using file pointer is better
                var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                var itemss = spotinfo.PeakInfos.Select(peakinfo => (id2cls[peakinfo.FileID], peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList()));

                var eicChromatograms = itemss
                    .Zip(target.AlignedPeakPropertiesModel,
                        (pair, peakProperty) =>
                        peakProperty.ObserveProperty(p => p.ChromXsLeft).Merge(
                            peakProperty.ObserveProperty(p => p.ChromXsRight))
                        .Throttle(TimeSpan.FromMilliseconds(50))
                        .Select(_ => new Chromatogram
                        {
                            Class = pair.Item1,
                            Peaks = pair.Item2,
                            PeakArea = pair.Item2.Where(item => peakProperty.ChromXsLeft.Value <= item.Time && item.Time <= peakProperty.ChromXsRight.Value).ToList(),
                        }))
                    .CombineLatest()
                    .Select(xs => xs.ToList());
                return eicChromatograms;
            }
            else {
                return Observable.Return(new List<Chromatogram>());
            }
        }
    }
}