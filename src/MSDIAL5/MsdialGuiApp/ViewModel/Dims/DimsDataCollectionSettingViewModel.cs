using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public sealed class DimsDataCollectionSettingViewModel : ViewModelBase
    {
        public DimsDataCollectionSettingViewModel(DimsDataCollectionSettingModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            UseMs1WithHighestTic = Model
                .ToReactivePropertySlimAsSynchronized(m => m.UseMs1WithHighestTic)
                .AddTo(Disposables);
            UseMs1WithHighestBpi = Model
                .ToReactivePropertySlimAsSynchronized(m => m.UseMs1WithHighestBpi)
                .AddTo(Disposables);
            UseAverageMs1 = Model
                .ToReactivePropertySlimAsSynchronized(m => m.UseAverageMs1)
                .AddTo(Disposables);
            TimeBegin = Model
                .ToReactivePropertySlimAsSynchronized(m => m.TimeBegin)
                .AddTo(Disposables);
            TimeEnd = Model
                .ToReactivePropertySlimAsSynchronized(m => m.TimeEnd)
                .AddTo(Disposables);
            MassTolerance = Model
                .ToReactivePropertySlimAsSynchronized(m => m.MassTolerance)
                .AddTo(Disposables);

            PeakPickParameter = new PeakPickBaseParameterViewModel(model.PeakPickParameter);
            ProcessParameter = new ProcessBaseParameterViewModel(model.ProcessParameter);
        }

        public void Commit() {
            ProcessParameter.Commit();
        }

        public DimsDataCollectionSettingModel Model { get; }

        public ReactivePropertySlim<bool> UseMs1WithHighestTic { get; }
        public ReactivePropertySlim<bool> UseMs1WithHighestBpi { get; }
        public ReactivePropertySlim<bool> UseAverageMs1 { get; }
        public ReactivePropertySlim<double> TimeBegin { get; }
        public ReactivePropertySlim<double> TimeEnd { get; }
        public ReactivePropertySlim<double> MassTolerance { get; }
        public PeakPickBaseParameterViewModel PeakPickParameter { get; }
        public ProcessBaseParameterViewModel ProcessParameter { get; }
    }
}
