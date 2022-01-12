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
            IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> container) {
            containerPairs = new List<(IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> Container)>
            {
                (queryFactory, container)
            };
        }

        public StandardAnnotationProcess(
            IAnnotationQueryFactory<T> queryFactory,
            IReadOnlyList<IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>> containers) {
            containerPairs = containers.Select(container => (queryFactory, container)).ToList();
        }

        public StandardAnnotationProcess(
            IAnnotationQueryFactory<T> queryFactory,
            params IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>[] containers) {
            containerPairs = containers.Select(container => (queryFactory, container)).ToList();
        }

        public StandardAnnotationProcess(
            List<(IAnnotationQueryFactory<T>, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>)> containerPairs) {
            this.containerPairs = containerPairs;
        }

        private readonly List<(IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> Container)> containerPairs;

        private void RunBySingleThread(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
            IReadOnlyList<MSDecResult> msdecResults,
            IDataProvider provider,
            Action<double> reportAction) {
            var spectrums = provider.LoadMsSpectrums();
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

            var spectrums = provider.LoadMsSpectrums();
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
            IReadOnlyList<RawSpectrum> msSpectrums) {

            foreach (var containerPair in containerPairs) {
                var query = containerPair.Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                    chromPeakFeature.PeakCharacter,
                    containerPair.Container.Parameter);
                SetAnnotationResult(chromPeakFeature, query, containerPair.Container);
            }
            SetRepresentativeProperty(chromPeakFeature);
        }

        private async Task RunAnnotationCoreAsync(
            ChromatogramPeakFeature chromPeakFeature,
            MSDecResult msdecResult,
            IReadOnlyList<RawSpectrum> msSpectrums,
            CancellationToken token = default) {

            foreach (var containerPair in containerPairs) {
                var query = containerPair.Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                    chromPeakFeature.PeakCharacter,
                    containerPair.Container.Parameter);
                token.ThrowIfCancellationRequested();
                await Task.Run(() => SetAnnotationResult(chromPeakFeature, query, containerPair.Container), token);
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
            var container = containerPairs.FirstOrDefault(containerPair => representative.AnnotatorID == containerPair.Container.AnnotatorID).Container;
            var annotator = container?.Annotator;
            if (annotator is null) {
                return;
            }
            else if (annotator.IsReferenceMatched(representative, container.Parameter)) {
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, annotator.Refer(representative), representative);
            }
            else if (annotator.IsAnnotationSuggested(representative, container.Parameter)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, annotator.Refer(representative), representative);
            }
        }
    }
}
