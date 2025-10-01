using CompMs.Common.Algorithm.PeakPick;
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
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialDimsCore;

public sealed class ProcessFile : IFileProcessor {
    private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
    private readonly IMsdialDataStorage<MsdialDimsParameter> _storage;
    private readonly IAnnotationProcess _annotationProcess;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

    public ProcessFile(IDataProviderFactory<AnalysisFileBean> providerFactory, IMsdialDataStorage<MsdialDimsParameter> storage, IAnnotationProcess annotationProcess, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        _providerFactory = providerFactory;
        _storage = storage;
        _annotationProcess = annotationProcess;
        _evaluator = evaluator;
    }

    public async Task RunAsync(AnalysisFileBean file, ProcessOption option, IProgress<int>? progress, CancellationToken token) {
        if (!option.HasFlag(ProcessOption.PeakSpotting) && !option.HasFlag(ProcessOption.Identification)) {
            return;
        }

        var parameter = _storage.Parameter;
        // parse raw data
        Console.WriteLine("Loading spectral information");
        var provider = _providerFactory.Create(file);

        var (peakFeatures, msdecResults) = option.HasFlag(ProcessOption.PeakSpotting)
            ? await FindPeaksAndScans(file, parameter, provider, progress, token).ConfigureAwait(false)
            : await LoadPeaksAndScans(file, provider, token).ConfigureAwait(false);
        if (peakFeatures is null || msdecResults is null) {
            return;
        }

        if (option.HasFlag(ProcessOption.Identification)) {
            Console.WriteLine("Annotation started");
            var reporter = ReportProgress.FromRange(progress, 60d, 90d);
            await _annotationProcess.RunAnnotationAsync(peakFeatures.Items, msdecResults, provider, parameter.NumThreads, reporter.Report, token).ConfigureAwait(false);
        }
        var characterEstimator = new Algorithm.PeakCharacterEstimator();
        characterEstimator.Process(file, peakFeatures.Items, msdecResults.MSDecResults, _evaluator, parameter, provider, ReportProgress.FromLength(progress, initialProgress: 90d, progressLength: 10d));

        await peakFeatures.SerializeAsync(file, token).ConfigureAwait(false);

        progress?.Report(100);
    }

    private async Task<(ChromatogramPeakFeatureCollection peakFeatures, MSDecResultCollection msdecResults)> LoadPeaksAndScans(AnalysisFileBean file, IDataProvider provider, CancellationToken token) {
        var peakFeaturesTask = file.LoadChromatogramPeakFeatureCollectionAsync(token);
        var msdecResultssTask = MSDecResultCollection.DeserializeAsync(file, token);

        var peakFeatures = await peakFeaturesTask.ConfigureAwait(false);
        peakFeatures.ClearMatchResultProperties();
        var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
        var msdecResultss = await Task.WhenAll(msdecResultssTask).ConfigureAwait(false);
        var msdecResults = msdecResultss.FirstOrDefault(results => results.CollisionEnergy == targetCE) ?? msdecResultss.First();
        return (peakFeatures, msdecResults);
    }

    private async Task<(ChromatogramPeakFeatureCollection PeakFeatures, MSDecResultCollection MsdecResults)> FindPeaksAndScans(AnalysisFileBean file, MsdialDimsParameter param, IDataProvider provider, IProgress<int> progress, CancellationToken token) {
        // faeture detections
        Console.WriteLine("Peak picking started");
        var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.Spectrum.Length);
        var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
        var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();

