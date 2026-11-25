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
using System.Globalization;
using System.Text;
using System.Windows;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal class SingleSpectrumViewModel : ViewModelBase
    {
        private readonly SingleSpectrumModel _model;

        public SingleSpectrumViewModel(SingleSpectrumModel model, IMessageBroker? broker) {
            _model = model;
            MsSpectrum = model.MsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MinimumAnnotationOrder = model.MinimumAnnotationOrder.ToReadOnlyReactivePropertySlim(double.PositiveInfinity).AddTo(Disposables);
            HorizontalAxis = model.HorizontalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalAxis = model.VerticalAxis.Cast<IAxisManager>().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Brush = Observable.Return(model.Brush).ToReadOnlyReactivePropertySlim<IBrushMapper>().AddTo(Disposables);
            LineThickness = model.LineThickness;
            StrokeDashArray = model.StrokeDashArray;
            IsVisible = model.IsVisible;
            IsAnnotationVisible = model.IsAnnotationVisible;
            SelectedVerticalAxisItem = model.VerticalAxisItemSelector.GetAxisItemAsObservable().ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CopyAsTableCommand = new ReactiveCommand().WithSubscribe(CopyAsTable).AddTo(Disposables);
            SaveAsTableCommand = new ReactiveCommand().WithSubscribe(() => SaveAsTable(broker ?? MessageBroker.Default)).AddTo(Disposables);
            if (broker is not null) {
                SaveCommand = model.CanSave.ToReactiveCommand().WithSubscribe(SaveSpectrum(model.Save, filter:  "NIST format(*.msp)|*.msp", broker)).AddTo(Disposables);
            }
        }

        public ReadOnlyReactivePropertySlim<MsSpectrum?> MsSpectrum { get; }
        public ReadOnlyReactivePropertySlim<double> MinimumAnnotationOrder { get; }
        public ReadOnlyReactivePropertySlim<IAxisManager?> HorizontalAxis { get; }
        public GraphLabels Labels => _model.Labels;
        public string HorizontalProperty => _model.HorizontalProperty;
        public ReadOnlyReactivePropertySlim<IAxisManager?> VerticalAxis { get; }
        public AxisItemSelector<double> VerticalAxisItemSelector => _model.VerticalPropertySelectors.AxisItemSelector;
        public ReadOnlyObservableCollection<AxisItemModel<double>> VerticalAxisItems => _model.VerticalAxisItemSelector.AxisItems;
        public ReadOnlyReactivePropertySlim<AxisItemModel<double>?> SelectedVerticalAxisItem { get; }
        public string VerticalProperty => _model.VerticalProperty;
        public ReadOnlyReactivePropertySlim<IBrushMapper> Brush { get; }
        public string HueProperty => _model.HueProperty;
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded => _model.SpectrumLoaded;
        public ReactivePropertySlim<double> LineThickness { get; }
        public ReactivePropertySlim<DoubleCollection?> StrokeDashArray { get; }
        public ReactivePropertySlim<bool> IsVisible { get; }
        public ReactivePropertySlim<bool> IsAnnotationVisible { get; }
        public ReactiveCommand? SaveCommand { get; }
        public ReactiveCommand CopyAsTableCommand { get; }
        public ReactiveCommand SaveAsTableCommand { get; }

        private string? SpectrumAsTable() {
            var spectrum = MsSpectrum.Value;
            if (spectrum is null) return null;
            var result = new StringBuilder("m/z\tIntensity\r\n");
            foreach (var peak in spectrum.Spectrum) {
                result.Append(peak.Mass.ToString("R", CultureInfo.InvariantCulture)).Append('\t')
                    .Append(peak.Intensity.ToString("R", CultureInfo.InvariantCulture)).Append("\r\n");
            }
            return result.ToString();
        }

        private void CopyAsTable() {
            var table = SpectrumAsTable();
            if (table is not null) Clipboard.SetText(table);
        }

        private void SaveAsTable(IMessageBroker broker) {
            var table = SpectrumAsTable();
            if (table is null) return;
            broker.Publish(new SaveFileNameRequest(path => File.WriteAllText(path, table, new UTF8Encoding(false))) {
                Title = "Save spectrum as table",
                Filter = "tab separated values (*.txt)|*.txt",
                RestoreDirectory = true,
                AddExtension = true,
            });
        }

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
