using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
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
    public sealed class MsSpectrumViewModel : ViewModelBase
    {
        private readonly MsSpectrumModel _model;

        public MsSpectrumViewModel(
            MsSpectrumModel model,
            IObservable<IAxisManager<double>> horizontalAxisSource = null,
            IObservable<IAxisManager<double>> upperVerticalAxisSource = null,
            IObservable<IAxisManager<double>> lowerVerticalAxisSource = null,
            Action focusAction = null,
            IObservable<bool> isFocused = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            UpperSpectraViewModel = model.UpperSpectraModel.ToReadOnlyReactiveCollection(m => new SingleSpectrumViewModel(m)).AddTo(Disposables);
            LowerSpectrumViewModel = new SingleSpectrumViewModel(model.LowerSpectrumModel).AddTo(Disposables);

            HorizontalAxis = (horizontalAxisSource ?? model.HorizontalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerVerticalAxis = (lowerVerticalAxisSource ?? model.LowerSpectrumModel.VerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            LowerVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel<double>>(model.LowerVerticalAxisItemCollection);

            UpperVerticalAxis = (upperVerticalAxisSource ?? model.UpperVerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            UpperVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel<double>>(model.UpperVerticalAxisItemCollection);

            UpperMsSpectrum = model.UpperSpectrumModel.MsSpectrum
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerMsSpectrum = model.LowerSpectrumModel.MsSpectrum
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            GraphTitle = Observable.Return(model.GraphLabels.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = Observable.Return(model.GraphLabels.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = Observable.Return(model.GraphLabels.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = Observable.Return(model.GraphLabels.AnnotationLabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            OrderingProperty = Observable.Return(model.GraphLabels.AnnotationOrderProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
           
            UpperSpectrumBrushSource = model.UpperSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrumBrushSource = model.LowerSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

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
            _model = model;
            FocusAction = focusAction;
            IsFocused = (isFocused ?? Observable.Never<bool>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<SingleSpectrumViewModel> UpperSpectraViewModel { get; }
        public SingleSpectrumViewModel LowerSpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<MsSpectrum> UpperMsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<MsSpectrum> LowerMsSpectrum { get; }

        public ReadOnlyReactivePropertySlim<IAxisManager<double>> HorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> UpperVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> UpperVerticalAxisItem => _model.UpperVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel<double>> UpperVerticalAxisItemCollection { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LowerVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem => _model.LowerVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }

        public ReadOnlyReactivePropertySlim<string> OrderingProperty { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> UpperSpectrumBrushSource { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> LowerSpectrumBrushSource { get; }

        public ReactiveCommand SwitchAllSpectrumCommand { get; }

        public ReactiveCommand SwitchCompareSpectrumCommand { get; }

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ReactiveCommand SaveMatchedSpectrumCommand { get; }

        public ReactiveCommand SaveUpperSpectrumCommand { get; }

        public ReactiveCommand SaveLowerSpectrumCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler, string filter) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using (var fs = File.Open(path, FileMode.Create)) {
                        handler(fs);
                    }
                })
                {
                    Title = "Save spectra",
                    Filter = filter,
                    RestoreDirectory = true,
                    AddExtension = true,
                };
                MessageBroker.Default.Publish(request);
            }
            return result;
        }
    }
}
