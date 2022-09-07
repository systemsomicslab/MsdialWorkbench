using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class WholeImageResultViewModel : ViewModelBase
    {
        private readonly WholeImageResultModel _model;

        public WholeImageResultViewModel(WholeImageResultModel model) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            PeakPlotViewModel = new AnalysisPeakPlotViewModel(model.PeakPlotModel, () => { }, Observable.Never<bool>());
        }

        public AnalysisPeakPlotViewModel PeakPlotViewModel { get; }
    }
}
