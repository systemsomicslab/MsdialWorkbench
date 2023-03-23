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
    internal abstract class LcmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        private readonly PeakSpotNavigatorViewModel _peakSpotNavigatorViewModel;

        protected LcmsPeakSpotTableViewModel(ILcmsPeakSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel.MetaboliteFilterKeyword, peakSpotNavigatorViewModel.CommentFilterKeyword, peakSpotNavigatorViewModel.OntologyFilterKeyword, peakSpotNavigatorViewModel.AdductFilterKeyword) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;

            SetUnknownCommand = setUnknownCommand;
            UndoManagerViewModel = undoManagerViewModel;
            _peakSpotNavigatorViewModel = peakSpotNavigatorViewModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorViewModel));
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower => _peakSpotNavigatorViewModel.MzLowerValue;
        public IReactiveProperty<double> MassUpper => _peakSpotNavigatorViewModel.MzUpperValue;

        public double RtMin { get; }
        public double RtMax { get; }
        public IReactiveProperty<double> RtLower => _peakSpotNavigatorViewModel.RtLowerValue;
        public IReactiveProperty<double> RtUpper => _peakSpotNavigatorViewModel.RtUpperValue;

        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
    }

    internal sealed class LcmsProteomicsPeakTableViewModel : LcmsPeakSpotTableViewModel {
        public LcmsProteomicsPeakTableViewModel(
            ILcmsPeakSpotTableModel model,
            IObservable<EicLoader> eicLoader,
            PeakSpotNavigatorViewModel peakSpotNavigatorViewModel,
            ICommand setUnknownCommand,
            UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            ProteinFilterKeyword = peakSpotNavigatorViewModel.ProteinFilterKeyword;
            IsEditting = peakSpotNavigatorViewModel.IsEditting;
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public IReactiveProperty<string> PeptideFilterKeyword => MetaboliteFilterKeyword;
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
            UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = peakSpotNavigatorViewModel.IsEditting;
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
   
    internal sealed class LcmsAlignmentSpotTableViewModel : LcmsPeakSpotTableViewModel
    {
        public LcmsAlignmentSpotTableViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
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
        public LcmsProteomicsAlignmentTableViewModel(LcmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            ProteinFilterKeyword = peakSpotNavigatorViewModel.ProteinFilterKeyword;
            IsEditting = peakSpotNavigatorViewModel.IsEditting;
                
        }
        public IReactiveProperty<string> PeptideFilterKeyword => MetaboliteFilterKeyword;
        public IReactiveProperty<string> ProteinFilterKeyword { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
}