        var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
        if (peakPickResults.IsEmptyOrNull()) return default;
        var peakFeatures_ = ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider, file.AcquisitionType);
        var peakFeatures = new ChromatogramPeakFeatureCollection(peakFeatures_);

        if (peakFeatures.Items.Count == 0) return default;
        // IsotopeEstimator.Process(peakFeatures, param, iupacDB); // in dims, skip the isotope estimation process.
        SetIsotopes(peakFeatures.Items);
        SetSpectrumPeaks(peakFeatures.Items, provider);

        // chrom deconvolutions
        Console.WriteLine("Deconvolution started");
        var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, peakFeatures.Items);
        var msdecProcess = new Algorithm.Ms2Dec();
        var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
        ReportProgress reporter = ReportProgress.FromLength(progress, initialProgress: 30d, progressLength: 30d);
        var msdecResults_ = msdecProcess.GetMS2DecResults(provider, peakFeatures_, param, summary, reporter, targetCE);
        var msdecResults = new MSDecResultCollection(msdecResults_, targetCE);
        await msdecResults.SerializeAsync(file, token).ConfigureAwait(false);
        return (peakFeatures, msdecResults);
    }

    public static List<ChromatogramPeakFeature> ConvertPeaksToPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, IDataProvider provider, AcquisitionType type) {
        var peakFeatures = new List<ChromatogramPeakFeature>();
        var ms2SpecObjects = provider.LoadMsNSpectrums(level: 2)
            .Where(spectra => spectra.MsLevel == 2 && spectra.Precursor != null)
            .OrderBy(spectra => spectra.Precursor.SelectedIonMz).ToList();
        IonMode ionMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? IonMode.Positive : IonMode.Negative;

        foreach (var result in peakPickResults) {
            var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz, ionMode);
            var chromScanID = peakFeature.PeakFeature.ChromScanIdTop;

            IChromatogramPeakFeature peak = peakFeature;
            peak.Mass = ms1Spectrum.Spectrum[chromScanID].Mz;
            peak.ChromXsTop = new ChromXs(peakFeature.PeakFeature.Mass, ChromXType.Mz, ChromXUnit.Mz);

            peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.Index;
            peakFeature.ScanID = ms1Spectrum.ScanNumber;
            switch (type) {
                case AcquisitionType.AIF:
                case AcquisitionType.SWATH:
                    peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDsDIA(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                    break;
                case AcquisitionType.DDA:
                    peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDsDDA(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                    break;
                default:
                    throw new NotSupportedException(nameof(type));
            }
            peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, provider);
            peakFeatures.Add(peakFeature);

#if DEBUG
            // check result
            Console.WriteLine($"Peak ID = {peakFeature.PeakID}, Scan ID = {peakFeature.PeakFeature.ChromScanIdTop}, MSSpecID = {peakFeature.PeakFeature.ChromXsTop.Mz.Value}, Height = {peakFeature.PeakFeature.PeakHeightTop}, Area = {peakFeature.PeakFeature.PeakAreaAboveZero}");
#endif
        }

        return peakFeatures;
    }

    /// <summary>
    /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
    /// the mass tolerance is considered by the basic quadrupole mass resolution.
    /// </summary>
    /// <param name="precursorMz"></param>
    /// <param name="ms2SpecObjects"></param>
    /// <param name="mzTolerance"></param>
    /// <returns></returns>
    /// 
    private static Dictionary<int, double> GetMS2RawSpectrumIDsDIA(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
        var ID2CE = new Dictionary<int, double>();
        int startID = SearchCollection.LowerBound(
            ms2SpecObjects,
            new RawSpectrum { Precursor = new RawPrecursorIon { IsolationTargetMz = precursorMz - mzTolerance, IsolationWindowUpperOffset = 0, } },
            (x, y) => (x.Precursor.IsolationTargetMz + x.Precursor.IsolationWindowUpperOffset).CompareTo(y.Precursor.IsolationTargetMz + y.Precursor.IsolationWindowUpperOffset));
        
        for (int i = startID; i < ms2SpecObjects.Count; i++) {
            var spec = ms2SpecObjects[i];
            if (spec.Precursor.IsolationTargetMz - precursorMz < - spec.Precursor.IsolationWindowUpperOffset - mzTolerance) continue;
            if (spec.Precursor.IsolationTargetMz - precursorMz > spec.Precursor.IsolationWindowLowerOffset + mzTolerance) break;

            ID2CE[spec.Index] = spec.CollisionEnergy;
        }
        return ID2CE; /// maybe, in msmsall, the id count is always one but for just in case
    }

    /// <summary>
    /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
    /// the mass tolerance is considered by the basic quadrupole mass resolution.
    /// </summary>
    /// <param name="precursorMz"></param>
    /// <param name="ms2SpecObjects"></param>
    /// <param name="mzTolerance"></param>
    /// <returns></returns>
    /// 
    private static Dictionary<int, double> GetMS2RawSpectrumIDsDDA(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
        var ID2CE = new Dictionary<int, double>();
        int startID = SearchCollection.LowerBound(
            ms2SpecObjects,
            new RawSpectrum { Precursor = new RawPrecursorIon { IsolationTargetMz = precursorMz - mzTolerance, IsolationWindowUpperOffset = 0, } },
            (x, y) => (x.Precursor.IsolationTargetMz).CompareTo(y.Precursor.IsolationTargetMz));
        
        for (int i = startID; i < ms2SpecObjects.Count; i++) {
            var spec = ms2SpecObjects[i];
            if (spec.Precursor.IsolationTargetMz - precursorMz < - mzTolerance) continue;
            if (spec.Precursor.IsolationTargetMz - precursorMz > + mzTolerance) break;

            ID2CE[spec.Index] = spec.CollisionEnergy;
        }
        return ID2CE;
    }

    private static int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, IDataProvider provider) {
        if (ms2RawSpectrumID2CE.Count == 0) return -1;
        return ms2RawSpectrumID2CE.Argmax(kvp => provider.LoadMsSpectrumFromIndex(kvp.Key).TotalIonCurrent).Key;
    }

    private static void SetIsotopes(IReadOnlyList<ChromatogramPeakFeature> chromFeatures) {
        foreach (var feature in chromFeatures) {
            feature.PeakCharacter.IsotopeWeightNumber = 0;
        }
    }

    private static void SetSpectrumPeaks(IReadOnlyList<ChromatogramPeakFeature> chromFeatures, IDataProvider provider) {
        var count = provider.Count();
        foreach (var feature in chromFeatures) {
            if (feature.MS2RawSpectrumID >= 0 && feature.MS2RawSpectrumID < count) {
                var peakElements = provider.LoadMsSpectrumFromIndex(feature.MS2RawSpectrumID).Spectrum;
                var spectrumPeaks = DataAccess.ConvertToSpectrumPeaks(peakElements);
                //var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                var centroidSpec = SpectralCentroiding.CentroidByLocalMaximumMethod(spectrumPeaks);
                feature.Spectrum = centroidSpec;
            }
        }
    }
}
