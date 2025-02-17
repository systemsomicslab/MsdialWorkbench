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
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal class SingleSpectrumViewModel : ViewModelBase
    {
        private readonly SingleSpectrumModel _model;

        public SingleSpectrumViewModel(SingleSpectrumModel model, IMessageBroker? broker) {
            _model = model;
            MsSpectrum = model.MsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            HorizontalAxis = model.HorizontalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxis = model.VerticalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Brush = Observable.Return(model.Brush).ToReadOnlyReactivePropertySlim<IBrushMapper>().AddTo(Disposables);
            LineThickness = model.LineThickness;
            IsVisible = model.IsVisible;
            SelectedVerticalAxisItem = model.VerticalAxisItemSelector.GetAxisItemAsObservable().SkipNull().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            if (broker is not null) {
                SaveCommand = model.CanSave.ToReactiveCommand().WithSubscribe(SaveSpectrum(model.Save, filter:  "NIST format(*.msp)|*.msp", broker)).AddTo(Disposables);
            }
        }

        public ReadOnlyReactivePropertySlim<MsSpectrum?> MsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager?> HorizontalAxis { get; }
        public GraphLabels Labels => _model.Labels;
        public string HorizontalProperty => _model.HorizontalProperty;
        public ReadOnlyReactivePropertySlim<IAxisManager?> VerticalAxis { get; }
        public ReadOnlyReactivePropertySlim<AxisItemModel<double>?> SelectedVerticalAxisItem { get; }
        public string VerticalProperty => _model.VerticalProperty;
        public ReadOnlyReactivePropertySlim<IBrushMapper> Brush { get; }
        public string HueProperty => _model.HueProperty;
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded => _model.SpectrumLoaded;
        public ReactivePropertySlim<double> LineThickness { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactiveCommand? SaveCommand { get; }

        private Action SaveSpectrum(Action<Stream> handler, string filter, IMessageBroker broker) {
            void result() {
                var request = new SaveFileNameRequest(path =>
                {
                    using var fs = File.Open(path, FileMode.Create, FileAccess.Write);
                    handler(fs);
                })
                {
                    Title = "Save spectra",
                    Filter = filter,
                    RestoreDirectory = true,
                    AddExtension = true,
                };
                broker.Publish(request);
            }
            return result;
        }
    }
}
