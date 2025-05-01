using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            if (model.Q1DecLoader is null) {
                Q1DecMs2IdList = Observable.Return(new List<MsSelectionItem>(0)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                Q1DecSelectedMs2Id = new ReactivePropertySlim<MsSelectionItem?>().AddTo(Disposables);
            }
            else {
                Q1DecMs2IdList = model.Q1DecLoader.Ms2List.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                Q1DecSelectedMs2Id = model.Q1DecLoader.Ms2IdSelector;
            }

            RawRefSpectrumViewModels = new MsSpectrumViewModel(model.RawRefSpectrumModels, focusAction: focusAction, isFocused: isFocused).AddTo(Disposables);
            if (model.Q1DecRefSpectrumModels is not null) {
                Q1DecRefSpectrumViewModels = new MsSpectrumViewModel(model.Q1DecRefSpectrumModels, focusAction: focusAction, isFocused: isFocused).AddTo(Disposables);
            }
            DecRefSpectrumViewModels = new MsSpectrumViewModel(model.DecRefSpectrumModels, focusAction: focusAction, isFocused: isFocused).AddTo(Disposables);

            if (model.ProductIonIntensityMapModel is not null && broker is not null) {
                ProductIonIntensityMapViewModel = new ProductIonIntensityMapViewModel(model.ProductIonIntensityMapModel).AddTo(Disposables);
                ShowProductIonIntensityMapCommand = new ReactiveCommand().WithSubscribe(() => broker.Publish(ProductIonIntensityMapViewModel)).AddTo(Disposables);
            }
        }

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }
        public MsSpectrumViewModel? Q1DecRefSpectrumViewModels { get; }
        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }

        public ReadOnlyReactivePropertySlim<List<MsSelectionItem>> Ms2IdList { get; }
        public ReactivePropertySlim<MsSelectionItem?> SelectedMs2Id { get; }
        public ReadOnlyReactivePropertySlim<List<MsSelectionItem>> Q1DecMs2IdList { get; }
        public ReactivePropertySlim<MsSelectionItem?> Q1DecSelectedMs2Id { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem => _model.DecRefSpectrumModels.LowerVerticalAxisItem;
        public ObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection => _model.DecRefSpectrumModels.LowerVerticalAxisItemCollection;

        public ProductIonIntensityMapViewModel? ProductIonIntensityMapViewModel { get; }

        public ReactiveCommand? ShowProductIonIntensityMapCommand { get; }

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }
    }
}
