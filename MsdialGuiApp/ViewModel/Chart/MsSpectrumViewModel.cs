using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class MsSpectrumViewModel : ViewModelBase
    {
        public MsSpectrumViewModel(
            MsSpectrumModel model,
            IObservable<IAxisManager<double>> horizontalAxisSource = null,
            IObservable<IAxisManager<double>> upperVerticalAxisSource = null,
            IObservable<IAxisManager<double>> lowerVerticalAxisSource = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            SpectrumLoaded = model.SpectrumLoaded;
            ReferenceHasSpectrumInfomation = model.ReferenceHasSpectrumInfomation;

            HorizontalAxis = (horizontalAxisSource ?? model.UpperSpectrumModel.HorizontalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerVerticalAxis = (lowerVerticalAxisSource ?? model.LowerSpectrumModel.VerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            UpperVerticalAxis = (upperVerticalAxisSource ?? model.UpperSpectrumModel.VerticalAxis)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            UpperSpectrum = model.UpperSpectrumModel.Spectrum
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrum = model.LowerSpectrumModel.Spectrum
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
           
            UpperSpectrumBrushSource = model.UpperSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrumBrushSource = model.LowerSpectrumModel.Brush
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SaveUpperSpectrumCommand = model.CanSaveUpperSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveUpperSpectrum))
                .AddTo(Disposables);

            SaveLowerSpectrumCommand = model.CanSaveLowerSpectrum.ToReactiveCommand()
                .WithSubscribe(SaveSpectrum(model.SaveLowerSpectrum))
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> UpperSpectrum { get; }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> LowerSpectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInfomation { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager<double>> HorizontalAxis { get; }

        public ReadOnlyReactivePropertySlim<IAxisManager<double>> UpperVerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<IAxisManager<double>> LowerVerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }

        public ReadOnlyReactivePropertySlim<string> OrderingProperty { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> UpperSpectrumBrushSource { get; }

        public ReadOnlyReactivePropertySlim<IBrushMapper> LowerSpectrumBrushSource { get; }

        public ReactiveCommand SaveUpperSpectrumCommand { get; }

        public ReactiveCommand SaveLowerSpectrumCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using (var fs = File.Open(path, FileMode.Create)) {
                        handler(fs);
                    }
                })
                {
                    Title = "Save spectra",
                    Filter = "NIST format(*.msp)|*.msp",
                    RestoreDirectory = true,
                    AddExtension = true,
                };
                MessageBroker.Default.Publish(request);
            }
            return result;
        }
    }
}
