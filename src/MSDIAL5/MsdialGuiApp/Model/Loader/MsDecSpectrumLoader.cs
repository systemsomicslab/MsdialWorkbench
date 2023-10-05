using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
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
        public MsDecSpectrumLoader(
            MSDecLoader loader,
            IReadOnlyList<object> ms1Peaks) {

            this.ms1Peaks = ms1Peaks;
            this.loader = loader;
        }

        public MSDecResult Result { get; private set; }

        private readonly MSDecLoader loader;
        private readonly IReadOnlyList<object> ms1Peaks;

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(object target, CancellationToken token) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            var ms2DecSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(object target) {
            if (target is ChromatogramPeakFeatureModel cpeak) {
                var idx = cpeak.MSDecResultIDUsedForAnnotation;
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
            else if (target is AlignmentSpotPropertyModel spot) {
                // var peak = (AlignmentSpotPropertyModel)ms1Peaks[idx];
                //idx = peak.MSDecResultIDUsedForAnnotation;
                var idx = spot.MasterAlignmentID;
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
            else {
                var idx = ms1Peaks.IndexOf(target);
                var msdecResult = loader.LoadMSDecResult(idx);
                Result = msdecResult;
                return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
            }
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(object target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
