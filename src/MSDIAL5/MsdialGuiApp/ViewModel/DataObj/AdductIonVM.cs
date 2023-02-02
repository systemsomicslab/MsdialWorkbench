using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM.Common;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public sealed class AdductIonVM : DynamicViewModelBase<AdductIon>
    {
        public AdductIonVM(AdductIon innerModel) : base(innerModel) {
            Model = innerModel;
        }

        public AdductIon Model { get; }
    }
}
