using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Gcms;

internal sealed class GcgcSpectrumPeakPlotViewModel : AnalysisPeakPlotViewModel<Ms1BasedSpectrumFeature, ReadOnlyObservableCollection<Ms1BasedSpectrumFeature>>
{
    public GcgcSpectrumPeakPlotViewModel(GcgcSpectrumPeakPlotModel model, Action focus, IObservable<bool> isFocused, IMessageBroker broker) : base(model, focus, isFocused, broker)
    {
        TimeStep = model.ToReactivePropertySlimAsSynchronized(m => m.TimeStep).AddTo(Disposables);
        SecondColumnAxis = model.ObserveProperty(m => m.SecondColumnAxis).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
    }

    public ReactivePropertySlim<double> TimeStep { get; }
    public ReadOnlyReactivePropertySlim<IAxisManager<double>?> SecondColumnAxis { get; }
}
