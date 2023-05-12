using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class RawDecSpectrumsViewModel : ViewModelBase
    {
        private readonly RawDecSpectrumsModel _model;

        public RawDecSpectrumsViewModel(RawDecSpectrumsModel model, Action focusAction, IObservable<bool> isFocused) {
            _model = model;
            FocusAction = focusAction;
            IsFocused = isFocused.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            if (model.RawLoader is null) {
                Ms2IdList = Observable.Return(new List<MsSelectionItem>(0)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = new ReactivePropertySlim<MsSelectionItem>().AddTo(Disposables);
            }
            else {
                Ms2IdList = model.RawLoader.Ms2List.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
                SelectedMs2Id = model.RawLoader.Ms2IdSelector;
            }
            var lowerVerticalAxis = model.DecRefSpectrumModels.LowerVerticalAxis;

            RawRefSpectrumViewModels = new MsSpectrumViewModel(
                model.RawRefSpectrumModels,
                lowerVerticalAxisSource: lowerVerticalAxis,
                focusAction: focusAction,
                isFocused: isFocused);

            DecRefSpectrumViewModels = new MsSpectrumViewModel(
                model.DecRefSpectrumModels,
                lowerVerticalAxisSource: lowerVerticalAxis,
                focusAction: focusAction,
                isFocused: isFocused);
        }

        public MsSpectrumViewModel RawRefSpectrumViewModels { get; }
        public MsSpectrumViewModel DecRefSpectrumViewModels { get; }

        public ReadOnlyReactivePropertySlim<List<MsSelectionItem>> Ms2IdList { get; }
        public ReactivePropertySlim<MsSelectionItem> SelectedMs2Id { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem => _model.DecRefSpectrumModels.LowerVerticalAxisItem;
        public ObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection => _model.DecRefSpectrumModels.LowerVerticalAxisItemCollection;

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }
    }

    public class AxisItemViewModel : BindableBase
    {
        public AxisItemViewModel(IAxisManager<double> axisManager, string label) {
            AxisManager = axisManager;
            Label = label;
        }

        public IAxisManager<double> AxisManager { get; }

        public string Label { get; }
    }
}
