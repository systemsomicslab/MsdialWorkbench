using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment;

public abstract class GcmsPeakJoiner : IPeakJoiner
{
    public static GcmsPeakJoiner CreateRTJoiner(MsdialGcmsParameter parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IFeatureAccessor<SpectrumFeature> accessor, IProgress<int> progress = null) {
        return new GcmsRTPeakJoiner(parameter.RiCompoundType, parameter, evaluator, refer, accessor, progress);
    }

    public static GcmsPeakJoiner CreateRIJoiner(double riTol, MsdialGcmsParameter parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IFeatureAccessor<SpectrumFeature> accessor, IProgress<int> progress = null) {
        return new GcmsRIPeakJoiner(parameter.RiCompoundType, riTol, parameter, evaluator, refer, accessor, progress);
    }

    protected readonly AlignmentIndexType _indextype;
    protected readonly IComparer<IMSScanProperty> _comparer;
    private readonly AlignmentBaseParameter _alignmentParameter;
    protected readonly RiCompoundType _riCompoundType;
    protected readonly MsRefSearchParameterBase _msMatchParam;
    private readonly MsdialGcmsParameter _parameter;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
    private readonly int _binPrecision;

    protected GcmsPeakJoiner(AlignmentIndexType indextype, RiCompoundType riCompoundType, IComparer<IMSScanProperty> comparer, MsdialGcmsParameter parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        _indextype = indextype;
        _comparer = comparer;
        _alignmentParameter = parameter.AlignmentBaseParam;
        _riCompoundType = riCompoundType;
        _msMatchParam = new MsRefSearchParameterBase { Ms1Tolerance = .5f, Ms2Tolerance = .5f, MassRangeBegin = 0, MassRangeEnd = 2000, };
        _parameter = parameter;
        _evaluator = evaluator;
        _refer = refer;
        _binPrecision = _parameter.AccuracyType == AccuracyType.IsNominal ? 0 : 2;
    }

