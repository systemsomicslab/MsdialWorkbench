using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    public sealed class ImmsDataCollectionSettingViewModel : ViewModelBase
    {
        public ImmsDataCollectionSettingViewModel(ImmsDataCollectionSettingModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            UseAverageMs1 = Model
                .ToReactivePropertySlimAsSynchronized(m => m.UseAverageMs1)
                .AddTo(Disposables);
            UseMs1WithHighestTic = Model
                .ToReactivePropertySlimAsSynchronized(m => m.UseMs1WithHighestTic)
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
            DriftTolerance = Model
                .ToReactivePropertySlimAsSynchronized(m => m.DriftTolerance)
                .AddTo(Disposables);

            Parameter = new MsdialImmsParameterViewModel(model.Parameter);
            PeakPickParameter = new PeakPickBaseParameterViewModel(Model.PeakPickParameter);
            ProcessParameter = new ProcessBaseParameterViewModel(Model.ProcessParameter);
        }

        public ImmsDataCollectionSettingModel Model { get; }
        public ReactivePropertySlim<bool> UseAverageMs1 { get; }
        public ReactivePropertySlim<bool> UseMs1WithHighestTic { get; }
        public ReactivePropertySlim<double> TimeBegin { get; }
        public ReactivePropertySlim<double> TimeEnd { get; }
        public ReactivePropertySlim<double> MassTolerance { get; }
        public ReactivePropertySlim<double> DriftTolerance { get; }
        public MsdialImmsParameterViewModel Parameter { get; }
        public PeakPickBaseParameterViewModel PeakPickParameter { get; }
        public ProcessBaseParameterViewModel ProcessParameter { get; }

        public void Commit() {
            ProcessParameter.Commit();
        }
    }
}
