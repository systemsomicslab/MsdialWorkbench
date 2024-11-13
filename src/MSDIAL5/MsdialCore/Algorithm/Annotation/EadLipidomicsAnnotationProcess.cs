using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public class EadLipidomicsAnnotationProcess : IAnnotationProcess
{
    private static readonly int NUMBER_OF_ANNOTATION_RESULTS = 3;

    public async Task RunAnnotationAsync(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, MSDecResultCollection msdecResults, IDataProvider provider, int numThread = 1, Action<double>? reportAction = null, CancellationToken token = default) {
        var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);

        var reporter = ReportProgress.FromRange(reportAction, 0, 1);
        for (int i = 0; i < chromPeakFeatures.Count; i++) {
            var chromPeakFeature = chromPeakFeatures[i];
            var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults.MSDecResults, parentID2IsotopePeakIDs);
            await Task.Run(() => RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, msdecResults.CollisionEnergy, token), token).ConfigureAwait(false);
            reporter?.Report(i + 1, chromPeakFeatures.Count);
        };
    }

    private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _moleculeQueryFactries;
    private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _eadQueryFactories;
    private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

    public EadLipidomicsAnnotationProcess(
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> moleculeQueryFactories,
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> eadQueryFactories,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
        IMatchResultEvaluator<MsScanMatchResult> evaluator) { 
        _moleculeQueryFactries = moleculeQueryFactories ?? throw new ArgumentNullException(nameof(moleculeQueryFactories));
        _eadQueryFactories = eadQueryFactories ?? throw new ArgumentNullException(nameof(eadQueryFactories));
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

    private Task RunAnnotationCoreAsync(
         ChromatogramPeakFeature chromPeakFeature,
         MSDecResult msdecResult,
         IDataProvider provider,
         double collisionEnergy,
         CancellationToken token = default) {

        foreach (var factory in _moleculeQueryFactries) {
            token.ThrowIfCancellationRequested();
            var rawSpectrum = provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop);
            var query = factory.Create(
                chromPeakFeature,
                msdecResult,
                rawSpectrum.Spectrum,
                chromPeakFeature.PeakCharacter,
                factory.PrepareParameter());
            SetAnnotationResult(chromPeakFeature, query, rawSpectrum.Spectrum, msdecResult, collisionEnergy);
        }
        token.ThrowIfCancellationRequested();
        SetRepresentativeProperty(chromPeakFeature);
        return Task.CompletedTask;
    }

    private void SetAnnotationResult(ChromatogramPeakFeature chromPeakFeature, IAnnotationQuery<MsScanMatchResult> query, RawPeakElement[] spectrums, MSDecResult msdecResult, double collisionEnergy) {
        var candidates = query.FindCandidates();
        var results = _evaluator.FilterByThreshold(candidates).Select(r => {
            r.CollisionEnergy = collisionEnergy;
            r.SpectrumID = msdecResult.RawSpectrumID;
            return r;
        }).ToList();
        if (results.Count > 0) {
            var matches = _evaluator.SelectReferenceMatchResults(results);
            var topResults = new List<MsScanMatchResult>();
            if (matches.Count > 0) {
                var best = _evaluator.SelectTopHit(matches);
                chromPeakFeature.MatchResults.AddResult(best);
                topResults.Add(best);
                foreach (var factory in _eadQueryFactories) {
                    var query2 = factory.Create(query.Property, query.Scan, spectrums, query.IonFeature, factory.PrepareParameter());
                    var candidates2 = query2.FindCandidates();
                    var results2 = _evaluator.FilterByThreshold(candidates2);
                    topResults.AddRange(_evaluator.SelectTopN(results2, NUMBER_OF_ANNOTATION_RESULTS));
                }
            }
            else {
                topResults.AddRange(_evaluator.SelectTopN(results, NUMBER_OF_ANNOTATION_RESULTS));
            }
            chromPeakFeature.MatchResults.AddResults(topResults);
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
