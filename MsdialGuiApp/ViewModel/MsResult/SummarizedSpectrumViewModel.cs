using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.MsResult
{
    public class SummarizedSpectrumViewModel : ViewModelBase
    {
        public SummarizedSpectrumViewModel(SummarizedSpectrumModel model) {
            Model = model;
            SpectrumViewModel = new SpectrumViewModel(Model.SpectrumModel).AddTo(Disposables);
        }

        public SummarizedSpectrumModel Model { get; }

        public SpectrumViewModel SpectrumViewModel { get; }
    }
}
