using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Search;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsCompoundSearchViewModel : CompoundSearchVM
    {
        public DimsCompoundSearchViewModel(ICompoundSearchModel model, ICommand setUnknownCommand) : base(model, setUnknownCommand) {
        }
    }
}
