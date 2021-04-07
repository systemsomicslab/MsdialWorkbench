using CompMs.App.Msdial.ViewModel;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using NSSplash;
using NSSplash.impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    public class DimsAnalysisModel : ValidatableBase
    {
        public DimsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            ParameterBase parameter,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator) {

            this.provider = provider;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            AnalysisFile = analysisFile;
            FileName = analysisFile.AnalysisFileName;
            peakAreaFile = analysisFile.PeakAreaBeanInformationFilePath;
            deconvolutionFile = analysisFile.DeconvolutionFilePath;
            Parameter = parameter;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(peakAreaFile);
            ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureVM>(
                peaks.Select(peak => new ChromatogramPeakFeatureVM(peak, parameter.TargetOmics != CompMs.Common.Enum.TargetOmics.Metabolomics)));
            Peaks = peaks;

            MsdecResultsReader.GetSeekPointers(deconvolutionFile, out _, out seekPointers, out _);

            Target = ms1Peaks.FirstOrDefault();
        }

        private readonly string peakAreaFile;
        private readonly string deconvolutionFile;
        private readonly List<long> seekPointers;
        private readonly IDataProvider provider;

        public AnalysisFileBean AnalysisFile { get; }
        public ParameterBase Parameter { get; }

        public IAnnotator<ChromatogramPeakFeature, MSDecResult> MspAnnotator => mspAnnotator;
        public IAnnotator<ChromatogramPeakFeature, MSDecResult> TextDBAnnotator => textDBAnnotator;

        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public ObservableCollection<ChromatogramPeakFeatureVM> Ms1Peaks => ms1Peaks;
        private readonly ObservableCollection<ChromatogramPeakFeatureVM> ms1Peaks;

        public List<ChromatogramPeakFeature> Peaks { get; } = new List<ChromatogramPeakFeature>();

        public ChromatogramPeakFeatureVM Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    _ = OnTargetChangedAsync(target);
                }
            }
        }
        private ChromatogramPeakFeatureVM target;

        private CancellationTokenSource cts;
        public async Task OnTargetChangedAsync(ChromatogramPeakFeatureVM target) {
            cts?.Cancel();
            var localCts = cts = new CancellationTokenSource();

            try {
                await OnTargetChangedAsync(target, localCts.Token).ContinueWith(
                    t => {
                        localCts.Dispose();
                        if (cts == localCts) {
                            cts = null;
                        }
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {

            }
        }

        async Task OnTargetChangedAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            await Task.WhenAll(
                LoadMs1SpectrumAsync(target, token),
                LoadEicAsync(target, token),
                LoadMs2SpectrumAsync(target, token),
                LoadMs2DecSpectrumAsync(target, token),
                LoadMs2ReferenceAsync(target, token)
            ).ConfigureAwait(false);
        }

        public List<SpectrumPeak> Ms1Spectrum {
            get => ms1Spectrum;
            set {
                if (SetProperty(ref ms1Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms1SpectrumMaxIntensity));
                }
            }
        }
        private List<SpectrumPeak> ms1Spectrum = new List<SpectrumPeak>();

        public double Ms1SpectrumMaxIntensity => Ms1Spectrum.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        public string Ms1SplashKey {
            get => ms1SplashKey;
            set => SetProperty(ref ms1SplashKey, value);
        }
        private string ms1SplashKey = string.Empty;

        async Task LoadMs1SpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeak>();
            var ms1SplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    ms1Spectrum = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], Parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    ms1SplashKey = CalculateSplashKey(ms1Spectrum);
                }, token);
            }

            token.ThrowIfCancellationRequested();
            Ms1Spectrum = ms1Spectrum;
            Ms1SplashKey = ms1SplashKey;
        }

        public List<ChromatogramPeakWrapper> Eic {
            get => eic;
            set {
                if (SetProperty(ref eic, value)) {
                    OnPropertyChanged(nameof(EicMaxIntensity));
                }
            }
        }
        private List<ChromatogramPeakWrapper> eic;

        public double EicMaxIntensity => Eic.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        public List<ChromatogramPeakWrapper> PeakEic {
            get => peakEic;
            set => SetProperty(ref peakEic, value);
        }
        private List<ChromatogramPeakWrapper> peakEic;

        public List<ChromatogramPeakWrapper> FocusedEic {
            get => focusedEic;
            set => SetProperty(ref focusedEic, value);
        }
        private List<ChromatogramPeakWrapper> focusedEic;

        async Task LoadEicAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var eic = new List<ChromatogramPeakWrapper>();
            var peakEic = new List<ChromatogramPeakWrapper>();
            var focusedEic = new List<ChromatogramPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    eic = DataAccess.GetSmoothedPeaklist(
                            DataAccess.GetMs1Peaklist(
                                provider.LoadMs1Spectrums(),
                                target.Mass, Parameter.CentroidMs1Tolerance,
                                Parameter.IonMode,
                                ChromXType.Drift, ChromXUnit.Msec),
                            Parameter.SmoothingMethod, Parameter.SmoothingLevel)
                    .Where(peak => peak != null)
                    .Select(peak => new ChromatogramPeakWrapper(peak))
                    .ToList();

                    if (eic.Count == 0)
                        return;

                    token.ThrowIfCancellationRequested();

                    peakEic = eic.Where(peak => target.ChromXLeftValue <= peak.ChromXValue && peak.ChromXValue <= target.ChromXRightValue).ToList();

                    token.ThrowIfCancellationRequested();
                    focusedEic = new List<ChromatogramPeakWrapper> {
                        eic.Where(peak => peak.ChromXValue.HasValue)
                           .Argmin(peak => Math.Abs(target.ChromXValue.Value - peak.ChromXValue.Value))
                    };
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Eic = eic;
            PeakEic = peakEic;
            FocusedEic = focusedEic;
        }

        public List<SpectrumPeak> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2Spectrum;

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public List<SpectrumPeak> Ms2DecSpectrum {
            get => ms2DecSpectrum;
            set {
                if (SetProperty(ref ms2DecSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2DecSpectrum;

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }
        private string deconvolutionSplashKey = string.Empty;

        public List<SpectrumPeak> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeak> ms2ReferenceSpectrum;

        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Min();
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Max();

        async Task LoadMs2SpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2Spectrum = new List<SpectrumPeak>(); 
            var rawSplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS2RawSpectrumId < 0)
                        return;
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMsSpectrums()[target.MS2RawSpectrumId], Parameter.MS2DataType, 0, float.MinValue, float.MaxValue);
                    if (Parameter.RemoveAfterPrecursor)
                        spectra = spectra.Where(peak => peak.Mass <= target.Mass + Parameter.KeptIsotopeRange).ToList();
                    token.ThrowIfCancellationRequested();
                    ms2Spectrum = spectra;
                    rawSplashKey = CalculateSplashKey(spectra);
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2Spectrum = ms2Spectrum;
            RawSplashKey = rawSplashKey;
        }

        public MSDecResult MsdecResult => msdecResult;
        private MSDecResult msdecResult = null;

        async Task LoadMs2DecSpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            var deconvolutionSplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    var idx = ms1Peaks.IndexOf(target);
                    msdecResult = MsdecResultsReader.ReadMSDecResult(deconvolutionFile, seekPointers[idx]);
                    token.ThrowIfCancellationRequested();
                    ms2DecSpectrum = msdecResult.Spectrum;
                    deconvolutionSplashKey = CalculateSplashKey(msdecResult.Spectrum);
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2DecSpectrum = ms2DecSpectrum;
            DeconvolutionSplashKey = deconvolutionSplashKey;
        }

        async Task LoadMs2ReferenceAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();

            if (target != null) {
                await Task.Run(() => {
                    var representative = RetrieveMspMatchResult(target.InnerModel);
                    if (representative == null)
                        return;

                    var reference = mspAnnotator.Refer(representative);
                    if (reference != null) {
                        token.ThrowIfCancellationRequested();
                        ms2ReferenceSpectrum = reference.Spectrum;
                    }
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2ReferenceSpectrum = ms2ReferenceSpectrum;
        }

        MsScanMatchResult RetrieveMspMatchResult(ChromatogramPeakFeature prop) {
            if (prop.MatchResults?.Representative is MsScanMatchResult representative) {
                if ((representative.Priority & (DataBasePriority.Unknown | DataBasePriority.Manual)) == (DataBasePriority.Unknown | DataBasePriority.Manual))
                    return null;
                if (prop.MatchResults.TextDbBasedMatchResults.Contains(representative)) {
                    return null;
                }
                if ((representative.Priority & DataBasePriority.Unknown) == DataBasePriority.None) {
                    return representative;
                }
            }
            return prop.MspBasedMatchResult;
        }

        static string CalculateSplashKey(IReadOnlyList<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }

        private static readonly double MzTol = 20;
        public void FocusByMz(IAxisManager axis, double mz) {
            axis?.Focus(mz - MzTol, mz + MzTol);
        }       

        public void FocusById(IAxisManager mzAxis, int id) {
            var focus = ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == id);
            Target = focus;
            FocusByMz(mzAxis, focus.Mass);
        }

        public void SaveSpectra(string filename) {
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                filename,
                Target.InnerModel,
                msdecResult,
                Parameter);
        }

        public bool CanSaveSpectra() => Target.InnerModel != null && msdecResult != null;
    }
}
