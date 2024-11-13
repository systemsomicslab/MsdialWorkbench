using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.ViewModel.Gcms;

internal sealed class GcgcAlignmentPeakPlotViewModel : AlignmentPeakPlotViewModel
{
    public GcgcAlignmentPeakPlotViewModel(GcgcAlignmentPeakPlotModel model, Action focus, IObservable<bool> isFocused, IMessageBroker broker) : base(model, focus, isFocused, broker)
    {
        TimeStep = model.ToReactivePropertySlimAsSynchronized(m => m.TimeStep).AddTo(Disposables);
    }

    public ReactivePropertySlim<double> TimeStep { get; }
}
