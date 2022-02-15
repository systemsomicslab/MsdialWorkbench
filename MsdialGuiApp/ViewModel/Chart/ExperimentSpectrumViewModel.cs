using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ExperimentSpectrumViewModel : ViewModelBase
    {
        public ExperimentSpectrumViewModel(ExperimentSpectrumModel model) {
            Model = model;
            RangeSelectableChromatogramViewModel = new RangeSelectableChromatogramViewModel(Model.RangeSelectableChromatogramModel);
            Spectrums = Model.Spectrums.ToReadOnlyReactiveCollection(m => new SummarizedSpectrumViewModel(m)).AddTo(Disposables);

            AccumulateSpectrumCommand = new ReactiveCommand().AddTo(Disposables);
            AccumulateSpectrumCommand
                .Where(_ => Model.CanSetExperimentSpectrum())
                .Subscribe(_ => Model.SetExperimentSpectrum())
                .AddTo(Disposables);

            SaveSpectraAsNistCommand = new ReactiveCommand()
                .WithSubscribe(SaveSpectraAsNist)
                .AddTo(Disposables);

            MessageBroker.Default.ToObservable<SaveNistFileNameResponse>()
                .Subscribe(response => Model.SaveSpectrumAsNist(response.FileName))
                .AddTo(Disposables);
        }

        public ExperimentSpectrumModel Model { get; }

        public RangeSelectableChromatogramViewModel RangeSelectableChromatogramViewModel { get; }

        public ReadOnlyReactiveCollection<SummarizedSpectrumViewModel> Spectrums { get; }

        public IAxisManager HorizontalAxis { get; }
        public IAxisManager VerticalAxis { get; }

        public ReactiveCommand AccumulateSpectrumCommand { get; }

        public ReactiveCommand SaveSpectraAsNistCommand { get; }

        private void SaveSpectraAsNist() {
            MessageBroker.Default.Publish(new SaveNistFileNameRequest());
        }
    }

    public class SaveNistFileNameRequest
    {
    }

    public class SaveNistFileNameResponse
    {
        public SaveNistFileNameResponse(string fileName) {
            FileName = fileName;
        }

        public string FileName { get; }
    }
}
