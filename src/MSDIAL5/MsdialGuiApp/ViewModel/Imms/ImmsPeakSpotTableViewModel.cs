using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Setting;
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
        protected ImmsPeakSpotTableViewModel(
            IImmsPeakSpotTableModel model,
            IReactiveProperty<double> massLower, IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower, IReactiveProperty<double> driftUpper,
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

            if (driftLower is null) {
                throw new ArgumentNullException(nameof(driftLower));
            }

            if (driftUpper is null) {
                throw new ArgumentNullException(nameof(driftUpper));
            }

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            MassLower = massLower;
            MassUpper = massUpper;

            DriftMin = model.DriftMin;
            DriftMax = model.DriftMax;
            DriftLower = driftLower;
            DriftUpper = driftUpper;
            SetUnknownCommand = setUnknownCommand;
            UndoManagerViewModel = undoManagerViewModel;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower { get; }
        public IReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public IReactiveProperty<double> DriftLower { get; }
        public IReactiveProperty<double> DriftUpper { get; }
        public ICommand SetUnknownCommand { get; }
        public UndoManagerViewModel UndoManagerViewModel { get; }
    }

    internal sealed class ImmsAnalysisPeakTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAnalysisPeakTableViewModel(
            ImmsAnalysisPeakTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower,
            IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            IReactiveProperty<bool> isEditting,
            ICommand setUnknownCommand,
            UndoManagerViewModel undoManagerViewModel)
            : base(model, massLower, massUpper, driftLower, driftUpper, metaboliteFilterKeyword,
                  commentFilterKeyword, ontologyFilterKeyword, adductFilterKeyword, setUnknownCommand, undoManagerViewModel) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }

            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }

    internal sealed class ImmsAlignmentSpotTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAlignmentSpotTableViewModel(
            ImmsAlignmentSpotTableModel model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower,
            IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword,
            IReactiveProperty<string> ontologyFilterKeyword,
            IReactiveProperty<string> adductFilterKeyword,
            IReactiveProperty<bool> isEditting,
            ICommand setUnknownCommand,
            UndoManagerViewModel undoManagerViewModel)
            : base(
                  model,
                  massLower,
                  massUpper,
                  driftLower,
                  driftUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword,
                  ontologyFilterKeyword,
                  adductFilterKeyword,
                  setUnknownCommand,
                  undoManagerViewModel) {
            BarItemsLoader = model.BarItemsLoader;
            ClassBrush = model.ClassBrush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsEditting = isEditting ?? throw new ArgumentNullException(nameof(isEditting));
            FileClassPropertiesModel = model.FileClassProperties;
        }

        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>> ClassBrush { get; }
        public FileClassPropertiesModel FileClassPropertiesModel { get; }
        public IReactiveProperty<bool> IsEditting { get; }
    }
}
