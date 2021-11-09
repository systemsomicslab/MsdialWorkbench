using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly MessageBroker broker;
        private readonly MainModel model;

        public MainViewModel(MessageBroker broker) {
            this.broker = broker;
            model = new MainModel();

            LipidQueries = new LipidQueryBeanViewModel(model.LipidQueries).AddTo(Disposables);
            Scans = model.ToReactivePropertySlimAsSynchronized(m => m.Scans).AddTo(Disposables);
            Scan = new ReactivePropertySlim<IMSScanProperty>().AddTo(Disposables);

            var spectrum = Scan.Where(scan => scan != null && !scan.Spectrum.IsEmptyOrNull()).Select(scan => scan.Spectrum);
            HorizontalAxis = spectrum.Select(spec => new Range(spec.Min(s => s.Mass), spec.Max(s => s.Mass)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(30), labelType: LabelType.Standard)
                .AddTo(Disposables);
            VerticalAxis = spectrum.Select(spec => new Range(spec.Min(s => s.Intensity), spec.Max(s => s.Intensity)))
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0, 0), labelType: LabelType.Order)
                .AddTo(Disposables);
            SpectrumCommentBrushes = new ConstantBrushMapper<SpectrumComment>(Brushes.White);

            broker.ToObservable<FileOpenRequest>()
                .Subscribe(model.FileOpen)
                .AddTo(Disposables);
        }

        public LipidQueryBeanViewModel LipidQueries { get; }

        public ReactivePropertySlim<ObservableCollection<IMSScanProperty>> Scans { get; }

        public ReactivePropertySlim<IMSScanProperty> Scan { get; }

        public ReactiveContinuousAxisManager<double> HorizontalAxis { get; }
        public ReactiveContinuousAxisManager<double> VerticalAxis { get; }
        public IBrushMapper<SpectrumComment> SpectrumCommentBrushes { get; }
    }
}