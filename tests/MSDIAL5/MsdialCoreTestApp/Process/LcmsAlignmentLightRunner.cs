using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process;

internal sealed class LcmsAlignmentLightRunner {
    private static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

    private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
    private readonly IProgress<int>? _progress;
    private readonly MsdialLcmsParameter _parameter;
    private readonly double _mztol;
    private readonly double _rttol;
    private readonly double _mzfactor;
    private readonly double _rtfactor;

    public LcmsAlignmentLightRunner(
        IMsdialDataStorage<MsdialLcmsParameter> storage,
        IMatchResultEvaluator<MsScanMatchResult> evaluator,
        IDataProviderFactory<AnalysisFileBean> providerFactory,
        IProgress<int>? progress) {
        _storage = storage;
        _evaluator = evaluator;
        _providerFactory = providerFactory;
        _progress = progress;
        _parameter = storage.Parameter;
        _mztol = _parameter.Ms1AlignmentTolerance;
        _rttol = _parameter.RetentionTimeAlignmentTolerance;
        _mzfactor = _parameter.Ms1AlignmentFactor;
        _rtfactor = _parameter.RetentionTimeAlignmentFactor;
    }

    public LcmsAlignmentLightResult Run(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile, AlignmentLightPeakStore peakStore) {
        var factory = new LcmsAlignmentProcessFactory(_storage, _evaluator) {
            Progress = _progress,
            SkipIonAbundanceCorrelationLinks = true,
        };
        var joiner = (LcmsPeakJoiner)factory.CreatePeakJoiner();
        var accessor = (IFeatureAccessor<ChromatogramPeakFeature>)factory.CreateDataAccessor();
        var master = joiner.GetMasterList(analysisFiles, _parameter.AlignmentReferenceFileID, accessor)
            .OrderBy(prop => (prop.PrecursorMz, prop.ChromXs.RT.Value))
            .ToList();
        peakStore.Initialize(master.Count, analysisFiles);
        var spots = InitializeSpots(master);
        var accumulators = spots.Select(spot => new LightSpotAccumulator(spot, analysisFiles.Count)).ToList();
        var qcFileCount = _parameter.FileID_AnalysisFileType.Count(pair => pair.Value == AnalysisFileType.QC);
        var classFileCounts = _parameter.FileID_ClassName.Values
            .GroupBy(className => className)
            .ToDictionary(group => group.Key, group => group.Count());

        Console.WriteLine("Alignment light mode: detected-peak pass started.");
        var reporter = ReportProgress.FromLength(_progress, 20.0, 20.0);
        var counter = 0;
        var matchedSpotIdsByFileId = analysisFiles.ToDictionary(file => file.AnalysisFileId, _ => new HashSet<int>());
        foreach (var file in analysisFiles) {
            var targets = accessor.GetMSScanProperties(file);
            foreach (var match in MatchTargetsToMaster(master, targets)) {
                var peak = CreateAlignmentPeak(file, master[match.MasterIndex].IonMode);
                DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(peak, match.Target);
                peakStore.WriteSpotPeak(spots[match.MasterIndex].MasterAlignmentID, peak);
                _parameter.FileID_AnalysisFileType.TryGetValue(peak.FileID, out var fileType);
                _parameter.FileID_ClassName.TryGetValue(peak.FileID, out var className);
                accumulators[match.MasterIndex].AddDetectedPeak(peak, fileType, className);
                matchedSpotIdsByFileId[file.AnalysisFileId].Add(spots[match.MasterIndex].MasterAlignmentID);
            }
            reporter.Report(++counter, analysisFiles.Count - 1);
        }

        var kept = accumulators
            .Where(acc => PassesLightAlignmentFilters(acc, qcFileCount, classFileCounts))
            .ToList();

        Console.WriteLine($"Alignment light mode: {kept.Count} alignment spots retained from {accumulators.Count} master spots.");
        Console.WriteLine("Alignment light mode: gap-fill pass started.");
        var gapFiller = new LcmsGapFiller(_parameter);
        reporter = ReportProgress.FromLength(_progress, 40.0, 40.0);
        counter = 0;
        foreach (var file in analysisFiles) {
            FillMissingPeaks(file, kept, matchedSpotIdsByFileId[file.AnalysisFileId], gapFiller, peakStore);
            reporter.Report(++counter, analysisFiles.Count - 1);
        }

        foreach (var acc in kept) {
            acc.ApplySpotSummary();
        }

        kept = RefineLightSpots(kept);
        kept = AssignLightAlignmentIds(kept, peakStore);
        SetLightIsotopeAnalysis(kept);
        Console.WriteLine($"Alignment light mode: {kept.Count} alignment spots retained after spot-level LC-MS refinement.");

        var resultSpots = kept.Select(acc => acc.Spot).ToList();
        SetRelativeAmplitude(resultSpots);
        MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, EnumerateRepresentativeDeconvolutions(analysisFiles, kept), kept.Count);
        var msdecResults = new FileBackedMsdecResults(alignmentFile.SpectraFilePath);

