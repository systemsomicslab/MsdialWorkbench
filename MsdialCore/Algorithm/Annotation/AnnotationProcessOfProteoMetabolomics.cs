using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation {
    public class AnnotationProcessOfProteoMetabolomics<T> : IAnnotationProcess {
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

        //public IAnnotationQueryFactory<T> QueryFactory { get; }
        public IAnnotationQueryFactory<T> QueryFactory { get; }
        public IReadOnlyList<IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>> AnnotatorContainers { get; }
        public IReadOnlyList<IAnnotatorContainer<T, PeptideMsReference, MsScanMatchResult>> PepAnnotatorContainers { get; }

        public AnnotationProcessOfProteoMetabolomics(
            IAnnotationQueryFactory<T> queryFactory,
            IReadOnlyList<IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>> containers,
            IReadOnlyList<IAnnotatorContainer<T, PeptideMsReference, MsScanMatchResult>> pepContainers) {
            AnnotatorContainers = containers;
            QueryFactory = queryFactory;
            PepAnnotatorContainers = pepContainers;
        }

        private Dictionary<int, List<int>> GetParentID2IsotopePeakIDs(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures) {
            var isotopeGroupDict = new Dictionary<int, List<int>>();
            var peaks = chromPeakFeatures.OrderBy(n => n.PeakCharacter.IsotopeParentPeakID).ThenBy(n => n.PeakCharacter.IsotopeWeightNumber).ToList();
            for (int i = 0; i < peaks.Count; i++) {
                if (peaks[i].PeakCharacter.IsotopeWeightNumber == 0) {
                    isotopeGroupDict[peaks[i].PeakCharacter.IsotopeParentPeakID] = new List<int>();
                }
                else {
                    isotopeGroupDict[peaks[i].PeakCharacter.IsotopeParentPeakID].Add(peaks[i].MasterPeakID);
                }
            }
            return isotopeGroupDict;
        }


        private void RunBySingleThread(
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, 
            IReadOnlyList<MSDecResult> msdecResults, 
            IDataProvider provider, 
            Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                    RunAnnotationCore(chromPeakFeature, msdecResult, spectrums);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        private MSDecResult GetRepresentativeMSDecResult(ChromatogramPeakFeature chromPeakFeature, int index, IReadOnlyList<MSDecResult> msdecResults, Dictionary<int, List<int>> parentID2IsotopePeakIDs) {
            var msdecResult = msdecResults[index];
            if (msdecResult.Spectrum.IsEmptyOrNull()) {
                var ids = parentID2IsotopePeakIDs[chromPeakFeature.PeakCharacter.IsotopeParentPeakID];
                foreach (var id in ids) {
                    if (!msdecResults[id].Spectrum.IsEmptyOrNull()) {
                        msdecResult = msdecResults[id];
                        chromPeakFeature.MSDecResultIdUsed = id;
                    }
                }
            }
            return msdecResult;
        }

        private void RunByMultiThread(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThreads, CancellationToken token, Action<double> reportAction) {
            var spectrums = provider.LoadMs1Spectrums();
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);
            Enumerable.Range(0, chromPeakFeatures.Count)
                .AsParallel()
                .WithCancellation(token)
                .WithDegreeOfParallelism(numThreads)
                .ForAll(i => {
                    var chromPeakFeature = chromPeakFeatures[i];
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                    //var msdecResult = msdecResults[i];
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
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);
            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                //var msdecResult = msdecResults[i];
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
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);
            using (var sem = new SemaphoreSlim(numThreads)) {
                var annotationTasks = new List<Task>();
                for (int i = 0; i < chromPeakFeatures.Count; i++) {
                    var v = Task.Run(async () => {
                        await sem.WaitAsync();

                        try {
                            var chromPeakFeature = chromPeakFeatures[i];
                            //var msdecResult = msdecResults[i];
                            var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                            if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                                await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, spectrums, token);
                            }
                        }
                        finally {
                            sem.Release();
                            reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
                        }
                    });
                    annotationTasks.Add(v);
                }
                //var annotationTasks = chromPeakFeatures.Zip(msdecResults, Tuple.Create)
                //    .Select(async (pair, i) => {
                //        await sem.WaitAsync();

                //        try {
                //            var chromPeakFeature = pair.Item1;
                //            var msdecResult = pair.Item2;
                //            if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                //                await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, spectrums, token);
                //            }
                //        }
                //        finally {
                //            sem.Release();
                //            reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
                //        }
                //    });
                await Task.WhenAll(annotationTasks);
            }
        }

        private void RunAnnotationCore(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IReadOnlyList<RawSpectrum> msSpectrums) {

            if (!PepAnnotatorContainers.IsEmptyOrNull()) {
                var pepAnnotatorContainers = PepAnnotatorContainers;

                foreach (var annotatorContainer in pepAnnotatorContainers) {
                    var pepQuery = QueryFactory.Create(
                        chromPeakFeature,
                        msdecResult,
                        msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                        chromPeakFeature.PeakCharacter,
                        annotatorContainer.Parameter);
                    SetPepAnnotationResult(chromPeakFeature, pepQuery, annotatorContainer);
                }
            }
            if (!AnnotatorContainers.IsEmptyOrNull()) {
                var annotatorContainers = AnnotatorContainers;

                foreach (var annotatorContainer in annotatorContainers) {
                    var query = QueryFactory.Create(
                        chromPeakFeature,
                        msdecResult,
                        msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                        chromPeakFeature.PeakCharacter,
                        annotatorContainer.Parameter);
                    SetAnnotationResult(chromPeakFeature, query, annotatorContainer);
                }
            }
            
            SetRepresentativeProperty(chromPeakFeature);
        }

        private async Task RunAnnotationCoreAsync(
            ChromatogramPeakFeature chromPeakFeature,
            MSDecResult msdecResult,
            IReadOnlyList<RawSpectrum> msSpectrums,
            CancellationToken token = default) {

            if (!PepAnnotatorContainers.IsEmptyOrNull()) {
                var pepAnnotatorContainers = PepAnnotatorContainers;

                foreach (var annotatorContainer in pepAnnotatorContainers) {
                    var pepQuery = QueryFactory.Create(
                        chromPeakFeature,
                        msdecResult,
                        msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                        chromPeakFeature.PeakCharacter,
                        annotatorContainer.Parameter);
                    token.ThrowIfCancellationRequested();
                    await Task.Run(() => SetPepAnnotationResult(chromPeakFeature, pepQuery, annotatorContainer), token);
                }
                token.ThrowIfCancellationRequested();
            }

            if (!AnnotatorContainers.IsEmptyOrNull()) {
                var annotatorContainers = AnnotatorContainers;

                foreach (var annotatorContainer in annotatorContainers) {
                    var query = QueryFactory.Create(
                        chromPeakFeature,
                        msdecResult,
                        msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                        chromPeakFeature.PeakCharacter,
                        annotatorContainer.Parameter);
                    token.ThrowIfCancellationRequested();
                    await Task.Run(() => SetAnnotationResult(chromPeakFeature, query, annotatorContainer), token);
                }
                token.ThrowIfCancellationRequested();
            }
            
            SetRepresentativeProperty(chromPeakFeature);
        }

        private void SetPepAnnotationResult(
            ChromatogramPeakFeature chromPeakFeature, T query,
            IAnnotatorContainer<T, PeptideMsReference, MsScanMatchResult> annotatorContainer) {
            var annotator = annotatorContainer.Annotator;
            var candidates = annotator.FindCandidates(query);
            if (candidates.Count == 2) {
                chromPeakFeature.MatchResults.AddResult(candidates[0]); // peptide query
                chromPeakFeature.MatchResults.AddResult(candidates[1]); // decoy query
            }
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
            var decoyRepresentative = chromPeakFeature.MatchResults.DecoyRepresentative;
            
            if (representative.Source == SourceType.FastaDB) {
                var container = PepAnnotatorContainers.FirstOrDefault(annotatorContainer => representative.AnnotatorID == annotatorContainer.AnnotatorID);
                var annotator = container?.Annotator;
                if (annotator is null) {
                    return;
                }
                else if (annotator.IsReferenceMatched(representative, container.Parameter)) {
                    DataAccess.SetPeptideMsProperty(chromPeakFeature, annotator.Refer(representative), representative);
                }
                else if (annotator.IsAnnotationSuggested(representative, container.Parameter)) {
                    DataAccess.SetPeptideMsPropertyAsSuggested(chromPeakFeature, annotator.Refer(representative), representative);
                }
            }
            else {
                var container = AnnotatorContainers.FirstOrDefault(annotatorContainer => representative.AnnotatorID == annotatorContainer.AnnotatorID);
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
}
