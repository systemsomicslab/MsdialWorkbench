using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation {
    public class ShotgunProteomicsStandardAnnotationProcess<T> : IAnnotationProcess {
        public void RunAnnotation(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            int numThreads = 1,
            CancellationToken token = default,
            Action<double> reportAction = null) {
            if (numThreads <= 1) {
                RunBySingleThread(chromPeakFeatures, msdecResults, provider, reportAction);
            }
            else {
                RunByMultiThread(chromPeakFeatures, msdecResults, provider, numThreads, token, reportAction);
            }
        }

        public async Task RunAnnotationAsync(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            int numThreads = 1,
            CancellationToken token = default,
            Action<double> reportAction = null) {
            if (numThreads <= 1) {
                await RunBySingleThreadAsync(chromPeakFeatures, msdecResults, provider, token, reportAction);
            }
            else {
                await RunByMultiThreadAsync(chromPeakFeatures, msdecResults, provider, numThreads, token, reportAction);
            }
        }

        public IPepAnnotationQueryFactory<T> QueryFactory { get; }
        public IReadOnlyList<IAnnotatorContainer<T, PeptideMsReference, MsScanMatchResult>> AnnotatorContainers { get; }


        private void RunBySingleThread(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, 
            IReadOnlyList<MSDecResult> msdecResults, 
            IDataProvider provider, 
            Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                var msdecResult = msdecResults[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    RunAnnotationCore(chromPeakFeature, msdecResult, spectrums);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        private void RunByMultiThread(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThreads, CancellationToken token, Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            Enumerable.Range(0, chromPeakFeatures.Count)
                .AsParallel()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .ForAll(i => {
                    var chromPeakFeature = chromPeakFeatures[i];
                    var msdecResult = msdecResults[i];
                    if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                        RunAnnotationCore(chromPeakFeature, msdecResult, spectrums);
                    }
                    reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
                });
        }

        private async Task RunBySingleThreadAsync(
             IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
             IReadOnlyList<MSDecResult> msdecResults,
             IDataProvider provider,
             CancellationToken token,
             Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                var msdecResult = msdecResults[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, spectrums, token);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        private async Task RunByMultiThreadAsync(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            int numThreads,
            CancellationToken token,
            Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            using (var sem = new SemaphoreSlim(numThreads)) {
                var annotationTasks = chromPeakFeatures.Zip(msdecResults, Tuple.Create)
                    .Select(async (pair, i) => {
                        await sem.WaitAsync();

                        try {
                            var chromPeakFeature = pair.Item1;
                            var msdecResult = pair.Item2;
                            if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                                await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, spectrums, token);
                            }
                        }
                        finally {
                            sem.Release();
                            reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
                        }
                    });
                await Task.WhenAll(annotationTasks);
            }
        }

        private void RunAnnotationCore(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IReadOnlyList<RawSpectrum> ms1Spectrums) {
            var query = QueryFactory.Create(chromPeakFeature, msdecResult, ms1Spectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum);
            var annotatorContainers = AnnotatorContainers;

            foreach (var annotatorContainer in annotatorContainers) {
                SetAnnotationResult(chromPeakFeature, query, annotatorContainer);
            }
            SetRepresentativeProperty(chromPeakFeature);
        }

        private async Task RunAnnotationCoreAsync(
            ChromatogramPeakFeature chromPeakFeature,
            MSDecResult msdecResult,
            IReadOnlyList<RawSpectrum> ms1Spectrums,
            CancellationToken token = default) {
            var query = QueryFactory.Create(chromPeakFeature, msdecResult, ms1Spectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum);
            var annotatorContainers = AnnotatorContainers;

            foreach (var annotatorContainer in annotatorContainers) {
                token.ThrowIfCancellationRequested();
                await Task.Run(() => SetAnnotationResult(chromPeakFeature, query, annotatorContainer), token);
            }
            token.ThrowIfCancellationRequested();
            SetRepresentativeProperty(chromPeakFeature);
        }

        private void SetAnnotationResult(
            ChromatogramPeakFeature chromPeakFeature, T query,
            IAnnotatorContainer<T, PeptideMsReference, MsScanMatchResult> annotatorContainer) {

            var annotator = annotatorContainer.Annotator;
            var candidates = annotator.FindCandidates(query);
            if (candidates.Count == 2) {
                chromPeakFeature.MatchResults.AddResult(candidates[0]);
                chromPeakFeature.MatchResults.AddResult(candidates[1]);
            }
        }

        private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
            var representative = chromPeakFeature.MatchResults.Representative;
            var annotator = AnnotatorContainers.FirstOrDefault(annotatorContainer => representative.AnnotatorID == annotatorContainer.AnnotatorID)?.Annotator;
            if (annotator is null) {
                return;
            }
            else if (annotator.IsReferenceMatched(representative)) {
               // DataAccess.SetMoleculeMsProperty(chromPeakFeature, annotator.Refer(representative), representative);
            }
            else if (annotator.IsAnnotationSuggested(representative)) {
               // DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, annotator.Refer(representative), representative);
            }
        }

    }
}
