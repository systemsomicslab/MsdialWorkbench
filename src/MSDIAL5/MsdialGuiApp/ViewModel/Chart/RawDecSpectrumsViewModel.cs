using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class RawDecSpectrumsViewModel : ViewModelBase
    {
        private readonly RawDecSpectrumsModel _model;

        public RawDecSpectrumsViewModel(RawDecSpectrumsModel model, Action focusAction, IObservable<bool> isFocused, IMessageBroker? broker = null) {
            _model = model;
            FocusAction = focusAction;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            if (model.RawLoader is null) {
                Ms2IdList = Observable.Return(new List<MsSelectionItem>(0)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
            }
            else {
                Ms2IdList = model.RawLoader.Ms2List.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = model.RawLoader.Ms2IdSelector;
            }

            RawRefSpectrumViewModels = new MsSpectrumViewModel(model.RawRefSpectrumModels, focusAction: focusAction, isFocused: isFocused).AddTo(Disposables);
            DecRefSpectrumViewModels = new MsSpectrumViewModel(model.DecRefSpectrumModels, focusAction: focusAction, isFocused: isFocused).AddTo(Disposables);

            if (model.CanShowMs2SpectrumList) {
                var messageBroker = broker ?? MessageBroker.Default;
                // Each panel opens a list that follows its own vertical axis type.
                RawRefSpectrumViewModels.ShowMs2SpectrumListCommand = new ReactiveCommand()
                    .WithSubscribe(() => messageBroker.Publish(new Ms2SpectrumListViewModel(model.CreateMs2SpectrumListModel(model.RawRefSpectrumModels))))
                    .AddTo(Disposables);
                DecRefSpectrumViewModels.ShowMs2SpectrumListCommand = new ReactiveCommand()
                    .WithSubscribe(() => messageBroker.Publish(new Ms2SpectrumListViewModel(model.CreateMs2SpectrumListModel(model.DecRefSpectrumModels))))
                    .AddTo(Disposables);
            }
        }

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }
        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }
        public ReactivePropertySlim<bool> IsRawSpectrumOverlayVisible => _model.IsRawSpectrumOverlayVisible;

        public ReadOnlyReactivePropertySlim<List<MsSelectionItem>> Ms2IdList { get; }
        public ReactivePropertySlim<MsSelectionItem?> SelectedMs2Id { get; }
        public AxisItemSelector<double> LowerVerticalAxisSelector => _model.DecRefSpectrumModels.LowerVerticalAxisSelector;

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }
    }
}
