using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Utility;
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
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class AlignmentMs2SpectrumViewModel : ViewModelBase
    {
        private readonly AlignmentMs2SpectrumModel _model;
        private readonly IMessageBroker _broker;

        public AlignmentMs2SpectrumViewModel(AlignmentMs2SpectrumModel model, IMessageBroker broker, Action focusAction = null, IObservable<bool> isFocused = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            SpectrumLoaded = model.SpectrumLoaded;
            ReferenceHasSpectrumInformation = model.ReferenceHasSpectrumInformation;

            UpperSpectraViewModel = model.UpperSpectraModel.ToReadOnlyReactiveCollection(m => new SingleSpectrumViewModel(m)).AddTo(Disposables);
            LowerSpectrumViewModel = new SingleSpectrumViewModel(model.LowerSpectrumModel).AddTo(Disposables);

            HorizontalAxis = model.HorizontalAxis.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            LowerVerticalAxis = model.LowerVerticalAxisItem.SkipNull().Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LowerVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel>(model.LowerVerticalAxisItemCollection);

            UpperVerticalAxis = model.UpperVerticalAxisItem.SkipNull().Select(item => item.AxisManager).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            UpperVerticalAxisItemCollection = new ReadOnlyObservableCollection<AxisItemModel>(model.UpperVerticalAxisItemCollection);

            GraphTitle = Observable.Return(model.GraphLabels.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = Observable.Return(model.GraphLabels.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = Observable.Return(model.GraphLabels.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = Observable.Return(model.HorizontalPropertySelector.Property)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = Observable.Return(model.VerticalPropertySelector.Property)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = Observable.Return(model.GraphLabels.AnnotationLabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            OrderingProperty = Observable.Return(model.GraphLabels.AnnotationOrderProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
           
            SaveMatchedSpectraCommand = model.CanSaveMatchedSpectra.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveMatchedSpectra,  filter: "tab separated values(*.txt)|*.txt"))
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
            _broker = broker;
            FocusAction = focusAction;
            IsFocused = (isFocused ?? Observable.Never<bool>()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactiveCollection<SingleSpectrumViewModel> UpperSpectraViewModel { get; }
        public SingleSpectrumViewModel LowerSpectrumViewModel { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInformation { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> HorizontalAxis { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> UpperVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel> UpperVerticalAxisItem => _model.UpperVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel> UpperVerticalAxisItemCollection { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LowerVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel> LowerVerticalAxisItem => _model.LowerVerticalAxisItem;
        public ReadOnlyObservableCollection<AxisItemModel> LowerVerticalAxisItemCollection { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }

        public ReadOnlyReactivePropertySlim<string> OrderingProperty { get; }

        public ReactiveCommand SwitchAllSpectrumCommand { get; }

        public ReactiveCommand SwitchCompareSpectrumCommand { get; }

        public Action FocusAction { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFocused { get; }

        public ReactiveCommand SaveMatchedSpectraCommand { get; }

        public ReactiveCommand SaveUpperSpectrumCommand { get; }

        public ReactiveCommand SaveLowerSpectrumCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler, string filter) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using (var fs = File.Open(path, FileMode.Create, FileAccess.Write)) {
                        handler(fs);
                    }
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
