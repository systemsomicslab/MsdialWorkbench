using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
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

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal abstract class LcimmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected LcimmsPeakSpotTableViewModel(ILcimmsPeakSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            MassMin = model.MassMin;
            MassMax = model.MassMax;
            RtMin = model.RtMin;
            RtMax = model.RtMax;
            DtMin = model.DtMin;
            DtMax = model.DtMax;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public double DtMin { get; }
        public double DtMax { get; }
    }

    internal sealed class LcimmsAnalysisPeakTableViewModel : LcimmsPeakSpotTableViewModel
    {
        public LcimmsAnalysisPeakTableViewModel(ILcimmsPeakSpotTableModel model, IObservable<EicLoader> eicLoader, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
            : base(model, peakSpotNavigatorViewModel, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }
            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }
   
    internal sealed class LcimmsAlignmentSpotTableViewModel : LcimmsPeakSpotTableViewModel
    {
        public LcimmsAlignmentSpotTableViewModel(LcimmsAlignmentSpotTableModel model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknownCommand, UndoManagerViewModel undoManagerViewModel)
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
