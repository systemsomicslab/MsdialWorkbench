using CompMs.CommonMVVM;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class ResultVM : ViewModelBase
    {
        public abstract ICollectionView PeakSpots { get; }
    }
}
