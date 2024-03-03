using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.MsResult
{
    internal sealed class AccumulatedMs2SpectrumViewModel : ViewModelBase
    {
        public AccumulatedMs2SpectrumViewModel(AccumulatedMs2SpectrumModel model)
        {
            Model = model;
            SpectrumViewModel = new SingleSpectrumViewModel(model.ChartSpectrumModel).AddTo(Disposables);

            Compounds = model.ObserveProperty(m => m.Compounds).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            SearchCompoundCommand = new ReactiveCommand().WithSubscribe(model.SearchCompound).AddTo(Disposables);
        }

        public AccumulatedMs2SpectrumModel Model { get; }

        public double Mz => Model.Chromatogram.Mz;
        public SingleSpectrumViewModel SpectrumViewModel { get; }

        public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; }

        public ReactiveCommand SearchCompoundCommand { get; }
    }
}
