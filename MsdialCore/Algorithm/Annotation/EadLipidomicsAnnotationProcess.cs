using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
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
    public class EadLipidomicsAnnotationProcess<T> : IAnnotationProcess where T : IAnnotationQuery
    {
        public void RunAnnotation(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
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

        public async Task RunAnnotationAsync(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
            var spectrums = provider.LoadMs1Spectrums();
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                    await Task.Run(() => RunAnnotationCoreAsync(chromPeakFeature, msdecResult, spectrums, token), token).ConfigureAwait(false);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        private readonly List<(IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> Container)> moleculeContainerPairs;
        private readonly List<(IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> Container)> eadLipidContainerPairs;
        private readonly (IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> Container) eadLipidContainerPair;

        public EadLipidomicsAnnotationProcess(
            List<(IAnnotationQueryFactory<T>, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>)> moleculeContainerPairs,
            List<(IAnnotationQueryFactory<T>, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>)> eadLipidContainerPairs) { 
            this.moleculeContainerPairs = moleculeContainerPairs ?? throw new ArgumentNullException(nameof(moleculeContainerPairs));
            this.eadLipidContainerPairs = eadLipidContainerPairs ?? throw new ArgumentNullException(nameof(eadLipidContainerPairs));
            eadLipidContainerPair = eadLipidContainerPairs.FirstOrDefault();
        }

        public EadLipidomicsAnnotationProcess(
            List<(IAnnotationQueryFactory<T>, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>)> moleculeContainerPairs,
            IAnnotationQueryFactory<T> eadQueryFactory, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> eadLipidAnnotator)
            : this(moleculeContainerPairs, (eadQueryFactory, eadLipidAnnotator)) {

        }

        public EadLipidomicsAnnotationProcess(
            List<(IAnnotationQueryFactory<T>, IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult>)> moleculeContainerPairs,
            (IAnnotationQueryFactory<T>, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>) eadLipidContainerPair) {
            this.moleculeContainerPairs = moleculeContainerPairs ?? throw new ArgumentNullException(nameof(moleculeContainerPairs));
            eadLipidContainerPairs = new List<(IAnnotationQueryFactory<T> Factory, IAnnotatorContainer<(T, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> Container)> { eadLipidContainerPair };
            this.eadLipidContainerPair = eadLipidContainerPair;
        }

        private Dictionary<int, List<int>> GetParentID2IsotopePeakIDs(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures) {
            var isotopeGroupDict = new Dictionary<int, List<int>>();
            var peaks = chromPeakFeatures.OrderBy(peak => peak.PeakCharacter.IsotopeWeightNumber);
            foreach (var peak in peaks) {
                if (!isotopeGroupDict.ContainsKey(peak.PeakCharacter.IsotopeParentPeakID)) {
                    isotopeGroupDict[peak.PeakCharacter.IsotopeParentPeakID] = new List<int>();
                }
                if (peak.PeakCharacter.IsotopeWeightNumber != 0) {
                    isotopeGroupDict[peak.PeakCharacter.IsotopeParentPeakID].Add(peak.MasterPeakID);
                }
            }
            return isotopeGroupDict;
        }

        private MSDecResult GetRepresentativeMSDecResult(
            ChromatogramPeakFeature chromPeakFeature,
            int index,
            IReadOnlyList<MSDecResult> msdecResults,
            Dictionary<int, List<int>> parentID2IsotopePeakIDs) {
            var msdecResult = msdecResults[index];
            if (msdecResult.Spectrum.IsEmptyOrNull()) {
                var ids = parentID2IsotopePeakIDs[chromPeakFeature.PeakCharacter.IsotopeParentPeakID];
                foreach (var id in ids) {
                    if (!msdecResults[id].Spectrum.IsEmptyOrNull()) {
                        msdecResult = msdecResults[id];
                        chromPeakFeature.MSDecResultIdUsed = id;
                        break;
                    }
                }
            }
            return msdecResult;
        }

        private void RunAnnotationCore(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IReadOnlyList<RawSpectrum> msSpectrums) {

            foreach (var containerPair in moleculeContainerPairs) {
                var query = containerPair.Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                    chromPeakFeature.PeakCharacter,
                    containerPair.Container.Parameter);
                SetAnnotationResult(chromPeakFeature, query, msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum, containerPair.Container);
            }
            SetRepresentativeProperty(chromPeakFeature);
        }

        private Task RunAnnotationCoreAsync(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IReadOnlyList<RawSpectrum> msSpectrums,
             CancellationToken token = default) {

            foreach (var containerPair in moleculeContainerPairs) {
                token.ThrowIfCancellationRequested();
                var query = containerPair.Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum,
                    chromPeakFeature.PeakCharacter,
                    containerPair.Container.Parameter);
                SetAnnotationResult(chromPeakFeature, query, msSpectrums[chromPeakFeature.MS1RawSpectrumIdTop].Spectrum, containerPair.Container);
            }
            token.ThrowIfCancellationRequested();
            SetRepresentativeProperty(chromPeakFeature);
            return Task.CompletedTask;
        }

        private void SetAnnotationResult(
            ChromatogramPeakFeature chromPeakFeature,
            T query,
            RawPeakElement[] spectrums,
            IAnnotatorContainer<T, MoleculeMsReference, MsScanMatchResult> annotatorContainer) {

            var annotator = annotatorContainer.Annotator;

            var candidates = annotator.FindCandidates(query);
            var results = annotator.FilterByThreshold(candidates);
            if (results.Count > 0) {
                var matches = annotator.SelectReferenceMatchResults(results);
                var best = annotator.SelectTopHit(matches.Count > 0 ? matches : results);
                chromPeakFeature.MatchResults.AddResult(best);

                foreach (var eadLipidContainerPair in eadLipidContainerPairs) {
                    var container2 = eadLipidContainerPair.Container;
                    var query2 = eadLipidContainerPair.Factory.Create(query.Property, query.Scan, spectrums, query.IonFeature, container2.Parameter);
                    var candidates2 = eadLipidContainerPair.Container.Annotator.FindCandidates((query2, annotatorContainer.Annotator.Refer(best)));
                    var results2 = container2.Annotator.FilterByThreshold(candidates2);
                    if (results2.Count > 0) {
                        var matches2 = container2.Annotator.SelectReferenceMatchResults(results2);
                        var best2 = container2.Annotator.SelectTopHit(matches2.Count > 0 ? matches2 : results2);
                        chromPeakFeature.MatchResults.AddResult(best2);
                    }
                }
            }
        }

        private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
            var representative = chromPeakFeature.MatchResults.Representative;

            (var evaluator, var refer, var parameter) = GetReferEvaluatorParameter(representative.AnnotatorID);
            if(evaluator is null || refer is null || parameter is null) {
                return;
            }
            if (evaluator.IsReferenceMatched(representative)) {
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, refer.Refer(representative), representative);
            }
            else if (evaluator.IsAnnotationSuggested(representative)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, refer.Refer(representative), representative);
            }
        }

        private (IMatchResultEvaluator<MsScanMatchResult>, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, MsRefSearchParameterBase) GetReferEvaluatorParameter(string id) {
            var container = moleculeContainerPairs.FirstOrDefault(pair => id == pair.Container.AnnotatorID).Container;
            if (!(container is null)) {
                return (container?.Annotator, container?.Annotator, container?.Parameter);
            }
            else if (eadLipidContainerPairs.FirstOrDefault(pair => pair.Container.AnnotatorID == id).Container is IAnnotatorContainer<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult> lipidContainer){
                return (lipidContainer.Annotator, lipidContainer.Annotator, lipidContainer.Parameter);
            }
            else {
                return default;
            }
        }
    }
}
