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
            SpectrumViewModel = new SingleSpectrumViewModel(model.ChartSpectrumModel).AddTo(Disposables);

            Compounds = model.ObserveProperty(m => m.Compounds).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            SelectedRange = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

            ProductIonChromatogram = model.ObserveProperty(m => m.ProductIonChromatogram)
                .DefaultIfNull(m => new ChromatogramsViewModel(m))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            SearchCompoundCommand = new ReactiveCommand().WithSubscribe(model.SearchCompound).AddTo(Disposables);
            CalculateProductIonChromatogramCommand = SelectedRange.Select(r => r is not null).ToReactiveCommand().WithSubscribe(model.CalculateProductIonChromatogram).AddTo(Disposables);
        }

        public AccumulatedMs2SpectrumModel Model { get; }

        public double Mz => Model.Chromatogram.Mz;
        public SingleSpectrumViewModel SpectrumViewModel { get; }

        public ReactivePropertySlim<AxisRange?> SelectedRange { get; }

        public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ProductIonChromatogram { get; }

        public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public ReactiveCommand CalculateProductIonChromatogramCommand { get; }
    }
}
