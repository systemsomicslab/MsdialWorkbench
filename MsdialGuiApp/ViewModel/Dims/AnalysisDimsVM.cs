using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class AnalysisDimsVM : AnalysisFileVM
    {
        public AnalysisDimsVM(DimsAnalysisModel model) {
            Model = model;

            var hAxis = Model.PlotModel2
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = Model.PlotModel2
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            IBrushMapper<ChromatogramPeakFeatureModel> brush = null;
            switch (model.Parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak.Ontology,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                    brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
            }

            PlotViewModel = new AnalysisPeakPlotViewModel(Model.PlotModel2, brushSource: Observable.Return(brush), horizontalAxis: hAxis, verticalAxis: vAxis) .AddTo(Disposables);
            EicViewModel = new Chart.EicViewModel(model.EicModel2, horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new Chart.RawDecSpectrumsViewModel(model.Ms2SpectrumModel2).AddTo(Disposables);

            Model.Target.Subscribe(UpdateGraphTitleOnTargetChanged).AddTo(Disposables);

            AmplitudeOrderMin = model.Ms1Peaks.Min(peak => peak.AmplitudeOrderValue);
            AmplitudeOrderMax = model.Ms1Peaks.Max(peak => peak.AmplitudeOrderValue);

            Ms1Peaks = CollectionViewSource.GetDefaultView(PlotViewModel.Spots);
            PropertyChanged += OnFilterChanged;
        }

        public DimsAnalysisModel Model { get; }

        public AnalysisPeakPlotViewModel PlotViewModel {
            get => plotViewModel2;
            set => SetProperty(ref plotViewModel2, value);
        }
        private AnalysisPeakPlotViewModel plotViewModel2;

        private void UpdateGraphTitleOnTargetChanged(ChromatogramPeakFeatureModel t) {
            if (t == null) {
                Model.PlotModel2.GraphTitle = string.Empty;
                Model.EicModel2.GraphTitle = string.Empty;
            }
            else {
                Model.PlotModel2.GraphTitle = $"Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}";
                Model.EicModel2.GraphTitle = $"{t.Mass:N4}[Da]  Max intensity: {Model.EicModel2.MaxIntensity:F0}";
            }
        }

        public Chart.EicViewModel EicViewModel {
            get => eicViewModel2;
            set => SetProperty(ref eicViewModel2, value);
        }
        private Chart.EicViewModel eicViewModel2;

        public Chart.RawDecSpectrumsViewModel RawDecSpectrumsViewModel {
            get => rawDecSpectrumsViewModel;
            set => SetProperty(ref rawDecSpectrumsViewModel, value);
        }
        private Chart.RawDecSpectrumsViewModel rawDecSpectrumsViewModel;

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
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
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

        public string CommentFilterKeyword {
            get => commentFilterKeyword;
            set {
                if (SetProperty(ref commentFilterKeyword, value)){
                    if (!string.IsNullOrEmpty(commentFilterKeyword)) {
                        commentFilterKeywords = commentFilterKeyword.Split().ToList();
                    }
                    else {
                        commentFilterKeywords = new List<string>(0);
                    }
                    Ms1Peaks?.Refresh();
                }
            }
        }
        private string commentFilterKeyword;
        private List<string> commentFilterKeywords = new List<string>(0);

        public string MetaboliteFilterKeyword {
            get => metaboliteFilterKeyword;
            set {
                if (SetProperty(ref metaboliteFilterKeyword, value)) {
                    if (!string.IsNullOrEmpty(metaboliteFilterKeyword)) {
                        metaboliteFilterKeywords = metaboliteFilterKeyword.Split().ToList();
                    }
                    else {
                        metaboliteFilterKeywords = new List<string>(0);
                    }
                    Ms1Peaks?.Refresh();
                }
            }
        }
        private string metaboliteFilterKeyword;
        private List<string> metaboliteFilterKeywords = new List<string>(0);

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

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && MetaboliteFilter(peak, metaboliteFilterKeywords)
                    && CommentFilter(peak, commentFilterKeywords)
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

        bool CommentFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        void OnFilterChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DisplayFilters)
                || e.PropertyName == nameof(AmplitudeLowerValue)
                || e.PropertyName == nameof(AmplitudeUpperValue))
                Ms1Peaks?.Refresh();
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            Model.FocusById(axis, FocusID);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            Model.FocusByMz(axis, FocusMz);
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM<ChromatogramPeakFeature>(Model.AnalysisFile, Model.Target.Value.InnerModel, Model.MsdecResult.Value, null, Model.MspAnnotator, Model.Parameter.MspSearchParam);
            var window = new View.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (window.ShowDialog() == true) {
                Model.Target.Value.RaisePropertyChanged();
                _ = Model.OnTargetChangedAsync(Model.Target.Value);
                Ms1Peaks?.Refresh();
            }
        }

        public DelegateCommand<Window> SaveMs2SpectrumCommand => saveMs2SpectrumCommand ?? (saveMs2SpectrumCommand = new DelegateCommand<Window>(SaveSpectra, CanSaveSpectra));
        private DelegateCommand<Window> saveMs2SpectrumCommand;

        private void SaveSpectra(Window owner)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true)
            {
                var filename = sfd.FileName;
                Model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner)
        {
            return Model.CanSaveSpectra();
        }


        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
