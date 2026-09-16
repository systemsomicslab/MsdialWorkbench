using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public class AnnotationProcessOfProteoMetabolomics : IAnnotationProcess {
    public async Task RunAnnotationAsync(
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection msdecResults,
        IDataProvider provider,
        int numThreads = 1,
        Action<double>? reportAction = null,
        CancellationToken token = default) {
        var reporter = ReportProgress.FromRange(reportAction, 0d, 1d);
        if (numThreads <= 1) {
            await RunBySingleThreadAsync(chromPeakFeatures, msdecResults, provider, token, reporter);
        }
        else {
            await RunByMultiThreadAsync(chromPeakFeatures, msdecResults, provider, numThreads, token, reporter);
        }
    }

    private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _moleculeQueryFactories;
    private readonly IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> _peptideQueryFactories;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _moleculeRefer;
    private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> _peptideRefer;

    public AnnotationProcessOfProteoMetabolomics(
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> moleculeQueryFactories,
        IReadOnlyList<IAnnotationQueryFactory<MsScanMatchResult>> peptideQueryFactories,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> moleculeRefer, IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?> peptideRefer) {

        _moleculeQueryFactories = moleculeQueryFactories ?? throw new ArgumentNullException(nameof(moleculeQueryFactories));
        _peptideQueryFactories = peptideQueryFactories ?? throw new ArgumentNullException(nameof(peptideQueryFactories));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _moleculeRefer = moleculeRefer ?? throw new ArgumentNullException(nameof(moleculeRefer));
        _peptideRefer = peptideRefer ?? throw new ArgumentNullException(nameof(peptideRefer));
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

    private MSDecResult GetRepresentativeMSDecResult(ChromatogramPeakFeature chromPeakFeature, int index, IReadOnlyList<MSDecResult> msdecResults, Dictionary<int, List<int>> parentID2IsotopePeakIDs) {
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

    private async Task RunBySingleThreadAsync(
         IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
         MSDecResultCollection msdecResults,
         IDataProvider provider,
         CancellationToken token,
         ReportProgress reporter) {
        var spectrums = provider.LoadMsSpectrums();
        var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);
        for (int i = 0; i < chromPeakFeatures.Count; i++) {
            var chromPeakFeature = chromPeakFeatures[i];
            var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults.MSDecResults, parentID2IsotopePeakIDs);
            await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, msdecResults.CollisionEnergy, token);
            reporter.Report(i + 1, chromPeakFeatures.Count);
        };
    }

    private async Task RunByMultiThreadAsync(
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MSDecResultCollection msdecResults,
        IDataProvider provider,
        int numThreads,
        CancellationToken token,
        ReportProgress reporter) {
        var parentID2IsotopePeakIDs = GetParentID2IsotopePeakIDs(chromPeakFeatures);
        using var sem = new SemaphoreSlim(numThreads);
        var counter = 0;
        var annotationTasks = chromPeakFeatures.Select((chromPeakFeature, i) =>
            Task.Run(async () => {
                await sem.WaitAsync(token);

                try {
                    var msdecResult = GetRepresentativeMSDecResult(chromPeakFeature, i, msdecResults.MSDecResults, parentID2IsotopePeakIDs);
                    await RunAnnotationCoreAsync(chromPeakFeature, msdecResult, provider, msdecResults.CollisionEnergy, token);
                }
                finally {
                    sem.Release();
                    reporter.Report(Interlocked.Increment(ref counter), chromPeakFeatures.Count);
                }
            }, token)).ToList();
        await Task.WhenAll(annotationTasks).ConfigureAwait(false);
    }

    private async Task RunAnnotationCoreAsync(
        ChromatogramPeakFeature chromPeakFeature,
        MSDecResult msdecResult,
        IDataProvider provider,
        double collisionEnergy,
        CancellationToken token = default) {

        foreach (var factory in _peptideQueryFactories) {
            var pepQuery = factory.Create(
                chromPeakFeature,
                msdecResult,
                provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop).Spectrum,
                chromPeakFeature.PeakCharacter,
                factory.PrepareParameter());
            token.ThrowIfCancellationRequested();
            await Task.Run(() => SetPepAnnotationResult(chromPeakFeature, pepQuery, msdecResult, collisionEnergy), token).ConfigureAwait(false);
        }
        token.ThrowIfCancellationRequested();

        foreach (var factory in _moleculeQueryFactories) {
            var query = factory.Create(
                chromPeakFeature,
                msdecResult,
                provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS1RawSpectrumIdTop).Spectrum,
                chromPeakFeature.PeakCharacter,
                factory.PrepareParameter());
            token.ThrowIfCancellationRequested();
            await Task.Run(() => SetAnnotationResult(chromPeakFeature, query, _evaluator, msdecResult, collisionEnergy), token).ConfigureAwait(false);
        }
        token.ThrowIfCancellationRequested();
        
        SetRepresentativeProperty(chromPeakFeature);
    }

    private void SetPepAnnotationResult(ChromatogramPeakFeature chromPeakFeature, IAnnotationQuery<MsScanMatchResult> query, MSDecResult msdecResult, double collisionEnergy) {
        var candidates = query.FindCandidates().ToList();
        if (candidates.Count == 2) {
            candidates[0].IsReferenceMatched = candidates[1].IsReferenceMatched = true;
            candidates[0].CollisionEnergy = candidates[1].CollisionEnergy = collisionEnergy;
            candidates[0].SpectrumID = candidates[1].SpectrumID = msdecResult.RawSpectrumID;
            chromPeakFeature.MatchResults.AddResult(candidates[0]); // peptide query
            chromPeakFeature.MatchResults.AddResult(candidates[1]); // decoy query
        }
    }

    private void SetAnnotationResult(ChromatogramPeakFeature chromPeakFeature, IAnnotationQuery<MsScanMatchResult> query, IMatchResultEvaluator<MsScanMatchResult> matchResultEvaluator, MSDecResult msdecResult, double collisionEnergy) {
        var candidates = query.FindCandidates();
        var results = matchResultEvaluator.FilterByThreshold(candidates);
        var matches = matchResultEvaluator.SelectReferenceMatchResults(results);
        if (matches.Count > 0) {
            var best = matchResultEvaluator.SelectTopHit(matches);
            best.IsReferenceMatched = true;
            best.CollisionEnergy = collisionEnergy;
            best.SpectrumID = msdecResult.RawSpectrumID;
            chromPeakFeature.MatchResults.AddResult(best);
        }
        else if (results.Count > 0) {
            var best = matchResultEvaluator.SelectTopHit(results);
            best.IsAnnotationSuggested = true;
            best.CollisionEnergy = collisionEnergy;
            best.SpectrumID = msdecResult.RawSpectrumID;
            chromPeakFeature.MatchResults.AddResult(best);
        }
    }

    private void SetRepresentativeProperty(ChromatogramPeakFeature chromPeakFeature) {
        var representative = chromPeakFeature.MatchResults.Representative;
        
        if (representative.Source == SourceType.FastaDB) {
            if (_evaluator is null) {
                return;
            }
            else if (_evaluator.IsReferenceMatched(representative)) {
                DataAccess.SetPeptideMsProperty(chromPeakFeature, _peptideRefer.Refer(representative), representative);
            }
            else if (_evaluator.IsAnnotationSuggested(representative)) {
                DataAccess.SetPeptideMsPropertyAsSuggested(chromPeakFeature, _peptideRefer.Refer(representative), representative);
            }
        }
        else {
            if (_evaluator is null) {
                return;
            }
            else if (_evaluator.IsReferenceMatched(representative)) {
                DataAccess.SetMoleculeMsProperty(chromPeakFeature, _moleculeRefer.Refer(representative), representative);
            }
            else if (_evaluator.IsAnnotationSuggested(representative)) {
                DataAccess.SetMoleculeMsPropertyAsSuggested(chromPeakFeature, _moleculeRefer.Refer(representative), representative);
            }
        }
    }
}
