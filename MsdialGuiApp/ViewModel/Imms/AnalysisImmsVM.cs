using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using NSSplash;
using NSSplash.impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public class AnalysisImmsVM : AnalysisFileVM
    {
        public AnalysisImmsVM(
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            ParameterBase parameter,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator) {

            this.analysisFile = analysisFile;
            this.provider = provider;
            this.parameter = parameter;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            FileName = analysisFile.AnalysisFileName;
            peakAreaFile = analysisFile.PeakAreaBeanInformationFilePath;
            deconvolutionFile = analysisFile.DeconvolutionFilePath;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(peakAreaFile);
            _ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureVM>(
                peaks.Select(peak => new ChromatogramPeakFeatureVM(peak, parameter.TargetOmics != CompMs.Common.Enum.TargetOmics.Metabolomics))
            );
            Peaks = peaks;
            AmplitudeOrderMin = _ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0;
            AmplitudeOrderMax = _ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0;
            Ms1Peaks = CollectionViewSource.GetDefaultView(_ms1Peaks);

            MsdecResultsReader.GetSeekPointers(deconvolutionFile, out _, out seekPointers, out _);

            PropertyChanged += OnFilterChanged;

            Target = _ms1Peaks.FirstOrDefault();
        }

        private readonly AnalysisFileBean analysisFile;
        private readonly ParameterBase parameter;
        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;
        private readonly string peakAreaFile;
        private readonly string deconvolutionFile;
        private readonly ObservableCollection<ChromatogramPeakFeatureVM> _ms1Peaks;
        private readonly List<long> seekPointers;
        private readonly IDataProvider provider;
        
        public ICollectionView Ms1Peaks {
            get => ms1Peaks;
            set {
                var old = ms1Peaks;
                if (SetProperty(ref ms1Peaks, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Peaks != null) ms1Peaks.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Peaks;

        public List<SpectrumPeakWrapper> Ms1Spectrum
        {
            get => ms1Spectrum;
            set {
                if (SetProperty(ref ms1Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms1SpectrumMaxIntensity));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms1Spectrum = new List<SpectrumPeakWrapper>();

        public double Ms1SpectrumMaxIntensity => Ms1Spectrum.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        public string Ms1SplashKey {
            get => ms1SplashKey;
            set => SetProperty(ref ms1SplashKey, value);
        }
        private string ms1SplashKey = string.Empty;


        public List<ChromatogramPeakWrapper> Eic {
            get => eic;
            set {
                if (SetProperty(ref eic, value)) {
                    OnPropertyChanged(nameof(EicMaxIntensity));
                }
            }
        }

        public double EicMaxIntensity => Eic.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        private List<ChromatogramPeakWrapper> eic;

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

        public double Ms1Tolerance => parameter.CentroidMs1Tolerance;

        public List<SpectrumPeakWrapper> Ms2Spectrum {
            get => ms2Spectrum;
            set {
                if (SetProperty(ref ms2Spectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms2Spectrum = new List<SpectrumPeakWrapper>();

        public string RawSplashKey {
            get => rawSplashKey;
            set => SetProperty(ref rawSplashKey, value);
        }
        private string rawSplashKey = string.Empty;

        public List<SpectrumPeakWrapper> Ms2DecSpectrum {
            get => ms2DecSpectrum;
            set {
                if (SetProperty(ref ms2DecSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms2DecSpectrum = new List<SpectrumPeakWrapper>();

        public string DeconvolutionSplashKey {
            get => deconvolutionSplashKey;
            set => SetProperty(ref deconvolutionSplashKey, value);
        }
        private string deconvolutionSplashKey = string.Empty;

        public List<SpectrumPeakWrapper> Ms2ReferenceSpectrum {
            get => ms2ReferenceSpectrum;
            set {
                if (SetProperty(ref ms2ReferenceSpectrum, value)) {
                    OnPropertyChanged(nameof(Ms2MassMax));
                    OnPropertyChanged(nameof(Ms2MassMin));
                }
            }
        }
        private List<SpectrumPeakWrapper> ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();

        public double Ms2MassMin => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Min();
        public double Ms2MassMax => Ms2Spectrum.Concat(Ms2ReferenceSpectrum).Concat(Ms2DecSpectrum).Select(peak => peak.Mass).DefaultIfEmpty().Max();

        public List<ChromatogramPeakFeature> Peaks { get; } = new List<ChromatogramPeakFeature>();

        public ChromatogramPeakFeatureVM Target {
            get => target;
            set {
                if (SetProperty(ref target, value)) {
                    _ = OnTargetChangedAsync(value);
                }
            }
        }
        private ChromatogramPeakFeatureVM target;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel;

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        internal bool ReadDisplayFilters(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }

        public double AmplitudeUpperValue {
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeLowerValue = 0d, amplitudeUpperValue = 1d;

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusDt {
            get => focusDt;
            set => SetProperty(ref focusDt, value);
        }
        private double focusDt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public double ChromMin => _ms1Peaks.Min(peak => peak.ChromXValue) ?? 0;
        public double ChromMax => _ms1Peaks.Max(peak => peak.ChromXValue) ?? 0;
        public double MassMin => _ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => _ms1Peaks.Max(peak => peak.Mass);
        public double IntensityMin => _ms1Peaks.Min(peak => peak.Intensity);
        public double IntensityMax => _ms1Peaks.Max(peak => peak.Intensity);

        private MSDecResult msdecResult = null;

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureVM peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureVM peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureVM peak) {
            return AmplitudeLowerValue * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        void OnFilterChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DisplayFilters)
                || e.PropertyName == nameof(AmplitudeLowerValue)
                || e.PropertyName == nameof(AmplitudeUpperValue))
                Ms1Peaks?.Refresh();
        }

        private CancellationTokenSource cts;
        async Task OnTargetChangedAsync(ChromatogramPeakFeatureVM target) {
            cts?.Cancel();
            var localCts = cts = new CancellationTokenSource();

            try {
                await OnTargetChangedAsync(target, localCts.Token).ContinueWith(
                    t => {
                        localCts.Dispose();
                        if (cts == localCts)
                            cts = null;
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {

            }
        }

        async Task OnTargetChangedAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }

            await Task.WhenAll(
                LoadMs1SpectrumAsync(target, token),
                LoadEicAsync(target, token),
                LoadMs2SpectrumAsync(target, token),
                LoadMs2DecSpectrumAsync(target, token),
                LoadMs2ReferenceAsync(target, token)
            ).ConfigureAwait(false);
        }

        async Task LoadMs1SpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms1Spectrum = new List<SpectrumPeakWrapper>();
            var ms1SplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS1RawSpectrumIdTop < 0) {
                        return;
                    }
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMs1Spectrums()[target.MS1RawSpectrumIdTop], parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                    ms1Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                    ms1SplashKey = CalculateSplashKey(spectra);
                }, token);
            }

            token.ThrowIfCancellationRequested();
            Ms1Spectrum = ms1Spectrum;
            Ms1SplashKey = ms1SplashKey;
        }

        async Task LoadEicAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var eic = new List<ChromatogramPeakWrapper>();
            var peakEic = new List<ChromatogramPeakWrapper>();
            var focusedEic = new List<ChromatogramPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    eic = DataAccess.GetSmoothedPeaklist(
                            DataAccess.GetMs1Peaklist(
                                provider.LoadMs1Spectrums(),
                                target.Mass, parameter.CentroidMs1Tolerance,
                                parameter.IonMode,
                                ChromXType.Drift, ChromXUnit.Msec),
                            parameter.SmoothingMethod, parameter.SmoothingLevel)
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

        async Task LoadMs2SpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2Spectrum = new List<SpectrumPeakWrapper>(); 
            var rawSplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    if (target.MS2RawSpectrumId < 0)
                        return;
                    var spectra = DataAccess.GetCentroidMassSpectra(provider.LoadMsSpectrums()[target.MS2RawSpectrumId], parameter.MS2DataType, 0, float.MinValue, float.MaxValue);
                    if (parameter.RemoveAfterPrecursor)
                        spectra = spectra.Where(peak => peak.Mass <= target.Mass + parameter.KeptIsotopeRange).ToList();
                    token.ThrowIfCancellationRequested();
                    ms2Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                    rawSplashKey = CalculateSplashKey(spectra);
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2Spectrum = ms2Spectrum;
            RawSplashKey = rawSplashKey;
        }

        async Task LoadMs2DecSpectrumAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeakWrapper>();
            var deconvolutionSplashKey = string.Empty;

            if (target != null) {
                await Task.Run(() => {
                    var idx = _ms1Peaks.IndexOf(target);
                    msdecResult = MsdecResultsReader.ReadMSDecResult(deconvolutionFile, seekPointers[idx]);
                    token.ThrowIfCancellationRequested();
                    ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
                    deconvolutionSplashKey = CalculateSplashKey(msdecResult.Spectrum);
                }, token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            Ms2DecSpectrum = ms2DecSpectrum;
            DeconvolutionSplashKey = deconvolutionSplashKey;
        }

        async Task LoadMs2ReferenceAsync(ChromatogramPeakFeatureVM target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();

            if (target != null) {
                await Task.Run(() => {
                    var representative = RetrieveMspMatchResult(target.InnerModel);
                    if (representative == null)
                        return;

                    var reference = mspAnnotator.Refer(representative);
                    if (reference != null) {
                        token.ThrowIfCancellationRequested();
                        ms2ReferenceSpectrum = reference.Spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
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

        static string CalculateSplashKey(IReadOnlyCollection<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            var focus = _ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
            Ms1Peaks.MoveCurrentTo(focus);
            axis?.Focus(focus.Mass - MzTol, focus.Mass + MzTol);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            axis?.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM<ChromatogramPeakFeature>(analysisFile, Target.InnerModel, msdecResult, null, mspAnnotator);
            var window = new View.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.ShowDialog();
        }
    }
}
