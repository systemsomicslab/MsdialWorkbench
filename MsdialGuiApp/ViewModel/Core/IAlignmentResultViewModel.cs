using CompMs.App.Msdial.ViewModel.Chart;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IAlignmentResultViewModel : IResultViewModel
    {
        BarChartViewModel BarChartViewModel { get; } 
    }
}
