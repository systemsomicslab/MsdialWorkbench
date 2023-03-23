using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
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

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal abstract class DimsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        private readonly PeakSpotNavigatorViewModel _peakSpotNavigatorViewModel;

        protected DimsPeakSpotTableViewModel(IDimsPeakSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel.MetaboliteFilterKeyword, peakSpotNavigatorViewModel.CommentFilterKeyword, peakSpotNavigatorViewModel.OntologyFilterKeyword, peakSpotNavigatorViewModel.AdductFilterKeyword) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            SetUnknownCommand = setUnknownCommand;
            UndoManagerViewModel = undoManagerViewModel;
            _peakSpotNavigatorViewModel = peakSpotNavigatorViewModel;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower => _peakSpotNavigatorViewModel.MzLowerValue;
        public IReactiveProperty<double> MassUpper => _peakSpotNavigatorViewModel.MzUpperValue;
        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
        public IReactiveProperty<bool> IsEdittng => _peakSpotNavigatorViewModel.IsEditting;
    }

    internal sealed class DimsAnalysisPeakTableViewModel : DimsPeakSpotTableViewModel
    {
        public DimsAnalysisPeakTableViewModel(DimsPeakSpotTableModel<ChromatogramPeakFeatureModel> model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    internal sealed class DimsAlignmentSpotTableViewModel : DimsPeakSpotTableViewModel
    {
        public DimsAlignmentSpotTableViewModel(DimsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            BarItemsLoader = model.BarItemsLoader;
            ClassBrush = model.ClassBrush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            FileClassPropertiesModel = model.FileClassProperties;
        }

        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>> ClassBrush { get; }
        public FileClassPropertiesModel FileClassPropertiesModel { get; }
    }
}
