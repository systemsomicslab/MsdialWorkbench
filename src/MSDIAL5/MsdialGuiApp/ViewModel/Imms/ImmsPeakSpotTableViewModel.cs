using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
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

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal abstract class ImmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        private readonly PeakSpotNavigatorViewModel _peakSpotNavigatorViewModel;

        protected ImmsPeakSpotTableViewModel(IImmsPeakSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel.MetaboliteFilterKeyword, peakSpotNavigatorViewModel.CommentFilterKeyword, peakSpotNavigatorViewModel.OntologyFilterKeyword, peakSpotNavigatorViewModel.AdductFilterKeyword) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            DriftMin = model.DriftMin;
            DriftMax = model.DriftMax;
            SetUnknownCommand = setUnknownCommand;
            UndoManagerViewModel = undoManagerViewModel;
            _peakSpotNavigatorViewModel = peakSpotNavigatorViewModel ?? throw new ArgumentNullException(nameof(peakSpotNavigatorViewModel));
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower => _peakSpotNavigatorViewModel.MzLowerValue;
        public IReactiveProperty<double> MassUpper => _peakSpotNavigatorViewModel.MzUpperValue;

        public double DriftMin { get; }
        public double DriftMax { get; }
        public IReactiveProperty<double> DriftLower => _peakSpotNavigatorViewModel.DtLowerValue;
        public IReactiveProperty<double> DriftUpper => _peakSpotNavigatorViewModel.DtUpperValue;
        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
        public IReactiveProperty<bool> IsEditting => _peakSpotNavigatorViewModel.IsEditting;
    }

    internal sealed class ImmsAnalysisPeakTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAnalysisPeakTableViewModel(ImmsAnalysisPeakTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }

            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    internal sealed class ImmsAlignmentSpotTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAlignmentSpotTableViewModel(ImmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
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
