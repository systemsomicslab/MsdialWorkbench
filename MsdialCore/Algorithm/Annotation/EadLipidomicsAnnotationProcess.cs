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
    public class EadLipidomicsAnnotationProcess : IAnnotationProcess
    {
        public void RunAnnotation(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                    RunAnnotationCore(chromPeakFeature, msdecResult, provider);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        public async Task RunAnnotationAsync(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, IDataProvider provider, int numThread = 1, CancellationToken token = default, Action<double> reportAction = null) {
            var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);

            for (int i = 0; i < chromPeakFeatures.Count; i++) {
                var chromPeakFeature = chromPeakFeatures[i];
                if (chromPeakFeature.PeakCharacter.IsotopeWeightNumber == 0) {
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults, parentID2IsotopePeakIDs);
                    await Task.Run(() => RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, token), token).ConfigureAwait(false);
                }
                reportAction?.Invoke((double)(i + 1) / chromPeakFeatures.Count);
            };
        }

        private readonly List<(IAnnotationQueryFactory<MsScanMatchResult> Factory, MsRefSearchParameterBase Parameter)> _moleculeContainerPairs;
        private readonly List<(IAnnotationQueryFactory<MsScanMatchResult> Factory, MsRefSearchParameterBase Parameter)> _eadAnnotationQueryFactories;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public EadLipidomicsAnnotationProcess(
            List<(IAnnotationQueryFactory<MsScanMatchResult>, MsRefSearchParameterBase)> moleculeContainerPairs,
            List<(IAnnotationQueryFactory<MsScanMatchResult>, MsRefSearchParameterBase)> eadAnnotationQueryFactories,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) { 
            _moleculeContainerPairs = moleculeContainerPairs ?? throw new ArgumentNullException(nameof(moleculeContainerPairs));
            _eadAnnotationQueryFactories = eadAnnotationQueryFactories ?? throw new ArgumentNullException(nameof(eadAnnotationQueryFactories));
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
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
            else {
                chromPeakFeature.MSDecResultIdUsed = index;
            }
            return msdecResult;
        }

        private void RunAnnotationCore(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IDataProvider provider) {

            foreach (var (Factory, Parameter) in _moleculeContainerPairs) {
                var rawSpectrum = provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop);
                var query = Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    rawSpectrum.Spectrum,
                    chromPeakFeature.PeakCharacter,
                    Parameter);
                SetAnnotationResult(chromPeakFeature, query, rawSpectrum.Spectrum);
            }
            SetRepresentativeProperty(chromPeakFeature);
        }

        private Task RunAnnotationCoreAsync(
             ChromatogramPeakFeature chromPeakFeature,
             MSDecResult msdecResult,
             IDataProvider provider,
             CancellationToken token = default) {

            foreach (var (Factory, Parameter) in _moleculeContainerPairs) {
                token.ThrowIfCancellationRequested();
                var rawSpectrum = provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop);
                var query = Factory.Create(
                    chromPeakFeature,
                    msdecResult,
                    rawSpectrum.Spectrum,
                    chromPeakFeature.PeakCharacter,
                    Parameter);
                SetAnnotationResult(chromPeakFeature, query, rawSpectrum.Spectrum);
            }
            token.ThrowIfCancellationRequested();
            SetRepresentativeProperty(chromPeakFeature);
            return Task.CompletedTask;
        }

        private void SetAnnotationResult(ChromatogramPeakFeature chromPeakFeature, IAnnotationQuery<MsScanMatchResult> query, RawPeakElement[] spectrums) {
            var candidates = query.FindCandidates();
            var results = _evaluator.FilterByThreshold(candidates);
            if (results.Count > 0) {
                var matches = _evaluator.SelectReferenceMatchResults(results);
                if (matches.Count > 0) {
                    var best = _evaluator.SelectTopHit(matches);
                    best.IsReferenceMatched = true;
                    chromPeakFeature.MatchResults.AddResult(best);

                    foreach (var eadAnnotationQueryFactory in _eadAnnotationQueryFactories) {
                        var query2 = eadAnnotationQueryFactory.Factory.Create(query.Property, query.Scan, spectrums, query.IonFeature, eadAnnotationQueryFactory.Parameter);
                        var candidates2 = query2.FindCandidates();
                        var results2 = _evaluator.FilterByThreshold(candidates2);
                        if (results2.Count > 0) {
                            var matches2 = _evaluator.SelectReferenceMatchResults(results2);
                            var best2 = _evaluator.SelectTopHit(matches2.Count > 0 ? matches2 : results2);
                            chromPeakFeature.MatchResults.AddResult(best2);
                        }
                    }
                }
                else if (results.Count > 0) {
                    var best = _evaluator.SelectTopHit(results);
                    best.IsAnnotationSuggested = true;
                    chromPeakFeature.MatchResults.AddResult(best);
                }

                //var best = annotator.SelectTopHit(matches.Count > 0 ? matches : results);
                //best.IsReferenceMatched = true;
                //chromPeakFeature.MatchResults.AddResult(best);

                //foreach (var eadLipidContainerPair in eadLipidContainerPairs) {
                //    var container2 = eadLipidContainerPair.Container;
                //    var query2 = eadLipidContainerPair.Factory.Create(query.Property, query.Scan, spectrums, query.IonFeature, container2.Parameter);
                //    var candidates2 = eadLipidContainerPair.Container.Annotator.FindCandidates((query2, annotatorContainer.Annotator.Refer(best)));
                //    var results2 = container2.Annotator.FilterByThreshold(candidates2);
                //    if (results2.Count > 0) {
                //        var matches2 = container2.Annotator.SelectReferenceMatchResults(results2);
                //        var best2 = container2.Annotator.SelectTopHit(matches2.Count > 0 ? matches2 : results2);
                        
                //        best2.IsReferenceMatched = true;
                //        chromPeakFeature.MatchResults.AddResult(best2);
                //    }
                //}
            }
        }

        private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
            var representative = chromPeakFeature.MatchResults.Representative;
            if (_evaluator is null || _refer is null) {
                return;
            }
            if (_evaluator.IsReferenceMatched(representative)) {
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, _refer.Refer(representative), representative);
            }
            else if (_evaluator.IsAnnotationSuggested(representative)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, _refer.Refer(representative), representative);
            }
        }
    }
}
