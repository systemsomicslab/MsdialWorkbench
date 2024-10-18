using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsDecSpectrumLoader : IMsSpectrumLoader<object>
    {
        public MsDecSpectrumLoader(MSDecLoader loader, IReadOnlyList<object> ms1Peaks) {
            this.ms1Peaks = ms1Peaks;
            this.loader = loader;
        }

        private readonly MSDecLoader loader;
        private readonly IReadOnlyList<object> ms1Peaks;

        public async Task<IMSScanProperty?> LoadSpectrumAsync(object target, CancellationToken token) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            var ms2DecSpectrum = await Task.Run(() => LoadCore(target), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private IMSScanProperty? LoadCore(object target) {
            if (target is ChromatogramPeakFeatureModel cpeak) {
                var idx = cpeak.MSDecResultIDUsedForAnnotation;
                var msdecResult = loader.LoadMSDecResult(idx);
                return msdecResult;
            }
            else if (target is AlignmentSpotPropertyModel spot) {
                // var peak = (AlignmentSpotPropertyModel)ms1Peaks[idx];
                //idx = peak.MSDecResultIDUsedForAnnotation;
                var idx = spot.MasterAlignmentID;
                var msdecResult = loader.LoadMSDecResult(idx);
                return msdecResult;
            }
            else {
                var idx = ms1Peaks.IndexOf(target);
                var msdecResult = loader.LoadMSDecResult(idx);
                return msdecResult;
            }
        }

        public IObservable<IMSScanProperty?> LoadScanAsObservable(object target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