    public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor _) {
        List<SpectrumFeature> master;
        using (StageTiming.Measure(null, "alignment master list")) {
            master = GetMasterList(analysisFiles, referenceId);
        }
        List<AlignmentSpotProperty> spots;
        using (StageTiming.Measure(null, "alignment join all")) {
            spots = JoinAll(master, analysisFiles);
        }
        using var timing = StageTiming.Measure(null, "alignment reset quant masses");
        var loaders = analysisFiles.ToDictionary(f => f.AnalysisFileId, f => new MSDecLoader(f.DeconvolutionFilePath, f.DeconvolutionFilePathList));
        try {
            if (_parameter.JoinSpotsSplitByRetentionDrift && _parameter.SplitSpotJoinWindow > 0) {
                spots = JoinSpotsSplitByRetentionDrift(spots, master, loaders);
            }
            return ResetQuantMasses(spots, loaders);
        }
        finally {
            foreach (var loader in loaders.Values) {
                loader.Dispose();
            }
        }
    }

    protected abstract List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId);
    protected abstract List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles);

    /// <summary>The retention tolerance of the joiner, in the unit of its index (minutes, or RI units).</summary>
    protected abstract double RetentionTolerance { get; }

    private double RetentionOf(ChromXs chromXs) => _indextype == AlignmentIndexType.RI ? chromXs.RI.Value : chromXs.RT.Value;

    private double MasterRetention(SpectrumFeature master) => RetentionOf(master.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop);

    /// <summary>
    /// Joins the spots of one compound whose retention drifted across the files by more than the
    /// alignment tolerance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A feature joins a master only within the retention tolerance, and a feature that no master
    /// takes makes a master of its own. When a compound's retention drifts over the files by more than
    /// the tolerance, its features end up in two spots. Each spot then gap-fills the files whose peak
    /// is in the other, and the gap filler integrates whatever lies at the quant mass near the spot's
    /// centre: noise, or a neighbouring compound.
    /// </para>
    /// <para>
    /// Two spots are joined when all of these hold:
    /// their masters are more than the retention tolerance apart, so that retention, not spectrum,
    /// is what kept the joiner from putting their features together; their centres (the mean
    /// retention of their detected peaks) are within <see cref="MsdialGcmsParameter.SplitSpotJoinWindow"/>
    /// times the retention tolerance (2 by default); no file has a detected peak in both, so that no file's peak is dropped; the EI
    /// spectra of their representative peaks pass the dot-product cutoffs the joiner's comparison
    /// carries (weighted and simple 0.6, reverse 0.8, matched peaks 25% and at least 3); each is the
    /// other's best such match by the integrated EI similarity; and their masters are not matched to
    /// two different references. The spot with more detected peaks keeps its master and takes the
    /// other's peaks. This repeats until no pair is left, so a compound split three ways is joined
    /// as well.
    /// </para>
    /// <para>
    /// The test is not the joiner's own (<see cref="IsSimilarTo"/>): its spectrum similarity tolerance
    /// (0.5 on the integrated EI similarity for GC-MS) is passed by spectra of two different
    /// compounds, and a pair of such spots, each detected in other files, would be joined into one.
    /// </para>
    /// <para>
    /// Two masters within the tolerance of each other are two masters because the joiner found their
    /// spectra different. Their spots are left apart even when their representative spectra match:
    /// on the Kovats demo (RI) such a pair, masters 3 RI units apart, was joined; the quant mass
    /// chosen over the joined peaks was that of an unannotated spot 1 RI unit away, and the alignment
    /// refiner removed the joined spot as that spot's duplicate, with all seven of its detected peaks.
    /// </para>
    /// </remarks>
    private List<AlignmentSpotProperty> JoinSpotsSplitByRetentionDrift(List<AlignmentSpotProperty> spots, List<SpectrumFeature> masters, Dictionary<int, MSDecLoader> loaders) {
        var window = _parameter.SplitSpotJoinWindow * RetentionTolerance;
        var alive = spots.Select((spot, i) => (Spot: spot, Master: masters[i])).ToList();
        var loaded = new Dictionary<(int File, int Id), MSDecResult>();
        MSDecResult? Representative(List<AlignmentChromPeakFeature> detected) {
            var peak = DataObjConverter.GetRepresentativePeak(detected);
            if (peak is null) {
                return null;
            }
            var key = (peak.FileID, peak.GetMSDecResultID());
            if (!loaded.TryGetValue(key, out var result)) {
                loaded[key] = result = loaders[peak.FileID].LoadMSDecResult(key.Item2);
            }
            return result;
        }
        while (true) {
            var members = alive.Select(s => s.Spot.AlignedPeakProperties.Where(p => p.MasterPeakID >= 0).ToList()).ToList();
            var representatives = members.Select(Representative).ToList();
            var files = members.Select(ps => new HashSet<int>(ps.Select(p => p.FileID))).ToList();
            var centres = members.Select(ps => ps.Count == 0 ? double.NaN : ps.Average(p => RetentionOf(p.ChromXsTop))).ToList();
            var order = Enumerable.Range(0, alive.Count).Where(i => files[i].Count > 0).OrderBy(i => centres[i]).ToArray();

            var best = Enumerable.Repeat((Partner: -1, Similarity: double.MinValue), alive.Count).ToArray();
            for (var a = 0; a < order.Length; a++) {
                for (var b = a + 1; b < order.Length && centres[order[b]] - centres[order[a]] <= window; b++) {
                    int i = order[a], j = order[b];
                    if (Math.Abs(MasterRetention(alive[i].Master) - MasterRetention(alive[j].Master)) <= RetentionTolerance
                        || files[i].Overlaps(files[j]) || !AreSpectraOfOneCompound(representatives[i], representatives[j], alive[i].Master, alive[j].Master, out var similarity)) {
                        continue;
                    }
                    if (similarity > best[i].Similarity) best[i] = (j, similarity);
                    if (similarity > best[j].Similarity) best[j] = (i, similarity);
                }
            }

            var gone = new bool[alive.Count];
            var joined = false;
            for (var i = 0; i < alive.Count; i++) {
                var j = best[i].Partner;
                if (j <= i || best[j].Partner != i) {
                    continue;
                }
                var (keep, give) = files[i].Count >= files[j].Count ? (i, j) : (j, i);
                var peaks = alive[keep].Spot.AlignedPeakProperties;
                foreach (var peak in members[give]) {
                    peaks[peaks.FindIndex(p => p.FileID == peak.FileID)] = peak;
                }
                gone[give] = true;
                joined = true;
            }
            if (!joined) {
                return alive.Select(s => s.Spot).ToList();
            }
            alive = alive.Where((_, i) => !gone[i]).ToList();
        }
    }

    private bool AreSpectraOfOneCompound(MSDecResult? xSpectrum, MSDecResult? ySpectrum, SpectrumFeature x, SpectrumFeature y, out double similarity) {
        similarity = double.MinValue;
        if (xSpectrum is null || ySpectrum is null) {
            return false;
        }
        // The overload without an EI threshold: IsSpectrumMatch is the dot-product cutoffs of _msMatchParam alone.
        var result = MsScanMatching.CompareEIMSScanProperties(xSpectrum, ySpectrum, _msMatchParam,
            _alignmentParameter.RetentionTimeAlignmentTolerance, _parameter.RetentionIndexAlignmentTolerance,
            _alignmentParameter.SpectrumSimilarityAlignmentFactor, _alignmentParameter.RetentionTimeAlignmentFactor, _indextype == AlignmentIndexType.RI);
        similarity = MsScanMatching.GetIntegratedSpectraSimilarity(result);
        if (!result.IsSpectrumMatch) {
            return false;
        }
        if (_evaluator is not null
            && x.AnnotatedMSDecResult.IsReferenceMatched(_evaluator) && y.AnnotatedMSDecResult.IsReferenceMatched(_evaluator)
            && x.AnnotatedMSDecResult.MatchResults.Representative.LibraryID != y.AnnotatedMSDecResult.MatchResults.Representative.LibraryID) {
            return false;
        }
        return true;
    }

    private List<AlignmentSpotProperty> ResetQuantMasses(List<AlignmentSpotProperty> spots, Dictionary<int, MSDecLoader> loaders) {
        var results = new List<AlignmentSpotProperty>(spots.Count);
        foreach (var spot in spots) {
            var repPeak = DataObjConverter.GetRepresentativePeak(spot.AlignedPeakProperties.Where(p => p.MasterPeakID >= 0).ToList());
            if (repPeak is null) {
                continue;
            }
            // The library quant mass is read from the representative peak's annotation, as MS-DIAL 4 read it from the
            // representative file's identification. The spot itself carries no match results yet: PeakAligner copies them
            // from that peak (DataObjConverter.SetRepresentativeProperty) after gap filling, so a test on the spot here was
            // always false and the library quant mass never reached the spot. It takes precedence over the base peak and
            // over the vote below, as in MS-DIAL 4.
            if (_parameter.IsReplaceQuantmassByUserDefinedValue && IsReferenceMatched(repPeak)) {
                var reference = _refer?.Refer(repPeak.MatchResults.Representative);
                if (reference is not null && reference.QuantMass > 0 && _parameter.PeakPickBaseParam.IsInMassRange(reference.QuantMass)) {
                    spot.MassCenter = reference.QuantMass;
                    spot.QuantMass = reference.QuantMass;
                    results.Add(spot);
                    continue;
                }
            }
            var rep = repPeak.FileID;
            var loader = loaders[rep];
            var msdec = loader.LoadMSDecResult(spot.AlignedPeakProperties[rep].GetMSDecResultID());

            if (_parameter.IsRepresentativeQuantMassBasedOnBasePeakMz) {
                spot.QuantMass = msdec.Spectrum.Argmax(s => s.Intensity).Mass;
                spot.MassCenter = spot.QuantMass;
                results.Add(spot);
                continue;
            }

            var maxIntensity = spot.AlignedPeakProperties.Max(p => p.PeakHeightTop);
            var group = spot.AlignedPeakProperties
                .Where(p => p.MasterPeakID >= 0 && p.PeakHeightTop / maxIntensity >= .1)
                .GroupBy(p => Math.Round(p.Mass, _binPrecision))
                .Argmax(g => g.Count());
            var quantMassCandidate = group.Average(p => p.Mass);
            
            if (QuantMassExists(quantMassCandidate, msdec)) {
                spot.QuantMass = quantMassCandidate;
            }
            else if (QuantMassExists(repPeak.Mass, msdec)) {
                spot.QuantMass = repPeak.Mass;
            }
            else {
                spot.QuantMass = msdec.Spectrum.Argmax(s => s.Intensity).Mass;
            }
            spot.MassCenter = spot.QuantMass;
            results.Add(spot);
        }
        var counter = 0;
        foreach (var spot in results) { spot.MasterAlignmentID = counter; spot.AlignmentID = counter; counter++; }

        return results;
    }

    // The test AlignmentSpotProperty.IsReferenceMatched applies to a spot, applied to one aligned peak.
    private bool IsReferenceMatched(AlignmentChromPeakFeature peak) {
        return peak.IsReferenceMatched() || peak.MatchResults.IsReferenceMatched(_evaluator);
    }

    private bool QuantMassExists(double quantMass, MSDecResult result) {
        var bin = _parameter.PeakPickBaseParam.CentroidMs1Tolerance;
        var maxIntensity = result.Spectrum.DefaultIfEmpty().Max(s => s?.Intensity ?? 0d);
        foreach (var peak in result.Spectrum) {
            if (quantMass - peak.Mass > bin) {
                continue;
            }
            if (peak.Mass - quantMass > bin) {
                break;
            }
            var diff = Math.Abs(peak.Mass - quantMass);
            if (diff <= bin && peak.Intensity > maxIntensity * .1d) {
                return true;
            }
        }
        return false;
    }

    /// <remarks>
    /// Retention and spectrum are judged separately: two features are similar when they are within
    /// the retention tolerance and their integrated EI similarity exceeds the spectrum similarity
    /// tolerance, and only then. <see cref="MsScanMatchResult.IsSpectrumMatch"/>, which the
    /// comparison also sets when the fixed dot-product cutoffs of <see cref="_msMatchParam"/> pass,
    /// is not read: those cutoffs are not settings, and a pair that passes them has an integrated
    /// similarity of at least 0.608, so with them any tolerance above that was not binding. The total
    /// score only chooses the best master (<see cref="GetSimilality"/>). Two spectra of one compound
    /// in two files of a GC-MS run commonly score 0.6-0.75 on the integrated similarity, because
    /// every small peak present in one spectrum lowers the matched-peaks term, so a tolerance of 0.7
    /// or more splits most compounds; the GC-MS default is 0.5.
    /// </remarks>
    protected bool IsSimilarTo(SpectrumFeature x, SpectrumFeature y) {
        var result = CompareSpectra(x, y);
        var isRetentionMatch = _indextype == AlignmentIndexType.RI ? result.IsRiMatch : result.IsRtMatch;
        return isRetentionMatch && MsScanMatching.GetIntegratedSpectraSimilarity(result) > _alignmentParameter.SpectrumSimilarityAlignmentTolerance;
    }

    protected double GetSimilality(SpectrumFeature x, SpectrumFeature y) {
        return CompareSpectra(x, y).TotalScore;
    }

    /// <remarks>
    /// The threshold and the EI weight are the alignment's spectrum similarity settings ("Spectrum
    /// similarity tolerance / factor for alignment" in a method file, "EI similarity tolerance /
    /// factor" in the dialog), not its MS1 settings: "MS1 tolerance for alignment" is an m/z
    /// tolerance (0.015 in a method file written like an LC-MS one), and as a similarity threshold
    /// it admits any spectrum within the retention window.
    /// </remarks>
    private MsScanMatchResult CompareSpectra(SpectrumFeature x, SpectrumFeature y) {
        return MsScanMatching.CompareEIMSScanProperties(x.AnnotatedMSDecResult.MSDecResult, y.AnnotatedMSDecResult.MSDecResult, _msMatchParam,
            _alignmentParameter.RetentionTimeAlignmentTolerance, _parameter.RetentionIndexAlignmentTolerance,
            _alignmentParameter.SpectrumSimilarityAlignmentFactor, _alignmentParameter.RetentionTimeAlignmentFactor, _indextype == AlignmentIndexType.RI);
    }

    // The joiners place a peak in a bucket, sort the masters, open the search window and stop the scan on the
    // retention of the deconvoluted (MSDec) result, because that is the retention IsSimilarTo and GetSimilality
    // compare. The top of the quantified (quant-mass) chromatogram peak can lie outside the tolerance of a master
    // while the MSDec retention lies inside it; a window on the quant top then never compared the two, and the peak
    // joined no spot. MS-DIAL 4 windowed, sorted and scored on the MS1Dec retention as well.
    protected static double GetRetentionTime(SpectrumFeature feature) {
        return feature.AnnotatedMSDecResult.MSDecResult.ChromXs.RT.Value;
    }

    protected static double GetRetentionIndex(SpectrumFeature feature) {
        return feature.AnnotatedMSDecResult.MSDecResult.ChromXs.RI.Value;
    }

    // A peak of a non-reference file that matches no master becomes a new master only if its height is above 0.1% of
    // the tallest master, as in MS-DIAL 4 (PeakAlignment.addJointAlignerMasterListByRetentionTime/-Index:
    // maxIntensity * 0.001 < BasepeakHeight). The tallest master is taken once per file, before the file's peaks are
    // read: the masters of the reference file and those added by earlier files count, the file's own new masters do
    // not. The reference file's peaks all become masters, whatever their height. Without the floor every unmatched
    // noise peak of every file became a master, and the number of spots grew with the number of files.
    private const double MasterHeightFloorRatio = 0.001;

    protected static double GetMasterHeightFloor(IDictionary<int, List<SpectrumFeature>> master) {
        var maxHeight = double.NegativeInfinity;
        foreach (var features in master.Values) {
            foreach (var feature in features) {
                var height = GetHeight(feature);
                if (height > maxHeight) {
                    maxHeight = height;
                }
            }
        }
        // No master to compare with (MS-DIAL 4 failed here on an empty master list): no floor.
        return double.IsNegativeInfinity(maxHeight) ? double.NegativeInfinity : maxHeight * MasterHeightFloorRatio;
    }

    protected static bool IsAboveMasterHeightFloor(SpectrumFeature feature, double floor) {
        return GetHeight(feature) > floor;
    }

    // MS-DIAL 4's BasepeakHeight is the intensity of the deconvoluted model m/z chromatogram at the deconvoluted peak
    // top (MS1Dec.ms1DecResultProperty). MSDecResult.ModelPeakHeight is the same value, computed the same way
    // (MSDecProcess.ms1DecResultProperty), and, like the retention the joiners compare, it belongs to the MSDec result.
    // The height of the quantified chromatogram peak is a different measurement: the smoothed raw chromatogram of the
    // quant mass, which is the library's quant mass for an annotated peak when IsReplaceQuantmassByUserDefinedValue is
    // set.
    private static double GetHeight(SpectrumFeature feature) {
        return feature.AnnotatedMSDecResult.MSDecResult.ModelPeakHeight;
    }

    protected List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<SpectrumFeature> masters, IEnumerable<AnalysisFileBean> analysisFiles, ChromXType mainType) {
        var masterId = 0;
        return InitSpots(masters, analysisFiles, mainType, ref masterId);
    }

    protected List<AlignmentSpotProperty> InitSpots(IEnumerable<SpectrumFeature> scanProps, IEnumerable<AnalysisFileBean> analysisFiles, ChromXType mainType, ref int masterId) {
        if (scanProps is null) return [];

        var spots = new List<AlignmentSpotProperty>();
        foreach ((var scanProp, var localId) in scanProps.WithIndex()) {
            var spot = new AlignmentSpotProperty
            {
                MasterAlignmentID = masterId++,
                AlignmentID = localId,
                TimesCenter = scanProp.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop,
                MassCenter = scanProp.QuantifiedChromatogramPeak.PeakFeature.Mass,
                IonMode = scanProp.AnnotatedMSDecResult.MSDecResult.IonMode,
                InternalStandardAlignmentID = -1
            };
            spot.TimesCenter.MainType = mainType;

            var peaks = new List<AlignmentChromPeakFeature>();
            foreach (var file in analysisFiles) {
                peaks.Add(new AlignmentChromPeakFeature
                {
                    MasterPeakID = -1,
                    PeakID = -1,
                    FileID = file.AnalysisFileId,
                    FileName = file.AnalysisFileName,
                    IonMode = scanProp.AnnotatedMSDecResult.MSDecResult.IonMode,
                });
            }
            spot.AlignedPeakProperties = peaks;
            spot.AlignmentDriftSpotFeatures = [];
            spots.Add(spot);
        }

        return spots;
    }
}

