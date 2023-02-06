using CompMs.App.Msdial.ViewModel.Chart;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IAlignmentResultViewModel : IResultViewModel
    {
        BarChartViewModel BarChartViewModel { get; } 
        ICommand InternalStandardSetCommand { get; }
    }
}
