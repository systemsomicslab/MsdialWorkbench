using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Graphics.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    abstract class LcmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected LcmsPeakSpotTableViewModel(
            ILcmsPeakSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            ICommand setUnknownCommand,
            UndoManagerViewModel undoManagerViewModel)
            : base(model, metaboliteFilterKeyword, commentFilterKeyword, ontologyFilterKeyword, adductFilterKeyword) {
            if (massLower is null) {
                throw new ArgumentNullException(nameof(massLower));
            }

            if (massUpper is null) {
                throw new ArgumentNullException(nameof(massUpper));
            }

            if (rtLower is null) {
                throw new ArgumentNullException(nameof(rtLower));
            }

            if (rtUpper is null) {
                throw new ArgumentNullException(nameof(rtUpper));
            }

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            MassLower = massLower;
            MassUpper = massUpper;

            RtMin = model.RtMin;
            RtMax = model.RtMax;
            RtLower = rtLower;
            RtUpper = rtUpper;

            SetUnknownCommand = setUnknownCommand;
            UndoManagerViewModel = undoManagerViewModel;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower { get; }
        public IReactiveProperty<double> MassUpper { get; }

        public double RtMin { get; }
        public double RtMax { get; }
        public IReactiveProperty<double> RtLower { get; }
        public IReactiveProperty<double> RtUpper { get; }

        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
    }

    internal sealed class LcmsProteomicsPeakTableViewModel : LcmsPeakSpotTableViewModel {
        public LcmsProteomicsPeakTableViewModel(
            ILcmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> proteinFilterKeyword,
            IReactiveProperty<string> peptideFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            ICommand setUnknownCommand,
            IReactiveProperty<bool> isEditting,
            UndoManagerViewModel undoManagerViewModel)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  peptideFilterKeyword,
                  commentFilterKeyword,
                  ontologyFilterKeyword,
                  adductFilterKeyword,
                  setUnknownCommand,
                  undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            ProteinFilterKeyword = proteinFilterKeyword;
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public IReactiveProperty<string> PeptideFilterKeyword { get => this.MetaboliteFilterKeyword; }
        public IReactiveProperty<string> ProteinFilterKeyword { get; }
        public IReactiveProperty<bool> IsEditting { get; }
        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    internal sealed class LcmsAnalysisPeakTableViewModel : LcmsPeakSpotTableViewModel
    {
        public LcmsAnalysisPeakTableViewModel(
            ILcmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            PeakSpotNavigatorViewModel peakSpotNavigatorViewModel,
            ICommand setUnknownCommand,
            IReactiveProperty<bool> isEditting,
            UndoManagerViewModel undoManagerViewModel)
            : base(
                  model,
                  peakSpotNavigatorViewModel.MzLowerValue,
                  peakSpotNavigatorViewModel.MzUpperValue,
                  peakSpotNavigatorViewModel.RtLowerValue,
                  peakSpotNavigatorViewModel.RtUpperValue,
                  peakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                  peakSpotNavigatorViewModel.CommentFilterKeyword,
                  peakSpotNavigatorViewModel.OntologyFilterKeyword,
                  peakSpotNavigatorViewModel.AdductFilterKeyword,
                  setUnknownCommand,
                  undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
   
    internal sealed class LcmsAlignmentSpotTableViewModel : LcmsPeakSpotTableViewModel
    {
        public LcmsAlignmentSpotTableViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(
                  model,
                  peakSpotNavigatorViewModel.MzLowerValue,
                  peakSpotNavigatorViewModel.MzUpperValue,
                  peakSpotNavigatorViewModel.RtLowerValue,
                  peakSpotNavigatorViewModel.RtUpperValue,
                  peakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                  peakSpotNavigatorViewModel.CommentFilterKeyword,
                  peakSpotNavigatorViewModel.OntologyFilterKeyword,
                  peakSpotNavigatorViewModel.AdductFilterKeyword,
                  setUnknownCommand,
                  undoManagerViewModel) {
            BarItemsLoader = model.BarItemsLoader;
            ClassBrush = model.ClassBrush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = peakSpotNavigatorViewModel.IsEditting;
            FileClassPropertiesModel = model.FileClassProperties;
        }

        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>> ClassBrush { get; }
        public FileClassPropertiesModel FileClassPropertiesModel { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }

    internal sealed class LcmsProteomicsAlignmentTableViewModel : LcmsPeakSpotTableViewModel {
        public LcmsProteomicsAlignmentTableViewModel(
            LcmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> rtLower,
            IReactiveProperty<double> rtUpper,
            IReactiveProperty<string> proteinFilterKeyword,
            IReactiveProperty<string> peptideFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            ICommand setUnknownCommand,
            IReactiveProperty<bool> isEditting,
            UndoManagerViewModel undoManagerViewModel)
            : base(
                  model,
                  massLower,
                  massUpper,
                  rtLower,
                  rtUpper,
                  peptideFilterKeyword,
                  commentFilterKeyword,
                  ontologyFilterKeyword,
                  adductFilterKeyword,
                  setUnknownCommand,
                  undoManagerViewModel) {
            ProteinFilterKeyword = proteinFilterKeyword;
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
        }
        public IReactiveProperty<string> PeptideFilterKeyword { get => this.MetaboliteFilterKeyword; }
        public IReactiveProperty<string> ProteinFilterKeyword { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
}
