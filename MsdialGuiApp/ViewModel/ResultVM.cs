using CompMs.CommonMVVM;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class ResultVM : ViewModelBase
    {
        public object Model { get; }
        public ResultVM(object model) {
            Model = model;
        }
        public abstract ICollectionView PeakSpotsView { get; }
    }
}
