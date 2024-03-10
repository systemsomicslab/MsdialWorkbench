using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.MsResult
{
    internal sealed class AccumulatedMs2SpectrumViewModel : ViewModelBase
    {
        public AccumulatedMs2SpectrumViewModel(AccumulatedMs2SpectrumModel model)
        {
            Model = model;
            MsSpectrumViewModel = model.ObserveProperty(m => m.PlotComparedSpectrum)
                .DefaultIfNull(m => new MsSpectrumViewModel(m.MsSpectrumModel))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Compounds = model.ObserveProperty(m => m.Compounds).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SelectedCompound = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedCompound).AddTo(Disposables);
            SelectedRange = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

            ProductIonChromatogram = model.ObserveProperty(m => m.ProductIonChromatogram)
                .DefaultIfNull(m => new ChromatogramsViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ProductIonRange = model.ToReactivePropertySlimAsSynchronized(m => m.ProductIonRange).AddTo(Disposables);

            SearchCompoundCommand = new ReactiveCommand().WithSubscribe(model.SearchCompound).AddTo(Disposables);
            CalculateProductIonChromatogramCommand = SelectedRange.Select(r => r is not null).ToReactiveCommand().WithSubscribe(model.CalculateProductIonChromatogram).AddTo(Disposables);

            DetectPeaksCommand = new ReactiveCommand().WithSubscribe(model.DetectPeaks).AddTo(Disposables);
            AddPeakCommand = new ReactiveCommand().WithSubscribe(model.AddPeak).AddTo(Disposables);
            ResetPeaksCommand = new ReactiveCommand().WithSubscribe(model.ResetPeaks).AddTo(Disposables);
        }

        public AccumulatedMs2SpectrumModel Model { get; }

        public double Mz => Model.Chromatogram.Mz;
        public ReadOnlyReactivePropertySlim<MsSpectrumViewModel?> MsSpectrumViewModel { get; }
        public ReactivePropertySlim<AxisRange?> SelectedRange { get; }

        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ProductIonChromatogram { get; }
        public ReactivePropertySlim<AxisRange?> ProductIonRange { get; }

        public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; }
        public ReactivePropertySlim<ICompoundResult?> SelectedCompound { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public ReactiveCommand CalculateProductIonChromatogramCommand { get; }

        public ReactiveCommand DetectPeaksCommand { get; }
        public ReactiveCommand AddPeakCommand { get; }
        public ReactiveCommand ResetPeaksCommand { get; }
    }
}
