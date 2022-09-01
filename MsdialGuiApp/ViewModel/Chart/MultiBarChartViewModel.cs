using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class MultiBarChartViewModel : ViewModelBase
    {
        public MultiBarChartViewModel(ObservableCollection<BarChartViewModel> barCharts) {
            BarCharts = barCharts ?? throw new System.ArgumentNullException(nameof(barCharts));
        }

        public MultiBarChartViewModel(params BarChartViewModel[] barCharts) {
            BarCharts = new ObservableCollection<BarChartViewModel>(barCharts);
        }

        public ObservableCollection<BarChartViewModel> BarCharts { get; }
    }
}
