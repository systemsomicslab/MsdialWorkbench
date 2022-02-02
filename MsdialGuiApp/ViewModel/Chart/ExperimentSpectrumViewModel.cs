using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    public class ExperimentSpectrumViewModel : ViewModelBase
    {
        public ExperimentSpectrumViewModel(ExperimentSpectrumModel model) {
            Model = model;
            RangeSelectableChromatogramViewModel = new RangeSelectableChromatogramViewModel(Model.RangeSelectableChromatogramModel);
            Spectrums = Model.ObserveProperty(m => m.Spectrums).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Spectrums.Where(spec => spec != null)
                .Select(spec => spec.FirstOrDefault())
                .Where(spec => spec != null)
                .Subscribe(spec => spec.Spectrums.ForEach(s => Console.WriteLine($"Mass: {s.Mass}, Intensity: {s.Intensity}")));

            // HorizontalAxis = Spectrums
            //     .Where(spec => spec != null)
            //     .Select(spec => spec.SelectMany(s => s).ToList())
            //     .Select(spec => new Range(0, 1000d))
            //     .ToReactiveAxisManager<double>(new RelativeMargin(0.05))
            //     .AddTo(Disposables);
            HorizontalAxis = new ContinuousAxisManager<double>(new Range(0, 1000d));

            // VerticalAxis = Spectrums
            //     .Where(spec => spec != null)
            //     .Select(spec => spec.SelectMany(s => s))
            //     .Select(spec => new Range(spec.Min(s => s.Intensity), spec.Max(s => s.Intensity)))
            //     .ToReactiveAxisManager<double>()
            //     .AddTo(Disposables);
            VerticalAxis = new ContinuousAxisManager<double>(new Range(0, 10000d));

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

        public ReadOnlyReactivePropertySlim<ObservableCollection<SpectrumCollection>> Spectrums { get; }

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