internal sealed class GcmsRTPeakJoiner : GcmsPeakJoiner
{
    private readonly double _rtTol;
    private readonly double _rtBucket;
    private readonly int _rtWidth;
    private readonly IFeatureAccessor<SpectrumFeature> _accessor;
    private readonly IProgress<int> _progress;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly AlignmentBaseParameter _alignmentParameter;

    public GcmsRTPeakJoiner(RiCompoundType riCompoundType, MsdialGcmsParameter parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IFeatureAccessor<SpectrumFeature> accessor, IProgress<int> progress = null)
        : base(AlignmentIndexType.RT, riCompoundType, ChromXsComparer.RTComparer, parameter, evaluator, refer) {
        _alignmentParameter = parameter.AlignmentBaseParam;
        _rtTol = parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance;
        _rtBucket = parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance * 2;
        _rtWidth = (int)Math.Ceiling(_rtTol / _rtBucket);
        _evaluator = evaluator;
        _accessor = accessor;
        _progress = progress;
    }

    protected override double RetentionTolerance => _rtTol;

    protected override List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceID) {
        var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceID);
        if (referenceFile is null) {
            return [];
        }
        IEnumerable<SpectrumFeature> spectrumFeatures = _accessor.GetMSScanProperties(referenceFile);
        if (_alignmentParameter.UseRefMatchedPeaksOnly) {
            spectrumFeatures = spectrumFeatures.Where(s => s.AnnotatedMSDecResult.IsReferenceMatched(_evaluator));
        }
        var master = spectrumFeatures
            .GroupBy(prop => (int)Math.Ceiling(GetRetentionTime(prop) / _rtBucket))
            .ToDictionary(group => group.Key, group => group.ToList());

        var reporter = ReportProgress.FromLength(_progress, 0d, 20d);
        var counter = 0;
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count);
            if (analysisFile.AnalysisFileId == referenceID) {
                continue;
            }
            IEnumerable<SpectrumFeature> target = _accessor.GetMSScanProperties(analysisFile);
            if (_alignmentParameter.UseRefMatchedPeaksOnly) {
                target = target.Where(s => s.AnnotatedMSDecResult.IsReferenceMatched(_evaluator));
            }
            MergeSpectrumFeatures(master, target);
        }
        return master.Values.SelectMany(props => props).OrderBy(prop => (GetRetentionTime(prop), prop.QuantifiedChromatogramPeak.PeakFeature.Mass)).ToList();
    }

    private void MergeSpectrumFeatures(IDictionary<int, List<SpectrumFeature>> master, IEnumerable<SpectrumFeature> targets) {
        // A file's peaks are compared only with the masters that existed before the file; the peaks that become new
        // masters are added after the whole file has been read. Adding each one at once made a similar later peak of the
        // same file look already covered, so that peak became no master and joined no spot (MS-DIAL 4 collected the new
        // masters in addedMs1DecResults and appended them after the file).
        var heightFloor = GetMasterHeightFloor(master);
        var added = new List<SpectrumFeature>();
        foreach (var target in targets) {
            SetToMaster(master, target, added, heightFloor);
        }
        foreach (var target in added) {
            AddToMaster(master, target);
        }
    }

    private void AddToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target) {
        var rtTarget = (int)Math.Ceiling(GetRetentionTime(target) / _rtBucket);
        if (!master.ContainsKey(rtTarget)) {
            master[rtTarget] = [];
        }
        master[rtTarget].Add(target);
    }

    private bool SetToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target, List<SpectrumFeature> added, double heightFloor) {
        var rtTarget = (int)Math.Ceiling(GetRetentionTime(target) / _rtBucket);
        for(int rtIdc = rtTarget - _rtWidth; rtIdc <= rtTarget + _rtWidth; rtIdc++) { // in many case, rtIdc is from rtTarget - 1 to rtTarget + 1
            if (master.ContainsKey(rtIdc)) {
                for (int i = 0; i < master[rtIdc].Count; i++) {
                    var candidate = master[rtIdc][i];
                    if (IsSimilarTo(candidate, target)) {
                        if (!candidate.AnnotatedMSDecResult.IsReferenceMatched(_evaluator) && target.AnnotatedMSDecResult.IsReferenceMatched(_evaluator)) {
                            master[rtIdc].RemoveAt(i);
                            if (!master.ContainsKey(rtTarget)) {
                                master[rtTarget] = [];
                            }
                            master[rtTarget].Add(target);
                            return true;
                        }
                        return false;
                    }
                }
            }
        }
        if (!IsAboveMasterHeightFloor(target, heightFloor)) {
            return false;
        }
        added.Add(target);
        return true;
    }

    protected override List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {
        var result = GetSpots(master, analysisFiles, ChromXType.RT);
        var counter = 0;
        var reporter = ReportProgress.FromLength(_progress, 20d, 20d);
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count);
            var spectrumFeatures = _accessor.GetMSScanProperties(analysisFile);
            AlignPeaksToMaster(result, master, spectrumFeatures, analysisFile.AnalysisFileId);
        }
        return result;
    }

    private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<SpectrumFeature> masters, IReadOnlyList<SpectrumFeature> targets, int fileId) {
        var n = masters.Count;
        var maxMatchs = new double[n];

        foreach (var target in targets) {
            int? matchIdx = null;
            double matchFactor = double.MinValue;
            var lo = SearchCollection.LowerBound(masters, (GetRetentionTime(target) - _rtTol, target.QuantifiedChromatogramPeak.PeakFeature.Mass), (s, p) => (GetRetentionTime(s), s.QuantifiedChromatogramPeak.PeakFeature.Mass).CompareTo(p));
            for (var i = lo; i < n; i++) {
                if (GetRetentionTime(target) + _rtTol < GetRetentionTime(masters[i])) {
                    break;
                }
                if (!IsSimilarTo(masters[i], target)) {
                    continue;
                }
                var factor = GetSimilality(masters[i], target);
                if (factor > maxMatchs[i] && factor > matchFactor) {
                    matchIdx = i;
                    matchFactor = factor;
                }
            }
            if (matchIdx.HasValue) {
                // Only the master the peak joins records its score. Recording every master that was the best so far
                // while scanning left a claim on masters the peak did not join, which then refused the file's other
                // peaks that matched them less well (MS-DIAL 4 recorded the chosen master only).
                maxMatchs[matchIdx.Value] = matchFactor;
                DataObjConverter.SetAlignmentChromPeakFeatureFromSpectrumFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target, ChromXType.RT);
            }
        }
    }
}

