using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using NSSplash;
using NSSplash.impl;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AnalysisImmsVM : AnalysisFileVM {
        public AnalysisImmsVM(
            ImmsAnalysisModel model,
            AnalysisFileBean analysisFile,
            IDataProvider provider,
            ParameterBase parameter,
            IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, IAnnotator<ChromatogramPeakFeature, MSDecResult> textDBAnnotator) {

            this.model = model;

            var hAxis = model.PlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);

            PlotViewModel = new AnalysisPeakPlotViewModel(model.PlotModel, horizontalAxis: hAxis);
            Disposables.Add(PlotViewModel);

            EicViewModel = new Chart.EicViewModel(model.EicModel, horizontalAxis: hAxis);
            Disposables.Add(EicViewModel);

            RawDecSpectrumsViewModel = new Chart.RawDecSpectrumsViewModel(model.Ms2SpectrumModel);
            Disposables.Add(RawDecSpectrumsViewModel);

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Subscribe(async t => await OnTargetChangedAsync(t));

            this.analysisFile = analysisFile;
            this.provider = provider;
            this.parameter = parameter;
            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;
            FileName = analysisFile.AnalysisFileName;

            AmplitudeOrderMax = model.PlotModel.Spots.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeOrderMin = model.PlotModel.Spots.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0d;
            Ms1Peaks = CollectionViewSource.GetDefaultView(PlotViewModel.Spots);

            PropertyChanged += OnFilterChanged;
        }

        private readonly ImmsAnalysisModel model;
        private readonly AnalysisFileBean analysisFile;
        private readonly ParameterBase parameter;
        private readonly IAnnotator<ChromatogramPeakFeature, MSDecResult> mspAnnotator, textDBAnnotator;
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

        public AnalysisPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AnalysisPeakPlotViewModel plotViewModel;

        public Chart.EicViewModel EicViewModel {
            get => eicViewModel;
            set => SetProperty(ref eicViewModel, value);
        }
        private Chart.EicViewModel eicViewModel;

        public Chart.RawDecSpectrumsViewModel RawDecSpectrumsViewModel {
            get => ms2ViewModel;
            set => SetProperty(ref ms2ViewModel, value);
        }
        private Chart.RawDecSpectrumsViewModel ms2ViewModel;

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

        public double Ms1Tolerance => parameter.CentroidMs1Tolerance;

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

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

        private MSDecResult msdecResult = null;

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureModel peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureModel peak) {
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
        async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target) {
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

        async Task OnTargetChangedAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target != null) {
                FocusID = target.InnerModel.MasterPeakID;
                FocusDt = target.ChromXValue ?? 0;
                FocusMz = target.Mass;
            }

            await Task.WhenAll(
                LoadMs1SpectrumAsync(target, token)
            ).ConfigureAwait(false);
        }

        async Task LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
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

        static string CalculateSplashKey(IReadOnlyCollection<SpectrumPeak> spectra) {
            if (spectra.IsEmptyOrNull() || spectra.Count <= 2 && spectra.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectra.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            var focus = model.Ms1Peaks.FirstOrDefault(peak => peak.InnerModel.MasterPeakID == FocusID);
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
            var vm = new CompoundSearchVM<ChromatogramPeakFeature>(analysisFile, Target.Value.InnerModel, msdecResult, null, mspAnnotator);
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
