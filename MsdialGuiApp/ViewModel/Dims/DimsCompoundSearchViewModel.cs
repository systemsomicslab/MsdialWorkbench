using CompMs.App.Msdial.Model.Search;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsCompoundSearchViewModel : CompoundSearchVM
    {
        public DimsCompoundSearchViewModel(CompoundSearchModel model, ICommand setUnknownCommand) : base(model, setUnknownCommand) {
        }
    }
}
