using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
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
        }

        public BarChartViewModel BarChartViewModel => throw new NotImplementedException();

        public ICommand InternalStandardSetCommand => throw new NotImplementedException();

        public IResultModel Model { get; }

        public ViewModelBase[] PeakDetailViewModels => throw new NotImplementedException();

        public ICommand ShowIonTableCommand => throw new NotImplementedException();

        public ICommand SetUnknownCommand => throw new NotImplementedException();

        public UndoManagerViewModel UndoManagerViewModel => throw new NotImplementedException();

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
    }
}