internal sealed class GcmsRIPeakJoiner : GcmsPeakJoiner
{
    private readonly double _riTol;
    private readonly IFeatureAccessor<SpectrumFeature> _accessor;
    private readonly IProgress<int> _progress;
    private readonly double _riBucket;
    private readonly int _riWidth;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly AlignmentBaseParameter _alignmentParameter;

    public GcmsRIPeakJoiner(RiCompoundType riCompoundType, double riTol, MsdialGcmsParameter parameter, IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IFeatureAccessor<SpectrumFeature> accessor, IProgress<int> progress = null)
        : base(AlignmentIndexType.RI, riCompoundType, ChromXsComparer.RIComparer, parameter, evaluator, refer) {
        _alignmentParameter = parameter.AlignmentBaseParam;
        _evaluator = evaluator;
        _riTol = riTol;
        _riBucket = riTol * 2;
        _riWidth = (int)Math.Ceiling(_riTol / _riBucket);
        _accessor = accessor;
        _progress = progress;
    }

    protected override double RetentionTolerance => _riTol;

    protected override List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId) {

        var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
        if (referenceFile is null) {
            return [];
        }

        IEnumerable<SpectrumFeature> spectrumFeatures = _accessor.GetMSScanProperties(referenceFile);
        if (_alignmentParameter.UseRefMatchedPeaksOnly) {
            spectrumFeatures = spectrumFeatures.Where(s => s.AnnotatedMSDecResult.IsReferenceMatched(_evaluator));
        }
        var master = spectrumFeatures
            .GroupBy(spectrum => (int)Math.Ceiling(GetRetentionIndex(spectrum) / _riBucket))
            .ToDictionary(group => group.Key, group => group.ToList());

        var counter = 0;
        var reporter = ReportProgress.FromLength(_progress, 0d, 20d);
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count);
            if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId) {
                continue;
            }
            IEnumerable<SpectrumFeature> target = _accessor.GetMSScanProperties(analysisFile);
            if (_alignmentParameter.UseRefMatchedPeaksOnly) {
                target = target.Where(s => s.AnnotatedMSDecResult.IsReferenceMatched(_evaluator));
            }
            MergeChromatogramPeaks(master, target);
        }

        // AlignPeaksToMaster binary-searches this list on (RI, Mass), so it must be sorted on that key.
        return master.Values.SelectMany(props => props).OrderBy(prop => (GetRetentionIndex(prop), prop.QuantifiedChromatogramPeak.PeakFeature.Mass)).ToList();
    }

    private void MergeChromatogramPeaks(IDictionary<int, List<SpectrumFeature>> master, IEnumerable<SpectrumFeature> targets) {
        // A file's peaks are compared only with the masters that existed before the file; the peaks that become new
        // masters are added after the whole file has been read. Adding each one at once made a similar later peak of the
        // same file look already covered, so that peak became no master and joined no spot (MS-DIAL 4 collected the new
        // masters in addedMs1DecResults and appended them after the file).
        var heightFloor = GetMasterHeightFloor(master);
        var added = new List<SpectrumFeature>();
        foreach (var target in targets) {
            SetToMaster(master, target, added, heightFloor);
        }
        foreach (var target in added) {
            AddToMaster(master, target);
        }
    }

    private void AddToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target) {
        var riTarget = (int)Math.Ceiling(GetRetentionIndex(target) / _riBucket);
        if (!master.ContainsKey(riTarget)) {
            master[riTarget] = [];
        }
        master[riTarget].Add(target);
    }

    private bool SetToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target, List<SpectrumFeature> added, double heightFloor) {
        var riTarget = (int)Math.Ceiling(GetRetentionIndex(target) / _riBucket);
        for(int riIdc = riTarget - _riWidth; riIdc <= riTarget + _riWidth; riIdc++) { // in many case, riIdc is from riTarget - 1 to riTarget + 1
            if (master.ContainsKey(riIdc)) {
                foreach (var candidate in master[riIdc]) {
                    if (IsSimilarTo(candidate, target)) {
                        return false;
                    }
                }
            }
        }
        if (!IsAboveMasterHeightFloor(target, heightFloor)) {
            return false;
        }
        added.Add(target);
        return true;
    }

    protected override List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {
        var result = GetSpots(master, analysisFiles, ChromXType.RI);

        var counter = 0;
        var reporter = ReportProgress.FromLength(_progress, 20d, 20d);
        foreach (var analysisFile in analysisFiles) {
            reporter.Report(++counter, analysisFiles.Count);
            var spectrums = _accessor.GetMSScanProperties(analysisFile);
            AlignPeaksToMaster(result, master, spectrums, analysisFile.AnalysisFileId);
        }
        
        return result;
    }

    private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<SpectrumFeature> masters, IReadOnlyList<SpectrumFeature> targets, int fileId) {
        var maxMatchs = new double[masters.Count];
        foreach (var target in targets) {
            int? matchIdx = null;
            double matchFactor = double.MinValue;
            var lo = SearchCollection.LowerBound(masters, (GetRetentionIndex(target) - _riTol, target.QuantifiedChromatogramPeak.PeakFeature.Mass), (s, p) => (GetRetentionIndex(s), s.QuantifiedChromatogramPeak.PeakFeature.Mass).CompareTo(p));
            for (var i = lo; i < masters.Count; i++) {
                if (GetRetentionIndex(target) + _riTol < GetRetentionIndex(masters[i])) {
                    break;
                }
                if (!IsSimilarTo(masters[i], target)) {
                    continue;
                }
                var factor = GetSimilality(masters[i], target);
                if (factor > maxMatchs[i] && factor > matchFactor) {
                    matchIdx = i;
                    matchFactor = factor;
                }
            }
            if (matchIdx.HasValue) {
                // Only the master the peak joins records its score. Recording every master that was the best so far
                // while scanning left a claim on masters the peak did not join, which then refused the file's other
                // peaks that matched them less well (MS-DIAL 4 recorded the chosen master only).
                maxMatchs[matchIdx.Value] = matchFactor;
                DataObjConverter.SetAlignmentChromPeakFeatureFromSpectrumFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target, ChromXType.RI);
            }
        }
    }
}
