using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        public GcmsAlignmentViewModel(GcmsAlignmentModel model, FocusControlManager focusControlManager, IMessageBroker broker) {
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            Model = model;

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(model.PlotModel, peakPlotAction, peakPlotFocused).AddTo(Disposables);

            var (barChartAction, barChartFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(model.BarChartModel, barChartAction, barChartFocused).AddTo(Disposables);

            var peakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var compoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var moleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { peakInformationViewModel, compoundDetailViewModel, moleculeStructureViewModel, matchResultCandidatesViewModel, };
        }

        public BarChartViewModel BarChartViewModel { get; }
        public ICommand InternalStandardSetCommand => throw new NotImplementedException();

        public IResultModel Model { get; }

        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand ShowIonTableCommand => throw new NotImplementedException();

        public ICommand SetUnknownCommand => throw new NotImplementedException();

        public UndoManagerViewModel UndoManagerViewModel => throw new NotImplementedException();

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
    }
}
