using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class AlignmentDimsVM : AlignmentFileVM
    {
        public AlignmentDimsVM(DimsAlignmentModel model) {
            Model = model;
            Ms1Spots = CollectionViewSource.GetDefaultView(model.Ms1Spots);
            MassLower = model.Ms1Spots.Min(spot => spot.MassCenter);
            MassUpper = model.Ms1Spots.Max(spot => spot.MassCenter);

            Brushes = Model.Brushes.AsReadOnly();
            SelectedBrush = Model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(Model.PlotModel, brushSource: SelectedBrush).AddTo(Disposables);
            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(Model.Ms2SpectrumModel).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(Model.AlignmentEicModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(Model.BarChartModel).AddTo(Disposables);
        }

        public DimsAlignmentModel Model { get; }

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            private set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            private set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel ms2SpectrumViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            private set => SetProperty(ref alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel alignmentEicViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            private set => SetProperty(ref barChartViewModel, value);
        }
        private Chart.BarChartViewModel barChartViewModel;

        public ReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public double MassLower {
            get => massLower;
            set {
                if (SetProperty(ref massLower, value))
                    Ms1Spots?.Refresh();
            }
        }
        public double MassUpper {
            get => massUpper;
            set {
                if (SetProperty(ref massUpper, value))
                    Ms1Spots?.Refresh();
            }
        }
        private double massLower, massUpper;

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        // public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        private DisplayFilter displayFilters = 0;

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
                    Ms1Spots?.Refresh();
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
                    Ms1Spots?.Refresh();
                }
            }
        }
        private string metaboliteFilterKeyword;
        private List<string> metaboliteFilterKeywords = new List<string>(0);

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered)
                    && MetaboliteFilter(spot, metaboliteFilterKeywords)
                    && CommentFilter(spot, commentFilterKeywords)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched
                || SuggestedChecked && spot.IsSuggested
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower <= spot.MassCenter
                && spot.MassCenter <= MassUpper;
        }

        bool CommentFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Name.Contains(keyword));
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound, CanSearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            if (Model.Target.Value?.innerModel == null || Model.MsdecResult == null)
                return;

            var vm = new CompoundSearchVM<AlignmentSpotProperty>(Model.AlignmentFile, Model.Target.Value.innerModel, Model.MsdecResult, null, Model.MspAnnotator, Model.Parameter.MspSearchParam);
            var window = new View.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (window.ShowDialog() == true) {
                Model.Target.Value.RaisePropertyChanged();
                Ms1Spots?.Refresh();
            }
        }

        private bool CanSearchCompound(Window owner) => (Model.Target.Value?.innerModel) != null && Model.MsdecResult != null;

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

        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        private void ShowIonTable(Window owner) {
            var window = new View.Dims.IonTableViewer
            {
                DataContext = this,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = owner,
            };

            window.Show();
        }

        public DelegateCommand<Window> NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand<Window>(Normalize));

        private DelegateCommand<Window> normalizeCommand;

        private void Normalize(Window owner) {
            var parameter = Model.Parameter;
            using (var vm = new NormalizationSetViewModel(Model.Container, Model.DataBaseRefer, parameter)) {
                var view = new NormalizationSetView
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                view.ShowDialog();
            }
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
