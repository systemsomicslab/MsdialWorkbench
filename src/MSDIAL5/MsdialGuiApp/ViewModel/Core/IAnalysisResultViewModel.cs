using CompMs.App.Msdial.ViewModel.Chart;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IAnalysisResultViewModel : IResultViewModel
    {
        RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel { get; }
    }
}
