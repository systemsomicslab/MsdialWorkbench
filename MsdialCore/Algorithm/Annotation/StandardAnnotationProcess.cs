using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class StandardAnnotationProcess<T> : IAnnotationProcess
    {
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

        public StandardAnnotationProcess(
            IAnnotationQueryFactory<T> queryFactory,
            IReadOnlyList<IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>> containers) {
            QueryFactory = queryFactory;
            AnnotatorContainers = containers;
        }

        public StandardAnnotationProcess(
            IAnnotationQueryFactory<T> queryFactory,
            params IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>[] containers) {
            QueryFactory = queryFactory;
            AnnotatorContainers = containers;
        }

        public IAnnotationQueryFactory<T> QueryFactory { get; }
        public IReadOnlyList<IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }

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

        private void RunByMultiThread(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            int numThreads,
            CancellationToken token,
            Action<double> reportAction) {

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
            IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {

            var annotator = annotatorContainer.Annotator;

            var candidates = annotator.FindCandidates(query);
            var results = annotator.FilterByThreshold(candidates, annotatorContainer.Parameter);
            var matches = annotator.SelectReferenceMatchResults(results, annotatorContainer.Parameter);
            if (matches.Count > 0) {
                var best = annotator.SelectTopHit(matches, annotatorContainer.Parameter);
                chromPeakFeature.MatchResults.AddResult(best);
            }
            else if (results.Count > 0) {
                var best = annotator.SelectTopHit(results, annotatorContainer.Parameter);
                chromPeakFeature.MatchResults.AddResult(best);
            }
        }

        private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
            var representative = chromPeakFeature.MatchResults.Representative;
            var annotator = AnnotatorContainers.FirstOrDefault(annotatorContainer => representative.AnnotatorID == annotatorContainer.AnnotatorID)?.Annotator;
            if (annotator is null) {
                return;
            }
            else if (annotator.IsReferenceMatched(representative)) {
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, annotator.Refer(representative), representative);
            }
            else if (annotator.IsAnnotationSuggested(representative)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, annotator.Refer(representative), representative);
            }
        }
    }
}
