using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
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
    public class ExperimentSpectrumViewModel : ViewModelBase
    {
        public ExperimentSpectrumViewModel(ExperimentSpectrumModel model) {
            this.model = model;
            RangeSelectableChromatogramViewModel = new RangeSelectableChromatogramViewModel(model.RangeSelectableChromatogramModel);
            Ms1Spectrum = model.ObserveProperty(m => m.Ms1Spectrum)
                .Select(m => m is null ? null : new SummarizedSpectrumViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            Ms2Spectrums = model.Ms2Spectrums.ToReadOnlyReactiveCollection(m => new SummarizedSpectrumViewModel(m)).AddTo(Disposables);

            AccumulateSpectrumCommand = new ReactiveCommand().AddTo(Disposables);
            AccumulateSpectrumCommand
                .Where(_ => CanAccumulateSpectrum())
                .Subscribe(_ => AccumulateSpectrum())
                .AddTo(Disposables);

            SaveSpectraAsNistCommand = new ReactiveCommand()
                .WithSubscribe(SaveSpectraAsNist)
                .AddTo(Disposables);
        }

        private readonly ExperimentSpectrumModel model;

        public RangeSelectableChromatogramViewModel RangeSelectableChromatogramViewModel { get; }

        public ReadOnlyReactivePropertySlim<SummarizedSpectrumViewModel> Ms1Spectrum { get; }

        public ReadOnlyReactiveCollection<SummarizedSpectrumViewModel> Ms2Spectrums { get; }

        public IAxisManager HorizontalAxis { get; }
        public IAxisManager VerticalAxis { get; }

        public ReactiveCommand AccumulateSpectrumCommand { get; }

        private bool CanAccumulateSpectrum() {
            return model.CanSetExperimentSpectrum();
        }

        private void AccumulateSpectrum() {
            model.SetExperimentSpectrum();
        }

        public ReactiveCommand SaveSpectraAsNistCommand { get; }

        private void SaveSpectraAsNist() {
            if (string.IsNullOrEmpty(Resources.EXPORT_DIR) || !Directory.Exists(Resources.EXPORT_DIR)) {
                MessageBroker.Default.Publish(new SaveFileNameRequest(model.SaveSpectrumAsNist)
                {
                    Title = "Save spectra",
                    Filter = "NIST format(*.msp)|*.msp",
                    RestoreDirectory = true,
                    AddExtension = true,
                });
            }
            else {
                var fileName = Path.GetFileNameWithoutExtension(model.AnalysisFile.AnalysisFileName);
                model.SaveSpectrumAsNist(Path.Combine(Resources.EXPORT_DIR, $"{fileName}.msp"));
            }
        }
    }
}
