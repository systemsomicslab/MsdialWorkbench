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
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialDimsCore
{
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

        public async Task RunAsync(AnalysisFileBean file, Action<int> reportAction = null, CancellationToken token = default) {
            var param = _storage.Parameter;
            // parse raw data
            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(file);

            // faeture detections
            Console.WriteLine("Peak picking started");
            var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.Spectrum.Length);
            var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
            var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();

            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            if (peakPickResults.IsEmptyOrNull()) return;
            var peakFeatures = ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider, file.AcquisitionType);

            if (peakFeatures.Count == 0) return;
            // IsotopeEstimator.Process(peakFeatures, param, iupacDB); // in dims, skip the isotope estimation process.
            SetIsotopes(peakFeatures);
            SetSpectrumPeaks(peakFeatures, provider);

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, peakFeatures);
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            var msdecProcess = new Algorithm.Ms2Dec(initial_msdec, max_msdec);
            var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
            var msdecResults = msdecProcess.GetMS2DecResults(provider, peakFeatures, param, summary, targetCE, reportAction);

            Console.WriteLine("Annotation started");
            await _annotationProcess.RunAnnotationAsync(peakFeatures, msdecResults, provider, param.NumThreads, v => reportAction?.Invoke((int)v), token).ConfigureAwait(false);

            var characterEstimator = new Algorithm.PeakCharacterEstimator(90, 10);
            characterEstimator.Process(file, peakFeatures, msdecResults, _evaluator, param, reportAction, provider);

            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath, peakFeatures);
            MsdecResultsWriter.Write(file.DeconvolutionFilePath, msdecResults);

            reportAction?.Invoke(100);
        }

        public async Task AnnotateAsync(AnalysisFileBean file, Action<int> reportAction = null, CancellationToken token = default) {
            var param = _storage.Parameter;
            // parse raw data
            Console.WriteLine("Loading spectral information");
            var provider = _providerFactory.Create(file);

            var peakFeaturesTask = file.LoadChromatogramPeakFeatureCollectionAsync(token);
            var msdecResultssTask = MSDecResultCollection.DeserializeAsync(file, token);

            Console.WriteLine("Annotation started");
            var peakFeatures = await peakFeaturesTask.ConfigureAwait(false);
            peakFeatures.ClearMatchResultProperties();
            var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
            var msdecResultss = await Task.WhenAll(msdecResultssTask).ConfigureAwait(false);
            var msdecResults = msdecResultss.FirstOrDefault(results => results.CollisionEnergy == targetCE) ?? msdecResultss.First();
            await _annotationProcess.RunAnnotationAsync(peakFeatures.Items, msdecResults.MSDecResults, provider, param.NumThreads, v => reportAction?.Invoke((int)v), token).ConfigureAwait(false);

            var characterEstimator = new Algorithm.PeakCharacterEstimator(90, 10);
            characterEstimator.Process(file, peakFeatures.Items, msdecResults.MSDecResults, _evaluator, param, reportAction, provider);

            await peakFeatures.SerializeAsync(file, token).ConfigureAwait(false);
            reportAction?.Invoke(100);
        }

        public static async Task RunAsync(
            AnalysisFileBean file,
            IDataProvider provider,
            IMsdialDataStorage<MsdialDimsParameter> storage,
            IAnnotationProcess annotationProcess,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            Action<int> reportAction = null,
            CancellationToken token = default) {

            var param = storage.Parameter;
            // parse raw data
            Console.WriteLine("Loading spectral information");

            // faeture detections
            Console.WriteLine("Peak picking started");
            var ms1Spectrum = provider.LoadMs1Spectrums().Argmax(spec => spec.Spectrum.Length);
            var chromPeaks = DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum);
            var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();

            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            if (peakPickResults.IsEmptyOrNull()) return;
            var peakFeatures = ConvertPeaksToPeakFeatures(peakPickResults, ms1Spectrum, provider, file.AcquisitionType);

            if (peakFeatures.Count == 0) return;
            // IsotopeEstimator.Process(peakFeatures, param, iupacDB); // in dims, skip the isotope estimation process.
            SetIsotopes(peakFeatures);
            SetSpectrumPeaks(peakFeatures, provider);

            // chrom deconvolutions
            Console.WriteLine("Deconvolution started");
            var summary = ChromFeatureSummarizer.GetChromFeaturesSummary(provider, peakFeatures);
            var initial_msdec = 30.0;
            var max_msdec = 30.0;
            var msdecProcess = new Algorithm.Ms2Dec(initial_msdec, max_msdec);
            var targetCE = Math.Round(provider.GetMinimumCollisionEnergy(), 2);
            var msdecResults = msdecProcess.GetMS2DecResults(provider, peakFeatures, param, summary, targetCE, reportAction);

            Console.WriteLine("Annotation started");
            await annotationProcess.RunAnnotationAsync(peakFeatures, msdecResults, provider, param.NumThreads, v => reportAction?.Invoke((int)v), token).ConfigureAwait(false);

            var characterEstimator = new Algorithm.PeakCharacterEstimator(90, 10);
            characterEstimator.Process(file, peakFeatures, msdecResults, evaluator, param, reportAction, provider);

            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath, peakFeatures);
            MsdecResultsWriter.Write(file.DeconvolutionFilePath, msdecResults);

            reportAction?.Invoke(100);
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

        private static void SetIsotopes(List<ChromatogramPeakFeature> chromFeatures) {
            foreach (var feature in chromFeatures) {
                feature.PeakCharacter.IsotopeWeightNumber = 0;
            }
        }

        private static void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, IDataProvider provider) {
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

        Task IFileProcessor.RunAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token) {
            return Task.Run(() => RunAsync(file, reportAction is null ? (Action<int>)null : reportAction.Report, token), token);
        }

        Task IFileProcessor.AnnotateAsync(AnalysisFileBean file, IProgress<int> reportAction, CancellationToken token) {
            return AnnotateAsync(file, reportAction is null ? (Action<int>)null : reportAction.Report, token);
        }
    }
}
