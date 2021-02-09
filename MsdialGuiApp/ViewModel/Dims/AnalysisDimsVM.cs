using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.RawDataHandler.Core;
using NSSplash;
using NSSplash.impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class AnalysisDimsVM : AnalysisFileVM
    {
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
            set => SetProperty(ref target, value);
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
        // public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        // public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);

        internal DisplayFilter DisplayFilters {
            get => displayFilters;
            set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = 0;


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

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public double MassMin => _ms1Peaks.Min(peak => peak.Mass);
        public double MassMax => _ms1Peaks.Max(peak => peak.Mass);
        public double IntensityMin => _ms1Peaks.Min(peak => peak.Intensity);
        public double IntensityMax => _ms1Peaks.Max(peak => peak.Intensity);

        private MSDecResult msdecResult = null;
        private readonly AnalysisFileBean analysisFile;
        private readonly string peakAreaFile;
        private readonly string deconvolutionFile;
        private readonly ObservableCollection<ChromatogramPeakFeatureVM> _ms1Peaks;
        private readonly List<long> seekPointers;
        private readonly ParameterBase param;
        private readonly List<RawSpectrum> spectrumList;
        private readonly IAnnotator mspAnnotator;

        public AnalysisDimsVM(AnalysisFileBean analysisFile, ParameterBase param, List<MoleculeMsReference> msps)
            : this(analysisFile, param, new DimsMspAnnotator(msps, param.MspSearchParam, param.TargetOmics)) { }

        public AnalysisDimsVM(AnalysisFileBean analysisFile, ParameterBase param, IAnnotator mspAnnotator) {
            this.analysisFile = analysisFile;
            this.param = param;
            this.mspAnnotator = mspAnnotator;

            FileName = analysisFile.AnalysisFileName;

            peakAreaFile = analysisFile.PeakAreaBeanInformationFilePath;
            deconvolutionFile = analysisFile.DeconvolutionFilePath;

            var peaks = MsdialSerializer.LoadChromatogramPeakFeatures(peakAreaFile);
            _ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureVM>(
                peaks.Select(peak => new ChromatogramPeakFeatureVM(peak))
            );
            Peaks = peaks;
            AmplitudeOrderMin = _ms1Peaks.Min(peak => peak.AmplitudeOrderValue);
            AmplitudeOrderMax = _ms1Peaks.Max(peak => peak.AmplitudeOrderValue);
            Ms1Peaks = CollectionViewSource.GetDefaultView(_ms1Peaks);

            MsdecResultsReader.GetSeekPointers(deconvolutionFile, out _, out seekPointers, out _);

            using (var access = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true)) {
                RawMeasurement rawObj = null;
                foreach (var i in Enumerable.Range(0, 5)) {
                    rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null) break;
                    Thread.Sleep(2000);
                }
                if (rawObj == null) {
                    throw new FileLoadException($"Loading {analysisFile.AnalysisFilePath} failed.");
                }
                spectrumList = rawObj.SpectrumList;
            }

            PropertyChanged += OnTargetChanged;
            PropertyChanged += OnFilterChanged;

            Target = _ms1Peaks.FirstOrDefault();
        }

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

        async void OnTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(Target))
                await OnTargetChanged(Target).ConfigureAwait(false);
        }
        async Task OnTargetChanged(ChromatogramPeakFeatureVM target) {
            await Task.WhenAll(
                LoadEicAsync(target),
                LoadMs2SpectrumAsync(target),
                LoadMs2DecSpectrumAsync(target),
                LoadMs2ReferenceAsync(target)
            ).ConfigureAwait(false);

            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusMz = target.Mass;
            }
        }

        async Task LoadEicAsync(ChromatogramPeakFeatureVM target) {
            Eic = new List<ChromatogramPeakWrapper>();
            PeakEic = new List<ChromatogramPeakWrapper>();
            FocusedEic = new List<ChromatogramPeakWrapper>();

            if (target == null)
                return;

            var ms1Spectrum = spectrumList.FirstOrDefault(spectrum => spectrum.MsLevel == 1);
            var leftMz = target.ChromXLeftValue * 2 - target.ChromXRightValue ?? double.MinValue;
            var rightMz = target.ChromXRightValue * 2 - target.ChromXLeftValue ?? double.MaxValue;
            await Task.Run(() => {
                Eic = DataAccess.GetSmoothedPeaklist(
                        DataAccess.ConvertRawPeakElementToChromatogramPeakList(ms1Spectrum.Spectrum, leftMz, rightMz),
                        param.SmoothingMethod, param.SmoothingLevel).Select(peak => new ChromatogramPeakWrapper(peak)).ToList();

                PeakEic = Eic.Where(peak => target.ChromXLeftValue <= peak.ChromXValue && peak.ChromXValue <= target.ChromXRightValue).ToList();

                FocusedEic = target.ChromXValue.HasValue
                    ? new List<ChromatogramPeakWrapper> {
                    Eic.Where(peak => peak.ChromXValue.HasValue)
                       .DefaultIfEmpty()
                       .Argmin(peak => Math.Abs(target.ChromXValue.Value - peak.ChromXValue.Value))
                    }
                    : new List<ChromatogramPeakWrapper>();
            }).ConfigureAwait(false);
        }

        async Task LoadMs2SpectrumAsync(ChromatogramPeakFeatureVM target) {
            Ms2Spectrum = new List<SpectrumPeakWrapper>();
            RawSplashKey = string.Empty;

            if (target == null)
                return;

            await Task.Run(() => {
                var spectra = DataAccess.GetCentroidMassSpectra(spectrumList, param.MS2DataType, target.MS2RawSpectrumId, 0, float.MinValue, float.MaxValue);
                Ms2Spectrum = spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                RawSplashKey = CalculateSplashKey(spectra);
            }).ConfigureAwait(false);
        }

        async Task LoadMs2DecSpectrumAsync(ChromatogramPeakFeatureVM target) {
            Ms2DecSpectrum = new List<SpectrumPeakWrapper>();
            DeconvolutionSplashKey = string.Empty;

            if (target == null)
                return;

            await Task.Run(() => {
                var idx = _ms1Peaks.IndexOf(target);
                msdecResult = MsdecResultsReader.ReadMSDecResult(deconvolutionFile, seekPointers[idx]);
                Ms2DecSpectrum = msdecResult.Spectrum.Select(spec => new SpectrumPeakWrapper(spec)).ToList();
            }).ConfigureAwait(false);
        }

        async Task LoadMs2ReferenceAsync(ChromatogramPeakFeatureVM target) {
            Ms2ReferenceSpectrum = new List<SpectrumPeakWrapper>();

            if (target == null)
                return;

            await Task.Run(() => {
                if (target.TextDbBasedMatchResult == null && target.MspBasedMatchResult is MsScanMatchResult matched) {
                    var reference = mspAnnotator.Refer(matched);
                    Ms2ReferenceSpectrum = reference?.Spectrum.Select(peak => new SpectrumPeakWrapper(peak)).ToList() ?? new List<SpectrumPeakWrapper>();
                }
            }).ConfigureAwait(false);
        }

        static string CalculateSplashKey(IReadOnlyCollection<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }

        public DelegateCommand<object> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<object>(FocusByID));
        private DelegateCommand<object> focusByIDCommand;

        private void FocusByID(object axis) {
            var focus = _ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
            Ms1Peaks.MoveCurrentTo(focus);
            (axis as AxisManager)?.Focus(focus.Mass - MzTol, focus.Mass + MzTol);
        }

        public DelegateCommand<AxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<AxisManager>(FocusByMz));
        private DelegateCommand<AxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(AxisManager axis) {
            axis.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM(analysisFile, Target.InnerModel, msdecResult, null, mspAnnotator);
            var window = new View.Dims.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.ShowDialog();
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
