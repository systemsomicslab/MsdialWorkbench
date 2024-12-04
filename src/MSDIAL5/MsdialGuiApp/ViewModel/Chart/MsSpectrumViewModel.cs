using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class MsSpectrumViewModel : ViewModelBase
    {
        private readonly MsSpectrumModel _model;
        private readonly IMessageBroker _broker;

        public MsSpectrumViewModel(MsSpectrumModel model, Action? focusAction = null, IObservable<bool>? isFocused = null, IMessageBroker? broker = null) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            UpperSpectraViewModel = model.UpperSpectraModel.ToReadOnlyReactiveCollection(m => new SingleSpectrumViewModel(m, broker)).AddTo(Disposables);
            LowerSpectrumViewModel = new SingleSpectrumViewModel(model.LowerSpectrumModel, broker).AddTo(Disposables);

            HorizontalAxis = model.HorizontalAxis.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            UpperVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel<double>>(model.UpperSpectrumModel.VerticalAxisItemSelector.AxisItems);
            LowerVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel<double>>(model.LowerVerticalAxisItemCollection);

            GraphTitle = model.ObserveProperty(m => m.GraphTitle).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);

            SaveMatchedSpectrumCommand = model.CanSaveMatchedSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveMatchedSpectrum,  filter: "tab separated values(*.txt)|*.txt"))
                .AddTo(Disposables);

            SaveUpperSpectrumCommand = model.CanSaveUpperSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveUpperSpectrum,  filter: "NIST format(*.msp)|*.msp"))
                .AddTo(Disposables);

            SaveLowerSpectrumCommand = model.CanSaveLowerSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveLowerSpectrum, filter:  "NIST format(*.msp)|*.msp"))
                .AddTo(Disposables);

            SwitchAllSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.SwitchViewToAllSpectrum)
                .AddTo(Disposables);

            SwitchCompareSpectrumCommand = new ReactiveCommand()
                .WithSubscribe(model.SwitchViewToCompareSpectrum)
                .AddTo(Disposables);
            FocusAction = focusAction;
            IsFocused = (isFocused ?? Observable.Never<bool>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            _broker = broker ?? MessageBroker.Default;
        }

        public ReadOnlyReactiveCollection<SingleSpectrumViewModel> UpperSpectraViewModel { get; }
        public SingleSpectrumViewModel LowerSpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<IAxisManager<double>?> HorizontalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> UpperVerticalAxisItem => _model.UpperVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel<double>> UpperVerticalAxisItemCollection { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem => _model.LowerVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection { get; }

        public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

        public ReactiveCommand SwitchAllSpectrumCommand { get; }

        public ReactiveCommand SwitchCompareSpectrumCommand { get; }

        public Action? FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ReactiveCommand SaveMatchedSpectrumCommand { get; }

        public ReactiveCommand SaveUpperSpectrumCommand { get; }

        public ReactiveCommand SaveLowerSpectrumCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler, string filter) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using var fs = File.Open(path, FileMode.Create);
                    handler(fs);
                })
                {
                    Title = "Save spectra",
                    Filter = filter,
                    RestoreDirectory = true,
                    AddExtension = true,
                };
                _broker.Publish(request);
            }
            return result;
        }
    }
}