        var container = new AlignmentResultContainer {
            Ionization = _parameter.Ionization,
            AlignmentResultFileID = -1,
            TotalAlignmentSpotCount = resultSpots.Count,
            AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(resultSpots),
        };
        return new LcmsAlignmentLightResult(container, msdecResults);
    }

    private List<AlignmentSpotProperty> InitializeSpots(IReadOnlyList<ChromatogramPeakFeature> master) {
        var spots = new List<AlignmentSpotProperty>(master.Count);
        for (var i = 0; i < master.Count; i++) {
            var scanProp = master[i];
            spots.Add(new AlignmentSpotProperty {
                MasterAlignmentID = i,
                AlignmentID = i,
                ParentAlignmentID = -1,
                TimesCenter = scanProp.ChromXs,
                TimesMin = scanProp.ChromXs,
                TimesMax = scanProp.ChromXs,
                MassCenter = scanProp.PrecursorMz,
                MassMin = (float)scanProp.PrecursorMz,
                MassMax = (float)scanProp.PrecursorMz,
                IonMode = scanProp.IonMode,
                InternalStandardAlignmentID = -1,
                AlignmentDriftSpotFeatures = [],
            });
        }
        return spots;
    }

    private IEnumerable<(int MasterIndex, ChromatogramPeakFeature Target)> MatchTargetsToMaster(
        IReadOnlyList<ChromatogramPeakFeature> masters,
        IReadOnlyList<ChromatogramPeakFeature> targets) {
        var n = masters.Count;
        var maxMatchs = new double[n];
        var matchedTargets = new ChromatogramPeakFeature[n];
        var dummy = new ChromatogramPeakFeature();

        foreach (var target in targets) {
            int? matchIdx = null;
            var matchFactor = double.MinValue;
            dummy.ChromXs = new ChromXs(target.ChromXs.RT.Value - _rttol, ChromXType.RT);
            dummy.PrecursorMz = target.PrecursorMz - _mztol;
            var lo = masters.LowerBound(dummy, Comparer);
            dummy.ChromXs.RT = new RetentionTime(target.ChromXs.RT.Value + _rttol, dummy.ChromXs.RT.Unit);
            dummy.PrecursorMz = target.PrecursorMz + _mztol;
            for (var i = lo; i < n; i++) {
                if (Comparer.Compare(masters[i], dummy) > 0) {
                    break;
                }
                if (!IsSimilarTo(masters[i], target)) {
                    continue;
                }
                var factor = GetSimilarity(masters[i], target);
                if (factor > maxMatchs[i] && factor > matchFactor) {
                    matchIdx = i;
                    maxMatchs[i] = matchFactor = factor;
                }
            }
            if (matchIdx.HasValue) {
                matchedTargets[matchIdx.Value] = target;
            }
        }
        for (var i = 0; i < matchedTargets.Length; i++) {
            if (matchedTargets[i] != null) {
                yield return (i, matchedTargets[i]);
            }
        }
    }

    private bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
        return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= _mztol && Math.Abs(x.ChromXs.RT.Value - y.ChromXs.RT.Value) <= _rttol;
    }

    private double GetSimilarity(IMSScanProperty x, IMSScanProperty y) {
        return _mzfactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / _mztol, 2))
             + _rtfactor * Math.Exp(-0.5 * Math.Pow((x.ChromXs.RT.Value - y.ChromXs.RT.Value) / _rttol, 2));
    }

    private static AlignmentChromPeakFeature CreateAlignmentPeak(AnalysisFileBean file, IonMode ionMode) {
        return new AlignmentChromPeakFeature {
            MasterPeakID = -1,
            PeakID = -1,
            FileID = file.AnalysisFileId,
            FileName = file.AnalysisFileName,
            IonMode = ionMode,
        };
    }

    private bool PassesLightAlignmentFilters(
        LightSpotAccumulator acc,
        int qcFileCount,
        IReadOnlyDictionary<string, int> classFileCounts) {
        if (acc.DetectedCount <= 0) {
            return false;
        }

        var minCount = _parameter.PeakCountFilter / 100d * _parameter.FileID_AnalysisFileType.Count;
        if (acc.DetectedCount < minCount) {
            return false;
        }

        if (_parameter.QcAtLeastFilter && !acc.IsDetectedInEveryQc(qcFileCount)) {
            return false;
        }

        var classThreshold = _parameter.NPercentDetectedInOneGroup / 100d;
        return acc.IsDetectedEnoughInAnyClass(classFileCounts, classThreshold);
    }

    private List<LightSpotAccumulator> RefineLightSpots(IReadOnlyList<LightSpotAccumulator> accumulators) {
        var cleaned = new List<LightSpotAccumulator>();
        var done = new HashSet<int>();

        foreach (var acc in accumulators.Where(acc => acc.Spot.MspID >= 0 && acc.Spot.IsReferenceMatched(_evaluator)).OrderByDescending(acc => acc.Spot.MspBasedMatchResult.TotalScore)) {
            TryMergeToMaster(acc, cleaned, done);
        }

        foreach (var acc in accumulators.Where(acc => acc.Spot.IsReferenceMatched(_evaluator)).OrderByDescending(acc => acc.Spot.MatchResults.Representative.TotalScore)) {
            TryMergeToMaster(acc, cleaned, done);
        }

        foreach (var acc in accumulators.Where(acc => acc.Spot.TextDbID >= 0 && acc.Spot.IsReferenceMatched(_evaluator)).OrderByDescending(acc => acc.Spot.TextDbBasedMatchResult.TotalScore)) {
            TryMergeToMaster(acc, cleaned, done);
        }

        foreach (var acc in accumulators.OrderByDescending(acc => acc.Spot.HeightAverage)) {
            if (acc.Spot.IsReferenceMatched(_evaluator)) {
                continue;
            }
            if (acc.Spot.PeakCharacter.IsotopeWeightNumber > 0) {
                continue;
            }
            TryMergeToMaster(acc, cleaned, done);
        }

        return cleaned;
    }

    private void TryMergeToMaster(LightSpotAccumulator acc, List<LightSpotAccumulator> cleaned, HashSet<int> done) {
        var spot = acc.Spot;
        if (done.Contains(spot.AlignmentID)) {
            return;
        }

        var spotRt = spot.TimesCenter.Value;
        var spotMz = spot.MassCenter;
        var rtTol = Math.Min(_parameter.RetentionTimeAlignmentTolerance, 0.1);
        var ms1Tol = (double)_parameter.Ms1AlignmentTolerance;
        if (spotMz > 500d) {
            ms1Tol = spotMz * (_parameter.Ms1AlignmentTolerance / 500d);
        }

        foreach (var existing in cleaned.Where(accum => Math.Abs(accum.Spot.MassCenter - spotMz) < ms1Tol)) {
            if (Math.Abs(existing.Spot.TimesCenter.Value - spotRt) < rtTol * 0.5) {
                return;
            }
        }

        cleaned.Add(acc);
        done.Add(spot.AlignmentID);
    }

    private static List<LightSpotAccumulator> AssignLightAlignmentIds(IReadOnlyList<LightSpotAccumulator> accumulators, AlignmentLightPeakStore peakStore) {
        var ordered = accumulators
            .OrderBy(acc => acc.Spot.MassCenter)
            .ToList();
        var oldToNew = new Dictionary<int, int>(ordered.Count);
        for (var newId = 0; newId < ordered.Count; newId++) {
            var acc = ordered[newId];
            oldToNew[acc.Spot.MasterAlignmentID] = newId;
            acc.Spot.MasterAlignmentID = acc.Spot.AlignmentID = newId;
        }
        peakStore.RemapSpotIds(oldToNew);
        return ordered;
    }

    private static void SetLightIsotopeAnalysis(IReadOnlyList<LightSpotAccumulator> accumulators) {
        foreach (var acc in accumulators) {
            acc.Spot.PeakCharacter.IsotopeParentPeakID = acc.Spot.AlignmentID;
            acc.Spot.PeakCharacter.IsotopeWeightNumber = 0;
        }
    }

    private void SetRepresentativeIsotopicPeaks(
        AnalysisFileBean file,
        IReadOnlyList<LightSpotAccumulator> accumulators,
        IReadOnlyList<RawSpectrum> spectra) {
        foreach (var acc in accumulators) {
            var representative = acc.RepresentativePeak ?? throw new InvalidOperationException("Alignment light spot has no representative peak.");
            if (representative.FileID != file.AnalysisFileId) {
                continue;
            }
            var index = spectra.LowerBound(representative.MS1RawSpectrumIdTop, (s, id) => s.Index.CompareTo(id));
            acc.Spot.IsotopicPeaks = index < 0 || index >= spectra.Count
                ? []
                : DataAccess.GetIsotopicPeaks(
                    spectra[index].Spectrum,
                    (float)representative.Mass,
                    _parameter.CentroidMs1Tolerance,
                    _parameter.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);
        }
    }

    private void FillMissingPeaks(
        AnalysisFileBean file,
        IReadOnlyList<LightSpotAccumulator> spots,
        HashSet<int> matchedSpotIds,
        LcmsGapFiller gapFiller,
        AlignmentLightPeakStore peakStore) {
        var spectra = LoadMs1Spectra(file);
        SetRepresentativeIsotopicPeaks(file, spots, spectra);
        var ms1Spectra = new Ms1Spectra(spectra, _parameter.IonMode, file.AcquisitionType);
        foreach (var acc in spots) {
            if (matchedSpotIds.Contains(acc.Spot.MasterAlignmentID)) {
                continue;
            }
            var peak = CreateAlignmentPeak(file, acc.Spot.IonMode);
            gapFiller.GapFill(ms1Spectra, peak, acc.GapFillCenter, acc.PeakWidth, acc.EstimatedNoise);
            peakStore.WriteSpotPeak(acc.Spot.MasterAlignmentID, peak);
        }
    }

    private IReadOnlyList<RawSpectrum> LoadMs1Spectra(AnalysisFileBean analysisFile) {
        var provider = _providerFactory?.Create(analysisFile);
        var spectra = provider?.LoadMs1Spectrums();
        if (spectra != null) {
            return spectra;
        }
        using var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, false, false, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt);
        return rawDataAccess.GetMeasurement()?.SpectrumList ?? [];
    }

    private static IEnumerable<MSDecResult> EnumerateRepresentativeDeconvolutions(
        IReadOnlyList<AnalysisFileBean> files,
        IReadOnlyList<LightSpotAccumulator> spots) {
        var deconvolutionInfo = new Dictionary<int, (int version, List<long> pointers, bool isAnnotationInfo)>();
        foreach (var file in files) {
            MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
            deconvolutionInfo[file.AnalysisFileId] = (version, pointers, isAnnotationInfo);
        }

        var streams = files.ToDictionary(file => file.AnalysisFileId, file => File.OpenRead(file.DeconvolutionFilePath));
        try {
            foreach (var acc in spots.OrderBy(acc => acc.Spot.MasterAlignmentID)) {
                var representative = acc.RepresentativePeak ?? throw new InvalidOperationException("Alignment light spot has no representative peak.");
                var fileId = representative.FileID;
                var peakId = representative.MasterPeakID;
                var info = deconvolutionInfo[fileId];
                yield return MsdecResultsReader.ReadMSDecResult(
                    streams[fileId], info.pointers[peakId],
                    info.version, info.isAnnotationInfo);
            }
        }
        finally {
            foreach (var stream in streams.Values) {
                stream.Close();
            }
        }
    }

    private static void SetRelativeAmplitude(IReadOnlyList<AlignmentSpotProperty> spots) {
        if (spots.Count == 0) {
            return;
        }
        var minInt = (double)spots.Min(spot => spot.HeightMin);
        var maxInt = (double)spots.Max(spot => spot.HeightMax);
        maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
        minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;
        foreach (var spot in spots) {
            var relativeValue = (float)((Math.Log(spot.HeightMax, 2) - minInt) / (maxInt - minInt));
            spot.RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
        }
    }

    private sealed class LightSpotAccumulator {
        private double _rtSum;
        private double _heightSum;
        private double _snSum;
        private double _rtMin = double.MaxValue;
        private double _rtMax = double.MinValue;
        private float _massMin = float.MaxValue;
        private float _massMax = float.MinValue;
        private float _heightMin = float.MaxValue;
        private float _heightMax = float.MinValue;
        private float _snMin = float.MaxValue;
        private float _snMax = float.MinValue;
        private double _maxHeightForMass = double.MinValue;
        private double _massAtMaxHeight;
        private readonly Dictionary<string, int> _detectedClassCounts = new Dictionary<string, int>();
        private int _detectedQcCount;

        public LightSpotAccumulator(AlignmentSpotProperty spot, int fileCount) {
            Spot = spot;
            FileCount = fileCount;
        }

        public AlignmentSpotProperty Spot { get; }
        public int FileCount { get; }
        public int DetectedCount { get; private set; }
        public double PeakWidth { get; private set; } = 0.2d;
        public float EstimatedNoise { get; private set; } = 1f;
        public AlignmentChromPeakFeature? RepresentativePeak { get; private set; }

        public ChromXs GapFillCenter => new ChromXs(new RetentionTime(_rtSum / Math.Max(1, DetectedCount), ChromXUnit.Min)) {
            Mz = new MzValue(_massAtMaxHeight),
        };

        public void AddDetectedPeak(AlignmentChromPeakFeature peak, AnalysisFileType fileType, string? className) {
            DetectedCount++;
            if (fileType == AnalysisFileType.QC) {
                _detectedQcCount++;
            }
            if (!string.IsNullOrEmpty(className)) {
                _detectedClassCounts.TryGetValue(className, out var classCount);
                _detectedClassCounts[className] = classCount + 1;
            }
            _rtSum += peak.ChromXsTop.RT.Value;
            _heightSum += peak.PeakHeightTop;
            _snSum += peak.PeakShape.SignalToNoise;
            _rtMin = Math.Min(_rtMin, peak.ChromXsTop.RT.Value);
            _rtMax = Math.Max(_rtMax, peak.ChromXsTop.RT.Value);
            _massMin = Math.Min(_massMin, (float)peak.Mass);
            _massMax = Math.Max(_massMax, (float)peak.Mass);
            _heightMin = Math.Min(_heightMin, (float)peak.PeakHeightTop);
            _heightMax = Math.Max(_heightMax, (float)peak.PeakHeightTop);
            _snMin = Math.Min(_snMin, peak.PeakShape.SignalToNoise);
            _snMax = Math.Max(_snMax, peak.PeakShape.SignalToNoise);
            PeakWidth = Math.Max(PeakWidth, peak.PeakWidth(ChromXType.RT));
            EstimatedNoise = Math.Max(EstimatedNoise, peak.PeakShape.EstimatedNoise);
            if (peak.PeakHeightTop > _maxHeightForMass) {
                _maxHeightForMass = peak.PeakHeightTop;
                _massAtMaxHeight = peak.Mass;
            }
            if (RepresentativePeak is null || IsBetterRepresentative(peak, RepresentativePeak)) {
                RepresentativePeak = peak;
            }
        }

        public bool IsDetectedInEveryQc(int qcFileCount) {
            return _detectedQcCount >= qcFileCount;
        }

        public bool IsDetectedEnoughInAnyClass(IReadOnlyDictionary<string, int> classFileCounts, double threshold) {
            foreach (var groupCount in classFileCounts) {
                _detectedClassCounts.TryGetValue(groupCount.Key, out var detected);
                if (groupCount.Value * threshold <= detected) {
                    return true;
                }
            }
            return false;
        }

        public void ApplySpotSummary() {
            var rep = RepresentativePeak ?? throw new InvalidOperationException("Alignment light spot has no representative peak.");
            Spot.RepresentativeFileID = rep.FileID;
            Spot.AlignedPeakProperties = [rep];
            Spot.IonMode = rep.IonMode;
            Spot.Name = rep.Name;
            Spot.Protein = rep.Protein;
            Spot.ProteinGroupID = rep.ProteinGroupID;
            Spot.Ontology = rep.Ontology;
            Spot.SMILES = rep.SMILES;
            Spot.InChIKey = rep.InChIKey;
            Spot.SetAdductType(CompMs.Common.DataObj.Property.AdductIon.GetAdductIon(rep.PeakCharacter.AdductType.AdductIonName));
            Spot.PeakCharacter = rep.PeakCharacter;
            Spot.Formula = rep.Formula;
            Spot.CollisionCrossSection = rep.CollisionCrossSection;
            Spot.MSRawID2MspIDs = rep.MSRawID2MspIDs;
            Spot.TextDbIDs = new List<int>(rep.TextDbIDs);
            Spot.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>(rep.MSRawID2MspBasedMatchResult);
            Spot.TextDbBasedMatchResult = rep.TextDbBasedMatchResult;
            Spot.MatchResults.MergeContainers(rep.MatchResults);
            Spot.MSDecResultIdUsed = rep.MSDecResultIdUsed;
            Spot.HeightAverage = (float)(_heightSum / DetectedCount);
            Spot.HeightMax = _heightMax;
            Spot.HeightMin = _heightMin;
            Spot.PeakWidthAverage = (float)PeakWidth;
            Spot.SignalToNoiseAve = (float)(_snSum / DetectedCount);
            Spot.SignalToNoiseMax = _snMax;
            Spot.SignalToNoiseMin = _snMin;
            Spot.EstimatedNoiseAve = EstimatedNoise;
            Spot.EstimatedNoiseMax = EstimatedNoise;
            Spot.EstimatedNoiseMin = EstimatedNoise;
            Spot.TimesMin = new ChromXs(new RetentionTime(_rtMin, ChromXUnit.Min));
            Spot.TimesMax = new ChromXs(new RetentionTime(_rtMax, ChromXUnit.Min));
            Spot.MassCenter = _massAtMaxHeight;
            Spot.MassMin = _massMin;
            Spot.MassMax = _massMax;
            Spot.FillParcentage = DetectedCount / (float)FileCount;
            Spot.MonoIsotopicPercentage = rep.PeakCharacter.IsotopeWeightNumber == 0 ? 1f : 0f;
            Spot.TimesCenter = new ChromXs {
                MainType = ChromXType.RT,
                RT = new RetentionTime(_rtSum / DetectedCount, ChromXUnit.Min),
                Mz = new MzValue(Spot.MassCenter),
            };
            if (Spot.QuantMass > 0) {
                Spot.MassCenter = Spot.QuantMass;
            }
        }

        private static bool IsBetterRepresentative(AlignmentChromPeakFeature candidate, AlignmentChromPeakFeature current) {
            var candidateScore = candidate.MatchResults?.Representative?.TotalScore ?? 0d;
            var currentScore = current.MatchResults?.Representative?.TotalScore ?? 0d;
            return (candidate.IsMsmsAssigned, candidateScore, candidate.PeakHeightTop)
                 .CompareTo((current.IsMsmsAssigned, currentScore, current.PeakHeightTop)) > 0;
        }
    }
}

internal sealed class LcmsAlignmentLightResult {
    public LcmsAlignmentLightResult(AlignmentResultContainer container, IReadOnlyList<MSDecResult> msdecResults) {
        Container = container;
        MsdecResults = msdecResults;
    }

    public AlignmentResultContainer Container { get; }
    public IReadOnlyList<MSDecResult> MsdecResults { get; }
}
